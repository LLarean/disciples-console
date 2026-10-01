using Disciples.Core.Items;
using Disciples.Core.Map;
using Disciples.Core.Session;
using Disciples.Tui.Widgets;

namespace Disciples.Tui.Screens;

public sealed class MerchantScreen(GameSession session, Site merchant) : MenuScreen
{
    protected override IReadOnlyList<string> Heading =>
    [
        merchant.Name,
        merchant.Items.Count == 0 ? $"{session.Gold} gold — sold out" : $"{session.Gold} gold"
    ];

    protected override IReadOnlyList<MenuItem> Items =>
        merchant.Items.GroupBy(i => i)
            .Select(g => new MenuItem(
                $"{ItemsScreen.Label(g.Key, g.Count())} — {g.Key.Cost}g", () => Buy(g.Key), session.Gold >= g.Key.Cost, ItemsScreen.Describe(g.Key)))
            .Append(new MenuItem("Leave", Back))
            .ToList();

    protected override string BackLabel => "leave";

    protected override void Back() => Shell.Pop();

    private void Buy(ItemDefinition item)
    {
        if (session.BuyItem(merchant, item) == BuyResult.Bought)
            Show($"{item.Name} goes into the bag.", Palette.Good);
        else
            Show("Not enough gold.", Palette.Bad);
    }
}
