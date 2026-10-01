using Disciples.Core.Cities;
using Disciples.Core.Session;
using Disciples.Core.Units;
using Disciples.Tui.Widgets;

namespace Disciples.Tui.Screens;

public sealed class LeaderScreen(GameSession session, City capital) : MenuScreen
{
    protected override IReadOnlyList<string> Heading => ["Hire a leader", $"{session.Gold} gold; the new party starts in {capital.Name}"];

    protected override IReadOnlyList<MenuItem> Items =>
        session.LeaderClasses
            .Select(c => new MenuItem($"{c.Name} {c.Cost}g", () => Hire(c), IsOffered(c), IsOffered(c) ? ClassScreen.Describe(c) : $"needs {GuildName}"))
            .ToList();

    protected override void Back() => Shell.Pop();

    private string GuildName => session.Content.Buildings.FirstOrDefault(b => b.AllowsThieves)?.Name ?? "a thieves guild";

    private bool IsOffered(UnitDefinition leader) => !leader.IsThief || session.CanHireThieves;

    private void Hire(UnitDefinition leader)
    {
        switch (session.HireLeader(capital, leader))
        {
            case HireResult.LeaderHired:
                Shell.Pop();
                break;
            case HireResult.NotEnoughGold:
                Show($"Not enough gold for a {leader.Name}.", Palette.Bad);
                break;
            case HireResult.NoRoom:
                Show($"{session.PartyAt(capital.Position)?.Name} is in {capital.Name}; move that party out first.", Palette.Bad);
                break;
            default:
                Show("No leaders for hire here.", Palette.Bad);
                break;
        }
    }
}
