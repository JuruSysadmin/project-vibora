using Godot;

[GlobalClass]
public partial class MedidorVida : Control
{
    [ExportGroup("Animacao")]
    [Export] public float Velocidade { get; set; } = 0.8f;
    [Export] public float AtrasoDoRastro { get; set; } = 0.25f;

    [ExportGroup("Limiares")]
    [Export] public float LimiarAmarelo { get; set; } = 0.5f;
    [Export] public float LimiarPerigo { get; set; } = 0.25f;

    [ExportGroup("Cores")]
    [Export] public Color CorVida { get; set; } = new(0f, 1f, 0f);
    [Export] public Color CorAmarelo { get; set; } = new(1f, 1f, 0f);
    [Export] public Color CorPerigo { get; set; } = new(1f, 0f, 0f);
    [Export] public Color CorRastro { get; set; } = new(1f, 0f, 0f, 0.5f);

    [Export] public NodePath? SaudePath { get; set; }

    [Signal] public delegate void PerigoMudouEventHandler(bool ativo);

    public float Vida => _vida;
    public bool EmPerigo => _perigo;

    private TextureProgressBar _vidaBarra = null!;
    private TextureProgressBar _rastroBarra = null!;
    private Saude? _saude;

    private int _jogadores = 1;
    private readonly float[] _vidaPorJogador = { -1f, -1f };
    private float _vida = 1f;
    private float _alvoRastro;
    private float _alvoCura = 1f;
    private bool _animandoDano;
    private bool _animandoCura;
    private bool _perigo;
    private float _rastroParado;

    private int _observadoAtual = -1;
    private int _observadoMaximo = -1;

    public override void _Ready()
    {
        _vidaBarra = GetNode<TextureProgressBar>("BarraVida");
        _rastroBarra = GetNode<TextureProgressBar>("BarraRastro");

        _vidaBarra.MinValue = 0.0f;
        _vidaBarra.MaxValue = 1.0f;
        _vidaBarra.Step = 0.0f;
        _rastroBarra.MinValue = 0.0f;
        _rastroBarra.MaxValue = 1.0f;
        _rastroBarra.Step = 0.0f;

        _vidaBarra.Value = 1.0f;
        _rastroBarra.Value = 1.0f;
        _vidaBarra.TintProgress = CorVida;
        _rastroBarra.TintProgress = CorRastro;

        if (SaudePath is { IsEmpty: false })
        {
            _saude = GetNodeOrNull<Saude>(SaudePath);
            if (_saude == null)
            {
                GD.PushWarning("[MedidorVida] SaudePath não aponta para um Saude.");
            }
        }
    }

    public override void _Process(double delta)
    {
        ObservarSaude();

        // HUD roda em tempo real. Sem dividir pelo TimeScale, o slow-motion da
        // sequencia de morte congela a barra (delta encolhe 200x).
        float dt = (float)(delta / System.Math.Max(Engine.TimeScale, 0.0001));
        ExecutarAnimacao(dt);
    }

    public void DefinirVida(int jogador, float proporcao)
    {
        float alvo = Mathf.Clamp(proporcao, 0f, 1f);

        if (_vidaPorJogador[jogador] < 0f)
        {
            _vidaPorJogador[jogador] = alvo;
            if (_jogadores == 2)
            {
                _vida = Mathf.Clamp(_vidaPorJogador[0] + _vidaPorJogador[1] - 1f, 0f, 1f);
            }
            else
            {
                _vida = alvo;
            }
            AplicarCor(_vida);
            _vidaBarra.Value = _vida;
            _rastroBarra.Value = _vida;
            return;
        }

        float reducao;
        if (_jogadores == 1)
        {
            reducao = _vidaPorJogador[jogador] - alvo;
            _vidaPorJogador[jogador] = alvo;
            _vida = alvo;
        }
        else
        {
            _vidaPorJogador[jogador] = alvo;
            float compartilhada = Mathf.Clamp(_vidaPorJogador[0] + _vidaPorJogador[1] - 1f, 0f, 1f);
            reducao = _vida - compartilhada;
            _vida = compartilhada;
        }

        if (reducao == 0f) return;
        AplicarCor(_vida);

        if (reducao > 0f)
        {
            _alvoRastro = _vida;
            _animandoDano = true;
            _rastroParado = AtrasoDoRastro;
            if (_animandoCura)
            {
                _alvoCura = _vida;
                _animandoCura = false;
            }
            _vidaBarra.Value = _vida;
        }
        else
        {
            // Cura interrompe um dano em andamento: sem isso o rastro escorre
            // para o valor antigo enquanto a vida sobe, e depois salta.
            _animandoDano = false;
            _rastroParado = 0f;
            _alvoRastro = _vida;
            _rastroBarra.Value = _vidaBarra.Value;

            _alvoCura = _vida;
            _animandoCura = true;
        }
    }

    public void DefinirJogadores(int quantidade)
    {
        _jogadores = Mathf.Clamp(quantidade, 1, 2);
        _vidaPorJogador[0] = -1f;
        _vidaPorJogador[1] = -1f;
        if (_jogadores == 2)
        {
            Scale = new Vector2(1.53f, 1f);
        }
    }

    private void ObservarSaude()
    {
        if (_saude == null) return;
        if (_saude.Atual == _observadoAtual && _saude.Maximo == _observadoMaximo) return;

        _observadoAtual = _saude.Atual;
        _observadoMaximo = _saude.Maximo;
        float proporcao = _saude.Maximo > 0 ? (float)_saude.Atual / _saude.Maximo : 0f;
        DefinirVida(0, proporcao);
    }

    private void ExecutarAnimacao(float delta)
    {
        float passo = Velocidade * delta;

        if (_animandoCura)
        {
            _vidaBarra.Value = Mathf.MoveToward(_vidaBarra.Value, _alvoCura, passo);
            if (!_animandoDano)
            {
                _rastroBarra.Value = _vidaBarra.Value;
            }
            if (_vidaBarra.Value <= _alvoCura)
            {
                _vidaBarra.Value = _alvoCura;
                if (!_animandoDano) _rastroBarra.Value = _alvoCura;
                _animandoCura = false;
            }
        }

        if (_animandoDano)
        {
            if (_rastroParado > 0f)
            {
                _rastroParado -= delta;
            }
            else
            {
                _rastroBarra.Value = Mathf.MoveToward(_rastroBarra.Value, _alvoRastro, passo);
                if (_rastroBarra.Value <= _alvoRastro)
                {
                    _rastroBarra.Value = _alvoRastro;
                    _animandoDano = false;
                }
            }
        }

        if (!_animandoDano && !_animandoCura)
        {
            _rastroBarra.Value = _vidaBarra.Value;
        }
    }

    private void AplicarCor(float valor)
    {
        bool perigo = valor <= LimiarPerigo;
        _vidaBarra.TintProgress = perigo ? CorPerigo
            : valor <= LimiarAmarelo ? CorAmarelo : CorVida;
        _rastroBarra.TintProgress = CorRastro;

        if (perigo != _perigo)
        {
            _perigo = perigo;
            EmitSignal(SignalName.PerigoMudou, _perigo);
        }
    }
}
