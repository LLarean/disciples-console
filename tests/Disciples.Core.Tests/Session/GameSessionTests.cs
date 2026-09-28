using Disciples.Core.Map;
using Disciples.Core.Session;
using Disciples.Core.Squads;

namespace Disciples.Core.Tests.Session;

public class GameSessionTests
{
    private static readonly Terrain Plains = new("plains", "Plains", 2);
    private static readonly Terrain Road = new("road", "Road", 1);
    private static readonly Terrain Water = new("water", "Water", null);

    private static GameSession CreateSession(Terrain east, int movementPoints = 10)
    {
        var tiles = new Terrain[3, 3];
        for (var x = 0; x < 3; x++)
        for (var y = 0; y < 3; y++)
            tiles[x, y] = Plains;
        tiles[2, 1] = east;

        var map = new WorldMap("Test", tiles, new[] { new City("Town", new Position(1, 0), false) });
        return new GameSession(map, new Leader("Knight", new Position(1, 1), movementPoints));
    }

    [Fact]
    public void TryMove_PassableTile_MovesLeaderAndSpendsCost()
    {
        var session = CreateSession(Road);

        var result = session.TryMove(Direction.East);

        Assert.Equal(MoveResult.Moved, result);
        Assert.Equal(new Position(2, 1), session.Leader.Position);
        Assert.Equal(9, session.Leader.MovementPoints);
    }

    [Fact]
    public void TryMove_ImpassableTile_KeepsLeaderInPlace()
    {
        var session = CreateSession(Water);

        Assert.Equal(MoveResult.Impassable, session.TryMove(Direction.East));
        Assert.Equal(new Position(1, 1), session.Leader.Position);
        Assert.Equal(10, session.Leader.MovementPoints);
    }

    [Fact]
    public void TryMove_BeyondEdge_ReturnsOutOfBounds()
    {
        var session = CreateSession(Plains);
        session.TryMove(Direction.West);

        Assert.Equal(MoveResult.OutOfBounds, session.TryMove(Direction.West));
        Assert.Equal(new Position(0, 1), session.Leader.Position);
    }

    [Fact]
    public void TryMove_CostAboveRemainingPoints_ReturnsNotEnoughMovement()
    {
        var session = CreateSession(Plains, movementPoints: 1);

        Assert.Equal(MoveResult.NotEnoughMovement, session.TryMove(Direction.East));
        Assert.Equal(1, session.Leader.MovementPoints);
    }

    [Fact]
    public void TryMove_Diagonal_MovesBothAxes()
    {
        var session = CreateSession(Plains);

        session.TryMove(Direction.SouthWest);

        Assert.Equal(new Position(0, 2), session.Leader.Position);
    }

    [Fact]
    public void EndTurn_RestoresMovementAndAdvancesTurn()
    {
        var session = CreateSession(Plains);
        session.TryMove(Direction.East);

        session.EndTurn();

        Assert.Equal(2, session.Turn);
        Assert.Equal(10, session.Leader.MovementPoints);
    }

    [Fact]
    public void CityAt_ReturnsCityOnItsTileOnly()
    {
        var session = CreateSession(Plains);

        Assert.Equal("Town", session.Map.CityAt(new Position(1, 0))?.Name);
        Assert.Null(session.Map.CityAt(new Position(1, 1)));
    }
}
