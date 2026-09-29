using Disciples.Core.Units;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Attribute = Terminal.Gui.Drawing.Attribute;

namespace Disciples.Tui.Widgets;

public static class ViewDrawing
{
    /// <summary>Draws text at the given viewport position and returns the column after it.</summary>
    public static int Text(this View view, int x, int y, string text, Color foreground, Color? background = null, TextStyle style = TextStyle.None)
    {
        view.SetAttribute(new Attribute(foreground, background ?? Palette.Background, style));
        view.AddStr(x, y, text);
        return x + text.Length;
    }

    public static int Bar(this View view, int x, int y, int value, int max, int length, Color color)
    {
        var filled = max == 0 ? 0 : (int)Math.Round((double)value / max * length);
        if (value > 0)
            filled = Math.Max(1, filled);

        x = view.Text(x, y, new string('█', filled), color);
        return view.Text(x, y, new string('█', length - filled), Palette.Faint);
    }

    public static int HpBar(this View view, int x, int y, Unit unit, int length) =>
        view.Bar(x, y, unit.Hp, unit.MaxHp, length, Palette.HpColor(unit.Hp, unit.MaxHp));

    public static int Hp(this View view, int x, int y, Unit unit)
    {
        x = view.Text(x, y, unit.Hp.ToString(), Palette.HpColor(unit.Hp, unit.MaxHp));
        return view.Text(x, y, $"/{unit.MaxHp}", Palette.Dim);
    }

    public static void Box(this View view, int x, int y, int width, int height, Color color, bool heavy = false)
    {
        var (topLeft, topRight, bottomLeft, bottomRight, horizontal, vertical) = heavy
            ? ("┏", "┓", "┗", "┛", '━', "┃")
            : ("╭", "╮", "╰", "╯", '─', "│");
        var line = new string(horizontal, width - 2);

        view.Text(x, y, topLeft + line + topRight, color);
        for (var row = y + 1; row < y + height - 1; row++)
        {
            view.Text(x, row, vertical, color);
            view.Text(x + width - 1, row, vertical, color);
        }

        view.Text(x, y + height - 1, bottomLeft + line + bottomRight, color);
    }

    /// <summary>Draws a horizontal rule with a title across the whole viewport.</summary>
    public static void Section(this View view, int y, string title, string right = "")
    {
        var width = view.Viewport.Width;
        view.Text(0, y, new string('─', width), Palette.Faint);
        view.Text(1, y, $" {title} ", Palette.Dim);
        if (right.Length > 0)
            view.Text(width - right.Length - 3, y, $" {right} ", Palette.Dim);
    }

    public static string Fit(string text, int width) =>
        text.Length <= width ? text : width <= 1 ? "" : text[..(width - 1)] + "…";

    public static string AttackLabel(UnitDefinition definition) => definition.AttackType switch
    {
        AttackType.Melee => "melee",
        AttackType.Ranged => "ranged",
        AttackType.AllEnemies => "all",
        AttackType.Heal => "heal",
        _ => "?"
    };
}
