using Godot;

public partial class DoorTransition : Node
{
    [Export] public float FadeOutTime { get; set; } = 0.35f;
    [Export] public float BlackHoldTime { get; set; } = 0.55f;
    [Export] public float FadeInTime { get; set; } = 0.45f;
    [Export] public string RequiredKeyId { get; set; } = "";
    [Export] public float YawIntoRoomFromFrente { get; set; } = 180.0f;
    [Export] public float YawIntoRoomFromFundo { get; set; } = 0.0f;
    [Export] public string PromptText { get; set; } = "Abrir porta";
    [Export] public string LockedText { get; set; } = "Trancada — precisa de cartão";
    [Export] public NodePath ActorPath { get; set; } = new("../Player");
    [Export] public NodePath PhantomCameraPath { get; set; } = new("../PCam_Main");

    private CharacterBody3D _actor = null!;
    private ColorRect _black = null!;
    private Label _prompt = null!;
    private Area3D _triggerFront = null!;
    private Area3D _triggerBack = null!;
    private Node _phantomCamera = null!;
    private bool _busy;
    private string? _nearbySide;

    public override void _Ready()
    {
        _actor = GetNode<CharacterBody3D>(ActorPath);
        _black = GetNode<ColorRect>("Fade/Black");
        _prompt = GetNode<Label>("HUD/Prompt");
        _triggerFront = GetNode<Area3D>("GatilhoFrente");
        _triggerBack = GetNode<Area3D>("GatilhoFundo");
        _phantomCamera = GetNode(PhantomCameraPath);

        SetBlack(0.0f);
        SetPrompt(null);

        _triggerFront.BodyEntered += body => OnZoneChanged(body, "Fundo", true);
        _triggerFront.BodyExited += body => OnZoneChanged(body, "Fundo", false);
        _triggerBack.BodyEntered += body => OnZoneChanged(body, "Frente", true);
        _triggerBack.BodyExited += body => OnZoneChanged(body, "Frente", false);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_busy || _nearbySide == null || !@event.IsActionPressed("interact"))
        {
            return;
        }

        GetViewport().SetInputAsHandled();

        if (!CanOpen())
        {
            SetPrompt(LockedText);
            return;
        }

        Cross(_nearbySide);
    }

    private bool CanOpen()
    {
        return string.IsNullOrEmpty(RequiredKeyId) || KeyHolderHasCard();
    }

    private bool KeyHolderHasCard()
    {
        Node? inventory = GetTree().GetFirstNodeInGroup("inventory");
        if (inventory == null || !inventory.HasMethod("has_key"))
        {
            return false;
        }

        return inventory.Call("has_key", RequiredKeyId).AsBool();
    }

    private void OnZoneChanged(Node3D body, string side, bool entered)
    {
        if (body != _actor)
        {
            return;
        }

        _nearbySide = entered ? side : null;
        SetPrompt(_nearbySide == null || _busy ? null : (CanOpen() ? PromptText : LockedText));
    }

    private void SetPrompt(string? text)
    {
        _prompt.Visible = text != null;
        _prompt.Text = text == null ? "" : $"[E]  {text}";
    }

    private void SetBlack(float alpha)
    {
        Color color = _black.Color;
        color.A = alpha;
        _black.Color = color;
    }

    private void SetActorInputLocked(bool locked)
    {
        _actor.Set("InputLocked", locked);
    }

    private void FaceActorYaw(float yawDegrees)
    {
        if (_actor.HasMethod("FaceYawDegrees"))
        {
            _actor.Call("FaceYawDegrees", yawDegrees);
        }
    }

    private void FaceIntoRoom(string destinationPoint)
    {
        float yaw = destinationPoint == "Fundo" ? YawIntoRoomFromFundo : YawIntoRoomFromFrente;

        if (_phantomCamera.HasMethod("get_follow_mode")
            && _phantomCamera.HasMethod("set_third_person_rotation_degrees")
            && _phantomCamera.Call("get_follow_mode").AsInt32() == 6)
        {
            _phantomCamera.Call("set_third_person_rotation_degrees", new Vector3(-15.0f, yaw, 0.0f));
        }

        FaceActorYaw(yaw);
    }

    private async void Cross(string destinationPoint)
    {
        _busy = true;
        SetPrompt(null);
        SetActorInputLocked(true);
        _actor.Velocity = Vector3.Zero;

        Tween escurecer = CreateTween();
        escurecer.TweenProperty(_black, "color:a", 1.0f, FadeOutTime);
        await ToSignal(escurecer, Tween.SignalName.Finished);

        if (!GodotObject.IsInstanceValid(this) || !GodotObject.IsInstanceValid(_actor))
        {
            return;
        }

        _actor.GlobalPosition = GetNode<Node3D>($"Pontos/{destinationPoint}").GlobalPosition;
        _actor.Velocity = Vector3.Zero;
        FaceIntoRoom(destinationPoint);

        await ToSignal(GetTree().CreateTimer(BlackHoldTime), SceneTreeTimer.SignalName.Timeout);

        if (!GodotObject.IsInstanceValid(this) || !GodotObject.IsInstanceValid(_actor))
        {
            return;
        }

        Tween clarear = CreateTween();
        clarear.TweenProperty(_black, "color:a", 0.0f, FadeInTime);
        await ToSignal(clarear, Tween.SignalName.Finished);

        if (!GodotObject.IsInstanceValid(this) || !GodotObject.IsInstanceValid(_actor))
        {
            return;
        }

        SetActorInputLocked(false);
        _busy = false;
    }
}
