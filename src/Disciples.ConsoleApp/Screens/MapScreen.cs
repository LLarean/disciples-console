using Disciples.ConsoleApp.Rendering;
using Disciples.Core.Map;
using Disciples.Core.Session;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Disciples.ConsoleApp.Screens;

public sealed class MapScreen(GameSession session) : IScreen
{
    private const int SideWidth = 34;
    private const int LogSize = 6;
    private const int TileWidth = 2;

    private static readonly Color Accent = Color.Gold3;
    private static readonly Style LeaderStyle = new(Color.White, decoration: Decoration.Bold);
    private static readonly Style CapitalStyle = new(Color.Gold1, new Color(128, 28, 28), Decoration.Bold);
    private static readonly Style CityStyle = new(Color.Grey93, new Color(72, 48, 96), Decoration.Bold);
    private static readonly string[] LegendTerrains = ["plains", "road", "forest", "hills", "mountains", "water"];

    private readonly List<string> _log = ["Welcome, Knight. Arrows or numpad to move."];

    public IRenderable Render(int width, int height)
    {
        var viewWidth = Math.Min(session.Map.Width, (width - SideWidth - 6) / TileWidth);
        var viewHeight = Math.Min(session.Map.Height, height - 4);

        var grid = new Grid()
            .AddColumn(new GridColumn().Padding(0, 0, 1, 0))
            .AddColumn(new GridColumn().Width(SideWidth))
            .AddRow(RenderMap(viewWidth, viewHeight), RenderSide());

        return new Rows(grid, RenderHints());
    }

    public bool HandleKey(ConsoleKeyInfo key)
    {
        if (key.Key is ConsoleKey.Escape or ConsoleKey.Q)
            return false;

        if (key.Key == ConsoleKey.E)
            EndTurn();
        else if (KeyToDirection(key.Key) is { } direction)
            Move(direction);

        return true;
    }

    private void Move(Direction direction)
    {
        var target = session.Leader.Position.Step(direction);

        switch (session.TryMove(direction))
        {
            case MoveResult.Moved when session.Map.CityAt(target) is { } city:
                Log($"Arrived at {city.Name}. City screen comes in M3.");
                break;
            case MoveResult.Moved when session.Leader.MovementPoints == 0:
                Log("Out of movement. Press E to end turn.");
                break;
            case MoveResult.OutOfBounds:
                Log("The edge of the world.");
                break;
            case MoveResult.Impassable:
                Log($"{session.Map.TerrainAt(target).Name} is impassable.");
                break;
            case MoveResult.NotEnoughMovement:
                var terrain = session.Map.TerrainAt(target);
                Log($"{terrain.Name} costs {terrain.MoveCost}, only {session.Leader.MovementPoints} left.");
                break;
        }
    }

    private void EndTurn()
    {
        session.EndTurn();
        Log($"Turn {session.Turn}. Movement restored.");
    }

    private void Log(string message)
    {
        _log.Add(message);
        if (_log.Count > LogSize)
            _log.RemoveAt(0);
    }

    private Panel RenderMap(int viewWidth, int viewHeight)
    {
        var map = session.Map;
        var leader = session.Leader.Position;
        var left = Math.Clamp(leader.X - viewWidth / 2, 0, map.Width - viewWidth);
        var top = Math.Clamp(leader.Y - viewHeight / 2, 0, map.Height - viewHeight);

        var paragraph = new Paragraph();
        for (var y = top; y < top + viewHeight; y++)
        {
            for (var x = left; x < left + viewWidth; x++)
                AppendTile(paragraph, new Position(x, y));

            if (y < top + viewHeight - 1)
                paragraph.Append("\n");
        }

        return new Panel(paragraph)
            .Header($" {Markup.Escape(map.Name)} ")
            .Border(BoxBorder.Rounded)
            .BorderColor(Accent);
    }

    private void AppendTile(Paragraph paragraph, Position position)
    {
        var terrain = session.Map.TerrainAt(position);
        var city = session.Map.CityAt(position);
        var cityStyle = city?.IsCapital == true ? CapitalStyle : CityStyle;

        if (position == session.Leader.Position)
        {
            var background = city == null ? TileStyles.Background(terrain) : cityStyle.Background;
            paragraph.Append("@ ", LeaderStyle.Background(background));
        }
        else if (city != null)
        {
            paragraph.Append(city.IsCapital ? "◆ " : "■ ", cityStyle);
        }
        else
        {
            paragraph.Append(TileStyles.Glyph(terrain, position) + " ", TileStyles.Style(terrain));
        }
    }

    private Panel RenderSide()
    {
        var leader = session.Leader;
        var rows = new List<IRenderable>
        {
            new Markup($"[bold gold1]{Markup.Escape(leader.Name)}[/]  [grey]turn[/] [bold]{session.Turn}[/]"),
            MovementBar(leader.MovementPoints, leader.MaxMovementPoints),
            new Text(""),
            new Markup($"[grey]Location[/]  {DescribeLocation()}"),
            new Text(""),
            RenderLegend(),
            new Text(""),
            new Rule("[grey]Log[/]").LeftJustified().RuleStyle(new Style(Color.Grey35))
        };
        rows.AddRange(_log.Select((m, i) => new Text(m, new Style(i == _log.Count - 1 ? Color.Grey93 : Color.Grey50))));

        return new Panel(new Rows(rows))
            .Header(" Leader ")
            .Border(BoxBorder.Rounded)
            .BorderColor(Accent)
            .Expand();
    }

    private static Markup MovementBar(int current, int max)
    {
        const int length = 14;
        var filled = max == 0 ? 0 : (int)Math.Round((double)current / max * length);
        var color = current == 0 ? "red" : "green3";
        return new Markup($"[grey]Move[/] [{color}]{new string('█', filled)}[/][grey23]{new string('█', length - filled)}[/] {current}/{max}");
    }

    private string DescribeLocation()
    {
        var position = session.Leader.Position;
        if (session.Map.CityAt(position) is { } city)
            return $"[bold]{Markup.Escape(city.Name)}[/]{(city.IsCapital ? " [gold1](capital)[/]" : "")}";

        return $"{Markup.Escape(session.Map.TerrainAt(position).Name)} [grey]{position}[/]";
    }

    private static Grid RenderLegend()
    {
        var items = LegendTerrains
            .Select(id => LegendItem(TileStyles.LegendGlyph(id), TileStyles.LegendStyle(id), char.ToUpper(id[0]) + id[1..]))
            .Append(LegendItem("◆", CapitalStyle, "Capital"))
            .Append(LegendItem("■", CityStyle, "City"))
            .ToList();

        var grid = new Grid().AddColumn().AddColumn();
        for (var i = 0; i < items.Count; i += 2)
            grid.AddRow(items[i], items[i + 1]);

        return grid;
    }

    private static Paragraph LegendItem(string glyph, Style style, string name)
    {
        return new Paragraph().Append(glyph + " ", style).Append(" " + name);
    }

    private static Markup RenderHints()
    {
        return new Markup("[gold1]←↑→↓[/] [grey]/ numpad / Home PgUp End PgDn — move[/]   [gold1]E[/] [grey]end turn[/]   [gold1]Esc[/] [grey]quit[/]");
    }

    private static Direction? KeyToDirection(ConsoleKey key)
    {
        return key switch
        {
            ConsoleKey.UpArrow or ConsoleKey.NumPad8 => Direction.North,
            ConsoleKey.PageUp or ConsoleKey.NumPad9 => Direction.NorthEast,
            ConsoleKey.RightArrow or ConsoleKey.NumPad6 => Direction.East,
            ConsoleKey.PageDown or ConsoleKey.NumPad3 => Direction.SouthEast,
            ConsoleKey.DownArrow or ConsoleKey.NumPad2 => Direction.South,
            ConsoleKey.End or ConsoleKey.NumPad1 => Direction.SouthWest,
            ConsoleKey.LeftArrow or ConsoleKey.NumPad4 => Direction.West,
            ConsoleKey.Home or ConsoleKey.NumPad7 => Direction.NorthWest,
            _ => null
        };
    }
}
