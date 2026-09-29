using Disciples.ConsoleApp.Content;
using Disciples.Tui;
using Disciples.Tui.Screens;
using Terminal.Gui.App;

var session = new ContentLoader(Path.Combine(AppContext.BaseDirectory, "content")).LoadSession("test-valley");

using IApplication app = Application.Create();
app.Init();
using var shell = new Shell();
shell.Push(new MapScreen(session));
app.Run(shell);
