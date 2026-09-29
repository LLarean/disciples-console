using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;

namespace Disciples.Tui.Screens;

public abstract class Screen : View
{
    protected Screen()
    {
        Width = Dim.Fill();
        Height = Dim.Fill();
    }

    public Shell Shell { get; internal set; } = null!;

    /// <summary>Returns true when the key was consumed.</summary>
    public abstract bool HandleKey(Key key);

    public void Refresh()
    {
        UpdateViews();
        SetNeedsDraw();
        foreach (var view in SubViews)
            view.SetNeedsDraw();
    }

    /// <summary>Pushes screen state into child views before they are redrawn.</summary>
    protected virtual void UpdateViews()
    {
    }
}
