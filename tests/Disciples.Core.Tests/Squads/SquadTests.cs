using Disciples.Core.Squads;
using Disciples.Core.Units;
using static Disciples.Core.Tests.TestUnits;

namespace Disciples.Core.Tests.Squads;

public class SquadTests
{
    private static readonly SquadSlot Front0 = new(SquadLine.Front, 0);
    private static readonly SquadSlot Back0 = new(SquadLine.Back, 0);
    private static readonly SquadSlot Front1 = new(SquadLine.Front, 1);
    private static readonly SquadSlot Back1 = new(SquadLine.Back, 1);

    [Fact]
    public void TryPlace_LargeUnit_OccupiesBothLinesOfColumn()
    {
        var squad = new Squad();
        var ogre = new Unit(Ogre);

        Assert.True(squad.TryPlace(ogre, Back0));

        Assert.Same(ogre, squad.UnitAt(Front0));
        Assert.Same(ogre, squad.UnitAt(Back0));
        Assert.Equal(2, squad.UsedSlots);
        Assert.Single(squad.Units);
    }

    [Fact]
    public void TryPlace_LargeUnitIntoHalfOccupiedColumn_Fails()
    {
        var squad = new Squad();
        squad.TryPlace(new Unit(Archer), Back0);

        Assert.False(squad.TryPlace(new Unit(Ogre), Front0));
    }

    [Fact]
    public void TryPlace_BeyondCapacity_Fails()
    {
        var squad = new Squad(capacity: 2);
        squad.TryPlace(new Unit(Knight), Front0);
        squad.TryPlace(new Unit(Squire), Front1);

        Assert.False(squad.TryPlace(new Unit(Archer), Back0));
    }

    [Fact]
    public void TryAdd_PrefersFrontForMeleeAndBackForRanged()
    {
        var squad = new Squad();
        var squire = new Unit(Squire);
        var archer = new Unit(Archer);

        squad.TryAdd(squire);
        squad.TryAdd(archer);

        Assert.Equal(SquadLine.Front, squad.SlotOf(squire).Line);
        Assert.Equal(SquadLine.Back, squad.SlotOf(archer).Line);
    }

    [Fact]
    public void RemoveDead_ClearsDeadUnits()
    {
        var squad = new Squad();
        var ogre = new Unit(Ogre);
        squad.TryPlace(ogre, Front0);
        ogre.TakeDamage(1000);

        squad.RemoveDead();

        Assert.Empty(squad.Units);
    }

    [Fact]
    public void Move_WithinSquad_SwapsUnits()
    {
        var squad = new Squad();
        var squire = new Unit(Squire);
        var archer = new Unit(Archer);
        squad.TryPlace(squire, Front0);
        squad.TryPlace(archer, Back0);

        Assert.True(SquadTransfer.Move(squad, Front0, squad, Back0));

        Assert.Same(archer, squad.UnitAt(Front0));
        Assert.Same(squire, squad.UnitAt(Back0));
    }

    [Fact]
    public void Move_LargeUnitToOtherColumn_SwapsWholeColumn()
    {
        var squad = new Squad();
        var ogre = new Unit(Ogre);
        var squire = new Unit(Squire);
        squad.TryPlace(ogre, Front0);
        squad.TryPlace(squire, Front1);

        Assert.True(SquadTransfer.Move(squad, Back0, squad, Front1));

        Assert.Same(ogre, squad.UnitAt(Front1));
        Assert.Same(ogre, squad.UnitAt(Back1));
        Assert.Same(squire, squad.UnitAt(Front0));
    }

    [Fact]
    public void Move_LargeUnitIntoColumnWithTwoUnits_FailsAndKeepsState()
    {
        var squad = new Squad();
        var ogre = new Unit(Ogre);
        var squire = new Unit(Squire);
        var archer = new Unit(Archer);
        squad.TryPlace(ogre, Front0);
        squad.TryPlace(squire, Front1);
        squad.TryPlace(archer, Back1);

        Assert.False(SquadTransfer.Move(squad, Front0, squad, Front1));

        Assert.Same(ogre, squad.UnitAt(Back0));
        Assert.Same(squire, squad.UnitAt(Front1));
        Assert.Same(archer, squad.UnitAt(Back1));
    }

    [Fact]
    public void Move_BetweenSquads_RespectsCapacity()
    {
        var party = new Squad(capacity: 2);
        var garrison = new Squad();
        party.TryPlace(new Unit(Knight), Front0);
        party.TryPlace(new Unit(Squire), Front1);
        garrison.TryPlace(new Unit(Archer), Back0);

        Assert.False(SquadTransfer.Move(garrison, Back0, party, Back1));
        Assert.True(SquadTransfer.Move(garrison, Back0, party, Front1));

        Assert.Equal("Archer", party.UnitAt(Front1)?.Name);
        Assert.Equal("Squire", garrison.UnitAt(Back0)?.Name);
    }

    [Fact]
    public void Move_LeaderToOtherSquad_Fails()
    {
        var party = new Squad();
        var garrison = new Squad();
        party.TryPlace(new Unit(Knight), Front0);

        Assert.False(SquadTransfer.Move(party, Front0, garrison, Front0));
        Assert.NotNull(party.Leader);
    }
}
