using Disciples.Core.Battles;
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
    public void Take_Might_StrengthensOnlyTheLeader()
    {
        var party = PartyAt(2);
        var squire = new Unit(Squire);
        party.Squad.TryAdd(squire);

        Assert.True(SessionWith(party).TakePerk(LeaderPerk.Might));

        Assert.Equal(25, party.BonusFor(party.Leader).PowerPercent);
        Assert.True(party.BonusFor(squire).IsEmpty);
    }

    [Fact]
    public void CanTake_AbilityAlreadyTaken_IsFalse()
    {
        var party = PartyAt(3, LeaderPerk.NaturalArmor);

        Assert.False(party.CanTake(LeaderPerk.NaturalArmor));
        Assert.True(party.CanTake(LeaderPerk.FirstStrike));
        Assert.Equal(20, party.BonusFor(party.Leader).Armor);
    }

    [Fact]
    public void EndTurn_NaturalHealing_RestoresTheLeaderInTheField()
    {
        var healer = PartyAt(2, LeaderPerk.NaturalHealing);
        var plain = PartyAt(1);
        healer.Leader.TakeDamage(100);
        plain.Leader.TakeDamage(100);

        SessionWith(healer).EndTurn();
        SessionWith(plain).EndTurn();

        Assert.Equal(healer.Leader.MaxHp - 100 + healer.Leader.MaxHp * 15 / 100, healer.Leader.Hp);
        Assert.Equal(plain.Leader.MaxHp - 100, plain.Leader.Hp);
    }

    [Fact]
    public void FinishBattle_WeaponMaster_AddsExperience()
    {
        var party = PartyAt(2, LeaderPerk.WeaponMaster);
        var band = new Squad();
        band.TryAdd(new Unit(Recruit));
        var neutral = new NeutralSquad("Band", new Position(1, 0), band, 0);
        var map = new WorldMap("Test", new[,] { { Plains }, { Plains } }, [], [neutral]);
        var session = new GameSession(TestContent, map, party, 0, new FixedRandom());
        var encounter = session.EncounterAt(neutral.Position)!;

        var battle = session.StartBattle(encounter);
        var ai = new SimpleBattleAi(session.Random);
        while (!battle.IsOver)
            ai.Act(battle);
        var report = session.FinishBattle(battle, encounter);

        Assert.Equal(Recruit.ExperienceValue * 125 / 100, report.Experience);
    }

    [Fact]
    public void TakePerk_WithoutUnspentPerks_Fails()
    {
        var party = PartyAt(2, LeaderPerk.Movement);

        Assert.False(SessionWith(party).TakePerk(LeaderPerk.Movement));
        Assert.Equal(14, party.MaxMovementPoints);
    }
}
