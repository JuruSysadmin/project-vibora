using Godot;

/// <summary>
/// Controlador do jogador: lê input, resolve a direção relativa à câmera,
/// cuida da órbita da PhantomCamera e delega a física ao PlayerMotor.
///
/// A física não mora aqui de propósito: movimento, pulo e giro de corpo são
/// do motor; input, câmera e morte são do controlador. Quando entrar ataque
/// e mira, eles entram neste arquivo sem inchar a parte de movimento.
/// </summary>
public partial class Player : CharacterBody3D
{
    /// <summary>Degrees per mouse pixel (Phantom Third Person API uses degrees).</summary>
    [Export] public float MouseSensitivity { get; set; } = 0.15f;
    [Export] public float PitchMinDegrees { get; set; } = -63.0f;
    [Export] public float PitchMaxDegrees { get; set; } = 25.0f;
    [Export] public NodePath PhantomCameraPath { get; set; } = new("%PCam_Main");
    [Export] public string CaminhoSomFrasco { get; set; } = "res://art/sons/curar.wav";
    [Export] public float AtrasoFrasco { get; set; } = 1.0f;

    /// <summary>
    /// Trava o controle (movimento e órbita da câmera) durante transições e
    /// cutscenes. Repassado ao motor a cada frame.
    /// </summary>
    public bool InputLocked { get; set; }

    private const int FollowModeThirdPerson = 6;

    private PlayerMotor _motor = null!;
    private Node3D _body = null!;
    private Node _pcam = null!;
    private AudioStreamPlayer _audioFrasco = null!;
    private float _atrasoFrasco;

    public override void _Ready()
    {
        _body = GetNode<Node3D>("Body");
        _motor = GetNode<PlayerMotor>("Motor");
        _motor.Configurar(this, _body);

        _pcam = GetNode(PhantomCameraPath);
        FloorSnapLength = 0.25f;
        Input.MouseMode = Input.MouseModeEnum.Captured;

        if (GetNodeOrNull<Saude>("Saude") is Saude saude)
        {
            saude.Morreu += Morrer;
        }

        if (GetNodeOrNull<SequenciaMorte>("%SequenciaMorte") is SequenciaMorte sequencia)
        {
            sequencia.Reviveu += AoReviver;
        }

        _audioFrasco = new AudioStreamPlayer { Bus = "SFX" };
        AddChild(_audioFrasco);
    }

    /// <summary>Vira o corpo de imediato (usado na chegada de uma transição).</summary>
    public void FaceYawDegrees(float yawDegrees)
    {
        _motor.OrientarPara(yawDegrees);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion
            && !InputLocked
            && Input.MouseMode == Input.MouseModeEnum.Captured
            && CanOrbitThirdPerson())
        {
            Vector3 rot = _pcam.Call("get_third_person_rotation_degrees").AsVector3();
            rot.X = Mathf.Clamp(rot.X - motion.Relative.Y * MouseSensitivity, PitchMinDegrees, PitchMaxDegrees);
            rot.Y -= motion.Relative.X * MouseSensitivity;
            _pcam.Call("set_third_person_rotation_degrees", rot);
        }
        else if (@event.IsActionPressed("ui_cancel"))
        {
            Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Captured
                ? Input.MouseModeEnum.Visible
                : Input.MouseModeEnum.Captured;
        }
        else if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }
                 && Input.MouseMode != Input.MouseModeEnum.Captured)
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_atrasoFrasco > 0f)
        {
            _atrasoFrasco -= (float)delta;
        }

        if (!InputLocked && Input.IsActionJustPressed("usar_item"))
        {
            UsarFrasco();
        }

        _motor.Travado = InputLocked;

        Vector2 entrada = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
        Vector3 direcao = entrada == Vector2.Zero ? Vector3.Zero : CameraRelativeDirection(entrada);

        _motor.Executar(
            (float)delta,
            direcao,
            Input.IsActionJustPressed("jump"),
            Input.IsActionJustReleased("jump"));
    }

    private void UsarFrasco()
    {
        if (_atrasoFrasco > 0f) return;

        if (GetNodeOrNull<Inventario>("Inventario") is not Inventario inventario) return;
        if (!inventario.TentarConsumirFrasco()) return;

        if (GetNodeOrNull<Saude>("Saude") is Saude saude)
        {
            saude.RestaurarVida();
        }

        var som = GD.Load<AudioStream>(CaminhoSomFrasco);
        if (som != null)
        {
            _audioFrasco.Stream = som;
            _audioFrasco.Play();
        }

        _atrasoFrasco = AtrasoFrasco;
    }

    /// <summary>
    /// Morte do jogador: entrega para a SequenciaMorte. O reload direto fica
    /// como fallback caso o nó não exista, senão o jogo travaria com o
    /// jogador morto e o input travado.
    /// </summary>
    private void Morrer()
    {
        InputLocked = true;
        Input.MouseMode = Input.MouseModeEnum.Visible;

        bool comFrasco = false;
        if (GetNodeOrNull<Inventario>("Inventario") is Inventario inventario)
        {
            comFrasco = inventario.ConsumirFrasco();
        }

        if (GetNodeOrNull<SequenciaMorte>("%SequenciaMorte") is SequenciaMorte sequencia)
        {
            sequencia.Iniciar(comFrasco);
            return;
        }

        GD.Print("[Player] morreu — recarregando a cena");
        GetTree().CreateTimer(2.5).Timeout += () => GetTree().ReloadCurrentScene();
    }

    private void AoReviver()
    {
        InputLocked = false;
        Input.MouseMode = Input.MouseModeEnum.Captured;

        if (GetNodeOrNull<Saude>("Saude") is Saude saude)
        {
            saude.Reviver();
        }
    }

    private bool CanOrbitThirdPerson()
    {
        if (_pcam == null || !GodotObject.IsInstanceValid(_pcam))
        {
            return false;
        }

        if (!_pcam.HasMethod("get_follow_mode")
            || !_pcam.HasMethod("get_third_person_rotation_degrees")
            || !_pcam.HasMethod("set_third_person_rotation_degrees"))
        {
            return false;
        }

        return _pcam.Call("get_follow_mode").AsInt32() == FollowModeThirdPerson;
    }

    private Vector3 CameraRelativeDirection(Vector2 input)
    {
        Camera3D? cam = GetViewport().GetCamera3D();
        if (cam == null)
        {
            return new Vector3(input.X, 0.0f, input.Y).Normalized();
        }

        Vector3 direction = cam.GlobalTransform.Basis * new Vector3(input.X, 0.0f, input.Y);
        direction.Y = 0.0f;
        return direction.Normalized();
    }
}
