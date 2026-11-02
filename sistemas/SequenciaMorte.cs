using Godot;

[GlobalClass]
public partial class SequenciaMorte : CanvasLayer
{
    [Signal] public delegate void ContinuouEventHandler();
    [Signal] public delegate void ReviveuEventHandler();

    [ExportGroup("Animacao")]
    [Export] public float TempoDesaceleracao { get; set; } = 0.4f;
    [Export] public float TempoVinheta { get; set; } = 1.4f;
    [Export] public float EsperaAntesPrompt { get; set; } = 0.6f;
    [Export] public float TempoUsoFrasco { get; set; } = 1.4f;
    [Export] public float EscalaMinima { get; set; } = 0.005f;

    [ExportGroup("Audio")]
    [Export] public string BarraMusica { get; set; } = "Music";
    [Export] public string BarraSfx { get; set; } = "SFX";
    [Export] public string BarraCue { get; set; } = "Master";
    [Export] public string CaminhoSom { get; set; } = "res://art/sons/morte.wav";
    [Export] public string CaminhoSomFrasco { get; set; } = "res://art/sons/frasco.wav";
    [Export] public string CaminhoBgm { get; set; } = "res://art/sons/bgm_gameover.ogg";
    [Export] public float TempoFadeAudio { get; set; } = 1.4f;

    [ExportGroup("Cores")]
    [Export] public Color CorMorte { get; set; } = new(0.58f, 0.08f, 0.06f);

    [ExportGroup("Arte")]
    [Export] public string PastaArte { get; set; } = "res://art/telas/gameover";
    [Export] public float EscalaArte { get; set; } = 2f;

    public static int Continuacoes { get; private set; }

    private enum Estado { Inativo, Desacelerando, Aguardando, Spray, Prompt }

    private Estado _estado = Estado.Inativo;

    private TextureRect _vinheta = null!;
    private Control _tela = null!;
    private Control? _caixa;
    private bool _ajustado;
    private AudioStreamPlayer _audio = null!;
    private AudioStreamPlayer _bgm = null!;
    private BaseButton _botaoSim = null!;
    private BaseButton _botaoNao = null!;

    private TextureRect? _fumacaA;
    private TextureRect? _fumacaB;
    private TextureRect? _mortoPan;

    private bool _arte;
    private float _escala = 2f;
    private bool _temFrasco;
    private float _tUsoFrasco;

    private int[] _barras = System.Array.Empty<int>();
    private float[] _barrasBase = System.Array.Empty<float>();

    private float _tDesaceleracao;
    private float _tVinheta;
    private float _tAudio;
    private float _tAguarda;
    private float _tPrompt;

    public override void _Ready()
    {
        Layer = 20;
        _escala = Mathf.Min(EscalaArte, Mathf.Max(GetViewport().GetVisibleRect().Size.Y / 480f, 0.5f));
        _vinheta = ConstruirVinheta();
        _audio = new AudioStreamPlayer
        {
            Bus = BarraCue,
            ProcessMode = Node.ProcessModeEnum.Always,
        };
        AddChild(_audio);
        _bgm = new AudioStreamPlayer
        {
            Bus = BarraCue,
            ProcessMode = Node.ProcessModeEnum.Always,
        };
        AddChild(_bgm);
        ConstruirTela();
    }

    public override void _ExitTree()
    {
        if (_estado != Estado.Inativo)
        {
            Restaurar();
        }
        _estado = Estado.Inativo;
    }

    public void Iniciar(bool temFrasco)
    {
        if (_estado != Estado.Inativo) return;

        _estado = Estado.Desacelerando;
        _temFrasco = temFrasco;
        _tUsoFrasco = 0f;
        _tDesaceleracao = 0f;
        _tVinheta = 0f;
        _tAudio = 0f;
        _tAguarda = 0f;
        _tPrompt = 0f;

        BuscarBarras();
        TocarCue(CaminhoSom);

        _vinheta.Visible = true;
        _vinheta.Modulate = new Color(1f, 1f, 1f, 0f);
        _tela.Visible = false;
        _ajustado = false;
    }

    public override void _Process(double delta)
    {
        if (_estado == Estado.Inativo) return;

        float dt = (float)(delta / System.Math.Max(Engine.TimeScale, 0.0001));

        switch (_estado)
        {
            case Estado.Desacelerando:
                Desacelerando(dt);
                break;
            case Estado.Aguardando:
                Aguardando(dt);
                break;
            case Estado.Spray:
                Spray(dt);
                break;
            case Estado.Prompt:
                AnimarPrompt(dt);
                break;
        }
    }

    private void Desacelerando(float dt)
    {
        _tDesaceleracao += dt;
        _tVinheta += dt;
        _tAudio += dt;

        float esc = Mathf.Clamp(_tDesaceleracao / TempoDesaceleracao, 0f, 1f);
        Engine.TimeScale = Mathf.Lerp(1f, EscalaMinima, esc);

        FadeAudio(1f - Mathf.Clamp(_tAudio / TempoFadeAudio, 0f, 1f));

        float alfa = Mathf.Clamp(_tVinheta / TempoVinheta, 0f, 1f);
        _vinheta.Modulate = new Color(1f, 1f, 1f, alfa);

        if (_tDesaceleracao >= TempoDesaceleracao && _tVinheta >= TempoVinheta)
        {
            Engine.TimeScale = EscalaMinima;
            FadeAudio(0f);
            _estado = Estado.Aguardando;
            _tAguarda = 0f;
        }
    }

    private void Aguardando(float dt)
    {
        _tAguarda += dt;
        if (_tAguarda >= EsperaAntesPrompt)
        {
            if (_temFrasco)
            {
                _estado = Estado.Spray;
                _tUsoFrasco = 0f;
                TocarCue(CaminhoSomFrasco);
                return;
            }

            _estado = Estado.Prompt;
            _tPrompt = 0f;
            _tVinheta = 0f;
            _tela.Visible = true;
            _tela.Modulate = new Color(1f, 1f, 1f, 0f);
            _botaoSim.GrabFocus();

            var bgm = GD.Load<AudioStream>(CaminhoBgm);
            if (bgm is AudioStreamOggVorbis ogg) ogg.Loop = true;
            if (bgm != null)
            {
                _bgm.Stream = bgm;
                _bgm.Play();
            }
        }
    }

    private void Spray(float dt)
    {
        _tUsoFrasco += dt;
        if (_tUsoFrasco >= TempoUsoFrasco)
        {
            Restaurar();
            EmitSignal(SignalName.Reviveu);
        }
    }

    private void AnimarPrompt(float dt)
    {
        if (!_ajustado)
        {
            // Repete até o layout existir: no primeiro frame do prompt os
            // sizes ainda são zero e o ajuste concluiria errado.
            _ajustado = AjustarATela();
        }

        _tPrompt += dt;
        _tVinheta += dt;

        _tela.Modulate = new Color(1f, 1f, 1f, Mathf.Clamp(_tPrompt / 0.8f, 0f, 1f));

        float alfa = 1f - Mathf.Clamp(_tVinheta / TempoVinheta, 0f, 1f);
        _vinheta.Modulate = new Color(1f, 1f, 1f, alfa);

        if (_fumacaA is { } futA && _fumacaB is { } futB)
        {
            futA.PivotOffset = futA.Size / 2f;
            futB.PivotOffset = futB.Size / 2f;
            futA.Rotation += dt * 0.04f;
            futB.Rotation -= dt * 0.03f;
        }

        if (_mortoPan is { } pan)
        {
            float oscilacao = Mathf.Sin(_tPrompt * 0.5f) * pan.Size.X * 0.06f;
            pan.Position = new Vector2(-pan.Size.X * 0.1f + oscilacao, 0f);
        }
    }

    /// <summary>
    /// Encaixa o conteúdo na tela. O container cresce além da viewport quando
    /// os filhos somam mais altura do que ela tem, e aí os botões saem por
    /// baixo. Medido depois do layout (o Size só é válido no frame seguinte),
    /// escala em torno do centro para caber sem constantes mágicas.
    /// </summary>
    /// <returns>false enquanto o layout não estiver calculado (tenta de novo).</returns>
    private bool AjustarATela()
    {
        if (_caixa == null)
        {
            return true;
        }

        float alturaTela = _tela.Size.Y;
        float alturaConteudo = _caixa.Size.Y;

        if (alturaTela <= 1f || alturaConteudo <= 1f)
        {
            return false;
        }

        // 4% de folga: sem margem o último botão encosta na borda e parece cortado.
        float fator = Mathf.Min(1f, alturaTela * 0.96f / alturaConteudo);
        if (fator < 1f)
        {
            // Escala a partir do canto (pivô zero) e centraliza na mão: com
            // PivotOffset o conteúdo encolhe em torno do centro e desce junto.
            _caixa.PivotOffset = Vector2.Zero;
            _caixa.Scale = Vector2.One * fator;
            _caixa.Position = (_tela.Size - _caixa.Size * fator) / 2f;
        }

        return true;
    }

    private void AoSim()
    {
        Continuacoes++;
        Restaurar();
        EmitSignal(SignalName.Continuou);
        GetTree().ReloadCurrentScene();
    }

    private void AoNao()
    {
        Restaurar();
        GetTree().Quit();
    }

    private void Restaurar()
    {
        Engine.TimeScale = 1f;
        RestaurarVolumes();
        _audio.Stop();
        _bgm.Stop();
        _vinheta.Visible = false;
        _tela.Visible = false;
        _estado = Estado.Inativo;
    }

    private void FadeAudio(float ratio)
    {
        for (int i = 0; i < _barras.Length; i++)
        {
            float linear = Mathf.DbToLinear(_barrasBase[i]) * Mathf.Max(ratio, 0f);
            AudioServer.SetBusVolumeDb(_barras[i], Mathf.LinearToDb(Mathf.Max(linear, 0.0001f)));
        }
    }

    private void RestaurarVolumes()
    {
        for (int i = 0; i < _barras.Length; i++)
        {
            AudioServer.SetBusVolumeDb(_barras[i], _barrasBase[i]);
        }
    }

    private void BuscarBarras()
    {
        _barras = System.Array.Empty<int>();
        _barrasBase = System.Array.Empty<float>();

        AdicionarBarra(BarraMusica);
        AdicionarBarra(BarraSfx);

        if (_barras.Length == 0)
        {
            AdicionarBarraPorIndice(AudioServer.GetBusIndex("Master"));
        }
    }

    private void AdicionarBarra(string nome)
    {
        AdicionarBarraPorIndice(AudioServer.GetBusIndex(nome));
    }

    private void AdicionarBarraPorIndice(int indice)
    {
        if (indice < 0 || System.Array.IndexOf(_barras, indice) >= 0) return;
        int n = _barras.Length;
        System.Array.Resize(ref _barras, n + 1);
        System.Array.Resize(ref _barrasBase, n + 1);
        _barras[n] = indice;
        _barrasBase[n] = AudioServer.GetBusVolumeDb(indice);
    }

    private void TocarCue(string caminho)
    {
        _audio.Stop();
        var som = GD.Load<AudioStream>(caminho);
        if (som == null) return;
        _audio.Stream = som;
        _audio.Play();
    }

    private Texture2D? CarregarArte(string nome)
    {
        var caminho = PastaArte + "/" + nome + ".png";
        return ResourceLoader.Exists(caminho) ? GD.Load<Texture2D>(caminho) : null;
    }

    private void ConstruirTela()
    {
        _tela = new Control { Name = "Tela", MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(_tela);
        _tela.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        Texture2D? bg = CarregarArte("gameover_BG");
        Texture2D? morto = CarregarArte("youAreDead");
        Texture2D? sim = CarregarArte("yes");
        Texture2D? nao = CarregarArte("No");
        _arte = bg != null && morto != null && sim != null && nao != null;

        if (_arte)
        {
            AdicionarTexturaCompleta(bg!, 1f);
        }

        var caixa = new VBoxContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Alignment = BoxContainer.AlignmentMode.Center,
        };
        caixa.AddThemeConstantOverride("separation", 18);

        if (_arte)
        {
            var aditivo = new CanvasItemMaterial
            {
                BlendMode = CanvasItemMaterial.BlendModeEnum.Add,
            };

            _fumacaA = AdicionarTexturaCompleta(CarregarArte("eff1"), 0.14f);
            _fumacaB = AdicionarTexturaCompleta(CarregarArte("eff2"), 0.2f);
            if (_fumacaA != null)
            {
                _fumacaA.Scale = Vector2.One * 1.3f;
                _fumacaA.Material = aditivo;
            }
            if (_fumacaB != null)
            {
                _fumacaB.Scale = Vector2.One * 1.3f;
                _fumacaB.Material = aditivo;
            }

            var larguraMorto = _escala * 428f;
            var alturaMorto = _escala * 240f;
            var areaMorto = new Control
            {
                ClipContents = true,
                CustomMinimumSize = new Vector2(larguraMorto, alturaMorto),
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            _mortoPan = new TextureRect
            {
                Texture = morto,
                StretchMode = TextureRect.StretchModeEnum.Scale,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                Material = aditivo,
                Position = new Vector2(-larguraMorto * 0.1f, 0f),
                Size = new Vector2(larguraMorto * 1.2f, alturaMorto),
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            areaMorto.AddChild(_mortoPan);

            TextureRect? rotuloContinue = null;
            var cont = CarregarArte("continue");
            if (cont != null)
            {
                rotuloContinue = new TextureRect
                {
                    Texture = cont,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    Material = aditivo,
                    CustomMinimumSize = Vector2.One * _escala * new Vector2(256f, 42f),
                    SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
                    MouseFilter = Control.MouseFilterEnum.Ignore,
                };
            }

            caixa.AddChild(areaMorto);
            if (rotuloContinue != null) caixa.AddChild(rotuloContinue);

            var vac0 = new Control
            {
                CustomMinimumSize = new Vector2(0f, 16f),
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            caixa.AddChild(vac0);

            _botaoSim = ConstruirBotaoTextura(sim, "SIM");
            _botaoNao = ConstruirBotaoTextura(nao, "NAO");
        }
        else
        {
            var rotulo = new Label
            {
                Text = "GAME OVER",
                LabelSettings = new LabelSettings
                {
                    FontSize = 72,
                    FontColor = Colors.White,
                    OutlineSize = 16,
                    OutlineColor = Colors.Black,
                },
                HorizontalAlignment = HorizontalAlignment.Center,
            };

            var pergunta = new Label
            {
                Text = "CONTINUAR?",
                LabelSettings = new LabelSettings
                {
                    FontSize = 28,
                    FontColor = Colors.White,
                    OutlineSize = 10,
                    OutlineColor = Colors.Black,
                },
                HorizontalAlignment = HorizontalAlignment.Center,
            };

            caixa.AddChild(pergunta);
            caixa.AddChild(rotulo);
            caixa.MoveChild(rotulo, 0);
            caixa.MoveChild(pergunta, 1);

            var tSim = new Button
            {
                Text = "SIM",
                CustomMinimumSize = new Vector2(180f, 52f),
            };
            tSim.AddThemeFontSizeOverride("font_size", 26);

            var tNao = new Button
            {
                Text = "NÃO",
                CustomMinimumSize = new Vector2(180f, 52f),
            };
            tNao.AddThemeFontSizeOverride("font_size", 26);

            _botaoSim = tSim;
            _botaoNao = tNao;
        }

        var vac1 = new Control
        {
            CustomMinimumSize = new Vector2(0f, 30f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };

        var botoes = new HBoxContainer
        {
            MouseFilter = Control.MouseFilterEnum.Pass,
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
        };
        botoes.AddThemeConstantOverride("separation", 64);
        botoes.AddChild(_botaoSim);
        botoes.AddChild(_botaoNao);

        caixa.AddChild(vac1);
        caixa.AddChild(botoes);

        _botaoSim.Pressed += AoSim;
        _botaoNao.Pressed += AoNao;
        _botaoSim.FocusEntered += AplicarBrilho;
        _botaoSim.FocusExited += AplicarBrilho;
        _botaoNao.FocusEntered += AplicarBrilho;
        _botaoNao.FocusExited += AplicarBrilho;

        _tela.AddChild(caixa);
        _caixa = caixa;
        caixa.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        caixa.Name = "Prompt";
        botoes.Name = "Botoes";
        _botaoSim.Name = "Sim";
        _botaoNao.Name = "Nao";

        _tela.Visible = false;
    }

    private BaseButton ConstruirBotaoTextura(Texture2D? tex, string texto)
    {
        if (tex == null)
        {
            var botao = new Button
            {
                Text = texto,
                CustomMinimumSize = new Vector2(180f, 52f),
            };
            botao.AddThemeFontSizeOverride("font_size", 26);
            return botao;
        }

        return new TextureButton
        {
            TextureNormal = tex,
            IgnoreTextureSize = true,
            StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = Vector2.One * _escala * new Vector2(128f, 64f),
        };
    }

    private void AplicarBrilho()
    {
        _botaoSim.Modulate = Colors.White;
        _botaoNao.Modulate = Colors.White;

        BaseButton focada = _botaoSim.HasFocus() ? _botaoSim
            : _botaoNao.HasFocus() ? _botaoNao : null;

        if (focada != null) focada.Modulate = new Color(1.4f, 1.4f, 1.4f);
    }

    private TextureRect AdicionarTexturaCompleta(Texture2D tex, float alfa)
    {
        var rect = new TextureRect
        {
            Texture = tex,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _tela.AddChild(rect);
        rect.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        rect.Modulate = new Color(1f, 1f, 1f, alfa);
        return rect;
    }

    private TextureRect ConstruirVinheta()
    {
        var grad = new Godot.Gradient();
        grad.Colors = new Color[]
        {
            new(CorMorte, 0f),
            new(CorMorte, 0f),
            new(CorMorte, 0.92f),
        };
        grad.Offsets = new float[] { 0f, 0.5f, 1f };

        var tex = new Godot.GradientTexture2D
        {
            Gradient = grad,
            Fill = Godot.GradientTexture2D.FillEnum.Radial,
            FillFrom = new Vector2(0.5f, 0.5f),
            FillTo = new Vector2(0.5f, 0f),
            Width = 512,
            Height = 512,
        };

        var vinheta = new TextureRect
        {
            Texture = tex,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        AddChild(vinheta);
        vinheta.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        vinheta.Visible = false;
        vinheta.Modulate = new Color(1f, 1f, 1f, 0f);
        return vinheta;
    }
}
