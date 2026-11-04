using Godot;
using System.Collections.Generic;

[GlobalClass]
public partial class InventarioTela : CanvasLayer
{
    private const int Colunas = 4;
    private const int Linhas = 2;
    private const int TotalSlots = Colunas * Linhas;

    [Export] public NodePath? InventarioPath { get; set; }
    [Export] public NodePath? SaudePath { get; set; }
    [Export] public NodePath? ActorPath { get; set; }
    [Export] public string CaminhoIconeFrasco { get; set; } = "res://art/telas/hud/esp_item.png";
    [Export] public string CaminhoSomUsar { get; set; } = "res://art/sons/curar.wav";
    [Export] public Vector2 TamanhoSlot { get; set; } = new(72f, 72f);

    // Whitelist do inventory pause: quem continua processando com o mundo congelado.
    // Vazio cai no caminho relativo padrão da cena (irmãos no root do level).
    [Export] public NodePath? CameraPath { get; set; }
    [Export] public NodePath? CameraHostPath { get; set; }
    [Export] public NodePath? PcamPath { get; set; }

    public bool Aberto { get; private set; }

    private Inventario? _inventario;
    private Saude? _saude;
    private Node? _actor;
    private Control _raiz = null!;
    private Label _status = null!;
    private Label _descricao = null!;
    private Label _ajuda = null!;
    private GridContainer _grade = null!;
    private AudioStreamPlayer _audio = null!;
    private Texture2D? _iconeFrasco;
    private readonly List<TextureRect> _icones = new();
    private readonly List<StyleBoxFlat> _estilos = new();
    private readonly Dictionary<Node, ProcessModeEnum> _modosOriginais = new();
    private readonly List<Node> _nosVivos = new();
    private bool _mundoPausado;
    private Input.MouseModeEnum _mouseAntes;
    private int _selecionado;
    private int _frascos;

    public override void _Ready()
    {
        Layer = 15;
        ProcessMode = ProcessModeEnum.Always;
        Visible = false;

        _iconeFrasco = GD.Load<Texture2D>(CaminhoIconeFrasco);
        _audio = new AudioStreamPlayer { Bus = "SFX", ProcessMode = ProcessModeEnum.Always };
        AddChild(_audio);

        ConstruirUi();
        ResolverRefs();

        if (_inventario != null)
        {
            _inventario.Mudou += AoInventarioMudou;
            AoInventarioMudou(_inventario.Frascos);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("inventory"))
        {
            if (Aberto) Fechar();
            else Abrir();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (!Aberto) return;

        if (@event.IsActionPressed("ui_cancel"))
        {
            Fechar();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event.IsActionPressed("ui_left") || @event.IsActionPressed("move_left"))
        {
            MoverSelecao(-1, 0);
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("ui_right") || @event.IsActionPressed("move_right"))
        {
            MoverSelecao(1, 0);
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("ui_up") || @event.IsActionPressed("move_forward"))
        {
            MoverSelecao(0, -1);
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("ui_down") || @event.IsActionPressed("move_back"))
        {
            MoverSelecao(0, 1);
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("ui_accept") || @event.IsActionPressed("usar_item") || @event.IsActionPressed("interact"))
        {
            UsarSelecionado();
            GetViewport().SetInputAsHandled();
        }
    }

    public void Abrir()
    {
        if (Aberto) return;
        if (_actor != null && _actor.Get("InputLocked").AsBool()) return;

        Aberto = true;
        Visible = true;
        _mouseAntes = Input.MouseMode;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        SetActorLocked(true);
        PausarMundo();
        AtualizarStatus();
        AtualizarDescricao();
        PintarSelecao();
    }

    public void Fechar()
    {
        if (!Aberto) return;

        Aberto = false;
        Visible = false;
        DespausarMundo();
        SetActorLocked(false);
        // Devolve o mouse ao modo que estava antes — quem abriu o inventário não
        // assume que o cursor deve voltar travado.
        Input.MouseMode = _mouseAntes;
    }

    public override void _ExitTree()
    {
        // Sair da árvore com o mundo pausado congelaria o jogo (a pausa é do tree, não do nó).
        if (_mundoPausado) DespausarMundo();
    }

    /// <summary>Garante que o nó continue processando com <c>GetTree().Paused = true</c>.</summary>
    private void ManterVivo(Node no)
    {
        if (!_modosOriginais.ContainsKey(no))
        {
            _modosOriginais[no] = no.ProcessMode;
        }

        no.ProcessMode = ProcessModeEnum.Always;
    }

    /// <summary>
    /// Congela o mundo. A whitelist precisa estar <c>Always</c> ANTES do pause,
    /// senão a câmera e a própria tela param junto.
    /// </summary>
    private void PausarMundo()
    {
        if (_mundoPausado) return;

        foreach (var no in _nosVivos)
        {
            if (GodotObject.IsInstanceValid(no)) ManterVivo(no);
        }

        GetTree().Paused = true;
        _mundoPausado = true;
    }

    /// <summary>Despausa e devolve cada nó da whitelist ao <c>ProcessMode</c> original.</summary>
    private void DespausarMundo()
    {
        if (!_mundoPausado) return;

        GetTree().Paused = false;

        foreach (var (no, modo) in _modosOriginais)
        {
            if (GodotObject.IsInstanceValid(no)) no.ProcessMode = modo;
        }

        _modosOriginais.Clear();
        _mundoPausado = false;
    }

    private void ResolverRefs()
    {
        if (InventarioPath is { IsEmpty: false } invPath)
        {
            _inventario = GetNodeOrNull<Inventario>(invPath);
        }

        if (SaudePath is { IsEmpty: false } saudePath)
        {
            _saude = GetNodeOrNull<Saude>(saudePath);
        }

        if (ActorPath is { IsEmpty: false } actorPath)
        {
            _actor = GetNodeOrNull(actorPath);
        }

        _nosVivos.Clear();
        _nosVivos.Add(this);
        AdicionarNoVivo(CameraPath, "../MainCamera");
        AdicionarNoVivo(CameraHostPath, "../MainCamera/PhantomCameraHost");
        AdicionarNoVivo(PcamPath, "../PCam_Main");
    }

    private void AdicionarNoVivo(NodePath? exportado, string fallbackRelativo)
    {
        Node? no = exportado is { IsEmpty: false } caminho ? GetNodeOrNull(caminho) : null;
        no ??= GetNodeOrNull(fallbackRelativo);

        if (no == null)
        {
            GD.PushWarning($"[InventarioTela] whitelist do pause: '{fallbackRelativo}' não encontrado — a câmera pode congelar ao abrir o inventário.");
            return;
        }

        if (!_nosVivos.Contains(no)) _nosVivos.Add(no);
    }

    private void AoInventarioMudou(int quantidade)
    {
        _frascos = quantidade;
        for (int i = 0; i < TotalSlots; i++)
        {
            bool ocupado = i < _frascos;
            _icones[i].Visible = ocupado && _iconeFrasco != null;
            if (ocupado && _iconeFrasco != null)
            {
                _icones[i].Texture = _iconeFrasco;
                _icones[i].Modulate = Colors.White;
            }
        }

        if (_selecionado >= TotalSlots) _selecionado = 0;
        PintarSelecao();
        AtualizarDescricao();
        AtualizarStatus();
    }

    private void MoverSelecao(int dx, int dy)
    {
        int col = _selecionado % Colunas;
        int lin = _selecionado / Colunas;
        col = Mathf.Clamp(col + dx, 0, Colunas - 1);
        lin = Mathf.Clamp(lin + dy, 0, Linhas - 1);
        _selecionado = lin * Colunas + col;
        PintarSelecao();
        AtualizarDescricao();
    }

    private void UsarSelecionado()
    {
        if (_inventario == null || _selecionado >= _frascos) return;

        // Usar com a vida cheia queimaria o frasco à toa (RE2 avisa e não deixa).
        if (_saude != null && !_saude.Morto && _saude.Atual >= _saude.Maximo)
        {
            Avisar("Vida já está cheia.\nNada a curar.");
            return;
        }

        if (!_inventario.TentarConsumirFrasco()) return;

        if (_saude != null && !_saude.Morto)
        {
            _saude.RestaurarVida();
        }

        var som = GD.Load<AudioStream>(CaminhoSomUsar);
        if (som != null)
        {
            _audio.Stream = som;
            _audio.Play();
        }

        AtualizarStatus();
    }

    private void AtualizarStatus()
    {
        if (_saude == null || _saude.Maximo <= 0)
        {
            _status.Text = "—";
            _status.Modulate = Colors.White;
            return;
        }

        float p = (float)_saude.Atual / _saude.Maximo;
        if (p > 0.5f)
        {
            _status.Text = "CONDIÇÃO\nBEM";
            _status.Modulate = new Color(0.35f, 0.95f, 0.45f);
        }
        else if (p > 0.25f)
        {
            _status.Text = "CONDIÇÃO\nCUIDADO";
            _status.Modulate = new Color(0.95f, 0.9f, 0.25f);
        }
        else
        {
            _status.Text = "CONDIÇÃO\nPERIGO";
            _status.Modulate = new Color(0.95f, 0.2f, 0.2f);
        }
    }

    private void AtualizarDescricao()
    {
        _descricao.Modulate = Colors.White;

        if (_selecionado < _frascos)
        {
            _descricao.Text = "Frasco de cura\nRestaura a vida por completo.\n\n[Enter / Q] Usar";
        }
        else
        {
            _descricao.Text = "Vazio";
        }
    }

    /// <summary>Escreve um aviso no painel de descrição; sai na próxima troca de seleção.</summary>
    private void Avisar(string texto)
    {
        _descricao.Text = texto;
        _descricao.Modulate = new Color(0.95f, 0.85f, 0.3f);
    }

    private void PintarSelecao()
    {
        for (int i = 0; i < _estilos.Count; i++)
        {
            bool sel = i == _selecionado;
            _estilos[i].BorderColor = sel
                ? new Color(0.95f, 0.85f, 0.2f)
                : new Color(0.45f, 0.7f, 0.55f, 0.85f);
            _estilos[i].BorderWidthLeft = sel ? 3 : 2;
            _estilos[i].BorderWidthTop = sel ? 3 : 2;
            _estilos[i].BorderWidthRight = sel ? 3 : 2;
            _estilos[i].BorderWidthBottom = sel ? 3 : 2;
            _estilos[i].BgColor = sel
                ? new Color(0.08f, 0.18f, 0.12f, 0.95f)
                : new Color(0.04f, 0.1f, 0.07f, 0.9f);
        }
    }

    private void SetActorLocked(bool locked)
    {
        _actor?.Set("InputLocked", locked);
    }

    private void ConstruirUi()
    {
        _raiz = new Control
        {
            Name = "Raiz",
            ProcessMode = ProcessModeEnum.WhenPaused,
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        AddChild(_raiz);
        // Âncoras só valem DEPOIS do nó entrar na árvore. Setadas no inicializador
        // do objeto, a Raiz ficava 0×0 e o painel se centralizava num pai sem
        // tamanho — aparecia cortado no canto superior esquerdo.
        _raiz.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        var fundo = new ColorRect
        {
            Color = new Color(0.02f, 0.06f, 0.04f, 0.82f),
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        _raiz.AddChild(fundo);
        fundo.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        var painel = new PanelContainer
        {
            Name = "Painel",
        };
        painel.GrowHorizontal = Control.GrowDirection.Both;
        painel.GrowVertical = Control.GrowDirection.Both;
        painel.CustomMinimumSize = new Vector2(720, 420);
        painel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.14f, 0.1f, 0.96f),
            BorderColor = new Color(0.4f, 0.75f, 0.55f, 0.9f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            ContentMarginLeft = 24,
            ContentMarginTop = 20,
            ContentMarginRight = 24,
            ContentMarginBottom = 20,
        });
        _raiz.AddChild(painel);
        painel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        painel.OffsetLeft = -360;
        painel.OffsetTop = -210;
        painel.OffsetRight = 360;
        painel.OffsetBottom = 210;

        var colunas = new HBoxContainer();
        colunas.AddThemeConstantOverride("separation", 24);
        painel.AddChild(colunas);

        var esquerda = new VBoxContainer();
        esquerda.CustomMinimumSize = new Vector2(140, 0);
        esquerda.AddThemeConstantOverride("separation", 16);
        colunas.AddChild(esquerda);

        var tituloStatus = new Label { Text = "STATUS" };
        tituloStatus.AddThemeColorOverride("font_color", new Color(0.65f, 0.85f, 0.7f));
        esquerda.AddChild(tituloStatus);

        _status = new Label { Text = "CONDIÇÃO\nBEM" };
        _status.AddThemeFontSizeOverride("font_size", 28);
        _status.Modulate = new Color(0.35f, 0.95f, 0.45f);
        esquerda.AddChild(_status);

        var centro = new VBoxContainer();
        centro.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        centro.AddThemeConstantOverride("separation", 12);
        colunas.AddChild(centro);

        var tituloInv = new Label { Text = "INVENTÁRIO" };
        tituloInv.AddThemeColorOverride("font_color", new Color(0.65f, 0.85f, 0.7f));
        centro.AddChild(tituloInv);

        _grade = new GridContainer { Columns = Colunas };
        _grade.AddThemeConstantOverride("h_separation", 10);
        _grade.AddThemeConstantOverride("v_separation", 10);
        centro.AddChild(_grade);

        for (int i = 0; i < TotalSlots; i++)
        {
            var estilo = new StyleBoxFlat
            {
                BgColor = new Color(0.04f, 0.1f, 0.07f, 0.9f),
                BorderColor = new Color(0.45f, 0.7f, 0.55f, 0.85f),
                BorderWidthLeft = 2,
                BorderWidthTop = 2,
                BorderWidthRight = 2,
                BorderWidthBottom = 2,
                ContentMarginLeft = 8,
                ContentMarginTop = 8,
                ContentMarginRight = 8,
                ContentMarginBottom = 8,
            };
            _estilos.Add(estilo);

            var slotPainel = new PanelContainer();
            slotPainel.CustomMinimumSize = TamanhoSlot;
            slotPainel.AddThemeStyleboxOverride("panel", estilo);

            var icone = new TextureRect
            {
                Visible = false,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                CustomMinimumSize = TamanhoSlot - new Vector2(16, 16),
            };

            slotPainel.AddChild(icone);
            _grade.AddChild(slotPainel);
            _icones.Add(icone);
        }

        var direita = new VBoxContainer();
        direita.CustomMinimumSize = new Vector2(170, 0);
        direita.AddThemeConstantOverride("separation", 12);
        colunas.AddChild(direita);

        var tituloItem = new Label { Text = "ITEM" };
        tituloItem.AddThemeColorOverride("font_color", new Color(0.65f, 0.85f, 0.7f));
        direita.AddChild(tituloItem);

        _descricao = new Label
        {
            Text = "Vazio",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(170, 120),
        };
        direita.AddChild(_descricao);

        _ajuda = new Label
        {
            Text = "[Tab] Fechar   [Setas] Mover",
            Modulate = new Color(0.55f, 0.7f, 0.6f),
        };
        direita.AddChild(_ajuda);
    }
}
