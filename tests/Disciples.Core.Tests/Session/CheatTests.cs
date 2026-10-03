using Disciples.Core.Cities;
using Disciples.Core.Content;
using Disciples.Core.Magic;
using Disciples.Core.Map;
using Disciples.Core.Session;
using Disciples.Core.Squads;
using Disciples.Core.Units;
using static Disciples.Core.Tests.TestUnits;

namespace Disciples.Core.Tests.Session;

public class CheatTests
{
    private static readonly Position Start = new(0, 0);
    private static readonly Building Barracks = new("barracks", "Barracks", "Fighter", 0, "", null);
    private static readonly GameContent Content = new([Knight, Recruit, Veteran], [Plains], [Barracks], new GameRules());

    /// <summary>A plains strip 12×1 with the party on the west and the capital next to it.</summary>
    private static GameSession CreateSession(bool barracks = true, params Unit[] squad)
    {
        var tiles = new Terrain[12, 1];
        for (var x = 0; x < 12; x++)
            tiles[x, 0] = Plains;

        var capital = new City("Capital", new Position(1, 0), true, 0, buildings: [Barracks], built: barracks ? [Barracks.Id] : []);
        var party = new Party(new Unit(Knight), Start);
        foreach (var unit in squad)
            party.Squad.TryAdd(unit);

        return new GameSession(Content, new WorldMap("Test", tiles, [capital]), party, 0, new FixedRandom());
    }

    [Fact]
    public void CheatGoldAndMana_FillTheStock()
    {
        var session = CreateSession();

        session.CheatGold(1000);
        session.CheatMana(100);

        Assert.Equal(1000, session.Gold);
        Assert.Equal(new Mana(100, 100, 100, 100), session.Mana);
    }

    [Fact]
    public void CheatExperience_UpgradesAndLevelsTheSquad()
    {
        var recruit = new Unit(Recruit);
        var veteran = new Unit(Veteran);
        var session = CreateSession(squad: [recruit, veteran]);

        session.CheatExperience();

        Assert.Same(Veteran, recruit.Definition);
        Assert.Equal(2, veteran.Level);
        Assert.Equal(2, session.Party.Leader.Level);
        Assert.Contains(session.TakeEvents(), e => e.Kind == GameEventKind.UnitUpgraded);
    }

    [Fact]
    public void CheatExperience_WithoutTheUpgradeBuilding_LeavesTheUnitWaiting()
    {
        var recruit = new Unit(Recruit);
        var session = CreateSession(barracks: false, recruit);

        session.CheatExperience();

        Assert.Same(Recruit, recruit.Definition);
        Assert.Contains(session.TakeEvents(), e => e.Kind == GameEventKind.UnitAwaitsBuilding);
    }

    [Fact]
    public void CheatRestore_HealsAndGivesMovementBack()
    {
        var recruit = new Unit(Recruit);
        var session = CreateSession(squad: recruit);
        recruit.TakeDamage(30);
        session.TryMove(Direction.East);

        session.CheatRestore();

        Assert.Equal(recruit.MaxHp, recruit.Hp);
        Assert.Equal(session.Party.MaxMovementPoints, session.Party.MovementPoints);
    }

    [Fact]
    public void CheatRevealMap_ExploresEveryTile()
    {
        var session = CreateSession();
        Assert.False(session.Fog.IsExplored(new Position(11, 0)));

        session.CheatRevealMap();

        Assert.True(session.Fog.IsExplored(new Position(11, 0)));
    }
}
