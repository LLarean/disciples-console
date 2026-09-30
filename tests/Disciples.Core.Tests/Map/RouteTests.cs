using Disciples.Core.Cities;
using Disciples.Core.Map;
using Disciples.Core.Session;
using Disciples.Core.Squads;
using Disciples.Core.Units;
using static Disciples.Core.Tests.TestUnits;

namespace Disciples.Core.Tests.Map;

public class RouteTests
{
    /// <summary>Plains 5×1 with an optional neutral squad at x = 2.</summary>
    private static GameSession CreateSession(bool guarded = false)
    {
        var tiles = new Terrain[5, 1];
        for (var x = 0; x < 5; x++)
            tiles[x, 0] = Plains;

        var squad = new Squad();
        squad.TryAdd(new Unit(Squire));
        NeutralSquad[] neutrals = guarded ? [new NeutralSquad("Bandits", new Position(2, 0), squad, 0)] : [];
        var map = new WorldMap("Test", tiles, [new City("Capital", new Position(4, 0), true, 10)], neutrals);
        return new GameSession(TestContent, map, new Party(new Unit(Knight), new Position(0, 0)), 0, new FixedRandom());
    }

    [Fact]
    public void PlanRoute_SumsTerrainCost()
    {
        var route = CreateSession().PlanRoute(new Position(3, 0));

        Assert.Equal([new Position(1, 0), new Position(2, 0), new Position(3, 0)], route.Steps);
        Assert.Equal(6, route.Cost);
        Assert.Equal(2, route.Reachable(5));
    }

    [Fact]
    public void PlanRoute_ThroughEnemy_IsEmpty()
    {
        Assert.True(CreateSession(guarded: true).PlanRoute(new Position(3, 0)).IsEmpty);
    }

    [Fact]
    public void PlanRoute_ToEnemy_EndsOnIt()
    {
        var route = CreateSession(guarded: true).PlanRoute(new Position(2, 0));

        Assert.Equal(new Position(2, 0), route.Steps[^1]);
    }

    [Fact]
    public void DirectionTo_AdjacentAndFar()
    {
        var origin = new Position(1, 1);

        Assert.Equal(Direction.SouthWest, origin.DirectionTo(new Position(0, 2)));
        Assert.Null(origin.DirectionTo(new Position(3, 1)));
    }
}
