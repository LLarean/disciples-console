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
        new("Reveal the map", () => Apply(session.CheatRevealMap, "Map revealed"))
    ];

    protected override void Back() => Shell.Pop();

    private void Apply(Action cheat, string done)
    {
        cheat();
        Show(done, Palette.Good);
    }
}
