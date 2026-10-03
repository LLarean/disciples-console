using Disciples.Core.Session;

namespace Disciples.Tui.Screens;

public sealed class PauseScreen(GameFlow flow, GameSession session) : MenuScreen
{
    protected override IReadOnlyList<string> Heading => ["Paused", $"{session.Map.Name}, turn {session.Turn}"];

    protected override IReadOnlyList<MenuItem> Items =>
    [
        new("Resume", Back),
        new("Save game", () => Shell.Push(new SlotsScreen(flow, session))),
        new("Load game", () => Shell.Push(new SlotsScreen(flow, null)), flow.Saves.HasAny()),
        new("Cheats", () => Shell.Push(new CheatScreen(session))),
        new("Main menu", () => Confirm("Leave to the main menu?", flow.ShowMainMenu)),
        new("Quit", () => Confirm("Quit the game?", flow.Quit))
    ];

    protected override string BackLabel => "resume";

    protected override void Back() => Shell.Pop();

    private void Confirm(string question, Action leave) =>
        Shell.Push(new ConfirmScreen(question, "unsaved progress is lost", leave));
}
