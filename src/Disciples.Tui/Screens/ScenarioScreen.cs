namespace Disciples.Tui.Screens;

public sealed class ScenarioScreen(GameFlow flow) : MenuScreen
{
    protected override IReadOnlyList<string> Heading => ["Choose a map", "content/maps"];

    protected override IReadOnlyList<MenuItem> Items =>
        flow.Scenarios.Select(s => new MenuItem(s.Name, () => Shell.Push(new RaceScreen(flow, s)), Detail: Describe(s))).ToList();

    protected override void Back() => Shell.Pop();

    private static string Describe(ScenarioInfo s) => $"{s.Width}×{s.Height}, {s.Cities} cities, {s.Neutrals} neutral squads";
}
