using Godot;

/// <summary>
/// IA do inimigo: máquina de estados de dois estados.
///
///   OCIOSO      — o jogador está fora do raio de visão; freia e fica parado.
///   PERSEGUINDO — o jogador entrou no raio; segue o caminho devolvido pelo
///                 NavigationAgent3D, que desvia das paredes usando a malha
///                 de navegação baked na NavigationRegion3D "Navegacao".
///
/// Para acrescentar um estado (ex.: Patrulhando), adicione no enum, trate a
/// transição em <see cref="AtualizarEstado"/> e o comportamento no
/// _PhysicsProcess. O agente de navegação não muda.
/// </summary>
public partial class InimigoIA : CharacterBody3D
{
    public enum Estado { Ocioso, Perseguindo }

    /// <summary>
    /// Arquétipo opcional. Quando preenchido, os valores dele sobrescrevem os
    /// exports abaixo em _Ready — os exports passam a ser fallback para
    /// inimigos sem arquétipo.
    /// </summary>
    [Export] public InimigoDados? Dados { get; set; }

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

    public override void _Ready()
    {
        _agente = GetNode<NavigationAgent3D>("NavigationAgent3D");
        _jogador = GetParent().GetNode<Node3D>("Player");
        _gravidade = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

        AplicarArquetipo();
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        Vector3 velocity = Velocity;

        // Gravidade sempre: o inimigo também cai.
        if (!IsOnFloor())
        {
            velocity.Y -= _gravidade * dt;
        }

        float distancia = GlobalPosition.DistanceTo(_jogador.GlobalPosition);
        AtualizarEstado(distancia);

        if (EstadoAtual == Estado.Perseguindo && distancia > DistanciaDeParada)
        {
            _agente.TargetPosition = _jogador.GlobalPosition;
            MoverPeloCaminho(ref velocity, dt);
        }
        else
        {
            Frear(ref velocity, dt);
        }

        Velocity = velocity;
        MoveAndSlide();
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

    /// <summary>Transições da máquina de estados.</summary>
    private void AtualizarEstado(float distancia)
    {
        switch (EstadoAtual)
        {
            case Estado.Ocioso when distancia < RaioDeVisao:
                EstadoAtual = Estado.Perseguindo;
                break;

            case Estado.Perseguindo when distancia > RaioDeVisao * FolgaParaPerder:
                EstadoAtual = Estado.Ocioso;
                break;
        }
    }

    private void MoverPeloCaminho(ref Vector3 velocity, float dt)
    {
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
}
