using Spectre.Console.Rendering;

namespace Disciples.ConsoleApp.Screens;

public interface IScreen
{
    IRenderable Render(int width, int height);

    /// <returns>False when the game should exit.</returns>
    bool HandleKey(ConsoleKeyInfo key);
}
