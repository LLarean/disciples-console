using Disciples.Core.Session;
using Terminal.Gui.Drawing;
using Attribute = Terminal.Gui.Drawing.Attribute;

namespace Disciples.Tui.Widgets;

public sealed class PartyView : Canvas
{
    private readonly GameSession _session;
    private readonly IReadOnlyList<string> _log;

    public PartyView(GameSession session, IReadOnlyList<string> log) : base(title: "Leader")
    {
        _session = session;
        _log = log;
    }

    protected override void Draw()
    {
        var y = DrawHeader(0);
        y = DrawSquad(y + 1);
        y = DrawLegend(y + 1);
        DrawLog(y + 1);
    }

    private int DrawHeader(int y)
    {
        var party = _session.Party;
        var x = this.Text(0, y, party.Name, Palette.Accent, style: TextStyle.Bold);
        x = this.Text(x + 2, y, "turn ", Palette.Dim);
        x = this.Text(x, y, _session.Turn.ToString(), Palette.Text, style: TextStyle.Bold);
        this.Text(x + 2, y, $"{_session.Gold}g", Palette.Accent);

        y++;
        x = this.Text(0, y, "Lvl  ", Palette.Dim);
        x = this.Text(x, y, party.Leader.Level.ToString(), Palette.Text, style: TextStyle.Bold);
        if (party.UnspentPerks > 0)
            this.Text(x + 2, y, $"{party.UnspentPerks} perk(s), press L", Palette.Good);

        y++;
        x = this.Text(0, y, "Move ", Palette.Dim);
        var moveColor = party.MovementPoints == 0 ? Palette.Bad : Palette.Good;
        x = this.Bar(x, y, party.MovementPoints, party.MaxMovementPoints, 14, moveColor);
        this.Text(x + 1, y, $"{party.MovementPoints}/{party.MaxMovementPoints}", Palette.Text);

        y++;
        x = this.Text(0, y, "At   ", Palette.Dim);
        if (_session.CurrentCity is { } city)
        {
            x = this.Text(x, y, city.Name, Palette.Text, style: TextStyle.Bold);
            if (city.IsCapital)
                this.Text(x + 1, y, "(capital)", Palette.Accent);
        }
        else
        {
            var position = party.Position;
            x = this.Text(x, y, _session.Map.TerrainAt(position).Name, Palette.Text);
            this.Text(x + 1, y, position.ToString(), Palette.Dim);
        }

        return y + 1;
    }

    private int DrawSquad(int y)
    {
        var squad = _session.Party.Squad;
        this.Section(y, "Squad", $"{squad.UsedSlots}/{squad.Capacity}");
        foreach (var unit in squad.Units)
        {
            y++;
            var hpWidth = $"{unit.Hp}/{unit.MaxHp}".Length;
            this.Text(0, y, unit.Name, unit.IsLeader ? Palette.Accent : Palette.Text);
            var x = this.HpBar(Viewport.Width - hpWidth - 7, y, unit, 6);
            this.Hp(x + 1, y, unit);
        }

        return y + 1;
    }

    private int DrawLegend(int y)
    {
        this.Section(y, "Legend");
        var items = Palette.TerrainIds
            .Select(id => (Glyph: Palette.LegendGlyph(id), Attribute: Palette.TerrainAttribute(id), Name: char.ToUpper(id[0]) + id[1..]))
            .Append(("◆", new Attribute(MapView.CapitalColors.Foreground, MapView.CapitalColors.Background), "Capital"))
            .Append(("■", new Attribute(MapView.CityColors.Foreground, MapView.CityColors.Background), "City"))
            .Append(("■", new Attribute(MapView.HostileCityColors.Foreground, MapView.HostileCityColors.Background), "Hostile"))
            .Append(("†", new Attribute(Palette.Enemy, Palette.Background), "Enemy"))
            .Append(("&", new Attribute(Palette.Enemy, Palette.Background), "Enemy lord"))
            .Append(("$", new Attribute(Palette.Accent, Palette.Background), "Treasure"))
            .Append(("¤", new Attribute(Palette.Accent, Palette.Background), "Mine"))
            .Append(("▲", new Attribute(Palette.Accent, Palette.Background), "Camp"))
            .Append(("@", new Attribute(MapView.LeaderColor, Palette.Background), "You"))
            .ToList();

        for (var i = 0; i < items.Count; i++)
        {
            var (glyph, attribute, name) = items[i];
            var row = y + 1 + i / 2;
            var x = this.Text(i % 2 * (Viewport.Width / 2), row, glyph + " ", attribute.Foreground, attribute.Background);
            this.Text(x + 1, row, name, Palette.Text);
        }

        return y + 1 + (items.Count + 1) / 2;
    }

    private void DrawLog(int y)
    {
        this.Section(y, "Log");
        var visible = Math.Max(0, Viewport.Height - y - 1);
        var lines = _log.Skip(Math.Max(0, _log.Count - visible)).ToList();
        for (var i = 0; i < lines.Count; i++)
            this.Text(0, y + 1 + i, ViewDrawing.Fit(lines[i], Viewport.Width), i == lines.Count - 1 ? Palette.Text : Palette.Dim);
    }
}
