using Disciples.Core.Cities;
using Disciples.Core.Content;
using Disciples.Core.Magic;
using Disciples.Core.Map;
using Disciples.Core.Persistence;
using Disciples.Core.Session;
using Disciples.Core.Squads;
using Disciples.Core.Units;
using static Disciples.Core.Tests.TestUnits;

namespace Disciples.Core.Tests.Session;

public class SpellTests
{
    private static readonly Position Near = new(3, 0);
    private static readonly Position Far = new(11, 0);

    private static readonly Building Tower = new("tower", "Tower", "Other", 0, "", null, allowsResearch: true);
    private static readonly SpellDefinition Bolt = new("bolt", "Bolt", SpellKind.Damage, 50, new Mana(life: 20), new Mana(life: 10), AttackSource.Fire);
    private static readonly SpellDefinition Mend = new("mend", "Mend", SpellKind.Heal, 30, new Mana(life: 20), new Mana(runic: 5));

    private static readonly UnitDefinition Imp =
        new("imp", "Imp", 40, 0, 50, 20, 80, AttackType.Melee, UnitSize.Small, 0, immunities: [AttackSource.Fire]);
    private static readonly UnitDefinition Warded =
        new("warded", "Warded", 40, 0, 50, 20, 80, AttackType.Melee, UnitSize.Small, 0, wards: [AttackSource.Fire]);
    private static readonly UnitDefinition Armored =
        new("armored", "Armored", 100, 50, 50, 20, 80, AttackType.Melee, UnitSize.Small, 0);
    private static readonly UnitDefinition WeakLord =
        new("weak-lord", "Weak Lord", 10, 0, 1, 1, 80, AttackType.Melee, UnitSize.Small, 0, leadership: 2, movement: 4);

    private static readonly GameContent Content = new(
        [Knight, Squire, Archer, Imp, Warded, Armored, WeakLord], [Plains], [Tower], new GameRules(), spells: [Bolt, Mend]);

    /// <summary>A plains strip 12×1: capital and party on the west; the far east is unexplored.</summary>
    private static GameSession CreateSession(
        IEnumerable<NeutralSquad>? neutrals = null, IEnumerable<Party>? enemies = null, Spellbook? spellbook = null, bool tower = true, int life = 100)
    {
        var tiles = new Terrain[12, 1];
        for (var x = 0; x < 12; x++)
            tiles[x, 0] = Plains;

        var capital = new City("Capital", new Position(0, 0), true, 10, buildings: [Tower], built: tower ? [Tower.Id] : []);
        var map = new WorldMap("Test", tiles, [capital], neutrals, enemies);
        return new GameSession(
            Content, map, [new Party(new Unit(Knight), new Position(1, 0))], 0, new FixedRandom(),
            mana: new Mana(life: life, runic: 10), spellbook: spellbook ?? new Spellbook([Bolt, Mend]));
    }

    private static NeutralSquad Band(Position position, params UnitDefinition[] units)
    {
        var squad = new Squad();
        foreach (var unit in units)
            squad.TryAdd(new Unit(unit));
        return new NeutralSquad("Band", position, squad, 0);
    }

    [Fact]
    public void Research_PaysManaAndLearnsTheSpell()
    {
        var session = CreateSession(spellbook: new Spellbook());

        Assert.Equal(ResearchResult.Researched, session.Research(Bolt));

        Assert.True(session.Spellbook.Knows(Bolt));
        Assert.Equal(80, session.Mana.Life);
        Assert.Equal(ResearchResult.AlreadyKnown, session.Research(Bolt));
    }

    [Fact]
    public void Research_OneSpellPerTurn()
    {
        var session = CreateSession(spellbook: new Spellbook());
        session.Research(Bolt);

        Assert.Equal(ResearchResult.AlreadyResearched, session.Research(Mend));

        session.EndTurn();
        Assert.Equal(ResearchResult.Researched, session.Research(Mend));
    }

    [Fact]
    public void Research_WithoutTower_IsRefused()
    {
        var session = CreateSession(spellbook: new Spellbook(), tower: false);

        Assert.False(session.CanResearch);
        Assert.Equal(ResearchResult.NoTower, session.Research(Bolt));
    }

    [Fact]
    public void Research_WithoutMana_IsRefused()
    {
        var session = CreateSession(spellbook: new Spellbook(), life: 19);

        Assert.Equal(ResearchResult.NotEnoughMana, session.Research(Bolt));
        Assert.Empty(session.Spellbook.Known);
    }

    [Fact]
    public void Cast_Damage_HitsEveryUnitIgnoringArmor()
    {
        var band = Band(Near, Armored, Archer);
        var session = CreateSession([band]);

        Assert.Equal(CastResult.Cast, session.Cast(Bolt, Near));

        var survivor = Assert.Single(band.Squad.Units);
        Assert.Equal(Armored.MaxHp - 50, survivor.Hp);
        Assert.Equal(90, session.Mana.Life);
        var cast = session.TakeEvents().Single(e => e.Kind == GameEventKind.SpellCast);
        Assert.Equal((Bolt, 50 + Archer.MaxHp, (Position?)Near), (cast.Spell, cast.Amount, cast.At));
    }

    [Fact]
    public void Cast_Damage_SparesImmuneAndWardedUnits()
    {
        var band = Band(Near, Imp, Warded);
        var session = CreateSession([band]);

        session.Cast(Bolt, Near);

        Assert.All(band.Squad.Units, u => Assert.Equal(u.MaxHp, u.Hp));
    }

    [Fact]
    public void Cast_WipesTheLastSquad_RemovesItAndWins()
    {
        var session = CreateSession([Band(Near, Archer, Archer)]);

        session.Cast(Bolt, Near);

        Assert.Empty(session.Map.Neutrals);
        Assert.Equal(GameStatus.Won, session.Status);
        Assert.Contains(session.TakeEvents(), e => e.Kind == GameEventKind.SquadDestroyed);
    }

    [Fact]
    public void Cast_KillsEnemyLeader_RemovesItsParty()
    {
        var enemy = new Party(new Unit(WeakLord), Near);
        enemy.Squad.TryAdd(new Unit(Armored));
        var session = CreateSession(enemies: [enemy]);

        session.Cast(Bolt, Near);

        Assert.Empty(session.Map.Enemies);
    }

    [Fact]
    public void Cast_SameSpellTwiceInATurn_IsRefused()
    {
        var session = CreateSession([Band(Near, Armored)]);
        session.Cast(Bolt, Near);

        Assert.Equal(CastResult.AlreadyCast, session.Cast(Bolt, Near));

        session.EndTurn();
        Assert.Equal(CastResult.Cast, session.Cast(Bolt, Near));
    }

    [Fact]
    public void Cast_AtUnexploredOrEmptyTile_HasNoTarget()
    {
        var session = CreateSession([Band(Far, Armored)]);

        Assert.Empty(session.SpellTargets(Bolt));
        Assert.Equal(CastResult.NoTarget, session.Cast(Bolt, Far));
        Assert.Equal(CastResult.NoTarget, session.Cast(Bolt, Near));
        Assert.Equal(100, session.Mana.Life);
    }

    [Fact]
    public void Cast_UnknownSpellOrWithoutMana_IsRefused()
    {
        var session = CreateSession([Band(Near, Armored)], spellbook: new Spellbook([Bolt]), life: 5);

        Assert.Equal(CastResult.Unknown, session.Cast(Mend, new Position(1, 0)));
        Assert.Equal(CastResult.NotEnoughMana, session.Cast(Bolt, Near));
    }

    [Fact]
    public void Cast_Heal_RestoresThePlayersSquad()
    {
        var session = CreateSession();
        var leader = session.Party.Leader;
        Assert.Empty(session.SpellTargets(Mend));
        leader.TakeDamage(50);

        Assert.Equal(CastResult.Cast, session.Cast(Mend, session.Party.Position));

        Assert.Equal(Knight.MaxHp - 20, leader.Hp);
        Assert.Equal(5, session.Mana.Runic);
    }

    [Fact]
    public void CaptureThenRestore_KeepsTheSpellbook()
    {
        var session = CreateSession([Band(Near, Armored)], spellbook: new Spellbook());
        session.Research(Mend);
        session.Party.Leader.TakeDamage(10);
        session.Cast(Mend, session.Party.Position);

        var restored = SnapshotMapper.Restore(SnapshotMapper.Capture(session), Content, new FixedRandom());

        Assert.Equal([Mend], restored.Spellbook.Known);
        Assert.True(restored.Spellbook.ResearchedThisTurn);
        Assert.True(restored.Spellbook.WasCast(Mend));
        Assert.Equal(session.Mana, restored.Mana);
    }
}
