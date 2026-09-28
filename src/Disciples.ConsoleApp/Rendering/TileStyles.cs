using Disciples.Core.Map;
using Spectre.Console;

namespace Disciples.ConsoleApp.Rendering;

public static class TileStyles
{
    private static readonly Dictionary<string, TerrainStyle> Terrains = new()
    {
        ["plains"] = new(new Color(56, 92, 40), new Color(96, 142, 62), [" ", " ", " ", "'", " ", " ", " ", ","]),
        ["road"] = new(new Color(128, 104, 68), new Color(84, 64, 40), ["·"]),
        ["forest"] = new(new Color(26, 64, 30), new Color(40, 150, 60), ["♣", "♠", "♣"]),
        ["hills"] = new(new Color(104, 100, 48), new Color(176, 164, 88), ["∩"]),
        ["mountains"] = new(new Color(86, 82, 80), new Color(228, 228, 228), ["▲", "▲", "^"]),
        ["water"] = new(new Color(24, 58, 118), new Color(92, 142, 212), ["≈", "~", "≈"])
    };

    private static readonly TerrainStyle Unknown = new(Color.Black, Color.Red, ["?"]);

    public static Color Background(Terrain terrain) => Get(terrain).Background;

    public static Style Style(Terrain terrain) => new(Get(terrain).Foreground, Get(terrain).Background);

    public static string Glyph(Terrain terrain, Position position)
    {
        var glyphs = Get(terrain).Glyphs;
        var hash = (position.X * 73856093) ^ (position.Y * 19349663);
        return glyphs[(hash & int.MaxValue) % glyphs.Length];
    }

    public static string LegendGlyph(string terrainId) => Terrains[terrainId].Glyphs.First(g => g != " ");

    public static Style LegendStyle(string terrainId) => new(Terrains[terrainId].Foreground, Terrains[terrainId].Background);

    private static TerrainStyle Get(Terrain terrain) => Terrains.GetValueOrDefault(terrain.Id, Unknown);

    private sealed record TerrainStyle(Color Background, Color Foreground, string[] Glyphs);
}
