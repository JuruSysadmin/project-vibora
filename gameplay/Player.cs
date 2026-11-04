using System;
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
    [Export] public float MouseSensitivity { get; set; } = 0.15f;
    [Export] public float PitchMinDegrees { get; set; } = -63.0f;
    [Export] public float PitchMaxDegrees { get; set; } = 25.0f;
    [Export] public NodePath PhantomCameraPath { get; set; } = new("../PCam_Main");
    [Export] public string CaminhoSomFrasco { get; set; } = "res://art/sons/curar.wav";
    [Export] public float AtrasoFrasco { get; set; } = 1.0f;
    [Export] public float VelocidadeRefWalk { get; set; } = 1.35f;
    [Export] public float TempoBlendAnimacao { get; set; } = 0.2f;
    [Export] public float LimiarAndando { get; set; } = 0.4f;
    [Export] public AnimationLibrary BibliotecaMixamo { get; set; } = null!;

    /// <summary>
    /// Trava o controle (movimento, órbita e uso de frasco) durante transições e
    /// cutscenes. É <c>[Export]</c> de propósito: DoorTransition e InventarioTela
    /// só têm um <c>Node</c> na mão e escrevem via <c>Set("InputLocked", ...)</c>,
    /// que enxerga apenas propriedades registradas no objeto Godot.
    /// </summary>
    [Export] public bool InputLocked { get; set; }

    private const int FollowModeThirdPerson = 6;
    private const string ClipIdle = "idle";
    private const string ClipWalk = "walk";

    private PlayerMotor _motor = null!;
    private Node3D _body = null!;
    private Node _pcam = null!;
    private SequenciaMorte? _sequenciaMorte;
    private AnimationPlayer _anim = null!;
    private AudioStreamPlayer _audioFrasco = null!;
    private float _atrasoFrasco;
    private string _animAtual = "";

    public override void _Ready()
    {
        _body = GetNode<Node3D>("Body");
        _motor = GetNode<PlayerMotor>("Motor");
        _motor.Configurar(this, _body);
        _anim = GetNode<AnimationPlayer>("Body/Mixamo/AnimationPlayer");
        PrepararAnimacoesMixamo();

        FloorSnapLength = 0.25f;
        Input.MouseMode = Input.MouseModeEnum.Captured;
        CallDeferred(MethodName.BindPhantomCamera);

        if (GetNodeOrNull<Saude>("Saude") is Saude saude)
        {
            saude.Morreu += Morrer;
        }

        _sequenciaMorte = GetNodeOrNull<SequenciaMorte>("../SequenciaMorte");
        if (_sequenciaMorte == null)
        {
            GD.PushError("[Player] '../SequenciaMorte' não encontrado — morrer vai recarregar a cena.");
        }
        else
        {
            _sequenciaMorte.Reviveu += AoReviver;
        }

        _audioFrasco = new AudioStreamPlayer { Bus = "SFX" };
        AddChild(_audioFrasco);
    }

    private void BindPhantomCamera()
    {
        _pcam = GetNodeOrNull(PhantomCameraPath);
        if (_pcam == null)
        {
            GD.PushError($"[Player] PhantomCamera '{PhantomCameraPath}' não encontrada — órbita third-person desligada.");
            return;
        }

        // Não é redundância com o follow_target do .tscn: set_follow_target arma
        // _should_follow, conecta tree_exiting, registra o alvo no spring arm e
        // detecta se é corpo físico. Sem esta chamada a câmera não segue.
        _pcam.Call("set_follow_target", this);
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
        else if (@event.IsActionPressed("ui_cancel") && !InputLocked && !GetTree().Paused)
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

        if (!InputLocked && !GetTree().Paused && Input.IsActionJustPressed("usar_item"))
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

        AtualizarAnimacao();
    }

    private void PrepararAnimacoesMixamo()
    {
        if (BibliotecaMixamo == null)
            throw new InvalidOperationException("[Player] BibliotecaMixamo não atribuída — defina no player.tscn (player_clips.tres).");

        AnimationLibrary? lib = _anim.GetAnimationLibrary("");
        if (lib == null)
            throw new InvalidOperationException("[Player] AnimationPlayer sem biblioteca padrão.");

        foreach (string nome in BibliotecaMixamo.GetAnimationList())
        {
            if (lib.HasAnimation(nome))
                lib.RemoveAnimation(nome);
            lib.AddAnimation(nome, BibliotecaMixamo.GetAnimation(nome));
        }

        foreach (string legado in new[] { "mixamo_com", "Take 001" })
        {
            if (lib.HasAnimation(legado))
                lib.RemoveAnimation(legado);
        }

        if (!_anim.HasAnimation(ClipIdle) || !_anim.HasAnimation(ClipWalk))
            throw new InvalidOperationException("[Player] BibliotecaMixamo precisa dos clips 'idle' e 'walk'.");

        _anim.Play(ClipIdle);
        _animAtual = ClipIdle;
    }

    private void AtualizarAnimacao()
    {
        float horizontal = new Vector2(Velocity.X, Velocity.Z).Length();
        bool andando = !InputLocked && horizontal > LimiarAndando;

        if (andando && _anim.HasAnimation(ClipWalk))
        {
            float escala = VelocidadeRefWalk > 0.01f ? horizontal / VelocidadeRefWalk : 1f;
            _anim.SpeedScale = Mathf.Clamp(escala, 0.6f, 2.2f);
            if (_animAtual != ClipWalk)
            {
                _anim.Play(ClipWalk, TempoBlendAnimacao);
                _animAtual = ClipWalk;
            }
        }
        else if (_anim.HasAnimation(ClipIdle))
        {
            _anim.SpeedScale = 1f;
            if (_animAtual != ClipIdle)
            {
                _anim.Play(ClipIdle, TempoBlendAnimacao);
                _animAtual = ClipIdle;
            }
        }
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
    /// Morte do jogador: entrega para a SequenciaMorte, resolvida uma vez no
    /// <c>_Ready</c>. Sem ela, recarrega a cena — o jogador morto com o input
    /// travado travaria o jogo de vez.
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

        if (_sequenciaMorte != null)
        {
            _sequenciaMorte.Iniciar(comFrasco);
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
        if (!GodotObject.IsInstanceValid(_pcam))
            return false;

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
