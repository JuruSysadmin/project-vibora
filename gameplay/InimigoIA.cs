using Godot;

/// <summary>
/// IA do inimigo: máquina de estados com pathfinding.
///
///   PATRULHANDO — segue a rota (<see cref="Rota"/>) quando existe.
///   OCIOSO      — sem rota e sem alvo: freia e fica parado.
///   PERSEGUINDO — jogador dentro do raio de visão; ignora a rota e caça.
///
/// Perder o jogador devolve para Patrulhando (se há rota) ou Ocioso.
/// A navegação vem do NavigationAgent3D filho, que usa a malha baked na
/// NavigationRegion3D; tanto patrulha quanto perseguição passam por ele,
/// então o inimigo contorna paredes nos dois casos.
/// </summary>
public partial class InimigoIA : CharacterBody3D
{
    public enum Estado { Ocioso, Patrulhando, Perseguindo }

    /// <summary>
    /// Arquétipo opcional. Quando preenchido, os valores dele sobrescrevem os
    /// exports abaixo em _Ready — os exports passam a ser fallback para
    /// inimigos sem arquétipo.
    /// </summary>
    [Export] public InimigoDados? Dados { get; set; }

    /// <summary>
    /// Rota de patrulha, em coordenadas de mundo. Desenhe os pontos de controle
    /// no Inspector. Sem rota, o inimigo fica Ocioso quando não vê o jogador.
    /// </summary>
    [Export] public Curve3D? Rota { get; set; }

    /// <summary>Vida atual, inicializada a partir do arquétipo.</summary>
    public int Vida { get; private set; }

    /// <summary>
    /// Nome de exibição do arquétipo. NÃO é o nome do nó: renomear nó em
    /// runtime quebra referências por caminho e por nome único.
    /// </summary>
    public string NomeExibicao => Dados?.Nome ?? Name;

    [Export] public float Velocidade { get; set; } = 3.5f;
    [Export] public float RaioDeVisao { get; set; } = 14.0f;
    /// <summary>Para de andar a esta distância do jogador.</summary>
    [Export] public float DistanciaDeParada { get; set; } = 1.8f;
    [Export] public float GiroSuave { get; set; } = 10.0f;
    /// <summary>Multiplicador do raio para perder o alvo (evita piscar no limite).</summary>
    [Export] public float FolgaParaPerder { get; set; } = 1.2f;

    public Estado EstadoAtual { get; private set; } = Estado.Ocioso;

    private NavigationAgent3D _agente = null!;
    private Node3D _jogador = null!;
    private float _gravidade;
    /// <summary>Distância já percorrida na rota (0 até GetBakedLength).</summary>
    private float _rotaDistancia;
    private int _rotaSentido = 1;

    public override void _Ready()
    {
        _agente = GetNode<NavigationAgent3D>("NavigationAgent3D");
        _jogador = GetParent().GetNode<Node3D>("Player");
        _gravidade = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

        AplicarArquetipo();

        // Começa patrulhando se houver rota; senão fica ocioso até ver o jogador.
        EstadoAtual = TemRota() ? Estado.Patrulhando : Estado.Ocioso;
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        Vector3 velocity = Velocity;

        if (!IsOnFloor())
        {
            velocity.Y -= _gravidade * dt;
        }

        float distancia = GlobalPosition.DistanceTo(_jogador.GlobalPosition);
        AtualizarEstado(distancia);

        switch (EstadoAtual)
        {
            case Estado.Perseguindo when distancia > DistanciaDeParada:
                MoverPara(_jogador.GlobalPosition, ref velocity, dt);
                break;

            case Estado.Patrulhando:
                Patrulhar(ref velocity, dt);
                break;

            default:
                Frear(ref velocity, dt);
                break;
        }

        Velocity = velocity;
        MoveAndSlide();
    }

    private bool TemRota()
    {
        return Rota != null && Rota.GetBakedLength() > 0.5f;
    }

    /// <summary>Transições da máquina de estados.</summary>
    private void AtualizarEstado(float distancia)
    {
        if (EstadoAtual != Estado.Perseguindo && distancia < RaioDeVisao)
        {
            EstadoAtual = Estado.Perseguindo;
            return;
        }

        if (EstadoAtual == Estado.Perseguindo && distancia > RaioDeVisao * FolgaParaPerder)
        {
            EstadoAtual = TemRota() ? Estado.Patrulhando : Estado.Ocioso;
        }
    }

    /// <summary>
    /// O alvo anda pela curva na velocidade do inimigo, e o inimigo persegue
    /// esse ponto pelo agente de navegação. Assim ele contorna paredes em vez
    /// de andar em linha reta até o próximo ponto.
    /// </summary>
    private void Patrulhar(ref Vector3 velocity, float dt)
    {
        if (!TemRota())
        {
            Frear(ref velocity, dt);
            return;
        }

        float total = Rota!.GetBakedLength();
        _rotaDistancia += _rotaSentido * Velocidade * dt;

        if (_rotaDistancia >= total)
        {
            _rotaDistancia = total;
            _rotaSentido = -1;
        }
        else if (_rotaDistancia <= 0.0f)
        {
            _rotaDistancia = 0.0f;
            _rotaSentido = 1;
        }

        MoverPara(Rota.SampleBaked(_rotaDistancia), ref velocity, dt);
    }

    /// <summary>Anda um passo na direção do alvo, seguindo o caminho navegável.</summary>
    private void MoverPara(Vector3 alvo, ref Vector3 velocity, float dt)
    {
        _agente.TargetPosition = alvo;

        if (_agente.IsNavigationFinished())
        {
            Frear(ref velocity, dt);
            return;
        }

        Vector3 proximo = _agente.GetNextPathPosition();
        Vector3 direcao = proximo - GlobalPosition;
        direcao.Y = 0.0f;

        if (direcao.Length() < 0.05f)
        {
            return;
        }

        direcao = direcao.Normalized();
        velocity.X = direcao.X * Velocidade;
        velocity.Z = direcao.Z * Velocidade;

        // A frente de um Node3D é -Z.
        float alvoYaw = Mathf.Atan2(-direcao.X, -direcao.Z);
        float yaw = Mathf.LerpAngle(Rotation.Y, alvoYaw, 1.0f - Mathf.Exp(-GiroSuave * dt));
        Rotation = new Vector3(0.0f, yaw, 0.0f);
    }

    private static void Frear(ref Vector3 velocity, float dt)
    {
        velocity.X = Mathf.MoveToward(velocity.X, 0.0f, 25.0f * dt);
        velocity.Z = Mathf.MoveToward(velocity.Z, 0.0f, 25.0f * dt);
    }

    /// <summary>Copia os valores do arquétipo e pinta a cápsula.</summary>
    private void AplicarArquetipo()
    {
        if (Dados == null)
        {
            Vida = 100;
            return;
        }

        Velocidade = Dados.Velocidade;
        RaioDeVisao = Dados.RaioDeVisao;
        DistanciaDeParada = Dados.DistanciaDeParada;
        GiroSuave = Dados.GiroSuave;
        Vida = Dados.Vida;
        AplicarCor(Dados.Cor);
    }

    /// <summary>
    /// Duplica o material antes de trocar a cor: sem duplicar, pintar um
    /// inimigo pintaria todos os que compartilham o mesmo .tres.
    /// </summary>
    private void AplicarCor(Color cor)
    {
        MeshInstance3D? corpo = GetNodeOrNull<MeshInstance3D>("Body");
        if (corpo?.MaterialOverride is StandardMaterial3D baseMaterial)
        {
            StandardMaterial3D copia = (StandardMaterial3D)baseMaterial.Duplicate();
            copia.AlbedoColor = cor;
            corpo.MaterialOverride = copia;
        }
    }
}
