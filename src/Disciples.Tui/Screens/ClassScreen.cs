using Disciples.Core.Content;
using Disciples.Core.Units;
using Disciples.Tui.Widgets;

namespace Disciples.Tui.Screens;

public sealed class ClassScreen(GameFlow flow, Race race) : MenuScreen
{
    protected override IReadOnlyList<string> Heading => ["Choose your leader", $"{race.Name} — the class stays for the whole game"];

    protected override IReadOnlyList<MenuItem> Items =>
        flow.LeadersOf(race).Where(c => !c.IsThief).Select(c => new MenuItem(c.Name, () => flow.StartNewGame(race, c), Detail: Describe(c))).ToList();

    protected override void Back() => Shell.Pop();

    public static string Describe(UnitDefinition c) =>
        $"HP {c.MaxHp}  {ViewDrawing.AttackLabel(c)} {c.Power}  armor {c.Armor}  init {c.Initiative}  move {c.Movement}  lead {c.Leadership}"
        + (c.PlantsRods ? "  plants rods" : "") + (c.IsThief ? "  thief" : "");
}
