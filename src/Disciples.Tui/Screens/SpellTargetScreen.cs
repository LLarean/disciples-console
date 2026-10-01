using Disciples.Core.Magic;
using Disciples.Core.Session;

namespace Disciples.Tui.Screens;

/// <summary>Picks the squad on the map the spell is cast at.</summary>
public sealed class SpellTargetScreen(GameFlow flow, GameSession session, SpellDefinition spell) : MenuScreen
{
    protected override IReadOnlyList<string> Heading => [spell.Name, SpellsScreen.Describe(spell)];

    protected override IReadOnlyList<MenuItem> Items =>
        session.SpellTargets(spell)
            .Select(t => new MenuItem(t.Name, () => Cast(t), Detail: $"{t.At} · {Health(t)}"))
            .Append(new MenuItem("Back", Back))
            .ToList();

    protected override void Back() => Shell.Pop();

    private static string Health(SpellTarget target)
    {
        var units = target.Squads.SelectMany(s => s.AliveUnits).ToList();
        return $"{units.Count} unit(s), {units.Sum(u => u.Hp)}/{units.Sum(u => u.MaxHp)} HP";
    }

    /// <summary>Returns to the map, where the log reports what the spell did.</summary>
    private void Cast(SpellTarget target)
    {
        if (session.Cast(spell, target.At) != CastResult.Cast)
            return;

        if (session.Status != GameStatus.Playing)
        {
            flow.EndGame(session);
            return;
        }

        var shell = Shell;
        shell.Pop();
        shell.Pop();
    }
}
