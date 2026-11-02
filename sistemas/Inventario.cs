using Godot;

[GlobalClass]
public partial class Inventario : Node
{
    [Export] public int Maximo { get; set; } = 3;

    [Signal] public delegate void MudouEventHandler(int quantidade);
    [Signal] public delegate void FaltouEventHandler();

    public int Frascos { get; private set; }

    public bool AdicionarFrasco()
    {
        if (Frascos >= Maximo) return false;
        Frascos++;
        EmitSignal(SignalName.Mudou, Frascos);
        return true;
    }

    public bool TentarConsumirFrasco()
    {
        if (Frascos <= 0)
        {
            EmitSignal(SignalName.Faltou);
            return false;
        }
        Frascos--;
        EmitSignal(SignalName.Mudou, Frascos);
        return true;
    }

    public bool ConsumirFrasco()
    {
        if (Frascos <= 0) return false;
        Frascos--;
        EmitSignal(SignalName.Mudou, Frascos);
        return true;
    }

    public bool TemFrasco => Frascos > 0;
}
