using Disciples.Tui.Widgets;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;

namespace Disciples.Tui.Screens;

public sealed record MenuItem(string Label, Action Select, bool Enabled = true, string Detail = "");

/// <summary>A centered vertical menu with a heading and a status line.</summary>
public abstract class MenuScreen : Screen
{
    private const int PanelWidth = 76;

    private int _index;
    private (string Text, Color Color) _message = ("", Palette.Text);

    protected MenuScreen()
    {
        var panel = new Canvas(Draw) { X = Pos.Center(), Y = 0, Width = PanelWidth, Height = Dim.Fill(1) };
        var hints = new HintBar(() => [("↑↓", "select"), ("Enter", "confirm"), ("Esc", BackLabel)]);
        Add(panel, hints);
    }

    protected abstract IReadOnlyList<string> Heading { get; }
    protected abstract IReadOnlyList<MenuItem> Items { get; }
    protected virtual string BackLabel => "back";

    public override bool HandleKey(Key key)
    {
        var items = Items;
        _message = ("", Palette.Text);

        if (key == Key.Esc)
            Back();
        else if (key == Key.CursorUp)
            _index = (_index + items.Count - 1) % items.Count;
        else if (key == Key.CursorDown)
            _index = (_index + 1) % items.Count;
        else if (key == Key.Enter && items[Selected(items)] is { Enabled: true } item)
            item.Select();
        else
            return false;

        return true;
    }

    protected abstract void Back();

    protected void Show(string text, Color color) => _message = (text, color);

    private int Selected(IReadOnlyList<MenuItem> items) => Math.Min(_index, items.Count - 1);

    private void Draw(Canvas canvas)
    {
        var width = canvas.Viewport.Width;
        var heading = Heading;
        var items = Items;
        var y = Math.Max(0, (canvas.Viewport.Height - heading.Count - items.Count - 4) / 2);

        for (var i = 0; i < heading.Count; i++, y++)
            canvas.Text((width - heading[i].Length) / 2, y, heading[i], i == 0 ? Palette.Accent : Palette.Dim, style: i == 0 ? TextStyle.Bold : TextStyle.None);

        y++;
        var selected = Selected(items);
        for (var i = 0; i < items.Count; i++, y++)
        {
            var item = items[i];
            var color = !item.Enabled ? Palette.Faint : i == selected ? Palette.Text : Palette.Dim;
            canvas.Text(4, y, i == selected ? "›" : " ", Palette.Accent);
            var x = canvas.Text(6, y, item.Label, color, style: i == selected ? TextStyle.Bold : TextStyle.None);
            canvas.Text(x + 2, y, ViewDrawing.Fit(item.Detail, Math.Max(0, width - x - 2)), Palette.Faint);
        }

        y++;
        canvas.Text((width - _message.Text.Length) / 2, y, _message.Text, _message.Color);
    }
}
