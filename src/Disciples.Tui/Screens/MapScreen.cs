using Disciples.Core.Cities;
using Disciples.Core.Map;
using Disciples.Core.Session;
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

    public MapScreen(GameFlow flow, GameSession session)
    {
        _flow = flow;
        _session = session;
        _log.Add($"Welcome, {session.Party.Name}. Arrows or numpad to move.");

        var map = new MapView(session) { X = 0, Y = 0, Width = Dim.Fill(SideWidth), Height = Dim.Fill(1) };
        var party = new PartyView(session, _log) { X = Pos.Right(map), Y = 0, Width = SideWidth, Height = Dim.Fill(1) };
        var hints = new HintBar(() => [("←↑→↓", "/ numpad — move"), ("Enter", "city"), ("S", "squad"), ("E", "end turn"), ("Esc", "menu")]);
        Add(map, party, hints);
    }

    public override bool HandleKey(Key key)
    {
        if (key == Key.Esc)
            Shell.Push(new PauseScreen(_flow, _session));
        else if (key == Key.E)
            EndTurn();
        else if (key == Key.S)
            Shell.Push(new SquadScreen(_session));
        else if (key == Key.Enter && _session.CurrentCity is { IsPlayerOwned: true } city)
            EnterCity(city);
        else if (KeyToDirection(key) is { } direction)
            Move(direction);
        else
            return false;

        return true;
    }

    private void Move(Direction direction)
    {
        var target = _session.Party.Position.Step(direction);

        var result = _session.TryMove(direction);
        DrainEvents();

        switch (result)
        {
            case MoveResult.Moved when _session.Map.CityAt(target) is { } city:
                EnterCity(city);
                break;
            case MoveResult.CityCaptured when _session.Status == GameStatus.Playing:
                EnterCity(_session.CurrentCity!);
                break;
            case MoveResult.CityCaptured:
                _flow.EndGame(_session);
                break;
            case MoveResult.EnemyEncountered:
                Engage(_session.EncounterAt(target)!);
                break;
            case MoveResult.Moved when _session.Party.MovementPoints == 0:
                Log("Out of movement. Press E to end turn.");
                break;
            case MoveResult.OutOfBounds:
                Log("The edge of the world.");
                break;
            case MoveResult.Impassable:
                Log($"{_session.Map.TerrainAt(target).Name} is impassable.");
                break;
            case MoveResult.NotEnoughMovement:
                var terrain = _session.Map.TerrainAt(target);
                Log($"{terrain.Name} costs {terrain.MoveCost}, only {_session.Party.MovementPoints} left.");
                break;
        }
    }

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

    protected override void UpdateViews() => DrainEvents();

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
