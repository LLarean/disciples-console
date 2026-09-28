using Disciples.Core.Battles;
using Disciples.Core.Squads;
using Disciples.Core.Units;
using static Disciples.Core.Tests.TestUnits;

namespace Disciples.Core.Tests.Battles;

public class BattleTests
{
    private static Squad SquadOf(params (UnitDefinition definition, SquadLine line, int column)[] units)
    {
        var squad = new Squad();
        foreach (var (definition, line, column) in units)
            squad.TryPlace(new Unit(definition), new SquadSlot(line, column));
        return squad;
    }

    [Fact]
    public void Queue_IsOrderedByInitiative()
    {
        var battle = new Battle(
            SquadOf((Squire, SquadLine.Front, 0), (Archer, SquadLine.Back, 0)),
            SquadOf((Acolyte, SquadLine.Back, 0)),
            new FixedRandom());

        Assert.Equal(["Archer", "Squire", "Acolyte"], battle.Queue.Select(u => u.Name));
    }

    [Fact]
    public void Melee_TargetsOnlyEnemyFrontLineWhileItHasLivingUnits()
    {
        var enemies = SquadOf((Squire, SquadLine.Front, 0), (Archer, SquadLine.Back, 1));
        var squire = new Unit(Squire);
        var own = new Squad();
        own.TryPlace(squire, new SquadSlot(SquadLine.Front, 0));

        Assert.Equal(["Squire"], TargetRules.ValidTargets(squire, own, enemies).Select(u => u.Name));

        enemies.Units[0].TakeDamage(1000);
        Assert.Equal(["Archer"], TargetRules.ValidTargets(squire, own, enemies).Select(u => u.Name));
    }

    [Fact]
    public void Melee_FromBackLine_IsBlockedByOwnFrontLine()
    {
        var backSquire = new Unit(Squire);
        var own = SquadOf((Squire, SquadLine.Front, 0));
        own.TryPlace(backSquire, new SquadSlot(SquadLine.Back, 2));

        Assert.Empty(TargetRules.ValidTargets(backSquire, own, SquadOf((Squire, SquadLine.Front, 0))));
    }

    [Fact]
    public void Ranged_ReachesBackLine()
    {
        var archer = new Unit(Archer);
        var own = new Squad();
        own.TryPlace(archer, new SquadSlot(SquadLine.Back, 0));
        var enemies = SquadOf((Squire, SquadLine.Front, 0), (Archer, SquadLine.Back, 1));

        Assert.Equal(2, TargetRules.ValidTargets(archer, own, enemies).Count);
    }

    [Fact]
    public void Act_AllEnemiesAttack_HitsEveryEnemy()
    {
        var enemies = SquadOf((Acolyte, SquadLine.Front, 0), (Acolyte, SquadLine.Front, 1));
        var battle = new Battle(SquadOf((Mage, SquadLine.Back, 0)), enemies, new FixedRandom());
        battle.Act(enemies.Units[0]);

        Assert.All(enemies.Units, u => Assert.Equal(35, u.Hp));
    }

    [Fact]
    public void Defend_HalvesIncomingDamageUntilOwnTurn()
    {
        var attackers = SquadOf((Squire, SquadLine.Front, 0));
        var defenders = SquadOf((Acolyte, SquadLine.Front, 0));
        var battle = new Battle(attackers, defenders, new FixedRandom());
        var acolyte = defenders.Units[0];

        battle.Defend();
        battle.Defend();
        battle.Act(acolyte);

        Assert.Equal(50 - 12, acolyte.Hp);
    }

    [Fact]
    public void Victory_WhenAllDefendersDie()
    {
        var defenders = SquadOf((Acolyte, SquadLine.Front, 0));
        var battle = new Battle(SquadOf((Ogre, SquadLine.Front, 0)), defenders, new FixedRandom());

        battle.Act(defenders.Units[0]);

        Assert.Equal(BattleOutcome.Victory, battle.Outcome);
        Assert.Null(battle.Current);
    }
}
