using Disciples.Core.Items;
using Disciples.Core.Session;
using Disciples.Core.Units;

namespace Disciples.Tui.Screens;

/// <summary>Picks the unit of the active party that drinks the potion.</summary>
public sealed class PotionScreen(GameSession session, ItemDefinition potion) : MenuScreen
{
    protected override IReadOnlyList<string> Heading => [potion.Name, ItemsScreen.Describe(potion)];

    protected override IReadOnlyList<MenuItem> Items =>
        session.Party.Squad.Units
            .Select(u => new MenuItem(u.Name, () => Drink(u), u.IsWounded, $"{u.Hp}/{u.MaxHp} HP"))
            .Append(new MenuItem("Back", Back))
            .ToList();

    protected override void Back() => Shell.Pop();

    private void Drink(Unit unit)
    {
        if (session.UseItem(potion, unit) == ItemResult.Used)
            Shell.Pop();
    }
}
