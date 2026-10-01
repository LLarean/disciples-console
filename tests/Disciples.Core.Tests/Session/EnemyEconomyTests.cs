using Disciples.Core.Cities;
using Disciples.Core.Content;
using Disciples.Core.Map;
using Disciples.Core.Persistence;
using Disciples.Core.Session;
using Disciples.Core.Squads;
using Disciples.Core.Units;
using static Disciples.Core.Tests.TestUnits;

namespace Disciples.Core.Tests.Session;

public class EnemyEconomyTests
{
    private const int Width = 16;

    private static readonly Position HoldPosition = new(15, 1);

    private static readonly UnitDefinition Warlord =
        new("warlord", "Warlord", 150, 0, 50, 50, 80, AttackType.Melee, UnitSize.Small, 100, leadership: 3, movement: 4);

    private static readonly GameRules Rules = new() { EnemyLeaderClasses = [Warlord.Id], EnemyLeaderLimit = 2 };

    private static readonly GameContent Content = new([Knight, Squire, Archer, Warlord], [Plains], [], Rules, items: [Potion, Sword]);

    private static City Hold(Squad? garrison = null) =>
        new("Hold", HoldPosition, true, 40, [Squire, Archer], owner: Owner.Enemy, garrison: garrison);

    private static Party Enemy(int x, int y) => new(new Unit(Warlord), new Position(x, y));

    /// <summary>A plains strip 16×2: the party and its capital on the west, the enemy capital on the east, neutral land between.</summary>
    private static GameSession CreateSession(
        City? hold = null, IEnumerable<Party>? enemies = null, IEnumerable<Site>? sites = null, int enemyGold = 0)
    {
        var tiles = new Terrain[Width, 2];
        for (var x = 0; x < Width; x++)
        for (var y = 0; y < 2; y++)
            tiles[x, y] = Plains;

        var capital = new City("Capital", new Position(0, 1), true, 10);
        var cities = hold == null ? new[] { capital } : new[] { capital, hold };
        var map = new WorldMap("Test", tiles, cities, enemies: enemies, sites: sites);
        return new GameSession(Content, map, [new Party(new Unit(Knight), new Position(0, 0))], 0, new FixedRandom(), enemyGold: enemyGold);
    }

    [Fact]
    public void EndTurn_EnemyCollectsIncomeFromCitiesAndMines()
    {
        var mine = new Site("Mine", SiteKind.Mine, new Position(14, 0), 10, owner: Owner.Enemy);
        var session = CreateSession(Hold(), sites: [mine]);

        session.EndTurn();

        Assert.Equal(50, session.EnemyGold);
        Assert.Equal(10, session.Gold);
    }

    [Fact]
    public void EndTurn_EnemyHealsInItsCity()
    {
        var garrison = new Squad();
        garrison.TryAdd(new Unit(Squire, hp: 10));
        var enemy = Enemy(HoldPosition.X, HoldPosition.Y);
        enemy.Leader.TakeDamage(100);
        var session = CreateSession(Hold(garrison), [enemy]);

        session.EndTurn();

        Assert.Equal(10 + Squire.MaxHp * 25 / 100, garrison.Units.Single().Hp);
        Assert.Equal(50 + Warlord.MaxHp * 25 / 100, enemy.Leader.Hp);
    }

    [Fact]
    public void EndTurn_EnemyLeaderInItsCity_HiresTheDearestRecruitsItAffords()
    {
        var enemy = Enemy(HoldPosition.X, HoldPosition.Y);
        var session = CreateSession(Hold(), [enemy], enemyGold: 120);

        session.EndTurn();

        Assert.Equal(3, enemy.Squad.Units.Count);
        Assert.Equal(2, enemy.Squad.Units.Count(u => u.Definition == Squire));
        Assert.Equal(120 + 40 - 2 * Squire.Cost, session.EnemyGold);
    }

    [Fact]
    public void EndTurn_VacantEnemyCapital_HiresLeader()
    {
        var session = CreateSession(Hold(), enemyGold: 60);

        session.EndTurn();

        var leader = Assert.Single(session.Map.Enemies);
        Assert.Equal(HoldPosition, leader.Position);
        Assert.Equal(60 + 40 - Warlord.Cost, session.EnemyGold);
    }

    [Fact]
    public void EndTurn_EnemyAtTheLeaderLimit_HiresNoMore()
    {
        var session = CreateSession(Hold(), [Enemy(12, 0), Enemy(13, 1)], enemyGold: 500);

        session.EndTurn();

        Assert.Equal(2, session.Map.Enemies.Count);
        Assert.Equal(540, session.EnemyGold);
    }

    [Fact]
    public void EndTurn_EnemyTakesTreasureAndWearsWhatItFinds()
    {
        var enemy = Enemy(13, 0);
        var chest = new Site("Chest", SiteKind.Treasure, new Position(11, 0), 70, items: [Sword, Potion]);
        var session = CreateSession(enemies: [enemy], sites: [chest]);

        session.EndTurn();

        Assert.Empty(session.Map.Sites);
        Assert.Equal(70, session.EnemyGold);
        Assert.Equal([Sword], enemy.Equipped);
        Assert.Equal([Potion], enemy.Items);
        Assert.Contains(session.TakeEvents(), e => e.Kind == GameEventKind.TreasureLost);
    }

    [Fact]
    public void EndTurn_EnemyCapturesMineAndEarnsFromIt()
    {
        var mine = new Site("Mine", SiteKind.Mine, new Position(11, 0), 10, owner: Owner.Player);
        var session = CreateSession(enemies: [Enemy(13, 0)], sites: [mine]);

        session.EndTurn();
        Assert.Equal(Owner.Enemy, mine.Owner);
        Assert.Contains(session.TakeEvents(), e => e.Kind == GameEventKind.MineLost);

        session.EndTurn();
        Assert.Equal(10, session.EnemyGold);
    }

    [Fact]
    public void CaptureThenRestore_KeepsEnemyGold()
    {
        var session = CreateSession(Hold(), enemyGold: 35);

        var restored = SnapshotMapper.Restore(SnapshotMapper.Capture(session), Content, new FixedRandom());

        Assert.Equal(35, restored.EnemyGold);
    }
}
