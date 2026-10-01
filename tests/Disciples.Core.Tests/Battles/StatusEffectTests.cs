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

    private static readonly UnitDefinition Armored = new("armored", "Armored", 200, 50, 10, 40, 100, AttackType.Melee, UnitSize.Small, 0);
    private static readonly UnitDefinition Warden = new("warden", "Warden", 200, 0, 10, 10, 100, AttackType.Melee, UnitSize.Small, 0, guardian: true);

    private static (Battle Battle, Unit Striker, Unit Target) Duel(AttackEffect effect, int strikerHp = 100, UnitDefinition? victim = null) =>
        Fight(new Unit(Striker(effect), hp: strikerHp), new Unit(victim ?? Dummy));

    private static (Battle Battle, Unit Attacker, Unit Defender) Fight(Unit attacker, Unit defender, Unit? secondDefender = null)
    {
        var attackers = new Squad();
        attackers.TryPlace(attacker, new SquadSlot(SquadLine.Front, 0));
        var defenders = new Squad();
        defenders.TryPlace(defender, new SquadSlot(SquadLine.Front, 0));
        if (secondDefender != null)
            defenders.TryPlace(secondDefender, new SquadSlot(SquadLine.Front, 1));
        return (new Battle(attackers, defenders, new FixedRandom(), Rules), attacker, defender);
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
    public void Polymorph_CutsPowerAndArmor()
    {
        var (battle, striker, target) = Duel(AttackEffect.Polymorph, victim: Armored);

        battle.Act(target);
        Assert.Equal(190, target.Hp);
        Assert.Equal(10, battle.PowerOf(target));

        battle.Act(striker);
        battle.Act(target);

        Assert.Equal(90, striker.Hp);
        Assert.Equal(170, target.Hp);
    }

    [Fact]
    public void Polymorph_WearsOffAfterEffectTurns()
    {
        var (battle, _, target) = Duel(AttackEffect.Polymorph);

        battle.Act(target);
        for (var i = 0; i < 3; i++)
            battle.Defend();
        Assert.Equal(AttackEffect.Polymorph, battle.EffectOn(target));

        battle.Defend();

        Assert.Same(target, battle.Current);
        Assert.Equal(AttackEffect.None, battle.EffectOn(target));
    }

    [Fact]
    public void Fear_TargetLeavesTheBattle()
    {
        var other = new Unit(Dummy);
        var (battle, striker, target) = Fight(new Unit(Striker(AttackEffect.Fear)), new Unit(Dummy), other);

        battle.Act(target);

        Assert.True(battle.HasFled(target));
        Assert.True(target.IsAlive);
        Assert.Same(other, battle.Current);
        Assert.Contains(battle.Log, e => e.Kind == BattleEventKind.Fled && e.Actor == target);

        battle.Defend();

        Assert.Same(striker, battle.Current);
        Assert.Equal([other], battle.ValidTargets());
        Assert.DoesNotContain(target, battle.Queue);
    }

    [Fact]
    public void Fear_LastDefenderFlees_BattleIsWonAndTheRunawayIsGone()
    {
        var (battle, _, target) = Duel(AttackEffect.Fear);

        battle.Act(target);

        Assert.Equal(BattleOutcome.Victory, battle.Outcome);
        Assert.False(target.IsAlive);
    }

    [Fact]
    public void Fear_LastAttackerFlees_BattleEndsInRetreat()
    {
        var (battle, attacker, _) = Fight(new Unit(Dummy), new Unit(Striker(AttackEffect.Fear)));

        battle.Act(attacker);

        Assert.Equal(BattleOutcome.Retreat, battle.Outcome);
        Assert.True(attacker.IsAlive);
    }

    [Fact]
    public void Fear_Guardian_StandsItsGround()
    {
        var (battle, _, target) = Duel(AttackEffect.Fear, victim: Warden);

        battle.Act(target);

        Assert.Same(target, battle.Current);
        Assert.Equal(AttackEffect.None, battle.EffectOn(target));
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
