using Disciples.Tui;
using Disciples.Tui.Content;
using Disciples.Tui.Persistence;
using Terminal.Gui.App;

var loader = new ContentLoader(Path.Combine(AppContext.BaseDirectory, "content"));

using IApplication app = Application.Create();
app.Init();
using var shell = new Shell();
new GameFlow(shell, loader, new SaveStore(SaveStore.DefaultDirectory)).ShowMainMenu();
app.Run(shell);
