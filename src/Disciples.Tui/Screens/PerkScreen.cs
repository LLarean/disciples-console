using Disciples.Core.Content;
using Disciples.Core.Session;
using Disciples.Core.Squads;
using Disciples.Tui.Widgets;

namespace Disciples.Tui.Screens;

public sealed class PerkScreen(GameSession session) : MenuScreen
{
    private Party Party => session.Party;
    private GameRules Rules => session.Rules;

    protected override IReadOnlyList<string> Heading =>
        ["Level up", $"{Party.Name}, level {Party.Leader.Level}: {Party.UnspentPerks} perk(s) to choose"];

    protected override IReadOnlyList<MenuItem> Items =>
    [
        Perk(LeaderPerk.Leadership, "Leadership", $"+1 squad slot, now {Party.Squad.Capacity}/{Squad.MaxSlots}"),
        Perk(LeaderPerk.Movement, "Pathfinding", $"+{Party.MovementPerk} movement, now {Party.MaxMovementPoints}"),
        Perk(LeaderPerk.Might, "Might", $"+{Rules.MightPerkPercent}% leader damage"),
        Perk(LeaderPerk.NaturalArmor, "Natural Armor", $"+{Rules.ArmorPerk} leader armor"),
        Perk(LeaderPerk.FirstStrike, "First Strike", $"+{Rules.InitiativePerk} leader initiative"),
        Perk(LeaderPerk.Accuracy, "Accuracy", $"+{Rules.AccuracyPerk} leader accuracy"),
        Perk(LeaderPerk.NaturalHealing, "Natural Healing", $"the leader regains {Rules.HealingPerkPercent}% HP every turn"),
        Perk(LeaderPerk.WeaponMaster, "Weapon Master", $"+{Rules.ExperiencePerkPercent}% battle experience for the squad")
    ];

    protected override void Back() => Shell.Pop();

    private MenuItem Perk(LeaderPerk perk, string label, string detail) =>
        new(label, () => Take(perk, label), Party.CanTake(perk), IsTakenAbility(perk) ? "taken" : detail);

    private bool IsTakenAbility(LeaderPerk perk) => perk is not (LeaderPerk.Leadership or LeaderPerk.Movement) && Party.Has(perk);

    private void Take(LeaderPerk perk, string label)
    {
        session.TakePerk(perk);
        if (Party.UnspentPerks == 0)
            Back();
        else
            Show($"{label} taken.", Palette.Good);
    }
}
