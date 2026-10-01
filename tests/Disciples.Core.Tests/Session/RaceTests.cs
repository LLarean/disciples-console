using Disciples.Core.Cities;
using Disciples.Core.Content;
using Disciples.Core.Magic;
using Disciples.Core.Persistence;
using Disciples.Core.Session;
using Disciples.Core.Squads;
using Disciples.Core.Units;
using static Disciples.Core.Tests.TestUnits;

namespace Disciples.Core.Tests.Session;

public class RaceTests
{
    private static readonly UnitDefinition Duke =
        new("duke", "Duke", 150, 0, 50, 50, 80, AttackType.Melee, UnitSize.Small, 100, leadership: 3, movement: 10);
    private static readonly UnitDefinition Fiend = new("fiend", "Fiend", 400, 0, 50, 60, 80, AttackType.Melee, UnitSize.Small, 0, guardian: true);

    private static readonly Building Barracks = new("barracks", "Barracks", "Fighter", 0, "", null, race: "empire");
    private static readonly Building Pit = new("pit", "Pit", "Fighter", 0, "", null, race: "legions");
    private static readonly Building Tower = new("tower", "Tower", "Other", 0, "", null, allowsResearch: true);

    private static readonly SpellDefinition Smite = new("smite", "Smite", SpellKind.Damage, 10, race: "empire");
    private static readonly SpellDefinition Hellfire = new("hellfire", "Hellfire", SpellKind.Damage, 10, race: "legions");
    private static readonly SpellDefinition Mending = new("mending", "Mending", SpellKind.Heal, 10);

    private static readonly Race Empire = new("empire", "Empire", ["knight"], ["squire"], "guardian", new Mana(life: 10), "Castle");
    private static readonly Race Legions = new("legions", "Legions", ["duke"], ["recruit", "archer"], "fiend", new Mana(infernal: 10), "Citadel");

    private static readonly GameContent Content = new(
        [Knight, Duke, Squire, Archer, Recruit, Veteran, Guardian, Fiend], [Plains], [Barracks, Pit, Tower], new GameRules(),
        spells: [Smite, Hellfire, Mending], races: [Empire, Legions]);

    private static GameSnapshot Scenario() => new()
    {
        Map = new MapSnapshot { Name = "Test", Legend = new() { ["."] = "plains" }, Rows = ["..."] },
        Gold = 200,
        Parties = [new PartySnapshot { X = 0, Y = 0, Units = [new UnitSnapshot { Id = "knight", Line = SquadLine.Front, Column = 1 }] }],
        Cities =
        [
            new CitySnapshot
            {
                Name = "Castle", X = 1, Y = 0, Capital = true, Owner = Owner.Player, Recruits = ["squire"], Buildings = true,
                Mana = new Mana(life: 10), Garrison = [new UnitSnapshot { Id = "guardian", Line = SquadLine.Back, Column = 1 }]
            },
            new CitySnapshot { Name = "Village", X = 2, Y = 0, Owner = Owner.Neutral, Recruits = ["squire"] }
        ]
    };

    private static GameSession Start(Race race, UnitDefinition leader)
    {
        var snapshot = Scenario();
        SnapshotMapper.StartAs(snapshot, race, leader, Content);
        return SnapshotMapper.Restore(snapshot, Content, new FixedRandom());
    }

    [Fact]
    public void Content_OffersARaceItsOwnAndTheCommonBuildingsAndSpells()
    {
        Assert.Equal([Pit, Tower], Content.BuildingsOf(Legions));
        Assert.Equal([Hellfire, Mending], Content.SpellsOf(Legions));
        Assert.Equal([Duke], Content.LeadersOf(Legions));
        Assert.Same(Empire, Content.DefaultRace);
    }

    [Fact]
    public void Content_RaceWithAnUnknownLeader_IsRejected()
    {
        var broken = new Race("broken", "Broken", ["squire"]);

        Assert.Throws<ContentException>(() => new GameContent([Squire], [Plains], [], new GameRules(), races: [broken]));
    }

    [Fact]
    public void StartAs_TurnsTheCapitalIntoTheRaceCapital()
    {
        var session = Start(Legions, Duke);
        var capital = session.Capital!;

        Assert.Same(Legions, session.Race);
        Assert.Same(Duke, session.Party.Leader.Definition);
        Assert.Equal("Citadel", capital.Name);
        Assert.Equal([Recruit, Archer], capital.Recruits);
        Assert.Equal([Pit, Tower], capital.Buildings);
        Assert.Same(Fiend, capital.Garrison.Units.Single().Definition);
        Assert.Equal(new Mana(infernal: 10), session.ManaIncome);
        Assert.Equal([Squire], session.Map.Cities[1].Recruits);
    }

    [Fact]
    public void StartAs_LeaderOfAnotherRace_IsRejected()
    {
        Assert.Throws<ContentException>(() => SnapshotMapper.StartAs(Scenario(), Legions, Knight, Content));
    }

    [Fact]
    public void Snapshot_KeepsTheRace_AndWithoutOneIsTheFirstRace()
    {
        var saved = SnapshotMapper.Capture(Start(Legions, Duke));

        Assert.Equal("legions", saved.Race);
        Assert.Same(Legions, SnapshotMapper.Restore(saved, Content, new FixedRandom()).Race);
        Assert.Same(Empire, SnapshotMapper.Restore(Scenario(), Content, new FixedRandom()).Race);
    }

    [Fact]
    public void HireLeader_OfAnotherRace_IsUnavailable()
    {
        var session = Start(Legions, Duke);

        Assert.Equal(HireResult.Unavailable, session.HireLeader(session.Capital!, Knight));
        Assert.Equal(HireResult.LeaderHired, session.HireLeader(session.Capital!, Duke));
        Assert.Equal([Duke], session.LeaderClasses);
    }

    [Fact]
    public void Research_SpellOfAnotherRace_IsUnavailable()
    {
        var session = Start(Legions, Duke);
        session.Build(session.Capital!, Tower);

        Assert.Equal(ResearchResult.Unavailable, session.Research(Smite));
        Assert.Equal(ResearchResult.Researched, session.Research(Hellfire));
        Assert.Equal([Hellfire, Mending], session.Spells);
    }
}
