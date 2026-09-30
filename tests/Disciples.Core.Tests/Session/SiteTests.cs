using Disciples.Core.Cities;
using Disciples.Core.Map;
using Disciples.Core.Persistence;
using Disciples.Core.Session;
using Disciples.Core.Squads;
using Disciples.Core.Units;
using static Disciples.Core.Tests.TestUnits;

namespace Disciples.Core.Tests.Session;

public class SiteTests
{
    private static readonly Position Start = new(0, 0);
    private static readonly Position East = new(1, 0);

    private static GameSession CreateSession(Site site, int gold = 100)
    {
        var tiles = new Terrain[3, 1];
        for (var x = 0; x < 3; x++)
            tiles[x, 0] = Plains;

        var capital = new City("Capital", new Position(2, 0), true, 10);
        var map = new WorldMap("Test", tiles, [capital], sites: [site]);
        return new GameSession(TestContent, map, new Party(new Unit(Knight), Start), gold, new FixedRandom());
    }

    [Fact]
    public void TryMove_OntoTreasure_GrantsGoldAndRemovesIt()
    {
        var session = CreateSession(new Site("Chest", SiteKind.Treasure, East, gold: 150));

        Assert.Equal(MoveResult.SiteVisited, session.TryMove(Direction.East));

        Assert.Equal(250, session.Gold);
        Assert.Empty(session.Map.Sites);
        Assert.Contains(session.TakeEvents(), e => e.Kind == GameEventKind.TreasureFound);
    }

    [Fact]
    public void EndTurn_CapturedMine_AddsIncome()
    {
        var mine = new Site("Mine", SiteKind.Mine, East, gold: 40);
        var session = CreateSession(mine, gold: 0);
        session.TryMove(Direction.East);

        session.EndTurn();

        Assert.Equal(Owner.Player, mine.Owner);
        Assert.Equal(10 + 40, session.Gold);
    }

    [Fact]
    public void HireMercenary_AtCamp_JoinsParty()
    {
        var camp = new Site("Camp", SiteKind.Camp, East, mercenaries: [Archer]);
        var session = CreateSession(camp);
        session.TryMove(Direction.East);

        Assert.Equal(HireResult.HiredToParty, session.HireMercenary(camp, Archer));

        Assert.Equal(100 - Archer.Cost, session.Gold);
        Assert.Contains(session.Party.Squad.Units, u => u.Definition == Archer);
    }

    [Fact]
    public void HireMercenary_AwayFromCamp_IsUnavailable()
    {
        var camp = new Site("Camp", SiteKind.Camp, East, mercenaries: [Archer]);
        var session = CreateSession(camp);

        Assert.Equal(HireResult.Unavailable, session.HireMercenary(camp, Archer));
        Assert.Equal(100, session.Gold);
    }

    [Fact]
    public void CaptureThenRestore_KeepsSites()
    {
        var mine = new Site("Mine", SiteKind.Mine, East, gold: 40, owner: Owner.Player);
        var session = CreateSession(mine);

        var restored = SnapshotMapper.Restore(SnapshotMapper.Capture(session), TestContent, new FixedRandom());

        var site = restored.Map.Sites.Single();
        Assert.Equal((SiteKind.Mine, East, 40, Owner.Player), (site.Kind, site.Position, site.Gold, site.Owner));
    }
}
