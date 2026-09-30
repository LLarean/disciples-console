using Disciples.Core.Content;
using Disciples.Core.Map;
using Disciples.Core.Session;
using Disciples.Core.Squads;
using Disciples.Core.Units;
using static Disciples.Core.Tests.TestUnits;

namespace Disciples.Core.Tests.Squads;

public class PartyTests
{
    private static Party PartyAt(int level, params LeaderPerk[] perks)
    {
        var squad = new Squad();
        squad.TryPlace(new Unit(Knight, level), new SquadSlot(SquadLine.Front, 1));
        return new Party(squad, new Position(0, 0), perks: perks, rules: new GameRules { MovementPerk = 4 });
    }

    private static GameSession SessionWith(Party party) =>
        new(TestContent, new WorldMap("Test", new[,] { { Plains } }, []), party, 0, new FixedRandom());

    [Fact]
    public void NewParty_TakesMovementAndCapacityFromLeader()
    {
        var party = PartyAt(1);

        Assert.Equal(10, party.MaxMovementPoints);
        Assert.Equal(10, party.MovementPoints);
        Assert.Equal(4, party.Squad.Capacity);
        Assert.Equal(0, party.UnspentPerks);
    }

    [Fact]
    public void UnspentPerks_OnePerLevelAboveFirst()
    {
        Assert.Equal(2, PartyAt(3).UnspentPerks);
    }

    [Fact]
    public void Take_Leadership_AddsSquadSlot()
    {
        var party = PartyAt(2);

        Assert.True(SessionWith(party).TakePerk(LeaderPerk.Leadership));

        Assert.Equal(5, party.Squad.Capacity);
        Assert.Equal(0, party.UnspentPerks);
    }

    [Fact]
    public void Take_Movement_RaisesMaximumAndCurrentPoints()
    {
        var party = PartyAt(2);

        Assert.True(SessionWith(party).TakePerk(LeaderPerk.Movement));

        Assert.Equal(14, party.MaxMovementPoints);
        Assert.Equal(14, party.MovementPoints);
    }

    [Fact]
    public void CanTake_LeadershipAtMaxSlots_IsFalse()
    {
        var party = PartyAt(4, LeaderPerk.Leadership, LeaderPerk.Leadership);

        Assert.False(party.CanTake(LeaderPerk.Leadership));
        Assert.True(party.CanTake(LeaderPerk.Movement));
    }

    [Fact]
    public void TakePerk_WithoutUnspentPerks_Fails()
    {
        var party = PartyAt(2, LeaderPerk.Movement);

        Assert.False(SessionWith(party).TakePerk(LeaderPerk.Movement));
        Assert.Equal(14, party.MaxMovementPoints);
    }
}
