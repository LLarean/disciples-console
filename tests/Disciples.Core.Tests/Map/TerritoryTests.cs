using Disciples.Core.Cities;
using Disciples.Core.Content;
using Disciples.Core.Map;
using Disciples.Core.Session;
using Disciples.Core.Squads;
using Disciples.Core.Units;
using static Disciples.Core.Tests.TestUnits;

namespace Disciples.Core.Tests.Map;

public class TerritoryTests
{
    private static GameSession CreateSession(Site mine)
    {
        var tiles = new Terrain[12, 1];
        for (var x = 0; x < 12; x++)
            tiles[x, 0] = Plains;

        City[] cities =
        [
            new("Capital", new Position(0, 0), true, 0),
            new("Hold", new Position(8, 0), false, 0, owner: Owner.Enemy)
        ];
        var content = new GameContent([Knight], [Plains], [], new GameRules { CapitalTerritoryRadius = 5, CityTerritoryRadius = 2 });
        var map = new WorldMap("Test", tiles, cities, sites: [mine]);
        return new GameSession(content, map, new Party(new Unit(Knight), new Position(0, 0)), 0, new FixedRandom());
    }

    [Theory]
    [InlineData(5, Owner.Player)]
    [InlineData(6, Owner.Enemy)]
    [InlineData(11, Owner.Neutral)]
    public void OwnerAt_NearestCoveringCityWins(int x, Owner expected)
    {
        var session = CreateSession(new Site("Mine", SiteKind.Mine, new Position(11, 0), 10));

        Assert.Equal(expected, session.Territory.OwnerAt(new Position(x, 0)));
    }

    [Fact]
    public void EndTurn_MineOnOwnLand_IsClaimedAndPays()
    {
        var mine = new Site("Mine", SiteKind.Mine, new Position(3, 0), 10);
        var session = CreateSession(mine);

        session.EndTurn();

        Assert.Equal(Owner.Player, mine.Owner);
        Assert.Equal(10, session.Gold);
    }

    [Fact]
    public void EndTurn_PlayerMineOnEnemyLand_IsLost()
    {
        var mine = new Site("Mine", SiteKind.Mine, new Position(9, 0), 10, owner: Owner.Player);
        var session = CreateSession(mine);

        session.EndTurn();

        Assert.Equal(Owner.Enemy, mine.Owner);
        Assert.Equal(0, session.Gold);
    }
}
