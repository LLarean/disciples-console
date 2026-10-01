using Disciples.Core.Cities;
using Disciples.Core.Content;
using Disciples.Core.Map;
using Disciples.Core.Session;
using Disciples.Core.Squads;
using Disciples.Core.Units;
using static Disciples.Core.Tests.TestUnits;

namespace Disciples.Core.Tests.Session;

public class PartiesTests
{
    private const int Width = 8;

    private static readonly Position FirstStart = new(0, 0);
    private static readonly Position SecondStart = new(2, 0);
    private static readonly Position CapitalPosition = new(0, 1);

    private static readonly UnitDefinition Warlord =
        new("warlord", "Warlord", 150, 0, 50, 50, 80, AttackType.Melee, UnitSize.Small, 0, leadership: 4, movement: 4);

    /// <summary>A plains strip 8×2 with the capital in the south-west corner and two parties on the north row.</summary>
    private static GameSession CreateSession(Position? secondAt = null, Position? enemyAt = null, int gold = 0)
    {
        var tiles = new Terrain[Width, 2];
        for (var x = 0; x < Width; x++)
        for (var y = 0; y < 2; y++)
            tiles[x, y] = Plains;

        var capital = new City("Capital", CapitalPosition, true, 10, [Squire]);
        var enemies = enemyAt is { } position ? new[] { new Party(new Unit(Warlord), position) } : [];
        var map = new WorldMap("Test", tiles, [capital], enemies: enemies);
        var parties = new[] { new Party(new Unit(Knight), FirstStart), new Party(new Unit(Knight), secondAt ?? SecondStart) };
        return new GameSession(TestContent, map, parties, gold, new FixedRandom());
    }

    [Fact]
    public void Select_MakesThePartyActive_AndMovesOnlyIt()
    {
        var session = CreateSession();
        var (first, second) = (session.Parties[0], session.Parties[1]);

        Assert.Same(first, session.Party);
        Assert.True(session.Select(second));
        Assert.Equal(MoveResult.Moved, session.TryMove(Direction.East));

        Assert.Equal(new Position(3, 0), second.Position);
        Assert.Equal(FirstStart, first.Position);
        Assert.Equal(first.MaxMovementPoints, first.MovementPoints);
    }

    [Fact]
    public void Select_ForeignParty_IsRefused()
    {
        var session = CreateSession();

        Assert.False(session.Select(new Party(new Unit(Knight), new Position(5, 0))));
        Assert.Same(session.Parties[0], session.Party);
    }

    [Fact]
    public void TryMove_OntoAnotherParty_IsOccupied()
    {
        var session = CreateSession(secondAt: new Position(1, 0));

        Assert.Equal(MoveResult.Occupied, session.TryMove(Direction.East));
        Assert.Equal(FirstStart, session.Party.Position);
    }

    [Fact]
    public void PlanRoute_GoesAroundAnotherParty_AndNeverEndsOnIt()
    {
        var session = CreateSession(secondAt: new Position(1, 0));

        var route = session.PlanRoute(new Position(2, 0));

        Assert.DoesNotContain(new Position(1, 0), route.Steps);
        Assert.False(route.IsEmpty);
        Assert.True(session.PlanRoute(new Position(1, 0)).IsEmpty);
    }

    [Fact]
    public void EndTurn_RestoresMovementOfEveryParty()
    {
        var session = CreateSession();
        session.TryMove(Direction.East);
        session.Select(session.Parties[1]);
        session.TryMove(Direction.East);

        session.EndTurn();

        Assert.All(session.Parties, p => Assert.Equal(p.MaxMovementPoints, p.MovementPoints));
    }

    [Fact]
    public void EndTurn_HealsThePartyInACity_EvenWhenItIsNotActive()
    {
        var session = CreateSession(secondAt: CapitalPosition);
        var visitor = session.Parties[1];
        visitor.Leader.TakeDamage(50);
        var wounded = visitor.Leader.Hp;

        session.EndTurn();

        Assert.True(visitor.Leader.Hp > wounded);
    }

    [Fact]
    public void Hire_GoesToThePartyVisitingTheCity()
    {
        var session = CreateSession(secondAt: CapitalPosition, gold: 100);
        var capital = session.Map.Cities[0];

        Assert.Equal(HireResult.HiredToParty, session.Hire(capital, Squire));
        Assert.Equal(2, session.Parties[1].Squad.Units.Count);
        Assert.Single(session.Parties[0].Squad.Units);
    }

    private static readonly UnitDefinition Captain =
        new("captain", "Captain", 150, 0, 50, 50, 80, AttackType.Melee, UnitSize.Small, 100, leadership: 3, movement: 10);

    private static GameSession CreateHiringSession(int gold, Position? partyAt = null)
    {
        var content = new GameContent([Knight, Captain, Squire], [Plains], [], new GameRules { LeaderClasses = { "captain" } });
        var tiles = new[,] { { Plains, Plains }, { Plains, Plains } };
        var capital = new City("Capital", CapitalPosition, true, 10);
        var village = new City("Village", new Position(1, 1), false, 10);
        var map = new WorldMap("Test", tiles, [capital, village]);
        return new GameSession(content, map, new Party(new Unit(Knight), partyAt ?? FirstStart), gold, new FixedRandom());
    }

    [Fact]
    public void HireLeader_InTheCapital_AddsAnActivePartyThere()
    {
        var session = CreateHiringSession(gold: 150);

        Assert.Equal(HireResult.LeaderHired, session.HireLeader(session.Map.Cities[0], Captain));

        Assert.Equal(2, session.Parties.Count);
        Assert.Same(session.Parties[1], session.Party);
        Assert.Same(Captain, session.Party.Leader.Definition);
        Assert.Equal(CapitalPosition, session.Party.Position);
        Assert.Equal((50, 3), (session.Gold, session.Party.Squad.Capacity));
    }

    [Fact]
    public void HireLeader_Refused_ChangesNothing()
    {
        var poor = CreateHiringSession(gold: 99);
        var visited = CreateHiringSession(gold: 150, partyAt: CapitalPosition);
        var rich = CreateHiringSession(gold: 150);

        Assert.Equal(HireResult.NotEnoughGold, poor.HireLeader(poor.Map.Cities[0], Captain));
        Assert.Equal(HireResult.NoRoom, visited.HireLeader(visited.Map.Cities[0], Captain));
        Assert.Equal(HireResult.Unavailable, rich.HireLeader(rich.Map.Cities[1], Captain));
        Assert.Equal(HireResult.Unavailable, rich.HireLeader(rich.Map.Cities[0], Knight));

        Assert.All(new[] { poor, visited, rich }, s => Assert.Single(s.Parties));
        Assert.Equal(150, rich.Gold);
    }

    [Fact]
    public void EndTurn_EnemyAttacksTheNearestParty_AndItBecomesActive()
    {
        var session = CreateSession(secondAt: new Position(4, 0), enemyAt: new Position(6, 0));

        session.EndTurn();

        Assert.NotNull(session.IncomingAttack);
        Assert.Same(session.Parties[1], session.Party);
    }

    [Fact]
    public void NewSession_RevealsFogAroundEveryParty()
    {
        var session = CreateSession(secondAt: new Position(7, 0));

        Assert.True(session.Fog.IsExplored(FirstStart));
        Assert.True(session.Fog.IsExplored(new Position(7, 0)));
    }
}
