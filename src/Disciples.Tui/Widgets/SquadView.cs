using Disciples.Core.Squads;
using Disciples.Core.Units;
using Terminal.Gui.Drawing;

namespace Disciples.Tui.Widgets;

/// <summary>Draws a 2×3 squad; each row is one column of the squad, front line faces the opponent.</summary>
public sealed class SquadView : Canvas
{
    private const int CardWidth = 16;
    private const int CardHeight = 5;
    private const int BarLength = 4;

    public const int PanelWidth = CardWidth * 2 + 1 + 2;
    public const int PanelHeight = CardHeight * SquadSlot.Columns + 2;

    public SquadView(string title, bool facesLeft = false) : base(title: title)
    {
        FacesLeft = facesLeft;
        Width = PanelWidth;
        Height = PanelHeight;
    }

    public Squad? Squad { get; set; }
    public bool FacesLeft { get; }
    public SquadSlot? Selection { get; set; }
    public Func<Unit, Color?> Highlight { get; set; } = _ => null;

    protected override void Draw()
    {
        if (Squad == null)
        {
            this.Text(1, 0, "No party here.", Palette.Dim);
            return;
        }

        for (var column = 0; column < SquadSlot.Columns; column++)
            DrawColumn(Squad, column, column * CardHeight);
    }

    private void DrawColumn(Squad squad, int column, int y)
    {
        var front = new SquadSlot(SquadLine.Front, column);
        var back = new SquadSlot(SquadLine.Back, column);

        if (squad.UnitAt(front) is { IsLarge: true } large)
        {
            DrawCard(0, y, CardWidth * 2 + 1, large, IsCursorOn(front) || IsCursorOn(back));
            return;
        }

        var (left, right) = FacesLeft ? (front, back) : (back, front);
        DrawCell(squad, 0, y, left);
        DrawCell(squad, CardWidth + 1, y, right);
    }

    private bool IsCursorOn(SquadSlot slot) => Selection.HasValue && Selection.Value == slot;

    private void DrawCell(Squad squad, int x, int y, SquadSlot slot)
    {
        if (squad.UnitAt(slot) is { } unit)
        {
            DrawCard(x, y, CardWidth, unit, IsCursorOn(slot));
            return;
        }

        var isCursor = IsCursorOn(slot);
        this.Box(x, y, CardWidth, CardHeight, isCursor ? Palette.Accent : Palette.Faint, isCursor);
        this.Text(x + 2, y + 1, slot.Line.ToString().ToLower(), Palette.Faint);
    }

    private void DrawCard(int x, int y, int width, Unit unit, bool isCursor)
    {
        var color = isCursor ? Palette.Accent : Highlight(unit) ?? (unit.IsAlive ? Palette.Dim : Palette.Faint);
        this.Box(x, y, width, CardHeight, color, isCursor);

        var name = ViewDrawing.Fit(unit.Name, width - 4);
        if (!unit.IsAlive)
        {
            this.Text(x + 2, y + 1, name, Palette.Dim, style: TextStyle.Strikethrough);
            this.Text(x + 2, y + 2, "dead", Palette.Dim);
            return;
        }

        this.Text(x + 2, y + 1, name, unit.IsLeader ? Palette.Accent : Palette.Text, style: TextStyle.Bold);
        var barEnd = this.HpBar(x + 2, y + 2, unit, BarLength);
        this.Hp(barEnd + 1, y + 2, unit);
        var labelEnd = this.Text(x + 2, y + 3, ViewDrawing.AttackLabel(unit.Definition), Palette.Dim);
        this.Text(labelEnd + 1, y + 3, unit.Definition.Power.ToString(), Palette.Text);
    }
}
