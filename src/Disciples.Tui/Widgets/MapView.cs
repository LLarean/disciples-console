using Disciples.Core.Cities;
using Disciples.Core.Map;
using Disciples.Core.Session;
using Terminal.Gui.Drawing;

namespace Disciples.Tui.Widgets;

public sealed class MapView : Canvas
{
    private const int TileWidth = 2;

    public static readonly Color LeaderColor = new(255, 255, 255);
    public static readonly (Color Foreground, Color Background) CapitalColors = (new Color(255, 215, 0), new Color(128, 28, 28));
    public static readonly (Color Foreground, Color Background) CityColors = (new Color(238, 238, 238), new Color(72, 48, 96));
    public static readonly (Color Foreground, Color Background) HostileCityColors = (new Color(255, 120, 120), new Color(60, 20, 20));

    private readonly GameSession _session;

    public MapView(GameSession session) : base(title: session.Map.Name)
    {
        _session = session;
    }

    protected override void Draw()
    {
        var map = _session.Map;
        var viewWidth = Math.Min(map.Width, Viewport.Width / TileWidth);
        var viewHeight = Math.Min(map.Height, Viewport.Height);
        var leader = _session.Party.Position;
        var left = Math.Clamp(leader.X - viewWidth / 2, 0, map.Width - viewWidth);
        var top = Math.Clamp(leader.Y - viewHeight / 2, 0, map.Height - viewHeight);

        for (var y = 0; y < viewHeight; y++)
        for (var x = 0; x < viewWidth; x++)
            DrawTile(x * TileWidth, y, new Position(left + x, top + y));
    }

    private void DrawTile(int x, int y, Position position)
    {
        var terrain = _session.Map.TerrainAt(position);
        var city = _session.Map.CityAt(position);
        var cityColors = ColorsOf(city);

        if (position == _session.Party.Position)
            this.Text(x, y, "@ ", LeaderColor, city == null ? Palette.TerrainBackground(terrain) : cityColors.Background, TextStyle.Bold);
        else if (_session.Map.EnemyAt(position) != null)
            this.Text(x, y, "& ", Palette.Enemy, city == null ? Palette.TerrainBackground(terrain) : cityColors.Background, TextStyle.Bold);
        else if (city != null)
            this.Text(x, y, city.IsCapital ? "◆ " : "■ ", cityColors.Foreground, cityColors.Background, TextStyle.Bold);
        else if (_session.Map.NeutralAt(position) != null)
            this.Text(x, y, "† ", Palette.Enemy, Palette.TerrainBackground(terrain), TextStyle.Bold);
        else
        {
            var attribute = Palette.TerrainAttribute(terrain.Id);
            this.Text(x, y, Palette.TerrainGlyph(terrain, position) + " ", attribute.Foreground, attribute.Background);
        }
    }

    private static (Color Foreground, Color Background) ColorsOf(City? city)
    {
        if (city is { IsPlayerOwned: false })
            return HostileCityColors;

        return city?.IsCapital == true ? CapitalColors : CityColors;
    }
}
