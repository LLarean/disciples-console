using Disciples.ConsoleApp.Rendering;
using Disciples.Core.Cities;
using Disciples.Core.Session;
using Disciples.Core.Squads;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Disciples.ConsoleApp.Screens;

public sealed class CityScreen(GameSession session, City city, ScreenStack screens) : IScreen
{
    private enum Focus
    {
        Recruits,
        Party,
        Garrison
    }

    private const int SideWidth = 30;

    private Focus _focus = Focus.Recruits;
    private int _recruitIndex;
    private int _buildingIndex;
    private bool _showBuildings;
    private SquadSlot _partyCursor = new(SquadLine.Front, 1);
    private SquadSlot _garrisonCursor = new(SquadLine.Front, 1);
    private (Squad Squad, SquadSlot Slot)? _picked;
    private string _message = "";

    private bool IsPartyHere => session.Party.Position == city.Position;

    public IRenderable Render(int width, int height)
    {
        IRenderable body = _showBuildings ? RenderBuildings() : RenderSquads();
        return new Rows(RenderHeader(), body, new Markup(_message), RenderHints());
    }

    public void HandleKey(ConsoleKeyInfo key)
    {
        _message = "";

        if (key.Key == ConsoleKey.Escape)
            Back();
        else if (key.Key == ConsoleKey.B && city.IsCapital)
            _showBuildings = !_showBuildings;
        else if (_showBuildings)
            HandleBuildingsKey(key.Key);
        else
            HandleSquadsKey(key.Key);
    }

    private void Back()
    {
        if (_picked != null)
            _picked = null;
        else if (_showBuildings)
            _showBuildings = false;
        else
            screens.Pop();
    }

    private void HandleSquadsKey(ConsoleKey key)
    {
        switch (key)
        {
            case ConsoleKey.Tab:
                CycleFocus();
                break;
            case ConsoleKey.UpArrow:
            case ConsoleKey.DownArrow:
                MoveVertical(key == ConsoleKey.UpArrow ? -1 : 1);
                break;
            case ConsoleKey.LeftArrow when _focus != Focus.Recruits:
            case ConsoleKey.RightArrow when _focus != Focus.Recruits:
                SetCursor(CurrentCursor.WithLine(key == ConsoleKey.LeftArrow ? SquadLine.Back : SquadLine.Front));
                break;
            case ConsoleKey.Enter when _focus == Focus.Recruits:
                Hire();
                break;
            case ConsoleKey.Enter:
                PickOrPlace();
                break;
            case ConsoleKey.D:
            case ConsoleKey.Delete:
                Dismiss();
                break;
        }
    }

    private void HandleBuildingsKey(ConsoleKey key)
    {
        var count = city.Buildings.Count;
        if (key == ConsoleKey.UpArrow)
            _buildingIndex = (_buildingIndex + count - 1) % count;
        else if (key == ConsoleKey.DownArrow)
            _buildingIndex = (_buildingIndex + 1) % count;
        else if (key == ConsoleKey.Enter)
            Build(city.Buildings[_buildingIndex]);
    }

    private void CycleFocus()
    {
        do
            _focus = (Focus)(((int)_focus + 1) % 3);
        while (_focus == Focus.Party && !IsPartyHere);
    }

    private Squad FocusedSquad => _focus == Focus.Party ? session.Party.Squad : city.Garrison;

    private SquadSlot CurrentCursor => _focus == Focus.Party ? _partyCursor : _garrisonCursor;

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
            var count = city.Recruits.Count;
            _recruitIndex = (_recruitIndex + delta + count) % count;
            return;
        }

        var cursor = CurrentCursor;
        SetCursor(new SquadSlot(cursor.Line, (cursor.Column + delta + SquadSlot.Columns) % SquadSlot.Columns));
    }

    private void Hire()
    {
        var recruit = city.Recruits[_recruitIndex];
        _message = session.Hire(city, recruit) switch
        {
            HireResult.HiredToParty => $"[green3]{recruit.Name} joins the party.[/]",
            HireResult.HiredToGarrison => $"[green3]{recruit.Name} joins the garrison.[/]",
            HireResult.NotEnoughGold => $"[red]Not enough gold for {recruit.Name}.[/]",
            _ => "[red]No room in the party or the garrison.[/]"
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
            _message = "[red]Can't move there: no room, or the leader would leave the party.[/]";
    }

    private void Dismiss()
    {
        if (_focus == Focus.Recruits || FocusedSquad.UnitAt(CurrentCursor) is not { } unit)
            return;

        _picked = null;
        _message = session.Dismiss(FocusedSquad, unit)
            ? $"[grey]{unit.Name} dismissed.[/]"
            : "[red]The leader can't be dismissed.[/]";
    }

    private void Build(Building building)
    {
        _message = session.Build(city, building) switch
        {
            BuildResult.Built => $"[green3]{building.Name} built.[/]",
            BuildResult.AlreadyBuilt => $"[grey]{building.Name} is already built.[/]",
            BuildResult.RequirementMissing => $"[red]Requires {RequirementName(building)}.[/]",
            _ => $"[red]Not enough gold for {building.Name}.[/]"
        };
    }

    private string RequirementName(Building building) =>
        building.Requires is { } id ? city.FindBuilding(id)?.Name ?? id : "";

    private Markup RenderHeader()
    {
        var kind = city.IsCapital ? "[gold1]Capital[/]" : "[grey]City[/]";
        return new Markup(
            $"[bold gold1]{Markup.Escape(city.Name)}[/]  {kind}  [grey]income[/] {city.Income}   " +
            $"[grey]gold[/] [gold1]{session.Gold}[/]   [grey]turn[/] {session.Turn}");
    }

    private Grid RenderSquads()
    {
        var party = session.Party.Squad;
        var partyPanel = IsPartyHere
            ? SquadViewFor(party, $"Party {party.UsedSlots}/{party.Capacity}", Focus.Party, _partyCursor).Render()
            : new Panel(new Markup("[grey]No party here.[/]")).Header(" Party ");
        var garrison = city.Garrison;
        var garrisonPanel = SquadViewFor(garrison, $"Garrison {garrison.UsedSlots}/{garrison.Capacity}", Focus.Garrison, _garrisonCursor).Render();

        return new Grid()
            .AddColumn(new GridColumn().Width(SideWidth))
            .AddColumn(new GridColumn().NoWrap())
            .AddColumn(new GridColumn().NoWrap())
            .AddRow(RenderRecruits(), partyPanel, garrisonPanel);
    }

    private SquadView SquadViewFor(Squad squad, string title, Focus focus, SquadSlot cursor)
    {
        var isFocused = _focus == focus;
        var picked = _picked is { } p && p.Squad == squad ? squad.UnitAt(p.Slot) : null;
        return new SquadView(squad)
        {
            Title = title,
            IsFocused = isFocused,
            Cursor = isFocused ? cursor : null,
            Highlight = unit => unit == picked ? Color.DeepSkyBlue1 : null
        };
    }

    private Panel RenderRecruits()
    {
        var table = new Table().NoBorder().HideHeaders().Expand()
            .AddColumn("").AddColumn("").AddColumn(new TableColumn("").RightAligned());

        for (var i = 0; i < city.Recruits.Count; i++)
        {
            var recruit = city.Recruits[i];
            var selected = i == _recruitIndex && _focus == Focus.Recruits;
            var marker = selected ? "[gold1]›[/]" : " ";
            var name = selected ? $"[bold gold1]{Markup.Escape(recruit.Name)}[/]" : Markup.Escape(recruit.Name);
            var costColor = recruit.Cost <= session.Gold ? "gold1" : "red";
            table.AddRow($"{marker} {name}", $"[grey]{UnitStyles.AttackLabel(recruit)}[/]", $"[{costColor}]{recruit.Cost}[/]");
        }

        var selectedRecruit = city.Recruits[_recruitIndex];
        var stats = new Markup(
            $"[grey]HP[/] {selectedRecruit.MaxHp}  [grey]Armor[/] {selectedRecruit.Armor}  [grey]Power[/] {selectedRecruit.Power}\n" +
            $"[grey]Accuracy[/] {selectedRecruit.Accuracy}  [grey]Initiative[/] {selectedRecruit.Initiative}");

        return new Panel(new Rows(table, new Rule().RuleStyle(new Style(Color.Grey23)), stats))
            .Header(" Recruits ")
            .Border(_focus == Focus.Recruits ? BoxBorder.Double : BoxBorder.Rounded)
            .BorderColor(_focus == Focus.Recruits ? Color.Gold1 : Color.Grey50)
            .Expand();
    }

    private Panel RenderBuildings()
    {
        var table = new Table().Border(TableBorder.Simple).BorderColor(Color.Grey35)
            .AddColumn("[grey]Branch[/]").AddColumn("[grey]Building[/]")
            .AddColumn(new TableColumn("[grey]Cost[/]").RightAligned()).AddColumn("[grey]Status[/]").AddColumn("[grey]Effect[/]");

        var buildings = city.Buildings;
        for (var i = 0; i < buildings.Count; i++)
        {
            var building = buildings[i];
            var selected = i == _buildingIndex;
            var branch = i == 0 || buildings[i - 1].Branch != building.Branch ? Markup.Escape(building.Branch) : "";
            var name = selected ? $"[gold1]› {Markup.Escape(building.Name)}[/]" : $"  {Markup.Escape(building.Name)}";
            table.AddRow($"[grey]{branch}[/]", name, building.Cost.ToString(), BuildingStatus(building), $"[grey]{Markup.Escape(building.Description)}[/]");
        }

        return new Panel(table).Header(" Buildings ").Border(BoxBorder.Double).BorderColor(Color.Gold1);
    }

    private string BuildingStatus(Building building)
    {
        if (building.IsBuilt)
            return "[green3]built[/]";

        if (building.Requires != null && city.FindBuilding(building.Requires)?.IsBuilt != true)
            return $"[grey35]needs {Markup.Escape(RequirementName(building))}[/]";

        return building.Cost <= session.Gold ? "[gold1]available[/]" : "[red]no gold[/]";
    }

    private Markup RenderHints()
    {
        if (_showBuildings)
            return new Markup("[gold1]↑↓[/] [grey]select[/]   [gold1]Enter[/] [grey]build[/]   [gold1]B/Esc[/] [grey]back[/]");

        var buildings = city.IsCapital ? "   [gold1]B[/] [grey]buildings[/]" : "";
        var enter = _focus == Focus.Recruits ? "hire" : _picked == null ? "pick unit" : "place unit";
        return new Markup(
            $"[gold1]Tab[/] [grey]switch panel[/]   [gold1]←↑→↓[/] [grey]select[/]   [gold1]Enter[/] [grey]{enter}[/]   " +
            $"[gold1]D[/] [grey]dismiss[/]{buildings}   [gold1]Esc[/] [grey]{(_picked == null ? "leave" : "cancel")}[/]");
    }
}
