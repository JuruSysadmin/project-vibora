using Godot;

/// <summary>
/// Física de movimento do personagem: gravidade, aceleração, pulo com coyote
/// time, jump buffer e jump cut, e o giro do corpo para a direção do movimento.
///
/// Não lê input e não conhece a câmera: recebe a direção já resolvida pelo
/// dono (Player), uma vez por frame de física. O motor não decide para onde
/// ir — decide como chegar. Essa é a mesma divisão do original entre Motor e
/// MotionSeq, e é o que impede o controlador de virar um arquivo ingovernável
/// quando entrar ataque, mira e inventário.
/// </summary>
public partial class PlayerMotor : Node
{
    [Export] public float MoveSpeed { get; set; } = 6.0f;
    [Export] public float Acceleration { get; set; } = 100.0f;
    [Export] public float Deceleration { get; set; } = 28.0f;
    [Export] public float AirControl { get; set; } = 0.2f;
    [Export] public float JumpVelocity { get; set; } = 6.0f;
    [Export] public float JumpCut { get; set; } = 0.45f;
    [Export] public float CoyoteTime { get; set; } = 0.08f;
    [Export] public float JumpBufferTime { get; set; } = 0.08f;
    [Export] public float TurnSpeed { get; set; } = 18.0f;

    /// <summary>
    /// Trava o controle: sem movimento horizontal e sem pulo, mas a gravidade
    /// continua valendo — travar no ar não congela o jogador flutuando.
    /// </summary>
    public bool Travado { get; set; }

    public bool NoChao => _corpo != null && _corpo.IsOnFloor();

    private CharacterBody3D _corpo = null!;
    private Node3D _visual = null!;
    private float _gravidade;
    private float _coyote;
    private float _buffer;
    private bool _estavaNoChao = true;

    public void Configurar(CharacterBody3D corpo, Node3D visual)
    {
        _corpo = corpo;
        _visual = visual;
        _gravidade = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
    }

    /// <summary>
    /// Um passo de física. <paramref name="direcao"/> já vem normalizada (ou
    /// zero); <paramref name="pulou"/> e <paramref name="soltou"/> são os
    /// eventos de input deste frame.
    /// </summary>
    public void Executar(float dt, Vector3 direcao, bool pulou, bool soltou)
    {
        if (Travado)
        {
            Vector3 parado = _corpo.Velocity;
            parado.X = 0.0f;
            parado.Z = 0.0f;
            parado.Y = _corpo.IsOnFloor() ? Mathf.Min(parado.Y, 0.0f) : parado.Y - _gravidade * dt;
            _corpo.Velocity = parado;
            _coyote = 0.0f;
            _buffer = 0.0f;
            _corpo.MoveAndSlide();
            _estavaNoChao = _corpo.IsOnFloor();
            return;
        }

        Vector3 velocidade = _corpo.Velocity;
        bool noChao = _corpo.IsOnFloor();

        _buffer = pulou ? JumpBufferTime : Mathf.Max(0.0f, _buffer - dt);

        if (noChao)
        {
            _coyote = CoyoteTime;
            if (!_estavaNoChao)
            {
                velocidade.Y = Mathf.Min(velocidade.Y, 0.0f);
            }
        }
        else
        {
            _coyote = Mathf.Max(0.0f, _coyote - dt);
            velocidade.Y -= _gravidade * dt;
        }

        if (_buffer > 0.0f && _coyote > 0.0f)
        {
            velocidade.Y = JumpVelocity;
            _buffer = 0.0f;
            _coyote = 0.0f;
            noChao = false;
        }
        else if (!noChao && velocidade.Y > 0.0f && soltou)
        {
            velocidade.Y *= JumpCut;
        }

        float taxa = direcao != Vector3.Zero ? Acceleration : Deceleration;
        if (!noChao)
        {
            taxa *= AirControl;
        }

        Vector3 alvo = direcao * MoveSpeed;
        Vector3 horizontal = new(velocidade.X, 0.0f, velocidade.Z);
        horizontal = horizontal.MoveToward(alvo, taxa * dt);
        velocidade.X = horizontal.X;
        velocidade.Z = horizontal.Z;

        _corpo.Velocity = velocidade;
        _corpo.MoveAndSlide();
        _estavaNoChao = _corpo.IsOnFloor();

        if (direcao != Vector3.Zero)
        {
            GirarPara(direcao, dt);
        }
    }

    /// <summary>Vira o corpo para uma direção de mundo (a frente é -Z).</summary>
    public void GirarPara(Vector3 direcao, float dt)
    {
        float alvoYaw = Mathf.Atan2(-direcao.X, -direcao.Z);
        float yaw = Mathf.LerpAngle(_visual.Rotation.Y, alvoYaw, 1.0f - Mathf.Exp(-TurnSpeed * dt));
        _visual.Rotation = new Vector3(0.0f, yaw, 0.0f);
    }

    /// <summary>Vira o corpo de imediato (usado na chegada de uma transição).</summary>
    public void OrientarPara(float yawGraus)
    {
        _visual.Rotation = new Vector3(0.0f, Mathf.DegToRad(yawGraus), 0.0f);
    }
}
