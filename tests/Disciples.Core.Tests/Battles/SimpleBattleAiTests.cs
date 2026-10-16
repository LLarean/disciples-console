using Disciples.Core.Battles;
using Disciples.Core.Squads;
using Disciples.Core.Units;
using static Disciples.Core.Tests.TestUnits;

namespace Disciples.Core.Tests.Battles;

public class SimpleBattleAiTests
{
    private static readonly UnitDefinition Dummy = new("dummy", "Dummy", 100, 0, 1, 10, 80, AttackType.Melee, UnitSize.Small, 0);

    private static readonly UnitDefinition Paralyzer = new("paralyzer", "Paralyzer", 100, 0, 90, 20, 100, AttackType.Melee, UnitSize.Small, 0,
        effect: AttackEffect.Paralysis);

    private static Squad SquadOf(params UnitDefinition[] definitions)
    {
        var squad = new Squad();
        for (var i = 0; i < definitions.Length; i++)
            squad.TryPlace(new Unit(definitions[i]), new SquadSlot(SquadLine.Front, i));
        return squad;
    }

    private static Battle ArcherAgainst(Squad defenders) => new(SquadOf(Archer), defenders, new FixedRandom());

    [Fact]
    public void Act_FinishesOffTheEnemyOneHitKills()
    {
        var defenders = SquadOf(Squire, Acolyte);
        var acolyte = defenders.Units[1];
        acolyte.TakeDamage(30);

        new SimpleBattleAi(new FixedRandom()).Act(ArcherAgainst(defenders));

        Assert.False(acolyte.IsAlive);
        Assert.Equal(100, defenders.Units[0].Hp);
    }

    [Fact]
    public void Act_SeveralKillable_KillsTheStrongest()
    {
        var defenders = SquadOf(Acolyte, Squire);
        var (acolyte, squire) = (defenders.Units[0], defenders.Units[1]);
        acolyte.TakeDamage(30);
        squire.TakeDamage(80);

        new SimpleBattleAi(new FixedRandom()).Act(ArcherAgainst(defenders));

        Assert.False(squire.IsAlive);
        Assert.True(acolyte.IsAlive);
    }

    [Fact]
    public void Act_NobodyKillable_HitsTheEnemyClosestToDeath()
    {
        var defenders = SquadOf(Squire, Acolyte);

        new SimpleBattleAi(new FixedRandom()).Act(ArcherAgainst(defenders));

        Assert.Equal(100, defenders.Units[0].Hp);
        Assert.True(defenders.Units[1].Hp < 50);
    }

    [Fact]
    public void Act_HealerWithNobodyWounded_Defends()
    {
        var healers = SquadOf(Acolyte);
        var battle = new Battle(healers, SquadOf(Dummy), new FixedRandom());

        new SimpleBattleAi(new FixedRandom()).Act(battle);

        Assert.True(battle.IsDefending(healers.Units[0]));
    }

    [Fact]
    public void Act_EffectAttacker_PrefersAnUnaffectedEnemy()
    {
        var defenders = SquadOf(Dummy, Dummy);
        var battle = new Battle(SquadOf(Paralyzer, Paralyzer), defenders, new FixedRandom());
        battle.Act(defenders.Units[0]);

        new SimpleBattleAi(new FixedRandom()).Act(battle);

        Assert.Equal(AttackEffect.Paralysis, battle.EffectOn(defenders.Units[1]));
    }
}
