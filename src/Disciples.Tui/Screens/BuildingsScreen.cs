using Disciples.Core.Cities;
using Disciples.Core.Session;
using Disciples.Tui.Widgets;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;

namespace Disciples.Tui.Screens;

public sealed class BuildingsScreen : Screen
{
    private static readonly (string Title, int Width)[] Columns = [("Branch", 12), ("Building", 24), ("Cost", 6), ("Status", 18), ("Effect", 0)];

    private readonly GameSession _session;
    private readonly City _city;
    private int _index;
    private (string Text, Color Color) _message = ("", Palette.Text);

    public BuildingsScreen(GameSession session, City city)
    {
        _session = session;
        _city = city;

        var header = new Canvas(DrawHeader) { X = 0, Y = 0, Width = Dim.Fill(), Height = 1 };
        var table = new Canvas(DrawTable, "Buildings") { X = 0, Y = 1, Width = Dim.Fill(), Height = city.Buildings.Count + 3 };
        table.IsFocused = true;
        var message = new Canvas(c => c.Text(1, 0, _message.Text, _message.Color)) { X = 0, Y = Pos.Bottom(table), Width = Dim.Fill(), Height = 1 };
        var hints = new HintBar(() => [("↑↓", "select"), ("Enter", "build"), ("B/Esc", "back")]);
        Add(header, table, message, hints);
    }

    public override bool HandleKey(Key key)
    {
        _message = ("", Palette.Text);
        var count = _city.Buildings.Count;

        if (key == Key.Esc || key == Key.B)
            Shell.Pop();
        else if (key == Key.CursorUp && count > 0)
            _index = (_index + count - 1) % count;
        else if (key == Key.CursorDown && count > 0)
            _index = (_index + 1) % count;
        else if (key == Key.Enter && count > 0)
            Build(_city.Buildings[_index]);
        else
            return false;

        return true;
    }

    private void Build(Building building)
    {
        _message = _session.Build(_city, building) switch
        {
            BuildResult.Built => ($"{building.Name} built.", Palette.Good),
            BuildResult.AlreadyBuilt => ($"{building.Name} is already built.", Palette.Dim),
            BuildResult.RequirementMissing => ($"Requires {RequirementName(building)}.", Palette.Bad),
            _ => ($"Not enough gold for {building.Name}.", Palette.Bad)
        };
    }

    private string RequirementName(Building building) =>
        building.Requires is { } id ? _city.FindBuilding(id)?.Name ?? id : "";

    private void DrawHeader(Canvas canvas)
    {
        var x = canvas.Text(1, 0, _city.Name, Palette.Accent, style: TextStyle.Bold);
        x = canvas.Text(x + 2, 0, "buildings", Palette.Dim);
        x = canvas.Text(x + 3, 0, "gold ", Palette.Dim);
        canvas.Text(x, 0, _session.Gold.ToString(), Palette.Accent);
    }

    private void DrawTable(Canvas canvas)
    {
        var x = 0;
        foreach (var (title, width) in Columns)
        {
            canvas.Text(x + 2, 0, title, Palette.Dim);
            x += width;
        }

        canvas.Text(0, 1, new string('─', canvas.Viewport.Width), Palette.Faint);

        var buildings = _city.Buildings;
        for (var i = 0; i < buildings.Count; i++)
            DrawRow(canvas, i + 2, buildings[i], i == _index, i == 0 || buildings[i - 1].Branch != buildings[i].Branch);
    }

    private void DrawRow(Canvas canvas, int y, Building building, bool selected, bool showBranch)
    {
        var x = 0;
        canvas.Text(x, y, selected ? "›" : " ", Palette.Accent);
        if (showBranch)
            canvas.Text(x + 2, y, building.Branch, Palette.Dim);

        x += Columns[0].Width;
        canvas.Text(x + 2, y, building.Name, selected ? Palette.Accent : Palette.Text, style: selected ? TextStyle.Bold : TextStyle.None);
        x += Columns[1].Width;
        canvas.Text(x + 2, y, building.Cost.ToString(), Palette.Text);
        x += Columns[2].Width;
        var (status, color) = Status(building);
        canvas.Text(x + 2, y, status, color);
        x += Columns[3].Width;
        canvas.Text(x + 2, y, ViewDrawing.Fit(building.Description, canvas.Viewport.Width - x - 2), Palette.Dim);
    }

    private (string, Color) Status(Building building)
    {
        if (building.IsBuilt)
            return ("built", Palette.Good);

        if (building.Requires != null && _city.FindBuilding(building.Requires)?.IsBuilt != true)
            return ($"needs {RequirementName(building)}", Palette.Faint);

        return building.Cost <= _session.Gold ? ("available", Palette.Accent) : ("no gold", Palette.Bad);
    }
}
