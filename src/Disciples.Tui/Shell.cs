using Disciples.Tui.Screens;
using Disciples.Tui.Widgets;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.Views;

namespace Disciples.Tui;

/// <summary>Root window holding a stack of screens; only the top screen is shown and receives keys.</summary>
public sealed class Shell : Window
{
    private readonly Stack<Screen> _screens = new();

    public Shell()
    {
        BorderStyle = LineStyle.None;
        SetScheme(Palette.Scheme);
    }

    public Screen? Current => _screens.Count > 0 ? _screens.Peek() : null;

    public void Push(Screen screen)
    {
        if (Current is { } previous)
            Remove(previous);

        Show(screen);
    }

    public void Pop()
    {
        Close(_screens.Pop());
        if (Current is { } previous)
        {
            Add(previous);
            previous.Refresh();
        }
        else
        {
            Quit();
        }
    }

    public void Replace(Screen screen)
    {
        Close(_screens.Pop());
        Show(screen);
    }

    /// <summary>Closes every screen and shows the given one alone.</summary>
    public void Reset(Screen screen)
    {
        while (_screens.Count > 0)
            Close(_screens.Pop());

        Show(screen);
    }

    public void Quit() => App?.RequestStop();

    protected override bool OnKeyDown(Key key)
    {
        if (Current is not { } screen || !screen.HandleKey(key))
            return false;

        Current?.Refresh();
        return true;
    }

    private void Show(Screen screen)
    {
        screen.Shell = this;
        _screens.Push(screen);
        Add(screen);
        screen.Refresh();
    }

    private void Close(Screen screen)
    {
        Remove(screen);
        screen.Dispose();
    }
}
