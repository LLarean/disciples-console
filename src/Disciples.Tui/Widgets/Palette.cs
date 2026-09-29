using Disciples.Core.Map;
using Terminal.Gui.Drawing;
using Attribute = Terminal.Gui.Drawing.Attribute;

namespace Disciples.Tui.Widgets;

public static class Palette
{
    public static readonly Color Background = new(18, 18, 22);
    public static readonly Color Text = new(200, 200, 200);
    public static readonly Color Dim = new(110, 110, 110);
    public static readonly Color Faint = new(55, 55, 60);
    public static readonly Color Accent = new(215, 175, 0);
    public static readonly Color Good = new(80, 200, 80);
    public static readonly Color Warn = new(220, 200, 60);
    public static readonly Color Bad = new(220, 60, 60);
    public static readonly Color Ally = new(0, 175, 255);
    public static readonly Color Enemy = new(255, 40, 40);
    public static readonly Color Picked = Ally;

    public static readonly Scheme Scheme = SchemeOf(Text);
    public static readonly Scheme PanelScheme = SchemeOf(new Color(128, 128, 128));
    public static readonly Scheme FocusedPanelScheme = SchemeOf(Accent);

    private static readonly Dictionary<string, TerrainStyle> Terrains = new()
    {
        ["plains"] = new(new Color(56, 92, 40), new Color(96, 142, 62), [" ", " ", " ", "'", " ", " ", " ", ","]),
        ["road"] = new(new Color(128, 104, 68), new Color(84, 64, 40), ["·"]),
        ["forest"] = new(new Color(26, 64, 30), new Color(40, 150, 60), ["♣", "♠", "♣"]),
        ["hills"] = new(new Color(104, 100, 48), new Color(176, 164, 88), ["∩"]),
        ["mountains"] = new(new Color(86, 82, 80), new Color(228, 228, 228), ["▲", "▲", "^"]),
        ["water"] = new(new Color(24, 58, 118), new Color(92, 142, 212), ["≈", "~", "≈"])
    };

    private static readonly TerrainStyle UnknownTerrain = new(new Color(0, 0, 0), Bad, ["?"]);

    public static IEnumerable<string> TerrainIds => Terrains.Keys;

    public static Color TerrainBackground(Terrain terrain) => GetTerrain(terrain.Id).Background;

    public static Attribute TerrainAttribute(string terrainId) =>
        new(GetTerrain(terrainId).Foreground, GetTerrain(terrainId).Background);

    public static string TerrainGlyph(Terrain terrain, Position position)
    {
        var glyphs = GetTerrain(terrain.Id).Glyphs;
        var hash = (position.X * 73856093) ^ (position.Y * 19349663);
        return glyphs[(hash & int.MaxValue) % glyphs.Length];
    }

    public static string LegendGlyph(string terrainId) => GetTerrain(terrainId).Glyphs.First(g => g != " ");

    public static Color HpColor(int hp, int maxHp)
    {
        if (hp <= 0)
            return Dim;

        var ratio = (double)hp / maxHp;
        return ratio > 0.6 ? Good : ratio > 0.3 ? Warn : Bad;
    }

    private static Scheme SchemeOf(Color foreground)
    {
        var normal = new Attribute(foreground, Background);
        return new Scheme(normal) { Focus = normal, Active = normal, HotNormal = normal, HotFocus = normal };
    }

    private static TerrainStyle GetTerrain(string id) => Terrains.GetValueOrDefault(id, UnknownTerrain);

    private sealed record TerrainStyle(Color Background, Color Foreground, string[] Glyphs);
}
