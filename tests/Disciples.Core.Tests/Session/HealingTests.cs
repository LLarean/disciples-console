using Disciples.Core.Cities;
using Disciples.Core.Content;
using Disciples.Core.Map;
using Disciples.Core.Session;
using Disciples.Core.Squads;
using Disciples.Core.Units;
using static Disciples.Core.Tests.TestUnits;

namespace Disciples.Core.Tests.Session;

public class HealingTests
{
    private static readonly Building Temple = new("temple", "Temple", "Support", 100, "", null, healBonusPercent: 25);

    private static GameSession CreateSession()
    {
        var content = new GameContent([Knight], [Plains], [Temple], new GameRules { CityHealPercent = 25 });
        var capital = new City("Capital", new Position(0, 0), true, 0, buildings: [Temple]);
        var map = new WorldMap("Test", new[,] { { Plains } }, [capital]);
        var party = new Party(new Unit(Knight, hp: 10), new Position(0, 0));
        return new GameSession(content, map, party, 100, new FixedRandom());
    }

    [Fact]
    public void EndTurn_InCity_HealsByBasePercent()
    {
        var session = CreateSession();

        session.EndTurn();

        Assert.Equal(10 + 150 * 25 / 100, session.Party.Leader.Hp);
    }

    [Fact]
    public void EndTurn_TempleInCapital_AddsHealBonus()
    {
        var session = CreateSession();
        session.Build(session.Capital!, Temple);

        session.EndTurn();

        Assert.Equal(10 + 150 * 50 / 100, session.Party.Leader.Hp);
    }
}
