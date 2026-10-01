namespace Disciples.Tui.Screens;

/// <summary>Yes/no question; "No" is preselected and Esc cancels.</summary>
public sealed class ConfirmScreen(string question, string detail, Action confirm) : MenuScreen
{
    protected override IReadOnlyList<string> Heading => [question, detail];

    protected override IReadOnlyList<MenuItem> Items => [new("No", Back), new("Yes", Confirm)];

    protected override string BackLabel => "cancel";

    protected override void Back() => Shell.Pop();

    private void Confirm()
    {
        Shell.Pop();
        confirm();
    }
}
