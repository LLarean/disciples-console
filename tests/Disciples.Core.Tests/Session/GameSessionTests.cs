using Disciples.Core.Battles;
using Disciples.Core.Cities;
using Disciples.Core.Map;
using Disciples.Core.Session;
using Disciples.Core.Squads;
using Disciples.Core.Units;
using static Disciples.Core.Tests.TestUnits;

namespace Disciples.Core.Tests.Session;

public class GameSessionTests
{
    private static readonly Position Start = new(1, 1);
    private static readonly Position TownPosition = new(1, 0);
    private static readonly Position EnemyPosition = new(0, 2);

    private static GameSession CreateSession(Terrain east, int movementPoints = 10, int gold = 100)
    {
        var tiles = new Terrain[3, 3];
        for (var x = 0; x < 3; x++)
        for (var y = 0; y < 3; y++)
            tiles[x, y] = Plains;
        tiles[2, 1] = east;

        var buildings = new[]
        {
            new Building("barracks", "Barracks", "Fighters", 50, "", null),
            new Building("order", "Order", "Fighters", 50, "", "barracks")
        };
        var town = new City("Town", TownPosition, true, 30, [Squire, Archer], buildings);
        var enemySquad = new Squad();
        enemySquad.TryAdd(new Unit(Squire));
        var enemy = new NeutralSquad("Bandits", EnemyPosition, enemySquad, 75);

        var map = new WorldMap("Test", tiles, [town], [enemy]);
        return new GameSession(TestContent, map, new Party(new Unit(Knight), Start, movementPoints), gold, new FixedRandom());
    }

    [Fact]
    public void TryMove_PassableTile_MovesPartyAndSpendsCost()
    {
        var session = CreateSession(Road);

        Assert.Equal(MoveResult.Moved, session.TryMove(Direction.East));
        Assert.Equal(new Position(2, 1), session.Party.Position);
        Assert.Equal(9, session.Party.MovementPoints);
    }

    [Fact]
    public void TryMove_ImpassableTile_KeepsPartyInPlace()
    {
        var session = CreateSession(Water);

        Assert.Equal(MoveResult.Impassable, session.TryMove(Direction.East));
        Assert.Equal(Start, session.Party.Position);
    }

    [Fact]
    public void TryMove_BeyondEdge_ReturnsOutOfBounds()
    {
        var session = CreateSession(Plains);
        session.TryMove(Direction.West);

        Assert.Equal(MoveResult.OutOfBounds, session.TryMove(Direction.West));
    }

    [Fact]
    public void TryMove_CostAboveRemainingPoints_ReturnsNotEnoughMovement()
    {
        var session = CreateSession(Plains, movementPoints: 1);

        Assert.Equal(MoveResult.NotEnoughMovement, session.TryMove(Direction.East));
    }

    [Fact]
    public void TryMove_IntoNeutral_ReportsEncounterWithoutMoving()
    {
        var session = CreateSession(Plains);

        Assert.Equal(MoveResult.EnemyEncountered, session.TryMove(Direction.SouthWest));
        Assert.Equal(Start, session.Party.Position);
    }

    [Fact]
    public void EndTurn_RestoresMovementAddsIncomeAndAdvancesTurn()
    {
        var session = CreateSession(Plains);
        session.TryMove(Direction.East);

        session.EndTurn();

        Assert.Equal(2, session.Turn);
        Assert.Equal(10, session.Party.MovementPoints);
        Assert.Equal(130, session.Gold);
    }

    [Fact]
    public void Hire_InCity_AddsToPartyAndSpendsGold()
    {
        var session = CreateSession(Plains);
        session.TryMove(Direction.North);
        var town = session.CurrentCity!;

        Assert.Equal(HireResult.HiredToParty, session.Hire(town, Squire));
        Assert.Equal(50, session.Gold);
        Assert.Equal(2, session.Party.Squad.Units.Count);
    }

    [Fact]
    public void Hire_PartyFull_GoesToGarrison()
    {
        var session = CreateSession(Plains, gold: 1000);
        session.TryMove(Direction.North);
        var town = session.CurrentCity!;

        for (var i = 0; i < 3; i++)
            session.Hire(town, Archer);

        Assert.Equal(HireResult.HiredToGarrison, session.Hire(town, Archer));
        Assert.Single(town.Garrison.Units);
    }

    [Fact]
    public void Hire_WithoutGold_Fails()
    {
        var session = CreateSession(Plains, gold: 10);

        Assert.Equal(HireResult.NotEnoughGold, session.Hire(session.Map.Cities[0], Squire));
    }

    [Fact]
    public void Dismiss_Leader_IsRejected()
    {
        var session = CreateSession(Plains);

        Assert.False(session.Dismiss(session.Party.Squad, session.Party.Leader));
    }

    [Fact]
    public void Build_RequiresPreviousBuildingAndGold()
    {
        var session = CreateSession(Plains);
        var town = session.Map.Cities[0];

        Assert.Equal(BuildResult.RequirementMissing, session.Build(town, town.Buildings[1]));
        Assert.Equal(BuildResult.Built, session.Build(town, town.Buildings[0]));
        Assert.Equal(BuildResult.Built, session.Build(town, town.Buildings[1]));
        Assert.Equal(BuildResult.NotEnoughGold, session.Build(town, new Building("x", "X", "", 999, "", null)));
    }

    [Fact]
    public void FinishBattle_Victory_RemovesNeutralAndGrantsReward()
    {
        var session = CreateSession(Plains);
        var neutral = session.Map.Neutrals[0];
        var battle = session.StartBattle(neutral);

        while (!battle.IsOver)
            new SimpleBattleAi(session.Random).Act(battle);
        session.FinishBattle(battle, neutral);

        Assert.Equal(BattleOutcome.Victory, battle.Outcome);
        Assert.Empty(session.Map.Neutrals);
        Assert.Equal(175, session.Gold);
    }
}
