using Disciples.Core.Items;
using Disciples.Core.Session;
using Disciples.Core.Squads;
using Disciples.Tui.Widgets;

namespace Disciples.Tui.Screens;

/// <summary>The active leader's worn items and bag.</summary>
public sealed class ItemsScreen(GameSession session) : MenuScreen
{
    private Party Party => session.Party;

    protected override IReadOnlyList<string> Heading =>
    [
        $"{Party.Name} — items",
        Party.Items.Count + Party.Equipped.Count == 0 ? "The bag is empty." : "Enter — drink, wear or take off"
    ];

    protected override IReadOnlyList<MenuItem> Items =>
        Party.Equipped.Select(i => new MenuItem($"{i.Name} (worn)", () => TakeOff(i), Detail: Describe(i)))
            .Concat(Party.Items.GroupBy(i => i).Select(g => new MenuItem(Label(g.Key, g.Count()), () => Use(g.Key), Detail: Describe(g.Key))))
            .Append(new MenuItem("Back", Back))
            .ToList();

    protected override void Back() => Shell.Pop();

    public static string Describe(ItemDefinition item)
    {
        var bonus = item.Bonus;
        var effects = new[]
        {
            item.Heal > 0 ? $"heals {item.Heal} HP" : "",
            bonus.PowerPercent != 0 ? $"power {bonus.PowerPercent:+0;-0}%" : "",
            bonus.Armor != 0 ? $"armor {bonus.Armor:+0;-0}" : "",
            bonus.Initiative != 0 ? $"initiative {bonus.Initiative:+0;-0}" : "",
            bonus.Accuracy != 0 ? $"accuracy {bonus.Accuracy:+0;-0}%" : ""
        }.Where(e => e != "");

        var target = item.Kind switch
        {
            ItemKind.Artifact => "artifact, leader: ",
            ItemKind.Banner => "banner, squad: ",
            _ => "potion: "
        };
        return target + string.Join(", ", effects);
    }

    public static string Label(ItemDefinition item, int count) => count > 1 ? $"{item.Name} ×{count}" : item.Name;

    private void Use(ItemDefinition item)
    {
        if (!item.IsEquipment)
            Shell.Push(new PotionScreen(session, item));
        else if (session.Equip(item))
            Show($"{Party.Name} takes up the {item.Name}.", Palette.Good);
    }

    private void TakeOff(ItemDefinition item)
    {
        if (session.Unequip(item))
            Show($"{item.Name} goes back to the bag.", Palette.Dim);
    }
}
