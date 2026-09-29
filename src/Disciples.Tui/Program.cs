using Disciples.Core;
using Disciples.Core.Persistence;
using Disciples.Tui;
using Disciples.Tui.Content;
using Disciples.Tui.Screens;
using Terminal.Gui.App;

var loader = new ContentLoader(Path.Combine(AppContext.BaseDirectory, "content"));
var session = SnapshotMapper.Restore(loader.LoadScenario("test-valley"), loader.LoadContent(), new SystemRandom());

using IApplication app = Application.Create();
app.Init();
using var shell = new Shell();
shell.Push(new MapScreen(session));
app.Run(shell);
