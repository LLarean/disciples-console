using Disciples.Core.Battles;
using Disciples.Core.Content;
using Disciples.Core.Squads;
using Disciples.Core.Units;
using static Disciples.Core.Tests.TestUnits;

namespace Disciples.Core.Tests.Battles;

public class AttackSourceTests
{
    private static readonly UnitDefinition Golem = new("golem", "Golem", 100, 0, 1, 10, 80, AttackType.Melee, UnitSize.Small, 0,
        immunities: [AttackSource.Weapon]);

    private static readonly UnitDefinition Warded = new("warded", "Warded", 100, 0, 1, 10, 80, AttackType.Melee, UnitSize.Small, 0,
        wards: [AttackSource.Weapon]);

    private static Squad SquadOf(params UnitDefinition[] definitions)
    {
        var squad = new Squad();
        for (var i = 0; i < definitions.Length; i++)
            squad.TryPlace(new Unit(definitions[i]), new SquadSlot(SquadLine.Front, i));
        return squad;
    }

    [Fact]
    public void Strike_ImmuneTarget_TakesNoDamage()
    {
        var defenders = SquadOf(Golem);
        var battle = new Battle(SquadOf(Squire), defenders, new FixedRandom());

        battle.Act(defenders.Units[0]);

        Assert.Equal(100, defenders.Units[0].Hp);
        Assert.Contains(battle.Log, e => e.Kind == BattleEventKind.Immune);
    }

    [Fact]
    public void Strike_Ward_AbsorbsOnlyFirstHit()
    {
        var defenders = SquadOf(Warded);
        var battle = new Battle(SquadOf(Squire, Squire), defenders, new FixedRandom());
        var target = defenders.Units[0];

        battle.Act(target);
        Assert.Equal(100, target.Hp);
        Assert.Contains(battle.Log, e => e.Kind == BattleEventKind.Warded);

        battle.Act(target);
        Assert.True(target.Hp < 100);
    }

    [Fact]
    public void SimpleAi_MutuallyImmune_BattleEndsAfterRoundLimit()
    {
        var battle = new Battle(SquadOf(Golem), SquadOf(Golem), new FixedRandom(), new GameRules { MaxBattleRounds = 3 });
        var ai = new SimpleBattleAi(new FixedRandom());

        while (!battle.IsOver)
            ai.Act(battle);

        Assert.Equal(BattleOutcome.Retreat, battle.Outcome);
        Assert.Equal(3, battle.Round);
    }
}
