using Disciples.Core.Session;
using Disciples.Tui.Widgets;

namespace Disciples.Tui.Screens;

public sealed class CheatScreen(GameSession session) : MenuScreen
{
    private const int Gold = 1000;
    private const int Mana = 100;

    protected override IReadOnlyList<string> Heading => ["Cheats", "for testing; a save keeps what they change"];

    protected override IReadOnlyList<MenuItem> Items =>
    [
        new($"+{Gold} gold", () => Apply(() => session.CheatGold(Gold), "Gold added"), Detail: $"{session.Gold} now"),
        new($"+{Mana} mana of each type", () => Apply(() => session.CheatMana(Mana), "Mana added"), Detail: $"{ManaText.Describe(session.Mana)} now"),
        new("Level up the squad", () => Apply(session.CheatExperience, $"{session.Party.Name}'s squad gained experience"),
            Detail: "experience to the next level or upgrade"),
        new("Heal and refresh all parties", () => Apply(session.CheatRestore, "Parties healed, movement restored")),
        new("Reveal the map", () => Apply(session.CheatRevealMap, "Map revealed")),
        new("Build everything in the capital", () => Apply(session.CheatBuildings, "Capital built up"), session.Capital != null),
        new("Learn every spell", () => Apply(session.CheatSpells, "Spells learned"), Detail: $"{session.Spellbook.Known.Count} of {session.Spells.Count()} known"),
        new("One of each item", () => Apply(session.CheatItems, $"Items put into {session.Party.Name}'s bag"), Detail: $"{session.Party.Items.Count} in the bag")
    ];

    protected override void Back() => Shell.Pop();

    private void Apply(Action cheat, string done)
    {
        cheat();
        Show(done, Palette.Good);
    }
}
