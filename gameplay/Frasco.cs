using Godot;

[GlobalClass]
public partial class Frasco : Area3D
{
    [Export] public string CaminhoSom { get; set; } = "res://art/sons/item_pego.wav";

    private AudioStreamPlayer _audio = null!;

    public override void _Ready()
    {
        BodyEntered += AoCorpo;
        _audio = new AudioStreamPlayer { Bus = "SFX" };
        AddChild(_audio);
    }

    private void AoCorpo(Node3D corpo)
    {
        if (corpo.GetNodeOrNull<Inventario>("Inventario") is not Inventario inventario) return;
        if (!inventario.AdicionarFrasco()) return;

        SetDeferred(Area3D.PropertyName.Monitoring, false);
        if (GetNodeOrNull<Node3D>("Visual") is { } visual)
        {
            visual.Visible = false;
        }

        var som = GD.Load<AudioStream>(CaminhoSom);
        if (som != null)
        {
            _audio.Stream = som;
            _audio.Play();
        }

        GetTree().CreateTimer(1.0).Timeout += QueueFree;
    }
}
