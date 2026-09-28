using System.Text;
using Disciples.ConsoleApp;
using Disciples.ConsoleApp.Content;
using Disciples.ConsoleApp.Screens;
using Spectre.Console;

Console.OutputEncoding = Encoding.UTF8;

var session = new ContentLoader(Path.Combine(AppContext.BaseDirectory, "content")).LoadSession("test-valley");

AnsiConsole.Clear();
AnsiConsole.Write(new FigletText("Disciples").Color(Color.Gold1));
AnsiConsole.MarkupLine("[grey]Console prototype. Press any key to start...[/]");
Console.ReadKey(true);

var screens = new ScreenStack();
screens.Push(new MapScreen(session, screens));
new GameLoop(screens).Run();
