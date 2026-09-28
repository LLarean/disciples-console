using Disciples.ConsoleApp.Screens;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Disciples.ConsoleApp;

public sealed class GameLoop(IScreen screen)
{
    public void Run()
    {
        Console.CursorVisible = false;
        AnsiConsole.Clear();

        AnsiConsole.Live(Render())
            .AutoClear(true)
            .Overflow(VerticalOverflow.Crop)
            .Cropping(VerticalOverflowCropping.Bottom)
            .Start(context =>
            {
                while (screen.HandleKey(Console.ReadKey(true)))
                    context.UpdateTarget(Render());
            });

        Console.CursorVisible = true;
    }

    private IRenderable Render() => screen.Render(Console.WindowWidth, Console.WindowHeight);
}
