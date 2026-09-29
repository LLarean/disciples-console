using Terminal.Gui.ViewBase;

namespace Disciples.Tui.Widgets;

public sealed class HintBar : Canvas
{
    public HintBar(Func<IEnumerable<(string Key, string Action)>> hints) : base(canvas => Draw(canvas, hints()))
    {
        X = 0;
        Y = Pos.AnchorEnd(1);
        Width = Dim.Fill();
        Height = 1;
    }

    private static void Draw(Canvas canvas, IEnumerable<(string Key, string Action)> hints)
    {
        var x = 1;
        foreach (var (key, action) in hints)
        {
            x = canvas.Text(x, 0, key, Palette.Accent);
            x = canvas.Text(x + 1, 0, action, Palette.Dim) + 3;
        }
    }
}
