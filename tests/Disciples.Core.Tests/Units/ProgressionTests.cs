using Disciples.Core.Units;
using static Disciples.Core.Tests.TestUnits;

namespace Disciples.Core.Tests.Units;

public class ProgressionTests
{
    private static IReadOnlyList<UnitProgress> Gain(Unit unit, int experience, params string[] built) =>
        Progression.Gain(unit, experience, TestContent.Unit, built.Contains);

    [Fact]
    public void Gain_ThresholdWithBuilding_UpgradesToNextTier()
    {
        var unit = new Unit(Recruit);

        var progress = Gain(unit, 60, "barracks");

        Assert.Equal(ProgressKind.Upgraded, Assert.Single(progress).Kind);
        Assert.Same(Veteran, unit.Definition);
        Assert.Equal(1, unit.Level);
        Assert.Equal(10, unit.Experience);
        Assert.Equal(unit.MaxHp, unit.Hp);
    }

    [Fact]
    public void Gain_ThresholdWithoutBuilding_CapsExperience()
    {
        var unit = new Unit(Recruit);

        var progress = Gain(unit, 80);

        Assert.Equal(ProgressKind.WaitingForBuilding, Assert.Single(progress).Kind);
        Assert.Same(Recruit, unit.Definition);
        Assert.Equal(Recruit.ExperienceToLevel, unit.Experience);
    }

    [Fact]
    public void Gain_EndOfTree_RaisesLevelAndStats()
    {
        var unit = new Unit(Veteran, hp: 1);

        var progress = Gain(unit, 100);

        Assert.Equal(ProgressKind.LeveledUp, Assert.Single(progress).Kind);
        Assert.Equal(2, unit.Level);
        Assert.Equal(88, unit.MaxHp);
        Assert.Equal(33, unit.Power);
        Assert.Equal(unit.MaxHp, unit.Hp);
    }

    [Fact]
    public void Share_SplitsBetweenLivingUnitsOnly()
    {
        var alive = new Unit(Veteran);
        var dead = new Unit(Veteran, hp: 0);

        Progression.Share([alive, dead], 40, TestContent.Unit, _ => false);

        Assert.Equal(40, alive.Experience);
        Assert.Equal(0, dead.Experience);
    }
}
