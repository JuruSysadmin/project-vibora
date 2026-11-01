using Godot;

public partial class Player : CharacterBody3D
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
    [Export] public float MouseSensitivity { get; set; } = 0.15f;
    [Export] public float PitchMinDegrees { get; set; } = -63.0f;
    [Export] public float PitchMaxDegrees { get; set; } = 25.0f;
    [Export] public NodePath PhantomCameraPath { get; set; } = new("%PCam_Main");

    /// <summary>
    /// Trava o controle (movimento e órbita da câmera) durante transições e
    /// cutscenes. A gravidade continua agindo: travar no ar não congela o
    /// jogador flutuando.
    /// </summary>
    public bool InputLocked { get; set; }

    private const int FollowModeThirdPerson = 6;

    private Node3D _body = null!;
    private Node _pcam = null!;
    private float _gravity;
    private float _coyoteTimer;
    private float _jumpBufferTimer;
    private bool _wasOnFloor = true;

    public override void _Ready()
    {
        _body = GetNode<Node3D>("Body");
        _pcam = GetNode(PhantomCameraPath);
        _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
        FloorSnapLength = 0.25f;
        Input.MouseMode = Input.MouseModeEnum.Captured;

        if (GetNodeOrNull<Saude>("Saude") is Saude saude)
        {
            saude.Morreu += Morrer;
        }
    }

    public void FaceYawDegrees(float yawDegrees)
    {
        _body.Rotation = new Vector3(0.0f, Mathf.DegToRad(yawDegrees), 0.0f);
    }

    private void Morrer()
    {
        InputLocked = true;
        Input.MouseMode = Input.MouseModeEnum.Visible;

        if (GetNodeOrNull<SequenciaMorte>("%SequenciaMorte") is SequenciaMorte sequencia)
        {
            sequencia.Iniciar();
            return;
        }

        GD.Print("[Player] morreu — recarregando a cena");
        GetTree().CreateTimer(2.5).Timeout += () => GetTree().ReloadCurrentScene();
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
        float dt = (float)delta;

        if (InputLocked)
        {
            // Sem controle horizontal, mas a gravidade continua valendo.
            Vector3 parado = Velocity;
            parado.X = 0.0f;
            parado.Z = 0.0f;
            parado.Y = IsOnFloor() ? Mathf.Min(parado.Y, 0.0f) : parado.Y - _gravity * dt;
            Velocity = parado;
            _coyoteTimer = 0.0f;
            _jumpBufferTimer = 0.0f;
            MoveAndSlide();
            _wasOnFloor = IsOnFloor();
            return;
        }

        Vector3 velocity = Velocity;
        bool onFloor = IsOnFloor();

        if (onFloor)
        {
            _coyoteTimer = CoyoteTime;
            if (!_wasOnFloor)
            {
                velocity.Y = Mathf.Min(velocity.Y, 0.0f);
            }
        }
        else
        {
            _coyoteTimer = Mathf.Max(0.0f, _coyoteTimer - dt);
            velocity.Y -= _gravity * dt;
        }

        if (Input.IsActionJustPressed("jump"))
        {
            _jumpBufferTimer = JumpBufferTime;
        }
        else
        {
            _jumpBufferTimer = Mathf.Max(0.0f, _jumpBufferTimer - dt);
        }

        if (_jumpBufferTimer > 0.0f && _coyoteTimer > 0.0f)
        {
            velocity.Y = JumpVelocity;
            _jumpBufferTimer = 0.0f;
            _coyoteTimer = 0.0f;
            onFloor = false;
        }
        else if (!onFloor && velocity.Y > 0.0f && Input.IsActionJustReleased("jump"))
        {
            velocity.Y *= JumpCut;
        }

        Vector2 input = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
        Vector3 targetVelocity = Vector3.Zero;

        if (input != Vector2.Zero)
        {
            Vector3 direction = CameraRelativeDirection(input);
            targetVelocity = direction * MoveSpeed;

            float targetYaw = Mathf.Atan2(-direction.X, -direction.Z);
            float bodyYaw = Mathf.LerpAngle(_body.Rotation.Y, targetYaw, 1.0f - Mathf.Exp(-TurnSpeed * dt));
            _body.Rotation = new Vector3(0.0f, bodyYaw, 0.0f);
        }

        float rate = input != Vector2.Zero ? Acceleration : Deceleration;
        if (!onFloor)
        {
            rate *= AirControl;
        }

        Vector3 horizontal = new Vector3(velocity.X, 0.0f, velocity.Z);
        horizontal = horizontal.MoveToward(targetVelocity, rate * dt);
        velocity.X = horizontal.X;
        velocity.Z = horizontal.Z;

        Velocity = velocity;
        MoveAndSlide();
        _wasOnFloor = IsOnFloor();
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
