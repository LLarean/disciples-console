using Disciples.Core.Content;
using Disciples.Core.Map;
using Disciples.Core.Persistence;
using Disciples.Core.Session;
using Disciples.Core.Squads;
using Disciples.Core.Units;
using static Disciples.Core.Tests.TestUnits;

namespace Disciples.Core.Tests.Map;

public class FogOfWarTests
{
    private static GameSession CreateSession()
    {
        var tiles = new Terrain[12, 1];
        for (var x = 0; x < 12; x++)
            tiles[x, 0] = Plains;

        var content = new GameContent([Knight], [Plains], [], new GameRules { SightRadius = 2 });
        return new GameSession(content, new WorldMap("Test", tiles, []), new Party(new Unit(Knight), new Position(0, 0)), 0, new FixedRandom());
    }

    [Fact]
    public void NewSession_RevealsAroundParty()
    {
        var fog = CreateSession().Fog;

        Assert.True(fog.IsExplored(new Position(2, 0)));
        Assert.False(fog.IsExplored(new Position(3, 0)));
    }

    [Fact]
    public void Move_RevealsNewTiles_AndKeepsOld()
    {
        var session = CreateSession();

        session.TryMove(Direction.East);

        Assert.True(session.Fog.IsExplored(new Position(3, 0)));
        Assert.True(session.Fog.IsExplored(new Position(0, 0)));
    }

    [Fact]
    public void Snapshot_KeepsExploredTiles()
    {
        var session = CreateSession();
        for (var i = 0; i < 4; i++)
            session.TryMove(Direction.East);

        var restored = SnapshotMapper.Restore(SnapshotMapper.Capture(session), session.Content, new FixedRandom());

        Assert.True(restored.Fog.IsExplored(new Position(0, 0)));
        Assert.False(restored.Fog.IsExplored(new Position(7, 0)));
    }
}
