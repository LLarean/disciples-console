namespace Disciples.ConsoleApp.Screens;

public sealed class ScreenStack
{
    private readonly Stack<IScreen> _screens = new();

    public IScreen? Current => _screens.TryPeek(out var screen) ? screen : null;

    public void Push(IScreen screen) => _screens.Push(screen);

    public void Pop() => _screens.TryPop(out _);

    public void Replace(IScreen screen)
    {
        Pop();
        Push(screen);
    }

    public void Clear() => _screens.Clear();
}
