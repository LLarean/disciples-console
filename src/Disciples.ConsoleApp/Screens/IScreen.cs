using Spectre.Console.Rendering;

namespace Disciples.ConsoleApp.Screens;

public interface IScreen
{
    IRenderable Render(int width, int height);

    void HandleKey(ConsoleKeyInfo key);
}
