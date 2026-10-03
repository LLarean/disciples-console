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
    private static readonly Position East = new(1, 0);

    /// <summary>A Knight with an Archer on a plains strip, a lone Squire standing to the east.</summary>
    private static GameSession CreateSession(params ItemDefinition[] items)
    {
        var tiles = new[,] { { Plains }, { Plains }, { Plains } };
        var capital = new City("Capital", new Position(2, 0), true, 10);
        var bandits = new Squad();
        bandits.TryAdd(new Unit(Squire));
        var map = new WorldMap("Test", tiles, [capital], [new NeutralSquad("Bandits", East, bandits, 0)]);

        var squad = new Squad();
        squad.TryPlace(new Unit(Knight), new SquadSlot(SquadLine.Front, 1));
        squad.TryPlace(new Unit(Archer), new SquadSlot(SquadLine.Back, 1));
        return new GameSession(TestContent, map, new Party(squad, Start, items: items), 100, new FixedRandom());
    }

    private static Unit ArcherOf(GameSession session) => session.Party.Squad.Units.Single(u => !u.IsLeader);

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
    public void CaptureThenRestore_KeepsTheBagAndWornItems()
    {
        var session = CreateSession(Potion, Sword, Banner, Potion);
        session.Equip(Sword);
        session.Equip(Banner);

        var restored = SnapshotMapper.Restore(SnapshotMapper.Capture(session), TestContent, new FixedRandom());

        Assert.Equal([Potion, Potion], restored.Party.Items);
        Assert.Equal([Sword, Banner], restored.Party.Equipped);
    }

    [Fact]
    public void CaptureThenRestore_WithASpareOfTheWornItem_KeepsTheBagOrder()
    {
        var session = CreateSession(Sword, Potion, Sword, Shield);
        session.Equip(Sword);

        var restored = SnapshotMapper.Restore(SnapshotMapper.Capture(session), TestContent, new FixedRandom());

        Assert.Equal([Potion, Sword, Shield], restored.Party.Items);
        Assert.Equal([Sword], restored.Party.Equipped);
    }

    [Fact]
    public void UseItem_Potion_HealsTheUnitAndIsSpent()
    {
        var session = CreateSession(Potion, Potion);
        var leader = session.Party.Leader;
        leader.TakeDamage(100);

        Assert.Equal(ItemResult.Used, session.UseItem(Potion, leader));

        Assert.Equal(Knight.MaxHp - 100 + Potion.Heal, leader.Hp);
        Assert.Equal([Potion], session.Party.Items);
    }

    [Fact]
    public void UseItem_OnAHealthyUnit_KeepsThePotion()
    {
        var session = CreateSession(Potion);

        Assert.Equal(ItemResult.NoEffect, session.UseItem(Potion, session.Party.Leader));
        Assert.Equal([Potion], session.Party.Items);
    }

    [Fact]
    public void UseItem_Refused_WhenNotCarriedNotAPotionOrOnAStranger()
    {
        var session = CreateSession(Sword, Potion);
        var leader = session.Party.Leader;
        leader.TakeDamage(100);
        var stranger = new Unit(Squire, hp: 1);

        Assert.Equal(ItemResult.Unavailable, session.UseItem(Shield, leader));
        Assert.Equal(ItemResult.Unavailable, session.UseItem(Sword, leader));
        Assert.Equal(ItemResult.Unavailable, session.UseItem(Potion, stranger));
        Assert.Equal(ItemResult.Unavailable, CreateSession().UseItem(Potion, leader));
    }

    [Fact]
    public void Equip_WearsOneItemOfAKind_AndReturnsThePreviousOneToTheBag()
    {
        var session = CreateSession(Sword, Shield, Potion);

        Assert.True(session.Equip(Sword));
        Assert.True(session.Equip(Shield));
        Assert.False(session.Equip(Potion));
        Assert.False(session.Equip(Banner));

        Assert.Equal([Shield], session.Party.Equipped);
        Assert.Equal([Potion, Sword], session.Party.Items);

        Assert.True(session.Unequip(Shield));
        Assert.Empty(session.Party.Equipped);
        Assert.False(session.Unequip(Shield));
    }

    [Fact]
    public void BonusFor_BannerCoversTheSquad_ArtifactOnlyTheLeader()
    {
        var session = CreateSession(Sword, Banner);
        session.Equip(Sword);
        session.Equip(Banner);
        var party = session.Party;

        var leader = party.BonusFor(party.Leader);
        var archer = party.BonusFor(ArcherOf(session));

        Assert.Equal((10, 20, 10, 5), (leader.Armor, leader.PowerPercent, leader.Initiative, leader.Accuracy));
        Assert.Equal((10, 0, 0, 5), (archer.Armor, archer.PowerPercent, archer.Initiative, archer.Accuracy));
        Assert.True(party.BonusFor(new Unit(Squire)).IsEmpty);
    }

    [Fact]
    public void StartBattle_AppliesWornItemsToDamageDealtAndTaken()
    {
        var session = CreateSession(Sword, Banner);
        session.Equip(Sword);
        session.Equip(Banner);
        var leader = session.Party.Leader;
        var encounter = session.EncounterAt(East)!;
        var bandit = encounter.Defenders.Units.Single();

        var battle = session.StartBattle(encounter);

        Assert.Same(leader, battle.Current);
        battle.Act(bandit);
        Assert.Equal(Squire.MaxHp - Knight.Power * 120 / 100, bandit.Hp);

        while (battle.Current != bandit)
            battle.Defend();
        battle.Act(leader);
        Assert.Equal(Knight.MaxHp - Squire.Power * 90 / 100, leader.Hp);
    }
}
