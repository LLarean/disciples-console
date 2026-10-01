using Disciples.Core.Magic;
using Terminal.Gui.Drawing;

namespace Disciples.Tui.Widgets;

public static class ManaText
{
    public static readonly IReadOnlyList<ManaType> Types = Enum.GetValues<ManaType>();

    /// <summary>"10 life, 5 runic"; types with no mana are skipped.</summary>
    public static string Describe(Mana mana)
    {
        var parts = Types.Where(t => mana[t] != 0).Select(t => $"{mana[t]} {t.ToString().ToLowerInvariant()}").ToList();
        return parts.Count > 0 ? string.Join(", ", parts) : "none";
    }

    public static ManaType Dominant(Mana mana) => Types.MaxBy(t => mana[t]);

    public static Color ColorOf(ManaType type) => type switch
    {
        ManaType.Life => new Color(240, 230, 160),
        ManaType.Death => new Color(130, 175, 120),
        ManaType.Infernal => new Color(240, 110, 40),
        _ => new Color(110, 190, 240)
    };
}
