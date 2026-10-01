using Disciples.Core.Map;
using Disciples.Core.Session;
using Disciples.Tui.Widgets;

namespace Disciples.Tui.Screens;

/// <summary>What the active thief can do to the hostile squads next to it.</summary>
public sealed class ThiefScreen(GameFlow flow, GameSession session) : MenuScreen
{
    private static readonly (ThiefAction Action, string Label, Func<GameSession, string> Detail)[] Actions =
    [
        (ThiefAction.Poison, "Poison", s => $"up to {s.Rules.ThiefPoisonDamage} damage to every unit, nobody dies"),
        (ThiefAction.Assassinate, "Assassinate", _ => "kills the weakest unit that is not a leader"),
        (ThiefAction.Steal, "Rob", s => $"up to {s.Rules.ThiefStealGold} gold from the enemy treasury")
    ];

    protected override IReadOnlyList<string> Heading =>
    [
        session.Party.Name,
        $"an attempt takes the rest of the movement and succeeds in {session.Rules.ThiefSuccessPercent}% of cases",
        session.ThiefTargets().Count == 0 ? "No hostile squad is next to the thief." : ""
    ];

    protected override IReadOnlyList<MenuItem> Items =>
        session.ThiefTargets()
            .SelectMany(at => Actions.Select(a => new MenuItem(
                $"{a.Label} {session.EncounterAt(at)!.Name}", () => Act(a.Action, at), session.Party.MovementPoints > 0, a.Detail(session))))
            .Append(new MenuItem("Back", Back))
            .ToList();

    protected override void Back() => Shell.Pop();

    /// <summary>Returns to the map, where the log reports how the attempt went.</summary>
    private void Act(ThiefAction action, Position at)
    {
        switch (session.Infiltrate(action, at))
        {
            case ThiefResult.Done or ThiefResult.Caught when session.Status != GameStatus.Playing:
                flow.EndGame(session);
                break;
            case ThiefResult.Done or ThiefResult.Caught:
                Shell.Pop();
                break;
            case ThiefResult.Pointless:
                Show("There is nothing to gain here.", Palette.Warn);
                break;
            default:
                Show("The thief cannot act now.", Palette.Bad);
                break;
        }
    }
}
