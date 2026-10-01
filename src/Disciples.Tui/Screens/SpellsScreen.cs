using Disciples.Core.Magic;
using Disciples.Core.Session;
using Disciples.Tui.Widgets;

namespace Disciples.Tui.Screens;

/// <summary>The spell book: research unknown spells, cast the known ones.</summary>
public sealed class SpellsScreen(GameFlow flow, GameSession session) : MenuScreen
{
    protected override IReadOnlyList<string> Heading => ["Spells", $"Mana: {ManaText.Describe(session.Mana)}", ResearchNote];

    protected override IReadOnlyList<MenuItem> Items =>
        session.Content.Spells.Select(ToItem).Append(new MenuItem("Back", Back)).ToList();

    protected override void Back() => Shell.Pop();

    public static string Describe(SpellDefinition spell) => spell.Kind == SpellKind.Heal
        ? $"heals every unit of your squad by {spell.Amount}"
        : $"{spell.Amount} {spell.Source.ToString().ToLowerInvariant()} damage to every unit of a hostile squad";

    private string ResearchNote
    {
        get
        {
            if (!session.CanResearch)
                return $"Research needs {TowerName} in the capital.";

            return session.Spellbook.ResearchedThisTurn ? "A spell is already researched this turn." : "One spell can be researched per turn.";
        }
    }

    private string TowerName => session.Content.Buildings.FirstOrDefault(b => b.AllowsResearch)?.Name ?? "a magic tower";

    private MenuItem ToItem(SpellDefinition spell)
    {
        var known = session.Spellbook.Knows(spell);
        var block = known ? CastBlock(spell) : ResearchBlock(spell);
        var cost = ManaText.Describe(known ? spell.CastCost : spell.ResearchCost);
        return new MenuItem(
            (known ? "Cast " : "Research ") + spell.Name,
            known ? () => Shell.Push(new SpellTargetScreen(flow, session, spell)) : () => Research(spell),
            block == null,
            $"{cost} · {block ?? Describe(spell)}");
    }

    private string? CastBlock(SpellDefinition spell)
    {
        if (session.Spellbook.WasCast(spell))
            return "already cast this turn";

        if (!session.Mana.Covers(spell.CastCost))
            return "not enough mana";

        return session.SpellTargets(spell).Count == 0 ? "no target in sight" : null;
    }

    private string? ResearchBlock(SpellDefinition spell)
    {
        if (!session.CanResearch)
            return "no tower";

        if (session.Spellbook.ResearchedThisTurn)
            return "next turn";

        return session.Mana.Covers(spell.ResearchCost) ? null : "not enough mana";
    }

    private void Research(SpellDefinition spell)
    {
        if (session.Research(spell) == ResearchResult.Researched)
            Show($"{spell.Name} is researched.", Palette.Good);
    }
}
