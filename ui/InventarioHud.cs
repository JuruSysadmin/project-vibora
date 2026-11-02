using Godot;

[GlobalClass]
public partial class InventarioHud : HBoxContainer
{
    [Export] public NodePath? InventarioPath { get; set; }
    [Export] public string CaminhoIcone { get; set; } = "res://art/telas/hud/esp_item.png";
    [Export] public Vector2 TamanhoIcone { get; set; } = new(64f, 64f);

    private readonly System.Collections.Generic.List<TextureRect> _slots = new();
    private int _quantidade;

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 8);

        var icone = GD.Load<Texture2D>(CaminhoIcone);
        if (icone == null)
        {
            GD.PushWarning("[InventarioHud] ícone do frasco não carregado.");
            return;
        }

        int total = 3;
        if (InventarioPath is { IsEmpty: false } caminho)
        {
            if (GetNodeOrNull<Inventario>(caminho) is Inventario inventario)
            {
                if (inventario.Maximo > 0) total = inventario.Maximo;
                inventario.Mudou += Montar;
                inventario.Faltou += PiscarFaltando;
            }
        }

        for (int i = 0; i < total; i++)
        {
            var slot = new TextureRect
            {
                Texture = icone,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                CustomMinimumSize = TamanhoIcone,
                Modulate = new Color(1f, 1f, 1f, 0.14f),
            };
            AddChild(slot);
            _slots.Add(slot);
        }
    }

    private void Montar(int quantidade)
    {
        _quantidade = quantidade;
        for (int i = 0; i < _slots.Count; i++)
        {
            _slots[i].Modulate = i < _quantidade
                ? new Color(1f, 1f, 1f, 1f)
                : new Color(1f, 1f, 1f, 0.14f);
        }
    }

    private void PiscarFaltando()
    {
        var cheia = _quantidade > 0 ? new Color(1f, 1f, 1f, 1f) : new Color(1f, 1f, 1f, 0.14f);

        foreach (TextureRect slot in _slots)
        {
            var pulso = CreateTween();
            pulso.TweenProperty(slot, "modulate", new Color(1f, 0.25f, 0.25f, 1f), 0.08);
            pulso.TweenProperty(slot, "modulate", cheia, 0.25);
        }
    }
}
