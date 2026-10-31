using Godot;


public partial class Player : CharacterBody3D
{
    [Export] public float MoveSpeed { get; set; } = 5.0f;
    [Export] public float Acceleration { get; set; } = 35.0f;
    [Export] public float Deceleration { get; set; } = 40.0f;
    [Export] public float AirControl { get; set; } = 0.45f;
    [Export] public float JumpVelocity { get; set; } = 5.5f;
    [Export] public float CoyoteTime { get; set; } = 0.12f;
    [Export] public float JumpBufferTime { get; set; } = 0.12f;
    [Export] public float MouseSensitivity { get; set; } = 0.0025f;
    [Export] public float PitchMin { get; set; } = -1.1f;
    [Export] public float PitchMax { get; set; } = 0.45f;

    private SpringArm3D _springArm = null!;
    private float _gravity;
    private float _pitch;
    private float _coyoteTimer;
    private float _jumpBufferTimer;

    public override void _Ready()
    {
        _springArm = GetNode<SpringArm3D>("SpringArm3D");
        _pitch = _springArm.Rotation.X;
        _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
        FloorSnapLength = 0.2f;
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            RotateY(-motion.Relative.X * MouseSensitivity);
            _pitch = Mathf.Clamp(_pitch - motion.Relative.Y * MouseSensitivity, PitchMin, PitchMax);
            _springArm.Rotation = new Vector3(_pitch, 0.0f, 0.0f);
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
        Vector3 velocity = Velocity;

        if (IsOnFloor())
        {
            _coyoteTimer = CoyoteTime;
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
        }

        Vector2 input = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
        Vector3 targetVelocity = Vector3.Zero;

        if (input != Vector2.Zero)
        {
            Vector3 direction = (Transform.Basis * new Vector3(input.X, 0.0f, input.Y)).Normalized();
            targetVelocity = direction * MoveSpeed;
        }

        float rate = input != Vector2.Zero ? Acceleration : Deceleration;
        if (!IsOnFloor())
        {
            rate *= AirControl;
        }

        Vector3 horizontal = new Vector3(velocity.X, 0.0f, velocity.Z);
        horizontal = horizontal.MoveToward(targetVelocity, rate * dt);
        velocity.X = horizontal.X;
        velocity.Z = horizontal.Z;

        Velocity = velocity;
        MoveAndSlide();
    }
}
