using Disciples.Core.Content;

namespace Disciples.Tui.Screens;

public sealed class RaceScreen(GameFlow flow) : MenuScreen
{
    protected override IReadOnlyList<string> Heading => ["Choose your race", "its units, buildings and spells stay for the whole game"];

    protected override IReadOnlyList<MenuItem> Items =>
        flow.Races.Select(r => new MenuItem(r.Name, () => Shell.Push(new ClassScreen(flow, r)), Detail: Describe(r))).ToList();

    protected override void Back() => Shell.Pop();

    private string Describe(Race race) => string.Join(", ", flow.RecruitsOf(race).Select(u => u.Name));
}
