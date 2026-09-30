using Disciples.Core.Battles;
using Disciples.Core.Content;
using Disciples.Core.Squads;
using Disciples.Core.Units;

namespace Disciples.Core.Tests.Battles;

public class StatusEffectTests
{
    private static readonly GameRules Rules = new() { EffectTurns = 2, PoisonPercent = 50, DrainPercent = 50, DamageSpreadPercent = 0 };

    private static UnitDefinition Striker(AttackEffect effect) =>
        new("striker", "Striker", 100, 0, 90, 20, 100, AttackType.Melee, UnitSize.Small, 0, effect: effect);

    private static readonly UnitDefinition Dummy = new("dummy", "Dummy", 200, 0, 10, 10, 100, AttackType.Melee, UnitSize.Small, 0);

    private static (Battle Battle, Unit Striker, Unit Target) Duel(AttackEffect effect, int strikerHp = 100)
    {
        var attackers = new Squad();
        var striker = new Unit(Striker(effect), hp: strikerHp);
        attackers.TryPlace(striker, new SquadSlot(SquadLine.Front, 0));
        var defenders = new Squad();
        var target = new Unit(Dummy);
        defenders.TryPlace(target, new SquadSlot(SquadLine.Front, 0));
        return (new Battle(attackers, defenders, new FixedRandom(), Rules), striker, target);
    }

    [Fact]
    public void Drain_RestoresPartOfDamage()
    {
        var (battle, striker, target) = Duel(AttackEffect.Drain, strikerHp: 50);

        battle.Act(target);

        Assert.Equal(180, target.Hp);
        Assert.Equal(60, striker.Hp);
    }

    [Fact]
    public void Poison_DamagesAtTurnStart()
    {
        var (battle, _, target) = Duel(AttackEffect.Poison);

        battle.Act(target);

        Assert.Equal(170, target.Hp);
        Assert.Same(target, battle.Current);
        Assert.Equal(AttackEffect.Poison, battle.EffectOn(target));
    }

    [Fact]
    public void Paralysis_SkipsTargetTurns()
    {
        var (battle, striker, target) = Duel(AttackEffect.Paralysis);

        battle.Act(target);

        Assert.Same(striker, battle.Current);
        Assert.Equal(2, battle.Round);
        Assert.Contains(battle.Log, e => e.Kind == BattleEventKind.TurnLost && e.Actor == target);
    }

    [Fact]
    public void Petrification_BlocksDamage()
    {
        var (battle, _, target) = Duel(AttackEffect.Petrification);

        battle.Act(target);
        battle.Act(target);

        Assert.Equal(180, target.Hp);
        Assert.Contains(battle.Log, e => e.Kind == BattleEventKind.Immune);
    }

    [Fact]
    public void Paralysis_WearsOffAfterEffectTurns()
    {
        var (battle, _, target) = Duel(AttackEffect.Paralysis);

        battle.Act(target);
        battle.Defend();
        battle.Defend();

        Assert.Same(target, battle.Current);
        Assert.Equal(AttackEffect.None, battle.EffectOn(target));
    }
}
