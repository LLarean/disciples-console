using Disciples.Core.Cities;
using Disciples.Core.Content;
using Disciples.Core.Items;
using Disciples.Core.Map;
using Disciples.Core.Persistence;
using Disciples.Core.Session;
using Disciples.Core.Squads;
using Disciples.Core.Units;
using static Disciples.Core.Tests.TestUnits;

namespace Disciples.Core.Tests.Session;

public class ItemTests
{
    private static readonly Position Start = new(0, 0);

    private static GameSession CreateSession(params ItemDefinition[] items)
    {
        var tiles = new[,] { { Plains }, { Plains }, { Plains } };
        var capital = new City("Capital", new Position(2, 0), true, 10);
        var map = new WorldMap("Test", tiles, [capital]);
        var squad = new Squad();
        squad.TryPlace(new Unit(Knight), new SquadSlot(SquadLine.Front, 1));
        return new GameSession(TestContent, map, new Party(squad, Start, items: items), 100, new FixedRandom());
    }

    [Fact]
    public void Content_Item_ResolvesRetiredIds_AndRejectsUnknownOnes()
    {
        var aliases = new ContentAliases { Items = { ["elixir"] = "potion" } };
        var content = new GameContent([Knight], [Plains], [], new GameRules(), aliases, [Potion]);

        Assert.Same(Potion, content.Item("elixir"));
        Assert.Throws<ContentException>(() => content.Item("sword"));
        Assert.Throws<ContentException>(() => new GameContent([Knight], [Plains], [], new GameRules(), aliases));
    }

    [Fact]
    public void CaptureThenRestore_KeepsCarriedItems()
    {
        var session = CreateSession(Potion, Sword, Potion);

        var restored = SnapshotMapper.Restore(SnapshotMapper.Capture(session), TestContent, new FixedRandom());

        Assert.Equal([Potion, Sword, Potion], restored.Party.Items);
    }
}
