using Disciples.Core.Cities;
using Disciples.Core.Content;
using Disciples.Core.Map;
using Disciples.Core.Persistence;
using Disciples.Core.Session;
using Disciples.Core.Squads;
using Disciples.Core.Units;
using static Disciples.Core.Tests.TestUnits;

namespace Disciples.Core.Tests.Session;

public class CityTierTests
{
    private static readonly GameRules Rules = new()
    {
        CityHealPercent = 10,
        CityTierHealPercent = 20,
        CityGarrisonSlots = 1,
        CityUpgradeCosts = [100, 200]
    };

    private static readonly GameContent Content = new([Knight, Squire], [Plains], [], Rules);

    private static City Town(int tier = 1, Owner owner = Owner.Player, Squad? garrison = null) =>
        new("Town", new Position(1, 0), false, 0, [Squire], owner: owner, tier: tier, rules: Rules, garrison: garrison);

    /// <summary>The party stands in the capital, the town lies next to it.</summary>
    private static GameSession CreateSession(City town, int gold = 500)
    {
        var capital = new City("Capital", new Position(0, 0), true, 0, rules: Rules);
        var map = new WorldMap("Test", new[,] { { Plains }, { Plains } }, [capital, town]);
        return new GameSession(Content, map, new Party(new Unit(Knight), new Position(0, 0)), gold, new FixedRandom());
    }

    [Fact]
    public void Garrison_HoldsOneMoreUnitPerTier()
    {
        var town = Town(tier: 2);
        var session = CreateSession(town);

        Assert.Equal(HireResult.HiredToGarrison, session.Hire(town, Squire));
        Assert.Equal(HireResult.HiredToGarrison, session.Hire(town, Squire));
        Assert.Equal(HireResult.NoRoom, session.Hire(town, Squire));
    }

    [Fact]
    public void Capital_IsAlwaysAtTheTopTier()
    {
        var session = CreateSession(Town());

        Assert.Equal(3, session.Capital!.Tier);
        Assert.Equal(3, session.Capital.Garrison.Capacity);
        Assert.Equal(UpgradeResult.TopTier, session.UpgradeCity(session.Capital));
    }

    [Fact]
    public void UpgradeCity_SpendsGoldAndRaisesTheTier()
    {
        var town = Town();
        var session = CreateSession(town);

        Assert.Equal(UpgradeResult.Upgraded, session.UpgradeCity(town));
        Assert.Equal(UpgradeResult.Upgraded, session.UpgradeCity(town));
        Assert.Equal(UpgradeResult.TopTier, session.UpgradeCity(town));

        Assert.Equal(500 - 100 - 200, session.Gold);
        Assert.Equal((3, 3), (town.Tier, town.Garrison.Capacity));
        Assert.Null(town.UpgradeCost);
    }

    [Fact]
    public void UpgradeCity_WithoutGoldOrOwnership_IsRefused()
    {
        var town = Town();
        var hostile = Town(owner: Owner.Neutral);

        Assert.Equal(UpgradeResult.NotEnoughGold, CreateSession(town, gold: 99).UpgradeCity(town));
        Assert.Equal(UpgradeResult.Unavailable, CreateSession(hostile).UpgradeCity(hostile));
        Assert.Equal(1, town.Tier);
    }

    [Fact]
    public void EndTurn_HealsFasterInHigherTiers()
    {
        var garrison = new Squad();
        garrison.TryAdd(new Unit(Squire, hp: 10));
        var session = CreateSession(Town(tier: 2, garrison: garrison));
        session.Party.Leader.TakeDamage(100);

        session.EndTurn();

        Assert.Equal(10 + Squire.MaxHp * 30 / 100, garrison.Units.Single().Hp);
        Assert.Equal(50 + Knight.MaxHp * 50 / 100, session.Party.Leader.Hp);
    }

    [Fact]
    public void CaptureThenRestore_KeepsTheTier()
    {
        var town = Town(tier: 2);
        var session = CreateSession(town);
        session.Hire(town, Squire);
        session.Hire(town, Squire);

        var restored = SnapshotMapper.Restore(SnapshotMapper.Capture(session), Content, new FixedRandom());

        var city = restored.Map.Cities.Single(c => !c.IsCapital);
        Assert.Equal((2, 2, 2), (city.Tier, city.Garrison.Capacity, city.Garrison.UsedSlots));
    }

    [Fact]
    public void Restore_GarrisonLargerThanTheTierAllows_IsKept()
    {
        var session = CreateSession(Town(tier: 2));
        var snapshot = SnapshotMapper.Capture(session);
        var town = snapshot.Cities.Single(c => !c.Capital);
        town.Tier = 1;
        town.Garrison =
        [
            new UnitSnapshot { Id = Squire.Id, Line = SquadLine.Front, Column = 0 },
            new UnitSnapshot { Id = Squire.Id, Line = SquadLine.Front, Column = 1 }
        ];

        var restored = SnapshotMapper.Restore(snapshot, Content, new FixedRandom());

        var city = restored.Map.Cities.Single(c => !c.IsCapital);
        Assert.Equal((1, 2), (city.Garrison.Capacity, city.Garrison.UsedSlots));
    }
}
