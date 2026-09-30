using Disciples.Core.Cities;
using Disciples.Core.Map;
using Disciples.Core.Session;
using Disciples.Core.Squads;
using Disciples.Core.Units;
using static Disciples.Core.Tests.TestUnits;

namespace Disciples.Core.Tests.Session;

public class RetreatTests
{
    [Fact]
    public void FinishBattle_Retreat_SpendsRemainingMovement()
    {
        var squad = new Squad();
        squad.TryAdd(new Unit(Squire));
        var neutral = new NeutralSquad("Bandits", new Position(1, 0), squad, 50);
        var map = new WorldMap("Test", new[,] { { Plains }, { Plains }, { Plains } }, [new City("Capital", new Position(2, 0), true, 10)], [neutral]);
        var session = new GameSession(TestContent, map, new Party(new Unit(Knight), new Position(0, 0)), 0, new FixedRandom());

        session.TryMove(Direction.East);
        var encounter = session.EncounterAt(new Position(1, 0))!;
        var battle = session.StartBattle(encounter);
        battle.Retreat();
        var report = session.FinishBattle(battle, encounter);

        Assert.Equal(0, session.Party.MovementPoints);
        Assert.Equal(0, report.Gold);
        Assert.Same(neutral, map.NeutralAt(new Position(1, 0)));
    }
}
