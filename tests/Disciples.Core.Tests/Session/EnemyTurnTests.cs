using Disciples.Core.Cities;
using Disciples.Core.Map;
using Disciples.Core.Session;
using Disciples.Core.Squads;
using Disciples.Core.Units;
using static Disciples.Core.Tests.TestUnits;

namespace Disciples.Core.Tests.Session;

public class EnemyTurnTests
{
    private const int Width = 8;

    private static readonly Position PartyStart = new(0, 0);
    private static readonly Position CapitalPosition = new(0, 1);

    /// <summary>A plains strip 8×2: party and capital on the west, the enemy on the east.</summary>
    private static GameSession CreateSession(Position enemyAt, IEnumerable<City>? cities = null, UnitDefinition? enemyLeader = null)
    {
        var tiles = new Terrain[Width, 2];
        for (var x = 0; x < Width; x++)
        for (var y = 0; y < 2; y++)
            tiles[x, y] = Plains;

        var capital = new City("Capital", CapitalPosition, true, 10);
        var enemy = new Party(new Unit(enemyLeader ?? Warlord), enemyAt);
        var map = new WorldMap("Test", tiles, new[] { capital }.Concat(cities ?? []), enemies: [enemy]);
        return new GameSession(TestContent, map, new Party(new Unit(Knight), PartyStart), 0, new FixedRandom());
    }

    [Fact]
    public void EndTurn_EnemyWalksTowardsParty()
    {
        var session = CreateSession(new Position(7, 0));

        session.EndTurn();

        Assert.Equal(5, session.Map.Enemies[0].Position.X);
        Assert.Null(session.IncomingAttack);
    }

    [Fact]
    public void EndTurn_EnemyNextToParty_Attacks()
    {
        var session = CreateSession(new Position(3, 0));

        session.EndTurn();

        Assert.Same(session.Map.Enemies[0], session.IncomingAttack!.Enemy);
        Assert.Contains(session.TakeEvents(), e => e.Kind == GameEventKind.EnemyAttacks);
    }

    [Fact]
    public void EndTurn_EnemyTakesEmptyCityOnTheWay()
    {
        var town = new City("Town", new Position(5, 1), false, 10);
        var session = CreateSession(new Position(7, 1), cities: [town]);

        session.EndTurn();

        Assert.Equal(Owner.Enemy, town.Owner);
        Assert.Equal(town.Position, session.Map.Enemies[0].Position);
        Assert.Contains(session.TakeEvents(), e => e.Kind == GameEventKind.CityFell);
    }

    [Fact]
    public void EndTurn_GarrisonDefeatsEnemy_EnemyIsRemoved()
    {
        var town = new City("Town", new Position(6, 1), false, 10);
        town.Garrison.TryAdd(new Unit(Ogre));
        var session = CreateSession(new Position(7, 1), cities: [town], enemyLeader: WeakLord);

        session.EndTurn();

        Assert.Empty(session.Map.Enemies);
        Assert.Equal(Owner.Player, town.Owner);
        Assert.Contains(session.TakeEvents(), e => e.Kind == GameEventKind.CityHeld);
    }

    [Fact]
    public void EndTurn_EnemyIgnoresCapital()
    {
        var session = CreateSession(new Position(1, 1));
        session.TryMove(Direction.East);
        session.TryMove(Direction.East);
        session.TryMove(Direction.East);

        session.EndTurn();

        Assert.Equal(Owner.Player, session.Map.Cities[0].Owner);
        Assert.NotNull(session.IncomingAttack);
    }

    [Fact]
    public void FinishBattle_EnemyLeaderKilled_RemovesEnemyAndWins()
    {
        var session = CreateSession(new Position(1, 0), enemyLeader: WeakLord);
        var encounter = session.EncounterAt(new Position(1, 0))!;

        var battle = session.StartBattle(encounter);
        var ai = new Disciples.Core.Battles.SimpleBattleAi(session.Random);
        while (!battle.IsOver)
            ai.Act(battle);
        session.FinishBattle(battle, encounter);

        Assert.Empty(session.Map.Enemies);
        Assert.Equal(GameStatus.Won, session.Status);
    }

    private static readonly UnitDefinition Warlord =
        new("warlord", "Warlord", 150, 0, 50, 50, 80, AttackType.Melee, UnitSize.Small, 0, leadership: 4, movement: 4);

    private static readonly UnitDefinition WeakLord =
        new("weak-lord", "Weak Lord", 10, 0, 1, 1, 80, AttackType.Melee, UnitSize.Small, 0, leadership: 2, movement: 4);
}
