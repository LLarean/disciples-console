using Disciples.Core.Session;
using Disciples.Core.Squads;
using Disciples.Tui.Widgets;

namespace Disciples.Tui.Screens;

public sealed class PerkScreen(GameSession session) : MenuScreen
{
    private Party Party => session.Party;

    protected override IReadOnlyList<string> Heading =>
        ["Level up", $"{Party.Name}, level {Party.Leader.Level}: {Party.UnspentPerks} perk(s) to choose"];

    protected override IReadOnlyList<MenuItem> Items =>
    [
        Perk(LeaderPerk.Leadership, "Leadership", $"+1 squad slot, now {Party.Squad.Capacity}/{Squad.MaxSlots}"),
        Perk(LeaderPerk.Movement, "Pathfinding", $"+{Party.MovementPerk} movement, now {Party.MaxMovementPoints}")
    ];

    protected override void Back() => Shell.Pop();

    private MenuItem Perk(LeaderPerk perk, string label, string detail) =>
        new(label, () => Take(perk, label), Party.CanTake(perk), detail);

    private void Take(LeaderPerk perk, string label)
    {
        session.TakePerk(perk);
        if (Party.UnspentPerks == 0)
            Back();
        else
            Show($"{label} taken.", Palette.Good);
    }
}
