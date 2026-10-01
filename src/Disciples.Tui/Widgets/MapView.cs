using Disciples.Core.Cities;
using Disciples.Core.Map;
using Disciples.Core.Session;
using Terminal.Gui.Drawing;

namespace Disciples.Tui.Widgets;

public sealed class MapView : Canvas
{
    private const int TileWidth = 2;

    public const string RodGlyph = "┃";

    public static readonly Color LeaderColor = new(255, 255, 255);
    public static readonly (Color Foreground, Color Background) CapitalColors = (new Color(255, 215, 0), new Color(128, 28, 28));
    public static readonly (Color Foreground, Color Background) CityColors = (new Color(238, 238, 238), new Color(72, 48, 96));
    public static readonly (Color Foreground, Color Background) HostileCityColors = (new Color(255, 120, 120), new Color(60, 20, 20));

    private readonly GameSession _session;
    private readonly Func<Position?> _cursor;
    private readonly Func<Route> _route;
    private readonly IReadOnlySet<Position> _enemyTrail;

    /// <param name="enemyTrail">Tiles enemy leaders walked through on their last turn.</param>
    public MapView(GameSession session, Func<Position?> cursor, Func<Route> route, IReadOnlySet<Position> enemyTrail) : base(title: session.Map.Name)
    {
        _session = session;
        _cursor = cursor;
        _route = route;
        _enemyTrail = enemyTrail;
    }

    protected override void Draw()
    {
        var map = _session.Map;
        var viewWidth = Math.Min(map.Width, Viewport.Width / TileWidth);
        var viewHeight = Math.Min(map.Height, Viewport.Height);
        var cursor = _cursor();
        var center = cursor ?? _session.Party.Position;
        var left = Math.Clamp(center.X - viewWidth / 2, 0, map.Width - viewWidth);
        var top = Math.Clamp(center.Y - viewHeight / 2, 0, map.Height - viewHeight);

        for (var y = 0; y < viewHeight; y++)
        for (var x = 0; x < viewWidth; x++)
            DrawTile(x * TileWidth, y, new Position(left + x, top + y));

        var route = _route();
        var reachable = route.Reachable(_session.Party.MovementPoints);
        for (var i = 0; i < route.Steps.Count; i++)
        {
            var step = route.Steps[i];
            if (IsPlain(step))
                this.Text((step.X - left) * TileWidth + 1, step.Y - top, i == route.Steps.Count - 1 ? "×" : "·", i < reachable ? Palette.Good : Palette.Warn,
                    BackgroundAt(step), TextStyle.Bold);
        }

        if (cursor is { } c)
            this.Text((c.X - left) * TileWidth + 1, c.Y - top, "◂", LeaderColor, BackgroundAt(c), TextStyle.Bold);
    }

    private Color BackgroundAt(Position position) =>
        _session.Fog.IsExplored(position) ? LandBackground(position) : Palette.Background;

    private Color LandBackground(Position position)
    {
        var background = Palette.TerrainBackground(_session.Map.TerrainAt(position));
        return _session.Territory.OwnerAt(position) switch
        {
            Owner.Player => Palette.Tint(background, Palette.Ally),
            Owner.Enemy => Palette.Tint(background, Palette.Enemy),
            _ => background
        };
    }

    private bool IsPlain(Position position) =>
        !_session.Fog.IsExplored(position) || _session.Map.CityAt(position) == null && _session.Map.NeutralAt(position) == null
        && _session.Map.EnemyAt(position) == null && _session.Map.SiteAt(position) == null && _session.Map.RodAt(position) == null;

    private void DrawTile(int x, int y, Position position)
    {
        if (!_session.Fog.IsExplored(position))
        {
            this.Text(x, y, "  ", Palette.Faint, Palette.Background);
            return;
        }

        var terrain = _session.Map.TerrainAt(position);
        var city = _session.Map.CityAt(position);
        var cityColors = ColorsOf(city);

        if (_session.PartyAt(position) is { } party)
            this.Text(x, y, "@ ", party == _session.Party ? LeaderColor : Palette.Ally, city == null ? LandBackground(position) : cityColors.Background, TextStyle.Bold);
        else if (_session.Map.EnemyAt(position) != null)
            this.Text(x, y, "& ", Palette.Enemy, city == null ? LandBackground(position) : cityColors.Background, TextStyle.Bold);
        else if (city != null)
            this.Text(x, y, city.IsCapital ? "◆ " : "■ ", cityColors.Foreground, cityColors.Background, TextStyle.Bold);
        else if (_session.Map.NeutralAt(position) != null)
            this.Text(x, y, "† ", Palette.Enemy, LandBackground(position), TextStyle.Bold);
        else if (_session.Map.SiteAt(position) is { } site)
            this.Text(x, y, SiteGlyph(site.Kind) + " ", SiteColor(site), LandBackground(position), TextStyle.Bold);
        else if (_session.Map.RodAt(position) is { } rod)
            this.Text(x, y, RodGlyph + " ", rod.Owner == Owner.Player ? Palette.Ally : Palette.Enemy, LandBackground(position), TextStyle.Bold);
        else if (_enemyTrail.Contains(position))
            this.Text(x, y, "· ", Palette.Enemy, LandBackground(position));
        else
        {
            var attribute = Palette.TerrainAttribute(terrain.Id);
            this.Text(x, y, Palette.TerrainGlyph(terrain, position) + " ", attribute.Foreground, LandBackground(position));
        }
    }

    public static string SiteGlyph(SiteKind kind) => kind switch
    {
        SiteKind.Treasure => "$",
        SiteKind.Mine => "¤",
        SiteKind.ManaSource => "*",
        SiteKind.Merchant => "§",
        SiteKind.Trainer => "♦",
        _ => "▲"
    };

    public static Color SiteColor(Site site)
    {
        if (site.IsResource && site.Owner == Owner.Player)
            return Palette.Ally;

        return site.Kind == SiteKind.ManaSource ? ManaText.ColorOf(ManaText.Dominant(site.Mana)) : Palette.Accent;
    }

    private static (Color Foreground, Color Background) ColorsOf(City? city)
    {
        if (city is { IsPlayerOwned: false })
            return HostileCityColors;

        return city?.IsCapital == true ? CapitalColors : CityColors;
    }
}
