using Disciples.Core.Battles;
using Disciples.Core.Map;
using Disciples.Core.Session;
using Disciples.Core.Squads;
using Disciples.Core.Units;
using static Disciples.Core.Tests.TestUnits;

namespace Disciples.Core.Tests.Session;

public class ResolveBattleTests
{
    private static readonly Position Lair = new(1, 0);
    private static readonly UnitDefinition Screamer =
        new("screamer", "Screamer", 40, 0, 90, 1, 100, AttackType.Ranged, UnitSize.Small, 0, experienceValue: 40, effect: AttackEffect.Fear);

    private static (GameSession Session, Encounter Encounter) SessionAgainst(UnitDefinition defender, params UnitDefinition[] followers)
    {
        var band = new Squad();
        band.TryAdd(new Unit(defender));
        var neutral = new NeutralSquad("Band", Lair, band, 50);
        var map = new WorldMap("Test", new[,] { { Plains }, { Plains } }, [], [neutral]);
        var party = new Party(new Unit(Knight), new Position(0, 0));
        foreach (var follower in followers)
            party.Squad.TryAdd(new Unit(follower));

        var session = new GameSession(TestContent, map, party, 0, new FixedRandom());
        return (session, session.EncounterAt(Lair)!);
    }

    [Fact]
    public void ResolveBattle_FightsToTheEndAndAppliesTheResult()
    {
        var (session, encounter) = SessionAgainst(Squire);

        var report = session.ResolveBattle(encounter);

        Assert.Equal(BattleOutcome.Victory, report.Outcome);
        Assert.Equal(50, session.Gold);
        Assert.Null(session.Map.NeutralAt(Lair));
    }

    [Fact]
    public void FinishBattle_UnitThatFled_GetsNoExperience()
    {
        var (session, encounter) = SessionAgainst(Screamer, Archer);
        var archer = session.Party.Squad.Units.Single(u => u.Definition == Archer);
        var screamer = encounter.Defenders.Units.Single();

        var battle = session.StartBattle(encounter);
        battle.Act(archer);
        battle.Act(screamer);
        session.FinishBattle(battle, encounter);

        Assert.True(battle.HasFled(archer));
        Assert.Equal(0, archer.Experience);
        Assert.Equal(40, session.Party.Leader.Experience);
    }
}
