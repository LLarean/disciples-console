using Disciples.Core.Cities;
using Disciples.Core.Magic;
using Disciples.Core.Map;
using Disciples.Core.Persistence;
using Disciples.Core.Session;
using Disciples.Core.Squads;
using Disciples.Core.Units;
using static Disciples.Core.Tests.TestUnits;

namespace Disciples.Core.Tests.Session;

public class ManaTests
{
    private static readonly Position Start = new(0, 0);
    private static readonly Position East = new(1, 0);

    /// <summary>A plains strip 12×1: the party on the west, the capital next to the source, the far east is nobody's land.</summary>
    private static GameSession CreateSession(Site? site = null, Mana? mana = null, IEnumerable<Party>? enemies = null)
    {
        var tiles = new Terrain[12, 1];
        for (var x = 0; x < 12; x++)
            tiles[x, 0] = Plains;

        var capital = new City("Capital", new Position(2, 0), true, 10, mana: new Mana(life: 10));
        var map = new WorldMap("Test", tiles, [capital], sites: site == null ? [] : [site], enemies: enemies);
        return new GameSession(TestContent, map, [new Party(new Unit(Knight), Start)], 0, new FixedRandom(), mana: mana);
    }

    private static Site Source(Position position, Owner owner = Owner.Neutral) =>
        new("Rune Stone", SiteKind.ManaSource, position, owner: owner, mana: new Mana(runic: 5));

    [Fact]
    public void Mana_AddsSubtractsAndCompares()
    {
        var stock = new Mana(life: 10, runic: 5);

        Assert.Equal(new Mana(life: 12, death: 3, runic: 5), stock.Plus(new Mana(life: 2, death: 3)));
        Assert.Equal(new Mana(life: 4, runic: 5), stock.Minus(Mana.Of(ManaType.Life, 6)));
        Assert.True(stock.Covers(new Mana(life: 10, runic: 1)));
        Assert.False(stock.Covers(new Mana(life: 1, infernal: 1)));
        Assert.Equal(5, stock[ManaType.Runic]);
    }

    [Fact]
    public void EndTurn_CapitalYieldsItsMana()
    {
        var session = CreateSession(mana: new Mana(death: 3));

        session.EndTurn();

        Assert.Equal(new Mana(life: 10, death: 3), session.Mana);
    }

    [Fact]
    public void TryMove_OntoManaSource_CapturesIt()
    {
        var source = Source(East);
        var session = CreateSession(source);

        Assert.Equal(MoveResult.SiteVisited, session.TryMove(Direction.East));

        Assert.Equal(Owner.Player, source.Owner);
        Assert.Same(source, session.TakeEvents().Single(e => e.Kind == GameEventKind.MineCaptured).Site);
        Assert.Equal(new Mana(life: 10, runic: 5), session.ManaIncome);
    }

    [Fact]
    public void EndTurn_OwnedSourceAddsMana()
    {
        var session = CreateSession(Source(new Position(11, 0), Owner.Player));

        session.EndTurn();
        session.EndTurn();

        Assert.Equal(new Mana(life: 20, runic: 10), session.Mana);
    }

    [Fact]
    public void EndTurn_SourceOnPlayerLand_IsClaimed()
    {
        var source = Source(East);
        var session = CreateSession(source);

        session.EndTurn();

        Assert.Equal(Owner.Player, source.Owner);
        Assert.Equal(new Mana(life: 10, runic: 5), session.Mana);
    }

    [Fact]
    public void EndTurn_EnemySeizesSourceOutsidePlayerLand()
    {
        var source = Source(new Position(10, 0), Owner.Player);
        var enemy = new Party(new Unit(Warlord), new Position(11, 0));
        var session = CreateSession(source, enemies: [enemy]);

        session.EndTurn();

        Assert.Equal(Owner.Enemy, source.Owner);
        Assert.Equal(new Mana(life: 10), session.Mana);
    }

    [Fact]
    public void CaptureThenRestore_KeepsMana()
    {
        var session = CreateSession(Source(East, Owner.Player), new Mana(infernal: 7));

        var snapshot = SnapshotMapper.Capture(session);
        var restored = SnapshotMapper.Restore(snapshot, TestContent, new FixedRandom());

        Assert.Equal(new Mana(infernal: 7), restored.Mana);
        Assert.Equal(new Mana(life: 10, runic: 5), restored.ManaIncome);
        Assert.Equal(SiteKind.ManaSource, restored.Map.Sites.Single().Kind);
    }

    private static readonly UnitDefinition Warlord =
        new("warlord", "Warlord", 150, 0, 50, 50, 80, AttackType.Melee, UnitSize.Small, 0, leadership: 4, movement: 4);
}
