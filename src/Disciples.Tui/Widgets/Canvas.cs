using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;

namespace Disciples.Tui.Widgets;

/// <summary>A view whose content is drawn by a delegate; optionally framed as a titled panel.</summary>
public class Canvas : View
{
    private readonly Action<Canvas>? _draw;

    public Canvas(Action<Canvas>? draw = null, string? title = null)
    {
        _draw = draw;
        if (title == null)
            return;

        Title = title;
        IsFocused = false;
    }

    /// <summary>Frames the panel with a highlighted border.</summary>
    public bool IsFocused
    {
        set
        {
            BorderStyle = value ? LineStyle.Double : LineStyle.Rounded;
            SetScheme(value ? Palette.FocusedPanelScheme : Palette.PanelScheme);
        }
    }

    protected override bool OnDrawingContent(DrawContext? context)
    {
        FillRect(Viewport with { X = 0, Y = 0 }, Palette.Background);
        Draw();
        return true;
    }

    protected virtual void Draw() => _draw?.Invoke(this);
}
