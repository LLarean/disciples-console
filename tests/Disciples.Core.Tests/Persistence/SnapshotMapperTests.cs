using Disciples.Core.Cities;
using Disciples.Core.Content;
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
        Parties =
        [
            new PartySnapshot
            {
                X = 0,
                Y = 0,
                Units =
                [
                    new UnitSnapshot { Id = "knight", Line = SquadLine.Front, Column = 1 },
                    new UnitSnapshot { Id = "recruit", Line = SquadLine.Back, Column = 0, Level = 2, Experience = 5, Hp = 10 }
                ]
            }
        ],
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
    public void Capture_UsesTerrainSymbols_FallingBackToTheIdLetter()
    {
        var map = SnapshotMapper.Capture(Restore(Scenario())).Map;

        Assert.Equal(["..=", ".w=", "..="], map.Rows);
        Assert.Equal("water", map.Legend["w"]);
    }

    [Fact]
    public void CaptureThenRestore_KeepsEnemyLeaders()
    {
        var snapshot = Scenario();
        snapshot.Enemies.Add(new PartySnapshot
        {
            X = 0, Y = 2, MovementPoints = 3, Perks = [LeaderPerk.Movement],
            Units = [new UnitSnapshot { Id = "knight", Line = SquadLine.Front, Column = 1 }]
        });
        snapshot.Neutrals.Clear();

        var restored = Restore(SnapshotMapper.Capture(Restore(snapshot)));

        var enemy = restored.Map.Enemies.Single();
        Assert.Equal((new Position(0, 2), 3, 14), (enemy.Position, enemy.MovementPoints, enemy.MaxMovementPoints));
        Assert.Same(Knight, enemy.Leader.Definition);
    }

    [Fact]
    public void CaptureThenRestore_KeepsEveryPartyAndTheActiveOne()
    {
        var snapshot = Scenario();
        snapshot.Parties.Add(new PartySnapshot
        {
            X = 0, Y = 1, MovementPoints = 4,
            Units = [new UnitSnapshot { Id = "knight", Line = SquadLine.Front, Column = 0 }]
        });
        snapshot.Active = 1;

        var restored = Restore(SnapshotMapper.Capture(Restore(snapshot)));

        Assert.Equal([new Position(0, 0), new Position(0, 1)], restored.Parties.Select(p => p.Position));
        Assert.Same(restored.Parties[1], restored.Party);
        Assert.Equal(4, restored.Party.MovementPoints);
    }

    [Fact]
    public void Restore_VersionOneSave_TurnsTheSinglePartyIntoTheList()
    {
        var snapshot = Scenario();
        snapshot.Version = 1;
        snapshot.Party = snapshot.Parties[0];
        snapshot.Parties.Clear();

        var session = Restore(snapshot);

        Assert.Same(Knight, session.Parties.Single().Leader.Definition);
        Assert.Equal(SnapshotMigrator.Default.CurrentVersion, snapshot.Version);
    }

    [Fact]
    public void Restore_NoParty_Throws()
    {
        var snapshot = Scenario();
        snapshot.Parties.Clear();

        Assert.Throws<ContentException>(() => Restore(snapshot));
    }

    [Fact]
    public void Restore_ResolvesRetiredIds_AndCaptureWritesCurrentOnes()
    {
        var barracks = new Building("barracks", "Barracks", "", 50, "", null);
        var aliases = new ContentAliases
        {
            Units = { ["footman"] = "squire" },
            Terrains = { ["sea"] = "water" },
            Buildings = { ["camp"] = "barracks" }
        };
        var content = new GameContent([Knight, Squire, Recruit, Veteran, Ogre], [Plains, Road, Water], [barracks], new GameRules(), aliases);
        var snapshot = Scenario();
        snapshot.Map.Legend["~"] = "sea";
        snapshot.Neutrals[0].Units[0].Id = "footman";
        snapshot.Cities[0].Recruits = ["footman"];
        snapshot.Cities[0].Built = ["camp"];

        var session = SnapshotMapper.Restore(snapshot, content, new FixedRandom());
        var saved = SnapshotMapper.Capture(session);

        Assert.True(session.Map.Cities[0].HasBuilt("barracks"));
        Assert.Same(Water, session.Map.TerrainAt(new Position(1, 1)));
        Assert.Equal("squire", saved.Neutrals[0].Units[0].Id);
        Assert.Equal(["squire"], saved.Cities[0].Recruits);
        Assert.Equal(["barracks"], saved.Cities[0].Built);
    }

    [Fact]
    public void Content_AliasToUnknownId_Throws()
    {
        var aliases = new ContentAliases { Units = { ["footman"] = "dragon" } };

        Assert.Throws<ContentException>(() => new GameContent([Knight], [Plains], [], new GameRules(), aliases));
    }

    [Fact]
    public void Restore_UnknownUnit_Throws()
    {
        var snapshot = Scenario();
        snapshot.Neutrals[0].Units[0].Id = "dragon";

        Assert.Throws<Disciples.Core.Content.ContentException>(() => Restore(snapshot));
    }
}
