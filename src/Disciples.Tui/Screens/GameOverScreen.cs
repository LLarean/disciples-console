using Disciples.Tui.Widgets;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;

namespace Disciples.Tui.Screens;

public sealed class GameOverScreen : Screen
{
    private const string Heading = "Defeat";
    private const string Message = "Your leader has fallen. Press any key to exit.";

    public GameOverScreen()
    {
        Add(new Canvas(Draw) { Width = Dim.Fill(), Height = Dim.Fill() });
    }

    public override bool HandleKey(Key key)
    {
        Shell.Quit();
        return true;
    }

    private static void Draw(Canvas canvas)
    {
        var viewport = canvas.Viewport;
        var y = viewport.Height / 2 - 1;
        canvas.Text((viewport.Width - Heading.Length) / 2, y, Heading, Palette.Bad, style: TextStyle.Bold);
        canvas.Text((viewport.Width - Message.Length) / 2, y + 2, Message, Palette.Dim);
    }
}
