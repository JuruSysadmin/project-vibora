using Godot;

[GlobalClass]
public partial class SequenciaMorte : CanvasLayer
{
    [Signal] public delegate void ContinuouEventHandler();

    [ExportGroup("Animacao")]
    [Export] public float TempoDesaceleracao { get; set; } = 0.4f;
    [Export] public float TempoVinheta { get; set; } = 1.4f;
    [Export] public float EsperaAntesPrompt { get; set; } = 0.6f;
    [Export] public float EscalaMinima { get; set; } = 0.005f;

    [ExportGroup("Audio")]
    [Export] public string BarraMusica { get; set; } = "Music";
    [Export] public string BarraSfx { get; set; } = "SFX";
    [Export] public string BarraCue { get; set; } = "Master";
    [Export] public string CaminhoSom { get; set; } = "res://art/sons/morte.wav";
    [Export] public float TempoFadeAudio { get; set; } = 1.4f;

    [ExportGroup("Cores")]
    [Export] public Color CorMorte { get; set; } = new(0.58f, 0.08f, 0.06f);

    public static int Continuacoes { get; private set; }

    private enum Estado { Inativo, Desacelerando, Aguardando, Prompt }

    private Estado _estado = Estado.Inativo;

    private TextureRect _vinheta = null!;
    private VBoxContainer _prompt = null!;
    private AudioStreamPlayer _audio = null!;

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
        _vinheta = ConstruirVinheta();
        _prompt = ConstruirPrompt();
        _audio = new AudioStreamPlayer
        {
            Bus = BarraCue,
            ProcessMode = Node.ProcessModeEnum.Always,
        };
        AddChild(_audio);
    }

    public override void _ExitTree()
    {
        if (_estado != Estado.Inativo)
        {
            Restaurar();
        }
        _estado = Estado.Inativo;
    }

    public void Iniciar()
    {
        if (_estado != Estado.Inativo) return;

        _estado = Estado.Desacelerando;
        _tDesaceleracao = 0f;
        _tVinheta = 0f;
        _tAudio = 0f;
        _tAguarda = 0f;
        _tPrompt = 0f;

        BuscarBarras();
        TocarCue();

        _vinheta.Visible = true;
        _vinheta.Modulate = new Color(1f, 1f, 1f, 0f);
        _prompt.Visible = false;
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
            _estado = Estado.Prompt;
            _tPrompt = 0f;
            _tVinheta = 0f;
            _prompt.Visible = true;
            _prompt.Modulate = new Color(1f, 1f, 1f, 0f);
            _prompt.GetNode<Button>("Botoes/Sim").GrabFocus();
        }
    }

    private void AnimarPrompt(float dt)
    {
        _tPrompt += dt;
        _tVinheta += dt;

        _prompt.Modulate = new Color(1f, 1f, 1f, Mathf.Clamp(_tPrompt / 0.8f, 0f, 1f));

        float alfa = 1f - Mathf.Clamp(_tVinheta / TempoVinheta, 0f, 1f);
        _vinheta.Modulate = new Color(1f, 1f, 1f, alfa);
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
        _vinheta.Visible = false;
        _prompt.Visible = false;
        _estado = Estado.Inativo;
    }

    private void TocarCue()
    {
        _audio.Stop();
        var som = GD.Load<AudioStream>(CaminhoSom);
        if (som == null) return;
        _audio.Stream = som;
        _audio.Play();
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

    private VBoxContainer ConstruirPrompt()
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

        var sim = new Button
        {
            Text = "SIM",
            CustomMinimumSize = new Vector2(180f, 52f),
        };
        sim.AddThemeFontSizeOverride("font_size", 26);

        var nao = new Button
        {
            Text = "NÃO",
            CustomMinimumSize = new Vector2(180f, 52f),
        };
        nao.AddThemeFontSizeOverride("font_size", 26);

        sim.Pressed += AoSim;
        nao.Pressed += AoNao;

        var botoes = new HBoxContainer();
        botoes.AddThemeConstantOverride("separation", 32);
        botoes.AddChild(sim);
        botoes.AddChild(nao);

        var prompt = new VBoxContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        prompt.AddThemeConstantOverride("separation", 20);
        prompt.AddChild(pergunta);
        prompt.AddChild(new Control { CustomMinimumSize = new Vector2(0f, 12f) });
        prompt.AddChild(botoes);

        AddChild(prompt);
        prompt.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);

        prompt.Name = "Prompt";
        botoes.Name = "Botoes";
        sim.Name = "Sim";
        nao.Name = "Nao";

        prompt.Visible = false;
        return prompt;
    }
}
