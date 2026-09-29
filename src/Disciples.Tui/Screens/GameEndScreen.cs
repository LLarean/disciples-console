using Disciples.Tui.Widgets;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;

namespace Disciples.Tui.Screens;

public sealed class GameEndScreen : Screen
{
    private readonly bool _won;
    private readonly int _turn;
    private readonly Action _onContinue;

    public GameEndScreen(bool won, int turn, Action onContinue)
    {
        _won = won;
        _turn = turn;
        _onContinue = onContinue;
        Add(new Canvas(Draw) { Width = Dim.Fill(), Height = Dim.Fill() });
    }

    public override bool HandleKey(Key key)
    {
        _onContinue();
        return true;
    }

    private void Draw(Canvas canvas)
    {
        var heading = _won ? "Victory" : "Defeat";
        var message = _won ? $"The land is yours after {_turn} turns." : "Your leader has fallen.";
        const string prompt = "Press any key to return to the main menu.";

        var viewport = canvas.Viewport;
        var y = viewport.Height / 2 - 2;
        canvas.Text((viewport.Width - heading.Length) / 2, y, heading, _won ? Palette.Good : Palette.Bad, style: TextStyle.Bold);
        canvas.Text((viewport.Width - message.Length) / 2, y + 2, message, Palette.Text);
        canvas.Text((viewport.Width - prompt.Length) / 2, y + 4, prompt, Palette.Dim);
    }
}
