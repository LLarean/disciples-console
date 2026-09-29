namespace Disciples.Tui.Screens;

public sealed class MainMenuScreen(GameFlow flow) : MenuScreen
{
    protected override IReadOnlyList<string> Heading => ["D I S C I P L E S", "console prototype"];

    protected override IReadOnlyList<MenuItem> Items =>
    [
        new("New game", flow.StartNewGame),
        new("Load game", () => Shell.Push(new SlotsScreen(flow, null)), flow.Saves.HasAny()),
        new("Quit", flow.Quit)
    ];

    protected override string BackLabel => "quit";

    protected override void Back() => flow.Quit();
}
