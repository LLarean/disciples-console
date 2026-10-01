using Disciples.Core.Cities;
using Disciples.Core.Content;
using Disciples.Core.Map;
using Disciples.Core.Session;
using Disciples.Core.Squads;
using Disciples.Core.Units;
using static Disciples.Core.Tests.TestUnits;

namespace Disciples.Core.Tests.Session;

public class ThiefTests
{
    private static readonly Position Start = new(2, 0);
    private static readonly Position East = new(3, 0);
    private static readonly Position West = new(1, 0);

    private static readonly Building Guild = new("guild", "Guild", "Other", 0, "", null, allowsThieves: true);
    private static readonly UnitDefinition Thief =
        new("thief", "Thief", 90, 0, 60, 30, 85, AttackType.Melee, UnitSize.Small, 100, leadership: 1, movement: 20, thief: true);
    private static readonly UnitDefinition Warlord =
        new("warlord", "Warlord", 200, 0, 45, 45, 80, AttackType.Melee, UnitSize.Small, 0, leadership: 3, movement: 20);

    private static readonly GameContent Content = new(
        [Knight, Thief, Warlord, Squire, Archer, Guardian], [Plains], [Guild],
        new GameRules { LeaderClasses = { "thief", "knight" }, ThiefSuccessPercent = 70, ThiefFailureDamage = 40, ThiefPoisonDamage = 25, ThiefStealGold = 100 });

    /// <summary>A plains strip 6×1: the capital on the west end, the thief at x=2 between an enemy party (west) and a neutral band (east).</summary>
    private static GameSession CreateSession(
        UnitDefinition? leader = null, int roll = 0, Party? enemy = null, NeutralSquad? band = null, int enemyGold = 0, bool guild = true,
        Position? at = null)
    {
        var tiles = new Terrain[6, 1];
        for (var x = 0; x < 6; x++)
            tiles[x, 0] = Plains;

        var capital = new City("Capital", new Position(0, 0), true, 0, buildings: [Guild], built: guild ? [Guild.Id] : []);
        var map = new WorldMap("Test", tiles, [capital], band == null ? [] : [band], enemy == null ? [] : [enemy]);
        return new GameSession(
            Content, map, [new Party(new Unit(leader ?? Thief), at ?? Start)], 500, new FixedRandom(roll), enemyGold: enemyGold);
    }

    private static NeutralSquad Band(params UnitDefinition[] units)
    {
        var squad = new Squad();
        foreach (var unit in units)
            squad.TryAdd(new Unit(unit));
        return new NeutralSquad("Band", East, squad, 0);
    }

    private static Party EnemyParty(params UnitDefinition[] units)
    {
        var party = new Party(new Unit(Warlord), West);
        foreach (var unit in units)
            party.Squad.TryAdd(new Unit(unit));
        return party;
    }

    [Fact]
    public void Infiltrate_Poison_WoundsEveryUnitWithoutKilling()
    {
        var band = Band(Squire, Squire);
        band.Squad.Units[1].TakeDamage(Squire.MaxHp - 10);
        var session = CreateSession(band: band);

        Assert.Equal(ThiefResult.Done, session.Infiltrate(ThiefAction.Poison, East));

        Assert.Equal([Squire.MaxHp - 25, 1], band.Squad.Units.Select(u => u.Hp));
        Assert.Equal(0, session.Party.MovementPoints);
        var poisoned = session.TakeEvents().Single(e => e.Kind == GameEventKind.ThiefPoisoned);
        Assert.Equal(25 + 9, poisoned.Amount);
    }

    [Fact]
    public void Infiltrate_Assassinate_KillsTheWeakestUnitButNeverTheLeader()
    {
        var enemy = EnemyParty(Squire, Archer);
        var session = CreateSession(enemy: enemy);

        Assert.Equal(ThiefResult.Done, session.Infiltrate(ThiefAction.Assassinate, West));

        Assert.Equal([Warlord, Squire], enemy.Squad.Units.Select(u => u.Definition).OrderBy(d => d == Squire));
        Assert.Contains(session.TakeEvents(), e => e.Kind == GameEventKind.ThiefAssassinated && e.Unit?.Definition == Archer);
    }

    [Fact]
    public void Infiltrate_Assassinate_LoneLeader_IsPointless()
    {
        var session = CreateSession(enemy: EnemyParty());

        Assert.Equal(ThiefResult.Pointless, session.Infiltrate(ThiefAction.Assassinate, West));
        Assert.Equal(20, session.Party.MovementPoints);
    }

    [Fact]
    public void Infiltrate_Assassinate_LastUnitOfABand_RemovesIt()
    {
        var session = CreateSession(band: Band(Archer));

        session.Infiltrate(ThiefAction.Assassinate, East);

        Assert.Empty(session.Map.Neutrals);
        Assert.Equal(GameStatus.Won, session.Status);
        Assert.Contains(session.TakeEvents(), e => e.Kind == GameEventKind.SquadDestroyed);
    }

    [Fact]
    public void Infiltrate_Steal_MovesGoldFromTheEnemyTreasury()
    {
        var session = CreateSession(enemy: EnemyParty(), band: Band(Squire), enemyGold: 60);

        Assert.Equal(ThiefResult.Pointless, session.Infiltrate(ThiefAction.Steal, East));
        Assert.Equal(ThiefResult.Done, session.Infiltrate(ThiefAction.Steal, West));

        Assert.Equal((560, 0), (session.Gold, session.EnemyGold));
    }

    [Fact]
    public void Infiltrate_Failed_WoundsTheThiefAndSparesTheTarget()
    {
        var band = Band(Squire);
        var session = CreateSession(roll: 70, band: band);

        Assert.Equal(ThiefResult.Caught, session.Infiltrate(ThiefAction.Poison, East));

        Assert.Equal(Thief.MaxHp - 40, session.Party.Leader.Hp);
        Assert.Equal(Squire.MaxHp, band.Squad.Units[0].Hp);
        Assert.Equal(0, session.Party.MovementPoints);
    }

    [Fact]
    public void Infiltrate_FailedByAWoundedThief_LosesTheParty()
    {
        var session = CreateSession(roll: 99, band: Band(Squire));
        session.Party.Leader.TakeDamage(Thief.MaxHp - 40);

        session.Infiltrate(ThiefAction.Poison, East);

        Assert.Empty(session.Parties);
        Assert.Equal(GameStatus.Lost, session.Status);
    }

    [Fact]
    public void Infiltrate_SecondAttemptInATurn_IsRefused()
    {
        var session = CreateSession(band: Band(Squire));
        session.Infiltrate(ThiefAction.Poison, East);

        Assert.Equal(ThiefResult.Exhausted, session.Infiltrate(ThiefAction.Poison, East));

        session.EndTurn();
        Assert.Equal(ThiefResult.Done, session.Infiltrate(ThiefAction.Poison, East));
    }

    [Fact]
    public void Infiltrate_ByAnotherLeaderOrFromAfar_IsRefused()
    {
        Assert.Equal(ThiefResult.NotAThief, CreateSession(Knight, band: Band(Squire)).Infiltrate(ThiefAction.Poison, East));

        var far = CreateSession(band: Band(Squire), at: new Position(5, 0));
        Assert.Empty(far.ThiefTargets());
        Assert.Equal(ThiefResult.NoTarget, far.Infiltrate(ThiefAction.Poison, East));
    }

    [Fact]
    public void HireLeader_Thief_NeedsTheGuild()
    {
        var without = CreateSession(Knight, guild: false);
        var with = CreateSession(Knight);

        Assert.Equal(HireResult.Unavailable, without.HireLeader(without.Capital!, Thief));
        Assert.Equal(HireResult.LeaderHired, with.HireLeader(with.Capital!, Thief));
        Assert.True(with.IsThief);
    }
}
