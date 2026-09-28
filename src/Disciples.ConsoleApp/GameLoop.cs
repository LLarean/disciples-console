using Disciples.ConsoleApp.Screens;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Disciples.ConsoleApp;

public sealed class GameLoop(ScreenStack screens)
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
                context.Refresh();
                while (screens.Current is { } screen)
                {
                    screen.HandleKey(Console.ReadKey(true));
                    if (screens.Current == null)
                        break;

                    context.UpdateTarget(Render());
                }
            });

        Console.CursorVisible = true;
    }

    private IRenderable Render() => screens.Current!.Render(Console.WindowWidth, Console.WindowHeight);
}
