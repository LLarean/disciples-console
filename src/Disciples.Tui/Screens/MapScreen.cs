using Disciples.Core.Cities;
using Disciples.Core.Map;
using Disciples.Core.Session;
using Disciples.Core.Squads;
using Disciples.Tui.Widgets;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;

namespace Disciples.Tui.Screens;

public sealed class MapScreen : Screen
{
    private const int SideWidth = 36;
    private const int LogCapacity = 50;

    private readonly GameFlow _flow;
    private readonly GameSession _session;
    private readonly List<string> _log = [];

    private Position? _cursor;
    private Position? _destination;
    private Party _leading;

    public MapScreen(GameFlow flow, GameSession session)
    {
        _flow = flow;
        _session = session;
        _leading = session.Party;
        _log.Add($"Welcome, {session.Party.Name}. Arrows or numpad to move.");

        var map = new MapView(session, () => _cursor, () => Route) { X = 0, Y = 0, Width = Dim.Fill(SideWidth), Height = Dim.Fill(1) };
        var party = new PartyView(session, _log) { X = Pos.Right(map), Y = 0, Width = SideWidth, Height = Dim.Fill(1) };
        var hints = new HintBar(() => _cursor != null ? TargetHints : MapHints);
        Add(map, party, hints);
    }

    private Route Route => (_cursor ?? _destination) is { } target ? _session.PlanRoute(target) : Route.None;

    private IEnumerable<(string, string)> MapHints =>
    [
        ("←↑→↓", "/ numpad — move"), ("T", "travel"), .. _destination != null ? [("G", "go on")] : Array.Empty<(string, string)>(),
        ("Enter", "city"), ("C", "capital"), ("S", "squad"), ("I", "items"), .. NextLeaderHint, .. LevelUpHint, ("E", "end turn"), ("Esc", "menu")
    ];

    private IEnumerable<(string, string)> NextLeaderHint =>
        _session.Parties.Count > 1 ? [("Tab", "next leader")] : [];

    private IEnumerable<(string, string)> TargetHints
    {
        get
        {
            var route = Route;
            var status = route.IsEmpty ? "no route" : $"cost {route.Cost}, {_session.Party.MovementPoints} left";
            return [("←↑→↓", "target"), ("Enter", "go"), ("Esc", "cancel"), ("·", status)];
        }
    }

    public override bool HandleKey(Key key) => _cursor is { } cursor ? HandleTargetKey(key, cursor) : HandleMapKey(key);

    private bool HandleTargetKey(Key key, Position cursor)
    {
        if (key == Key.Esc)
            _cursor = null;
        else if (key == Key.Enter || key == Key.T)
        {
            _destination = cursor;
            _cursor = null;
            Walk();
        }
        else if (KeyToDirection(key) is { } direction && _session.Map.Contains(cursor.Step(direction)))
            _cursor = cursor.Step(direction);
        else
            return false;

        return true;
    }

    private bool HandleMapKey(Key key)
    {
        if (key == Key.Esc)
            Shell.Push(new PauseScreen(_flow, _session));
        else if (key == Key.E)
            EndTurn();
        else if (key == Key.S)
            Shell.Push(new SquadScreen(_session));
        else if (key == Key.I)
            Shell.Push(new ItemsScreen(_session));
        else if (key == Key.L && _session.Party.UnspentPerks > 0)
            Shell.Push(new PerkScreen(_session));
        else if (key == Key.Enter && _session.CurrentCity is { IsPlayerOwned: true } city)
            EnterCity(city);
        else if (key == Key.Enter && _session.CurrentSite is { Kind: SiteKind.Camp or SiteKind.Merchant } site)
            OpenSite(site);
        else if (key == Key.C && _session.Capital is { } capital)
            EnterCity(capital);
        else if (key == Key.Tab && _session.Parties.Count > 1)
            SelectNextParty();
        else if (key == Key.T)
            _cursor = _destination ?? _session.Party.Position;
        else if (key == Key.G && _destination != null)
            Walk();
        else if (KeyToDirection(key) is { } direction)
        {
            _destination = null;
            Move(direction);
        }
        else
            return false;

        return true;
    }

    private IEnumerable<(string, string)> LevelUpHint =>
        _session.Party.UnspentPerks > 0 ? [("L", "level up")] : [];

    /// <summary>Follows the route to the destination until movement runs out or something stops the party.</summary>
    private void Walk()
    {
        var route = Route;
        if (route.IsEmpty)
        {
            Log("No route there.");
            _destination = null;
            return;
        }

        foreach (var step in route.Steps)
        {
            if (!Move(_session.Party.Position.DirectionTo(step)!.Value))
                break;
        }

        if (_session.Party.Position == _destination || _session.Status != GameStatus.Playing)
            _destination = null;
    }

    /// <returns>Whether the party moved and may keep walking.</returns>
    private bool Move(Direction direction)
    {
        var target = _session.Party.Position.Step(direction);

        var result = _session.TryMove(direction);
        DrainEvents();

        switch (result)
        {
            case MoveResult.Moved when _session.Map.CityAt(target) is { } city:
                EnterCity(city);
                return false;
            case MoveResult.CityCaptured when _session.Status == GameStatus.Playing:
                EnterCity(_session.CurrentCity!);
                return false;
            case MoveResult.CityCaptured:
                _flow.EndGame(_session);
                return false;
            case MoveResult.SiteVisited when _session.CurrentSite is { Kind: SiteKind.Camp or SiteKind.Merchant } visited:
                Log($"Visited {visited.Name}.");
                OpenSite(visited);
                return false;
            case MoveResult.EnemyEncountered:
                Engage(_session.EncounterAt(target)!);
                return false;
            case MoveResult.Moved when _session.Party.MovementPoints == 0:
                Log("Out of movement. Press E to end turn.");
                return false;
            case MoveResult.Moved or MoveResult.SiteVisited:
                return true;
            case MoveResult.OutOfBounds:
                Log("The edge of the world.");
                break;
            case MoveResult.Occupied:
                Log($"{_session.PartyAt(target)!.Name} holds the way.");
                break;
            case MoveResult.Impassable:
                Log($"{_session.Map.TerrainAt(target).Name} is impassable.");
                break;
            case MoveResult.NotEnoughMovement:
                var terrain = _session.Map.TerrainAt(target);
                Log($"{terrain.Name} costs {terrain.MoveCost}, only {_session.Party.MovementPoints} left.");
                break;
        }

        return false;
    }

    private void SelectNextParty()
    {
        var parties = _session.Parties.ToList();
        var next = parties[(parties.IndexOf(_session.Party) + 1) % parties.Count];
        _session.Select(next);
        Log($"{next.Name} takes the lead.");
    }

    private void OpenSite(Site site) =>
        Shell.Push(site.Kind == SiteKind.Merchant ? new MerchantScreen(_session, site) : new CampScreen(_session, site));

    private void EnterCity(City city)
    {
        Log($"Visited {city.Name}.");
        Shell.Push(new CityScreen(_session, city));
    }

    private void Engage(Encounter encounter)
    {
        Log($"Battle with {encounter.Name}.");
        Shell.Push(new BattleScreen(_flow, _session, encounter));
    }

    private void EndTurn()
    {
        _session.EndTurn();
        DrainEvents();
        Log($"Gold {_session.Gold}.");

        if (_session.IncomingAttack is { } attack)
            Engage(attack);
    }

    protected override void UpdateViews()
    {
        DrainEvents();
        if (_leading == _session.Party)
            return;

        _leading = _session.Party;
        _destination = null;
    }

    private void DrainEvents()
    {
        foreach (var gameEvent in _session.TakeEvents())
            Log(GameEventText.Describe(gameEvent));
    }

    private void Log(string message)
    {
        _log.Add(message);
        if (_log.Count > LogCapacity)
            _log.RemoveAt(0);
    }

    private static Direction? KeyToDirection(Key key)
    {
        if (key == Key.CursorUp || key == Key.D8) return Direction.North;
        if (key == Key.PageUp || key == Key.D9) return Direction.NorthEast;
        if (key == Key.CursorRight || key == Key.D6) return Direction.East;
        if (key == Key.PageDown || key == Key.D3) return Direction.SouthEast;
        if (key == Key.CursorDown || key == Key.D2) return Direction.South;
        if (key == Key.End || key == Key.D1) return Direction.SouthWest;
        if (key == Key.CursorLeft || key == Key.D4) return Direction.West;
        if (key == Key.Home || key == Key.D7) return Direction.NorthWest;
        return null;
    }
}
