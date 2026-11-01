using Godot;

/// <summary>
/// Arquétipo de inimigo descrito como dado, não como cena.
///
/// A ideia (tirada da estrutura de engine de estúdio: um arquivo de entidade
/// por tipo, no original os .emt) é que criar um inimigo novo seja criar um
/// .tres novo — sem duplicar cena, sem tocar em código.
///
/// Uso: preencha o campo <c>Dados</c> do nó de inimigo com um destes recursos.
/// Os valores aqui sobrescrevem os do Inspector, que ficam como fallback para
/// inimigos sem arquétipo.
/// </summary>
[GlobalClass]
public partial class InimigoDados : Resource
{
    [Export] public string Nome { get; set; } = "Zumbi";

    [Export(PropertyHint.Range, "0.5,12,0.1")]
    public float Velocidade { get; set; } = 3.5f;

    [Export(PropertyHint.Range, "1,50,0.5")]
    public float RaioDeVisao { get; set; } = 14.0f;

    [Export(PropertyHint.Range, "0.5,6,0.1")]
    public float DistanciaDeParada { get; set; } = 1.8f;

    [Export(PropertyHint.Range, "1,30,0.5")]
    public float GiroSuave { get; set; } = 10.0f;

    [Export] public int Vida { get; set; } = 100;

    /// <summary>Cor da cápsula: é como se distingue o tipo em jogo.</summary>
    [Export] public Color Cor { get; set; } = new(0.82f, 0.29f, 0.24f);
}
