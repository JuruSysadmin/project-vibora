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
    [Export] public Vector2 TamanhoSlot { get; set; } = new(88f, 88f);

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
    private readonly List<PanelContainer> _paineis = new();
    private readonly List<TextureRect> _icones = new();
    private readonly List<StyleBoxFlat> _estilos = new();
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
        Input.MouseMode = Input.MouseModeEnum.Visible;
        SetActorLocked(true);
        GetTree().Paused = true;
        AtualizarStatus();
        AtualizarDescricao();
        PintarSelecao();
    }

    public void Fechar()
    {
        if (!Aberto) return;

        Aberto = false;
        Visible = false;
        GetTree().Paused = false;
        SetActorLocked(false);
        Input.MouseMode = Input.MouseModeEnum.Captured;
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
            _status.Text = "STATUS\n—";
            _status.Modulate = Colors.White;
            return;
        }

        float p = (float)_saude.Atual / _saude.Maximo;
        if (p > 0.5f)
        {
            _status.Text = "CONDITION\nFINE";
            _status.Modulate = new Color(0.35f, 0.95f, 0.45f);
        }
        else if (p > 0.25f)
        {
            _status.Text = "CONDITION\nCAUTION";
            _status.Modulate = new Color(0.95f, 0.9f, 0.25f);
        }
        else
        {
            _status.Text = "CONDITION\nDANGER";
            _status.Modulate = new Color(0.95f, 0.2f, 0.2f);
        }
    }

    private void AtualizarDescricao()
    {
        if (_selecionado < _frascos)
        {
            _descricao.Text = "Herb / First Aid\nRestores health to full.\n\n[Enter / Q] Use";
        }
        else
        {
            _descricao.Text = "Empty slot";
        }
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
            AnchorsPreset = (int)Control.LayoutPreset.FullRect,
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        AddChild(_raiz);

        var fundo = new ColorRect
        {
            Color = new Color(0.02f, 0.06f, 0.04f, 0.82f),
            AnchorsPreset = (int)Control.LayoutPreset.FullRect,
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        _raiz.AddChild(fundo);

        var painel = new PanelContainer
        {
            Name = "Painel",
        };
        painel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        painel.GrowHorizontal = Control.GrowDirection.Both;
        painel.GrowVertical = Control.GrowDirection.Both;
        painel.CustomMinimumSize = new Vector2(720, 420);
        painel.OffsetLeft = -360;
        painel.OffsetTop = -210;
        painel.OffsetRight = 360;
        painel.OffsetBottom = 210;
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

        var colunas = new HBoxContainer();
        colunas.AddThemeConstantOverride("separation", 28);
        painel.AddChild(colunas);

        var esquerda = new VBoxContainer();
        esquerda.CustomMinimumSize = new Vector2(160, 0);
        esquerda.AddThemeConstantOverride("separation", 16);
        colunas.AddChild(esquerda);

        var tituloStatus = new Label { Text = "STATUS" };
        tituloStatus.AddThemeColorOverride("font_color", new Color(0.65f, 0.85f, 0.7f));
        esquerda.AddChild(tituloStatus);

        _status = new Label { Text = "CONDITION\nFINE" };
        _status.AddThemeFontSizeOverride("font_size", 28);
        _status.Modulate = new Color(0.35f, 0.95f, 0.45f);
        esquerda.AddChild(_status);

        var centro = new VBoxContainer();
        centro.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        centro.AddThemeConstantOverride("separation", 12);
        colunas.AddChild(centro);

        var tituloInv = new Label { Text = "INVENTORY" };
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
            _paineis.Add(slotPainel);
            _icones.Add(icone);
        }

        var direita = new VBoxContainer();
        direita.CustomMinimumSize = new Vector2(200, 0);
        direita.AddThemeConstantOverride("separation", 12);
        colunas.AddChild(direita);

        var tituloItem = new Label { Text = "ITEM" };
        tituloItem.AddThemeColorOverride("font_color", new Color(0.65f, 0.85f, 0.7f));
        direita.AddChild(tituloItem);

        _descricao = new Label
        {
            Text = "Empty slot",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(200, 120),
        };
        direita.AddChild(_descricao);

        _ajuda = new Label
        {
            Text = "[Tab] Close   [Arrows] Move",
            Modulate = new Color(0.55f, 0.7f, 0.6f),
        };
        direita.AddChild(_ajuda);
    }
}
