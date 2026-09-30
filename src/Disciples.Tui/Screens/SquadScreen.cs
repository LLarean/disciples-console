using Disciples.Core.Session;
using Disciples.Core.Squads;
using Disciples.Core.Units;
using Disciples.Tui.Widgets;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;

namespace Disciples.Tui.Screens;

/// <summary>Party overview: unit details, progression path and formation changes.</summary>
public sealed class SquadScreen : Screen
{
    private const int DetailsWidth = 40;

    private readonly GameSession _session;
    private readonly SquadView _squadView;

    private SquadSlot _cursor = new(SquadLine.Front, 1);
    private SquadSlot? _picked;
    private (string Text, Color Color) _message = ("", Palette.Text);

    public SquadScreen(GameSession session)
    {
        _session = session;

        var header = new Canvas(DrawHeader) { X = 0, Y = 0, Width = Dim.Fill(), Height = 1 };
        _squadView = new SquadView("Party") { X = 0, Y = 1, IsFocused = true };
        var details = new Canvas(DrawDetails, "Unit") { X = Pos.Right(_squadView) + 1, Y = 1, Width = DetailsWidth, Height = SquadView.PanelHeight };
        var message = new Canvas(c => c.Text(1, 0, _message.Text, _message.Color)) { X = 0, Y = Pos.Bottom(_squadView), Width = Dim.Fill(), Height = 1 };
        var hints = new HintBar(() => [("←↑→↓", "select"), ("Enter", _picked == null ? "pick" : "place"), ("S/Esc", "back")]);
        Add(header, _squadView, details, message, hints);
    }

    private Squad Squad => _session.Party.Squad;

    public override bool HandleKey(Key key)
    {
        _message = ("", Palette.Text);

        if (key == Key.Esc || key == Key.S)
            Back();
        else if (key == Key.CursorUp || key == Key.CursorDown)
            _cursor = new SquadSlot(_cursor.Line, (_cursor.Column + (key == Key.CursorUp ? -1 : 1) + SquadSlot.Columns) % SquadSlot.Columns);
        else if (key == Key.CursorLeft || key == Key.CursorRight)
            _cursor = _cursor.WithLine(key == Key.CursorLeft ? SquadLine.Back : SquadLine.Front);
        else if (key == Key.Enter)
            PickOrPlace();
        else
            return false;

        return true;
    }

    protected override void UpdateViews()
    {
        var picked = _picked is { } slot ? Squad.UnitAt(slot) : null;
        _squadView.Squad = Squad;
        _squadView.Title = $"Party {Squad.UsedSlots}/{Squad.Capacity}";
        _squadView.Selection = _cursor;
        _squadView.Highlight = unit => unit == picked ? Palette.Picked : null;
    }

    private void Back()
    {
        if (_picked != null)
            _picked = null;
        else
            Shell.Pop();
    }

    private void PickOrPlace()
    {
        if (_picked is not { } picked)
        {
            if (Squad.UnitAt(_cursor) is { } unit)
                _picked = Squad.SlotOf(unit);
            return;
        }

        _picked = null;
        if (!SquadTransfer.Move(Squad, picked, Squad, _cursor))
            _message = ("Can't move there.", Palette.Bad);
    }

    private void DrawHeader(Canvas canvas)
    {
        var x = canvas.Text(1, 0, _session.Party.Name, Palette.Accent, style: TextStyle.Bold);
        canvas.Text(x + 2, 0, "squad", Palette.Dim);
    }

    private void DrawDetails(Canvas canvas)
    {
        if (Squad.UnitAt(_cursor) is not { } unit)
        {
            canvas.Text(1, 0, "Empty slot.", Palette.Dim);
            return;
        }

        var definition = unit.Definition;
        var y = 0;
        canvas.Text(1, y++, unit.Name, unit.IsLeader ? Palette.Accent : Palette.Text, style: TextStyle.Bold);
        y++;
        Stat(canvas, y++, "Level", unit.Level.ToString());
        Stat(canvas, y++, "Experience", $"{unit.Experience}/{definition.ExperienceToLevel}");
        Stat(canvas, y++, "HP", $"{unit.Hp}/{unit.MaxHp}");
        Stat(canvas, y++, "Attack", $"{ViewDrawing.AttackLabel(definition)} {unit.Power}");
        Stat(canvas, y++, "Accuracy", $"{unit.Accuracy}%");
        Stat(canvas, y++, "Armor", unit.Armor.ToString());
        Stat(canvas, y++, "Initiative", unit.Initiative.ToString());
        Stat(canvas, y++, "Source", definition.Source.ToString());
        if (definition.Immunities.Count > 0)
            Stat(canvas, y++, "Immune", string.Join(", ", definition.Immunities));
        if (definition.Wards.Count > 0)
            Stat(canvas, y++, "Ward", string.Join(", ", definition.Wards));
        if (unit.IsLeader)
            Stat(canvas, y++, "Leadership", Squad.Capacity.ToString());

        y++;
        canvas.Section(y++, "Upgrade");
        DrawUpgrade(canvas, y, definition);
    }

    private void DrawUpgrade(Canvas canvas, int y, UnitDefinition definition)
    {
        if (definition.UpgradesTo is not { } next)
        {
            canvas.Text(1, y, "Levels up in place.", Palette.Dim);
            return;
        }

        var x = canvas.Text(1, y, "→ ", Palette.Dim);
        canvas.Text(x, y, _session.Content.Unit(next).Name, Palette.Text);
        if (definition.UpgradeBuilding is not { } building)
            return;

        var isBuilt = _session.Capital?.HasBuilt(building) == true;
        x = canvas.Text(1, y + 1, "needs ", Palette.Dim);
        x = canvas.Text(x, y + 1, _session.Content.BuildingName(building), Palette.Text);
        canvas.Text(x + 1, y + 1, isBuilt ? "(built)" : "(not built)", isBuilt ? Palette.Good : Palette.Bad);
    }

    private static void Stat(Canvas canvas, int y, string label, string value)
    {
        canvas.Text(1, y, label, Palette.Dim);
        canvas.Text(14, y, value, Palette.Text);
    }
}
