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
        new("Main menu", flow.ShowMainMenu, Detail: "unsaved progress is lost"),
        new("Quit", flow.Quit, Detail: "unsaved progress is lost")
    ];

    protected override string BackLabel => "resume";

    protected override void Back() => Shell.Pop();
}
