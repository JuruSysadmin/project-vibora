using Godot;

/// <summary>
/// Vida compartilhada por jogador e inimigos. Um componente, dois usos — a
/// mesma ideia do "Common" do original: o sistema de vida não sabe o que é
/// um jogador.
///
/// Responsabilidades: guardar a vida, aplicar dano com i-frames, emitir
/// sinais e piscar o corpo. Quem morre decide o que fazer no sinal Morreu.
/// </summary>
[GlobalClass]
public partial class Saude : Node
{
    [Export] public int Maximo { get; set; } = 100;

    /// <summary>Tempo de invulnerabilidade após levar dano, em segundos.</summary>
    [Export] public float Invulnerabilidade { get; set; } = 0.5f;

    /// <summary>Mesh que pisca ao levar dano. Opcional.</summary>
    [Export] public MeshInstance3D? Corpo { get; set; }

    public int Atual { get; private set; }
    public bool Morto => Atual <= 0;

    [Signal] public delegate void FeridoEventHandler(int atual, int dano);
    [Signal] public delegate void MorreuEventHandler();

    private double _protegidoAte;
    private StandardMaterial3D? _material;
    private Color _corOriginal = Colors.White;

    public override void _Ready()
    {
        if (Atual <= 0)
        {
            Atual = Maximo;
        }

        PrepararFlash();
    }

    /// <summary>
    /// Define a vida a partir de um arquétipo. Precisa existir porque os
    /// filhos ficam prontos ANTES do pai: o _Ready daqui já rodou quando o
    /// InimigoIA aplica o arquétipo.
    /// </summary>
    public void Configurar(int maximo)
    {
        Maximo = maximo;
        Atual = maximo;
    }

    /// <returns>true se o dano realmente entrou (não estava morto nem protegido).</returns>
    public bool AplicarDano(int dano)
    {
        if (Morto || dano <= 0)
        {
            return false;
        }

        double agora = Time.GetTicksMsec() / 1000.0;
        if (agora < _protegidoAte)
        {
            return false;
        }

        Atual = Mathf.Max(0, Atual - dano);
        _protegidoAte = agora + Invulnerabilidade;

        Piscar();
        EmitSignal(SignalName.Ferido, Atual, dano);

        if (Atual == 0)
        {
            EmitSignal(SignalName.Morreu);
        }

        return true;
    }

    public void Curar(int quantidade)
    {
        if (Morto || quantidade <= 0)
        {
            return;
        }

        Atual = Mathf.Min(Maximo, Atual + quantidade);
    }

    public void Reviver()
    {
        if (Atual > 0)
        {
            return;
        }

        Atual = Maximo;
        double agora = Time.GetTicksMsec() / 1000.0;
        _protegidoAte = agora + Invulnerabilidade;
    }

    public void RestaurarVida()
    {
        if (Morto)
        {
            return;
        }

        Atual = Maximo;
    }

    /// <summary>
    /// Duplica o material antes de piscar: sem duplicar, piscar um inimigo
    /// piscaria todos os que compartilham o mesmo .tres.
    /// </summary>
    private void PrepararFlash()
    {
        if (Corpo?.MaterialOverride is not StandardMaterial3D baseMaterial)
        {
            return;
        }

        _material = (StandardMaterial3D)baseMaterial.Duplicate();
        Corpo.MaterialOverride = _material;
        _corOriginal = _material.AlbedoColor;
    }

    private void Piscar()
    {
        if (_material == null)
        {
            return;
        }

        Tween tween = CreateTween();
        tween.TweenProperty(_material, "albedo_color", Colors.White, 0.06);
        tween.TweenProperty(_material, "albedo_color", _corOriginal, 0.14);
    }
}
