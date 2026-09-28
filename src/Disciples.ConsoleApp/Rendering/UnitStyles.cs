using Disciples.Core.Units;

namespace Disciples.ConsoleApp.Rendering;

public static class UnitStyles
{
    public static string HpColor(Unit unit)
    {
        if (!unit.IsAlive)
            return "grey35";

        var ratio = (double)unit.Hp / unit.MaxHp;
        return ratio > 0.6 ? "green3" : ratio > 0.3 ? "yellow" : "red";
    }

    public static string HpMarkup(Unit unit) => $"[{HpColor(unit)}]{unit.Hp}[/][grey]/{unit.MaxHp}[/]";

    public static string HpBar(Unit unit, int length)
    {
        var filled = unit.IsAlive ? Math.Max(1, (int)Math.Round((double)unit.Hp / unit.MaxHp * length)) : 0;
        return $"[{HpColor(unit)}]{new string('█', filled)}[/][grey23]{new string('█', length - filled)}[/]";
    }

    public static string AttackLabel(UnitDefinition definition) => definition.AttackType switch
    {
        AttackType.Melee => "melee",
        AttackType.Ranged => "ranged",
        AttackType.AllEnemies => "all",
        AttackType.Heal => "heal",
        _ => "?"
    };
}
