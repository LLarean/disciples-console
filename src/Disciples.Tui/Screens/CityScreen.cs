using Disciples.Core.Cities;
using Disciples.Core.Session;
using Disciples.Core.Squads;
using Disciples.Tui.Widgets;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;

namespace Disciples.Tui.Screens;

public sealed class CityScreen : Screen
{
    private enum Focus
    {
        Recruits,
        Party,
        Garrison
    }

    private const int RecruitsWidth = 30;

    private readonly GameSession _session;
    private readonly City _city;
    private readonly Canvas _recruits;
    private readonly SquadView _partyView;
    private readonly SquadView _garrisonView;

    private Focus _focus = Focus.Recruits;
    private int _recruitIndex;
    private SquadSlot _partyCursor = new(SquadLine.Front, 1);
    private SquadSlot _garrisonCursor = new(SquadLine.Front, 1);
    private (Squad Squad, SquadSlot Slot)? _picked;
    private (string Text, Color Color) _message = ("", Palette.Text);

    public CityScreen(GameSession session, City city)
    {
        _session = session;
        _city = city;

        var header = new Canvas(DrawHeader) { X = 0, Y = 0, Width = Dim.Fill(), Height = 1 };
        _recruits = new Canvas(DrawRecruits, "Recruits") { X = 0, Y = 1, Width = RecruitsWidth, Height = SquadView.PanelHeight };
        _partyView = new SquadView("Party") { X = Pos.Right(_recruits) + 1, Y = 1 };
        _garrisonView = new SquadView("Garrison") { X = Pos.Right(_partyView) + 1, Y = 1 };
        var message = new Canvas(c => c.Text(1, 0, _message.Text, _message.Color)) { X = 0, Y = Pos.Bottom(_recruits), Width = Dim.Fill(), Height = 1 };
        var hints = new HintBar(Hints);
        Add(header, _recruits, _partyView, _garrisonView, message, hints);
    }

    private bool IsPartyHere => _session.Party.Position == _city.Position;
    private Squad FocusedSquad => _focus == Focus.Party ? _session.Party.Squad : _city.Garrison;
    private SquadSlot CurrentCursor => _focus == Focus.Party ? _partyCursor : _garrisonCursor;

    public override bool HandleKey(Key key)
    {
        _message = ("", Palette.Text);

        if (key == Key.Esc)
            Back();
        else if (key == Key.B && _city.IsCapital)
            Shell.Push(new BuildingsScreen(_session, _city));
        else if (key == Key.Tab)
            CycleFocus();
        else if (key == Key.CursorUp || key == Key.CursorDown)
            MoveVertical(key == Key.CursorUp ? -1 : 1);
        else if ((key == Key.CursorLeft || key == Key.CursorRight) && _focus != Focus.Recruits)
            SetCursor(CurrentCursor.WithLine(key == Key.CursorLeft ? SquadLine.Back : SquadLine.Front));
        else if (key == Key.Enter)
            Activate();
        else if (key == Key.D || key == Key.Delete)
            Dismiss();
        else
            return false;

        return true;
    }

    protected override void UpdateViews()
    {
        var party = _session.Party.Squad;
        _partyView.Squad = IsPartyHere ? party : null;
        _partyView.Title = IsPartyHere ? $"Party {party.UsedSlots}/{party.Capacity}" : "Party";
        _garrisonView.Squad = _city.Garrison;
        _garrisonView.Title = $"Garrison {_city.Garrison.UsedSlots}/{_city.Garrison.Capacity}";

        _recruits.IsFocused = _focus == Focus.Recruits;
        UpdateSquadView(_partyView, party, Focus.Party, _partyCursor);
        UpdateSquadView(_garrisonView, _city.Garrison, Focus.Garrison, _garrisonCursor);
    }

    private void UpdateSquadView(SquadView view, Squad squad, Focus focus, SquadSlot cursor)
    {
        var isFocused = _focus == focus;
        view.IsFocused = isFocused;
        view.Selection = isFocused ? cursor : null;
        view.Picked = _picked is { } picked && picked.Squad == squad ? squad.UnitAt(picked.Slot) : null;
    }

    private void Back()
    {
        if (_picked != null)
            _picked = null;
        else
            Shell.Pop();
    }

    private void CycleFocus()
    {
        do
            _focus = (Focus)(((int)_focus + 1) % 3);
        while (_focus == Focus.Party && !IsPartyHere);
    }

    private void SetCursor(SquadSlot slot)
    {
        if (_focus == Focus.Party)
            _partyCursor = slot;
        else if (_focus == Focus.Garrison)
            _garrisonCursor = slot;
    }

    private void MoveVertical(int delta)
    {
        if (_focus == Focus.Recruits)
        {
            var count = _city.Recruits.Count;
            if (count > 0)
                _recruitIndex = (_recruitIndex + delta + count) % count;
            return;
        }

        var cursor = CurrentCursor;
        SetCursor(new SquadSlot(cursor.Line, (cursor.Column + delta + SquadSlot.Columns) % SquadSlot.Columns));
    }

    private void Activate()
    {
        if (_focus == Focus.Recruits)
            Hire();
        else
            PickOrPlace();
    }

    private void Hire()
    {
        if (_city.Recruits.Count == 0)
            return;

        var recruit = _city.Recruits[_recruitIndex];
        _message = _session.Hire(_city, recruit) switch
        {
            HireResult.HiredToParty => ($"{recruit.Name} joins the party.", Palette.Good),
            HireResult.HiredToGarrison => ($"{recruit.Name} joins the garrison.", Palette.Good),
            HireResult.NotEnoughGold => ($"Not enough gold for {recruit.Name}.", Palette.Bad),
            _ => ("No room in the party or the garrison.", Palette.Bad)
        };
    }

    private void PickOrPlace()
    {
        var squad = FocusedSquad;
        var cursor = CurrentCursor;

        if (_picked is not { } picked)
        {
            if (squad.UnitAt(cursor) is { } unit)
                _picked = (squad, squad.SlotOf(unit));
            return;
        }

        _picked = null;
        if (!SquadTransfer.Move(picked.Squad, picked.Slot, squad, cursor))
            _message = ("Can't move there: no room, or the leader would leave the party.", Palette.Bad);
    }

    private void Dismiss()
    {
        if (_focus == Focus.Recruits || FocusedSquad.UnitAt(CurrentCursor) is not { } unit)
            return;

        _picked = null;
        _message = _session.Dismiss(FocusedSquad, unit)
            ? ($"{unit.Name} dismissed.", Palette.Dim)
            : ("The leader can't be dismissed.", Palette.Bad);
    }

    private void DrawHeader(Canvas canvas)
    {
        var x = canvas.Text(1, 0, _city.Name, Palette.Accent, style: TextStyle.Bold);
        x = canvas.Text(x + 2, 0, _city.IsCapital ? "Capital" : "City", _city.IsCapital ? Palette.Accent : Palette.Dim);
        x = canvas.Text(x + 2, 0, "income ", Palette.Dim);
        x = canvas.Text(x, 0, _city.Income.ToString(), Palette.Text);
        x = canvas.Text(x + 3, 0, "gold ", Palette.Dim);
        x = canvas.Text(x, 0, _session.Gold.ToString(), Palette.Accent);
        x = canvas.Text(x + 3, 0, "turn ", Palette.Dim);
        canvas.Text(x, 0, _session.Turn.ToString(), Palette.Text);
    }

    private void DrawRecruits(Canvas canvas)
    {
        var width = canvas.Viewport.Width;
        var recruits = _city.Recruits;
        if (recruits.Count == 0)
        {
            canvas.Text(1, 0, "Nobody to hire here.", Palette.Dim);
            return;
        }

        for (var i = 0; i < recruits.Count; i++)
        {
            var recruit = recruits[i];
            var selected = i == _recruitIndex && _focus == Focus.Recruits;
            canvas.Text(0, i, selected ? "›" : " ", Palette.Accent);
            canvas.Text(2, i, recruit.Name, selected ? Palette.Accent : Palette.Text, style: selected ? TextStyle.Bold : TextStyle.None);
            canvas.Text(width - 12, i, ViewDrawing.AttackLabel(recruit), Palette.Dim);
            var cost = recruit.Cost.ToString();
            canvas.Text(width - cost.Length, i, cost, recruit.Cost <= _session.Gold ? Palette.Accent : Palette.Bad);
        }

        var y = recruits.Count;
        canvas.Text(0, y, new string('─', width), Palette.Faint);
        var stats = recruits[_recruitIndex];
        Stat(canvas, 0, y + 1, "HP", stats.MaxHp);
        Stat(canvas, 10, y + 1, "Armor", stats.Armor);
        Stat(canvas, 0, y + 2, "Power", stats.Power);
        Stat(canvas, 10, y + 2, "Accuracy", stats.Accuracy);
        Stat(canvas, 0, y + 3, "Initiative", stats.Initiative);
    }

    private static void Stat(Canvas canvas, int x, int y, string name, int value)
    {
        x = canvas.Text(x, y, name + " ", Palette.Dim);
        canvas.Text(x, y, value.ToString(), Palette.Text);
    }

    private IEnumerable<(string, string)> Hints()
    {
        yield return ("Tab", "switch panel");
        yield return ("←↑→↓", "select");
        yield return ("Enter", _focus == Focus.Recruits ? "hire" : _picked == null ? "pick unit" : "place unit");
        yield return ("D", "dismiss");
        if (_city.IsCapital)
            yield return ("B", "buildings");
        yield return ("Esc", _picked == null ? "leave" : "cancel");
    }
}
