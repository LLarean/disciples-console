using Disciples.Core.Map;
using Disciples.Core.Session;
using Disciples.Core.Units;
using Disciples.Tui.Widgets;

namespace Disciples.Tui.Screens;

public sealed class CampScreen(GameSession session, Site camp) : MenuScreen
{
    protected override IReadOnlyList<string> Heading =>
    [
        camp.Name,
        $"{session.Gold} gold, party {session.Party.Squad.UsedSlots}/{session.Party.Squad.Capacity}"
    ];

    protected override IReadOnlyList<MenuItem> Items =>
        camp.Mercenaries
            .Select(m => new MenuItem($"{m.Name} — {m.Cost}g", () => Hire(m), session.Gold >= m.Cost, Describe(m)))
            .Append(new MenuItem("Leave", Back))
            .ToList();

    protected override string BackLabel => "leave";

    protected override void Back() => Shell.Pop();

    private void Hire(UnitDefinition mercenary)
    {
        if (session.HireMercenary(camp, mercenary) == HireResult.HiredToParty)
            Show($"{mercenary.Name} joins the party.", Palette.Good);
        else
            Show("No room in the party.", Palette.Bad);
    }

    private static string Describe(UnitDefinition m) =>
        $"HP {m.MaxHp}  {ViewDrawing.AttackLabel(m)} {m.Power}{(m.Size == UnitSize.Large ? "  large" : "")}";
}
