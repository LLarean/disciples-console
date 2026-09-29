using Disciples.Core.Cities;
using Disciples.Core.Map;
using Disciples.Core.Persistence;
using Disciples.Core.Session;
using Disciples.Core.Squads;
using Disciples.Core.Units;
using static Disciples.Core.Tests.TestUnits;

namespace Disciples.Core.Tests.Persistence;

public class SnapshotMapperTests
{
    private static GameSnapshot Scenario() => new()
    {
        Map = new MapSnapshot
        {
            Name = "Test",
            Legend = new() { ["."] = "plains", ["="] = "road", ["~"] = "water" },
            Rows = ["..=", ".~=", "..="]
        },
        Gold = 200,
        Party = new PartySnapshot
        {
            X = 0,
            Y = 0,
            MaxMovementPoints = 10,
            Units =
            [
                new UnitSnapshot { Id = "knight", Line = SquadLine.Front, Column = 1 },
                new UnitSnapshot { Id = "recruit", Line = SquadLine.Back, Column = 0, Level = 2, Experience = 5, Hp = 10 }
            ]
        },
        Cities =
        [
            new CitySnapshot { Name = "Home", X = 2, Y = 0, Capital = true, Owner = Owner.Player, Income = 50, Recruits = ["squire"] },
            new CitySnapshot
            {
                Name = "Keep", X = 2, Y = 2, Owner = Owner.Neutral, Income = 20,
                Garrison = [new UnitSnapshot { Id = "ogre", Line = SquadLine.Front, Column = 2 }]
            }
        ],
        Neutrals =
        [
            new NeutralSnapshot { Name = "Bandits", X = 0, Y = 2, Reward = 40, Units = [new UnitSnapshot { Id = "squire", Line = SquadLine.Front, Column = 0 }] }
        ]
    };

    private static GameSession Restore(GameSnapshot snapshot) => SnapshotMapper.Restore(snapshot, TestContent, new FixedRandom());

    [Fact]
    public void Restore_BuildsSessionFromScenario()
    {
        var session = Restore(Scenario());

        Assert.Equal(new Position(0, 0), session.Party.Position);
        Assert.Same(Knight, session.Party.Leader.Definition);
        Assert.Equal(Knight.Leadership, session.Party.Squad.Capacity);
        Assert.Same(Water, session.Map.TerrainAt(new Position(1, 1)));
        Assert.Equal(Owner.Neutral, session.Map.Cities[1].Owner);
        Assert.True(session.Map.Cities[1].Garrison.Units.Single().IsLarge);

        var recruit = session.Party.Squad.UnitAt(new SquadSlot(SquadLine.Back, 0))!;
        Assert.Equal((2, 5, 10), (recruit.Level, recruit.Experience, recruit.Hp));
    }

    [Fact]
    public void CaptureThenRestore_PreservesProgress()
    {
        var session = Restore(Scenario());
        session.TryMove(Direction.East);
        session.Build(session.Map.Cities[0], new Building("barracks", "Barracks", "", 50, "", null));
        session.EndTurn();

        var restored = Restore(SnapshotMapper.Capture(session));

        Assert.Equal(session.Turn, restored.Turn);
        Assert.Equal(session.Gold, restored.Gold);
        Assert.Equal(session.Party.Position, restored.Party.Position);
        Assert.True(restored.Map.Cities[0].HasBuilt("barracks"));
        Assert.Equal(
            SnapshotMapper.Capture(session).Map.Rows,
            SnapshotMapper.Capture(restored).Map.Rows);
        Assert.Equal(
            session.Party.Squad.Units.Select(u => (u.Definition.Id, u.Level, u.Experience, u.Hp)),
            restored.Party.Squad.Units.Select(u => (u.Definition.Id, u.Level, u.Experience, u.Hp)));
    }

    [Fact]
    public void Restore_UnknownUnit_Throws()
    {
        var snapshot = Scenario();
        snapshot.Neutrals[0].Units[0].Id = "dragon";

        Assert.Throws<Disciples.Core.Content.ContentException>(() => Restore(snapshot));
    }
}
