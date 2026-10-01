using Disciples.Core.Cities;
using Disciples.Core.Content;
using Disciples.Core.Map;
using Disciples.Core.Persistence;
using Disciples.Core.Session;
using Disciples.Core.Squads;
using Disciples.Core.Units;
using static Disciples.Core.Tests.TestUnits;

namespace Disciples.Core.Tests.Session;

public class RodTests
{
    private static readonly Position Start = new(6, 0);

    private static readonly UnitDefinition Bearer =
        new("bearer", "Bearer", 100, 0, 50, 30, 80, AttackType.Melee, UnitSize.Small, 0, leadership: 3, movement: 20, plantsRods: true);
    private static readonly UnitDefinition Warlord =
        new("warlord", "Warlord", 200, 0, 45, 45, 80, AttackType.Melee, UnitSize.Small, 0, leadership: 3, movement: 10);

    private static readonly GameContent Content = new(
        [Knight, Bearer, Warlord], [Plains], [],
        new GameRules { CapitalTerritoryRadius = 2, CityTerritoryRadius = 2, RodTerritoryRadius = 2, RodCost = 100 });

    /// <summary>A plains strip 12×1: the capital on the west claims x 0–2, the party stands on nobody's land.</summary>
    private static GameSession CreateSession(
        UnitDefinition? leader = null, int gold = 200, IEnumerable<Site>? sites = null, IEnumerable<Rod>? rods = null,
        IEnumerable<Party>? enemies = null, IEnumerable<City>? cities = null)
    {
        var tiles = new Terrain[12, 1];
        for (var x = 0; x < 12; x++)
            tiles[x, 0] = Plains;

        City[] all = [new("Capital", new Position(0, 0), true, 0), .. cities ?? []];
        var map = new WorldMap("Test", tiles, all, enemies: enemies, sites: sites, rods: rods);
        return new GameSession(Content, map, new Party(new Unit(leader ?? Bearer), Start), gold, new FixedRandom());
    }

    [Fact]
    public void PlantRod_PaysGoldAndClaimsTheLandAround()
    {
        var session = CreateSession();
        Assert.Equal(Owner.Neutral, session.Territory.OwnerAt(new Position(8, 0)));

        Assert.Equal(RodResult.Planted, session.PlantRod());

        Assert.Equal(100, session.Gold);
        Assert.Equal(Owner.Player, session.Map.RodAt(Start)?.Owner);
        Assert.Equal(Owner.Player, session.Territory.OwnerAt(new Position(8, 0)));
        Assert.Equal(Owner.Neutral, session.Territory.OwnerAt(new Position(9, 0)));
        Assert.Contains(session.TakeEvents(), e => e.Kind == GameEventKind.RodPlanted && e.At == Start);
    }

    [Fact]
    public void PlantRod_MineOnTheClaimedLand_IsTakenAtTurnEnd()
    {
        var mine = new Site("Mine", SiteKind.Mine, new Position(8, 0), 10);
        var session = CreateSession(sites: [mine]);
        session.PlantRod();

        session.EndTurn();

        Assert.Equal(Owner.Player, mine.Owner);
        Assert.Equal(110, session.Gold);
    }

    [Fact]
    public void PlantRod_ByALeaderWithoutRods_IsRefused()
    {
        var session = CreateSession(Knight);

        Assert.False(session.CanPlantRods);
        Assert.Equal(RodResult.NotARodBearer, session.PlantRod());
        Assert.Empty(session.Map.Rods);
    }

    [Fact]
    public void PlantRod_WithoutGold_IsRefused()
    {
        var session = CreateSession(gold: 99);

        Assert.Equal(RodResult.NotEnoughGold, session.PlantRod());
        Assert.Empty(session.Map.Rods);
    }

    [Fact]
    public void PlantRod_OnARodOrASite_IsRefused()
    {
        var session = CreateSession(sites: [new Site("Camp", SiteKind.Camp, new Position(7, 0))]);
        session.PlantRod();

        Assert.Equal(RodResult.Occupied, session.PlantRod());

        session.TryMove(Direction.East);
        Assert.Equal(RodResult.Occupied, session.PlantRod());
        Assert.Single(session.Map.Rods);
        Assert.Equal(100, session.Gold);
    }

    [Fact]
    public void OwnerAt_RodAndCity_NearestClaimWins()
    {
        var hold = new City("Hold", new Position(11, 0), false, 0, owner: Owner.Enemy);
        var session = CreateSession(rods: [new Rod(new Position(8, 0))], cities: [hold]);

        Assert.Equal(Owner.Player, session.Territory.OwnerAt(new Position(9, 0)));
        Assert.Equal(Owner.Enemy, session.Territory.OwnerAt(new Position(10, 0)));
    }

    [Fact]
    public void TryMove_OntoAnEnemyRod_BreaksIt()
    {
        var session = CreateSession(Knight, rods: [new Rod(new Position(7, 0), Owner.Enemy)]);
        Assert.Equal(Owner.Enemy, session.Territory.OwnerAt(Start));

        session.TryMove(Direction.East);

        Assert.Empty(session.Map.Rods);
        Assert.Equal(Owner.Neutral, session.Territory.OwnerAt(Start));
        Assert.Contains(session.TakeEvents(), e => e.Kind == GameEventKind.RodDestroyed);
    }

    [Fact]
    public void TryMove_OntoOwnRod_KeepsIt()
    {
        var session = CreateSession(rods: [new Rod(new Position(7, 0))]);

        session.TryMove(Direction.East);

        Assert.Single(session.Map.Rods);
    }

    [Fact]
    public void EndTurn_EnemyLeaderSeeksAndBreaksThePlayersRod()
    {
        var enemy = new Party(new Unit(Warlord), new Position(11, 0));
        var session = CreateSession(rods: [new Rod(new Position(9, 0))], enemies: [enemy]);

        session.EndTurn();

        Assert.Empty(session.Map.Rods);
        Assert.Equal(new Position(9, 0), enemy.Position);
        Assert.Contains(session.TakeEvents(), e => e.Kind == GameEventKind.RodLost && e.Party == enemy);
    }

    [Fact]
    public void CaptureThenRestore_KeepsRods()
    {
        var session = CreateSession(rods: [new Rod(new Position(9, 0), Owner.Enemy)]);
        session.PlantRod();

        var restored = SnapshotMapper.Restore(SnapshotMapper.Capture(session), Content, new FixedRandom());

        Assert.Equal(
            [(Owner.Enemy, new Position(9, 0)), (Owner.Player, Start)],
            restored.Map.Rods.Select(r => (r.Owner, r.Position)));
        Assert.Equal(Owner.Player, restored.Territory.OwnerAt(new Position(5, 0)));
    }
}
