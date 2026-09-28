using Spectre.Console;
using Spectre.Console.Rendering;

namespace Disciples.ConsoleApp.Screens;

public sealed class GameOverScreen(ScreenStack screens) : IScreen
{
    public IRenderable Render(int width, int height) =>
        new Rows(
            new FigletText("Defeat").Color(Color.Red1),
            new Markup("[grey]Your leader has fallen. Press any key to exit.[/]"));

    public void HandleKey(ConsoleKeyInfo key) => screens.Clear();
}
