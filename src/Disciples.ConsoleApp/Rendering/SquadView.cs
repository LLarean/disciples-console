using Disciples.Core.Squads;
using Disciples.Core.Units;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Disciples.ConsoleApp.Rendering;

/// <summary>Draws a 2×3 squad; each row is one column of the squad, front line faces the opponent.</summary>
public sealed class SquadView(Squad squad)
{
    private const int CardWidth = 16;
    private const int BarLength = 4;

    public string Title { get; init; } = "";
    public bool FacesLeft { get; init; }
    public bool IsFocused { get; init; }
    public SquadSlot? Cursor { get; init; }
    public Func<Unit, Color?> Highlight { get; init; } = _ => null;

    public Panel Render()
    {
        var rows = Enumerable.Range(0, SquadSlot.Columns).Select(RenderRow);
        return new Panel(new Rows(rows))
            .Header($" {Title} ")
            .Border(IsFocused ? BoxBorder.Double : BoxBorder.Rounded)
            .BorderColor(IsFocused ? Color.Gold1 : Color.Grey50);
    }

    private IRenderable RenderRow(int column)
    {
        var front = new SquadSlot(SquadLine.Front, column);
        var back = new SquadSlot(SquadLine.Back, column);

        if (squad.UnitAt(front) is { IsLarge: true } large)
            return Card(large, IsCursorOn(front) || IsCursorOn(back), CardWidth * 2 + 1);

        var (left, right) = FacesLeft ? (front, back) : (back, front);
        return new Grid()
            .AddColumn(new GridColumn().NoWrap().Padding(0, 0, 1, 0))
            .AddColumn(new GridColumn().NoWrap().Padding(0, 0, 0, 0))
            .AddRow(Cell(left), Cell(right));
    }

    private bool IsCursorOn(SquadSlot slot) => Cursor.HasValue && Cursor.Value == slot;

    private Panel Cell(SquadSlot slot) =>
        squad.UnitAt(slot) is { } unit ? Card(unit, IsCursorOn(slot), CardWidth) : Empty(slot);

    private Panel Empty(SquadSlot slot)
    {
        var text = new Markup($"[grey23]{slot.Line.ToString().ToLower()}[/]\n\n");
        return Frame(text, IsCursorOn(slot) ? Color.Gold1 : Color.Grey23, IsCursorOn(slot), CardWidth);
    }

    private Panel Card(Unit unit, bool isCursor, int width)
    {
        var name = Markup.Escape(Truncate(unit.Name, width - 4));
        var definition = unit.Definition;
        string content;

        if (unit.IsAlive)
        {
            var nameStyle = unit.IsLeader ? "bold gold1" : "bold";
            content = $"[{nameStyle}]{name}[/]\n" +
                      $"{UnitStyles.HpBar(unit, BarLength)} {UnitStyles.HpMarkup(unit)}\n" +
                      $"[grey]{UnitStyles.AttackLabel(definition)}[/] {definition.Power}";
        }
        else
        {
            content = $"[grey35 strikethrough]{name}[/]\n[grey35]dead[/]\n";
        }

        var color = isCursor ? Color.Gold1 : Highlight(unit) ?? (unit.IsAlive ? Color.Grey50 : Color.Grey23);
        return Frame(new Markup(content), color, isCursor, width);
    }

    private static Panel Frame(IRenderable content, Color color, bool isCursor, int width) =>
        new Panel(content) { Width = width }
            .Border(isCursor ? BoxBorder.Heavy : BoxBorder.Rounded)
            .BorderColor(color)
            .Padding(1, 0);

    private static string Truncate(string text, int length) =>
        text.Length <= length ? text : text[..(length - 1)] + "…";
}
