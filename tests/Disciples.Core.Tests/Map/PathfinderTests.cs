using Disciples.Core.Map;
using static Disciples.Core.Tests.TestUnits;

namespace Disciples.Core.Tests.Map;

public class PathfinderTests
{
    /// <summary>Rows: '.' plains (2), '=' road (1), '~' water.</summary>
    private static WorldMap MapOf(params string[] rows)
    {
        var tiles = new Terrain[rows[0].Length, rows.Length];
        for (var y = 0; y < rows.Length; y++)
        for (var x = 0; x < rows[0].Length; x++)
            tiles[x, y] = rows[y][x] switch { '=' => Road, '~' => Water, _ => Plains };
        return new WorldMap("Test", tiles, []);
    }

    [Fact]
    public void FindPath_PrefersCheaperTerrain()
    {
        var map = MapOf(
            "....",
            "====");

        var path = Pathfinder.FindPath(map, new Position(0, 0), p => p == new Position(3, 0), _ => false);

        Assert.Equal([new(1, 1), new(2, 1), new(3, 0)], path);
    }

    [Fact]
    public void FindPath_GoesAroundWaterAndBlockedTiles()
    {
        var map = MapOf(
            ".~.",
            ".~.",
            "...");
        var blocked = new Position(1, 2);

        var path = Pathfinder.FindPath(map, new Position(0, 0), p => p == new Position(2, 0), p => p == blocked);

        Assert.Empty(path);
    }

    [Fact]
    public void FindPath_ReachesBlockedGoal()
    {
        var map = MapOf("...");
        var goal = new Position(2, 0);

        var path = Pathfinder.FindPath(map, new Position(0, 0), p => p == goal, p => p == goal);

        Assert.Equal([new(1, 0), goal], path);
    }
}
