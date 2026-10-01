using Disciples.Core.Cities;
using Disciples.Core.Map;
using Disciples.Core.Session;
using Disciples.Core.Squads;
using Disciples.Core.Units;
using static Disciples.Core.Tests.TestUnits;

namespace Disciples.Core.Tests.Session;

public class GuardianTests
{
    private static readonly SquadSlot Centre = new(SquadLine.Front, 1);

    /// <summary>The party stands in a capital whose garrison holds a guardian in the centre and a Squire next to it.</summary>
    private static GameSession CreateSession(Owner owner = Owner.Player)
    {
        var garrison = new Squad();
        garrison.TryPlace(new Unit(Guardian), Centre);
        garrison.TryPlace(new Unit(Squire), new SquadSlot(SquadLine.Front, 0));
        var capital = new City("Capital", new Position(0, 0), true, 0, owner: owner, garrison: garrison);
        var map = new WorldMap("Test", new[,] { { Plains }, { Plains } }, [capital]);
        var position = owner == Owner.Player ? capital.Position : new Position(1, 0);
        return new GameSession(TestContent, map, new Party(new Unit(Knight), position), 100, new FixedRandom());
    }

    private static Squad GarrisonOf(GameSession session) => session.Map.Cities.Single().Garrison;

    [Fact]
    public void Dismiss_Guardian_IsRefused()
    {
        var session = CreateSession();
        var garrison = GarrisonOf(session);

        Assert.False(session.Dismiss(garrison, garrison.UnitAt(Centre)!));
        Assert.Equal(2, garrison.Units.Count);
    }

    [Fact]
    public void Move_Guardian_StaysInsideTheGarrison()
    {
        var session = CreateSession();
        var garrison = GarrisonOf(session);
        var party = session.Party.Squad;
        var leaderSlot = party.SlotOf(session.Party.Leader);
        var free = new SquadSlot(SquadLine.Back, 0);

        Assert.False(SquadTransfer.Move(garrison, Centre, party, free));
        Assert.False(SquadTransfer.Move(party, leaderSlot, garrison, Centre));
        Assert.True(SquadTransfer.Move(garrison, Centre, garrison, new SquadSlot(SquadLine.Back, 1)));
        Assert.Contains(garrison.Units, u => u.IsGuardian);
    }

    [Fact]
    public void EndTurn_RestoresTheGuardianOfAnyOwner()
    {
        var session = CreateSession(Owner.Enemy);
        var garrison = GarrisonOf(session);
        foreach (var unit in garrison.Units)
            unit.TakeDamage(60);

        session.EndTurn();

        Assert.Equal(Guardian.MaxHp, garrison.Units.Single(u => u.IsGuardian).Hp);
        Assert.Equal(Squire.MaxHp - 60 + Squire.MaxHp * 25 / 100, garrison.Units.Single(u => !u.IsGuardian).Hp);
    }

    [Fact]
    public void EncounterAt_HostileCapital_FieldsTheGuardian()
    {
        var session = CreateSession(Owner.Enemy);

        var encounter = session.EncounterAt(new Position(0, 0))!;

        Assert.Contains(encounter.Defenders.Units, u => u.IsGuardian);
    }
}
