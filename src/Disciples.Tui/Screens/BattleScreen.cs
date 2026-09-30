using Disciples.Core.Battles;
using Disciples.Core.Session;
using Disciples.Core.Squads;
using Disciples.Core.Units;
using Disciples.Tui.Widgets;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;

namespace Disciples.Tui.Screens;

public sealed class BattleScreen : Screen
{
    private const int QueueWidth = 22;
    private const int LogSize = 7;

    private readonly GameFlow _flow;
    private readonly GameSession _session;
    private readonly Encounter _encounter;
    private readonly Battle _battle;
    private readonly SimpleBattleAi _ai;
    private readonly SquadView _playerView;
    private readonly SquadView _enemyView;
    private int _targetIndex;
    private BattleReport? _report;

    public BattleScreen(GameFlow flow, GameSession session, Encounter encounter)
    {
        _flow = flow;
        _session = session;
        _encounter = encounter;
        _battle = session.StartBattle(encounter);
        _ai = new SimpleBattleAi(session.Random);
        RunEnemyTurns();

        var header = new Canvas(DrawHeader) { X = 0, Y = 0, Width = Dim.Fill(), Height = 1 };
        _playerView = new SquadView(session.Party.Name) { X = 0, Y = 1, Squad = _battle.Attackers };
        var queue = new Canvas(DrawQueue, "Turn order") { X = Pos.Right(_playerView) + 1, Y = 1, Width = QueueWidth, Height = SquadView.PanelHeight };
        _enemyView = new SquadView(encounter.Name, facesLeft: true) { X = Pos.Right(queue) + 1, Y = 1, Squad = _battle.Defenders };
        var log = new Canvas(DrawLog, "Log") { X = 0, Y = Pos.Bottom(_playerView), Width = Dim.Fill(), Height = LogSize + 2 };
        var hints = new HintBar(Hints);
        Add(header, _playerView, queue, _enemyView, log, hints);
    }

    private IReadOnlyList<Unit> Targets => _battle.ValidTargets();
    private Unit? SelectedTarget => Targets.Count == 0 ? null : Targets[Math.Min(_targetIndex, Targets.Count - 1)];

    public override bool HandleKey(Key key)
    {
        if (_report != null)
        {
            Leave();
            return true;
        }

        if (key == Key.CursorUp || key == Key.CursorLeft)
            CycleTarget(-1);
        else if (key == Key.CursorDown || key == Key.CursorRight)
            CycleTarget(1);
        else if (key == Key.Enter && SelectedTarget is { } target)
            AfterPlayerAction(() => _battle.Act(target));
        else if (key == Key.D)
            AfterPlayerAction(_battle.Defend);
        else if (key == Key.W && _battle.CanWait)
            AfterPlayerAction(() => _battle.Wait());
        else if (key == Key.A)
            AfterPlayerAction(() => _ai.Act(_battle));
        else if (key == Key.X)
            AfterPlayerAction(_battle.Retreat);
        else
            return false;

        return true;
    }

    protected override void UpdateViews()
    {
        _playerView.IsFocused = _battle.IsAttackersTurn && !_battle.IsOver;
        _playerView.Selection = CursorIn(_battle.Attackers);
        _playerView.Highlight = HighlightOf;
        _enemyView.Selection = CursorIn(_battle.Defenders);
        _enemyView.Highlight = HighlightOf;
    }

    private void CycleTarget(int delta)
    {
        var count = Targets.Count;
        if (count > 0)
            _targetIndex = (Math.Min(_targetIndex, count - 1) + delta + count) % count;
    }

    private void AfterPlayerAction(Action action)
    {
        action();
        RunEnemyTurns();
        _targetIndex = 0;
    }

    private void RunEnemyTurns()
    {
        while (!_battle.IsOver && !_battle.IsAttackersTurn)
            _ai.Act(_battle);

        if (_battle.IsOver && _report == null)
            _report = _session.FinishBattle(_battle, _encounter);
    }

    private void Leave()
    {
        if (_session.Status == GameStatus.Playing)
            Shell.Pop();
        else
            _flow.EndGame(_session);
    }

    private SquadSlot? CursorIn(Squad squad) =>
        !_battle.IsOver && SelectedTarget is { } target && squad.Contains(target) ? squad.SlotOf(target) : null;

    private Color? HighlightOf(Unit unit)
    {
        if (_battle.IsOver)
            return null;

        if (unit == _battle.Current)
            return Palette.Ally;

        if (!Targets.Contains(unit))
            return null;

        return _battle.Current?.Definition.AttackType == AttackType.Heal ? Palette.Good : Palette.Enemy;
    }

    private Color SideColor(Unit unit) => _battle.Attackers.Contains(unit) ? Palette.Ally : Palette.Enemy;

    private void DrawHeader(Canvas canvas)
    {
        var x = canvas.Text(1, 0, "Battle", Palette.Accent, style: TextStyle.Bold);
        x = canvas.Text(x + 1, 0, "vs", Palette.Dim);
        x = canvas.Text(x + 1, 0, _encounter.Name, Palette.Enemy, style: TextStyle.Bold) + 3;

        switch (_battle.Outcome)
        {
            case BattleOutcome.Victory:
                x = canvas.Text(x, 0, "Victory!", Palette.Good, style: TextStyle.Bold);
                x = canvas.Text(x + 1, 0, $"+{_report?.Gold} gold", Palette.Accent);
                canvas.Text(x + 1, 0, $"+{_report?.Experience} exp", Palette.Ally);
                break;
            case BattleOutcome.Defeat:
                canvas.Text(x, 0, "Defeat. Your party has fallen.", Palette.Bad, style: TextStyle.Bold);
                break;
            case BattleOutcome.Retreat:
                canvas.Text(x, 0, "You retreat from the battle.", Palette.Warn);
                break;
            default:
                x = canvas.Text(x, 0, "round ", Palette.Dim);
                x = canvas.Text(x, 0, _battle.Round.ToString(), Palette.Text);
                x = canvas.Text(x + 3, 0, "acting ", Palette.Dim);
                canvas.Text(x, 0, _battle.Current?.Name ?? "", Palette.Text, style: TextStyle.Bold);
                break;
        }
    }

    private void DrawQueue(Canvas canvas)
    {
        var y = 0;
        foreach (var unit in _battle.Queue)
        {
            var first = y == 0;
            canvas.Text(0, y, first ? "›" : " ", Palette.Accent);
            canvas.Text(2, y, ViewDrawing.Fit(unit.Name, canvas.Viewport.Width - 2), SideColor(unit), style: first ? TextStyle.Bold : TextStyle.None);
            y++;
        }
    }

    private void DrawLog(Canvas canvas)
    {
        if (_report != null)
        {
            DrawReport(canvas, _report);
            return;
        }

        var log = _battle.Log;
        var start = Math.Max(0, log.Count - LogSize);
        for (var i = start; i < log.Count; i++)
        {
            var text = i == log.Count - 1 ? Palette.Text : Palette.Dim;
            var x = 1;
            foreach (var (segment, color) in Describe(log[i], text))
                x = canvas.Text(x, i - start, segment, color);
        }
    }

    private void DrawReport(Canvas canvas, BattleReport report)
    {
        var lines = new List<(string Text, Color Color)>();
        if (report.Outcome == BattleOutcome.Victory)
            lines.Add(($"Gained {report.Gold} gold and {report.Experience} experience.", Palette.Accent));
        lines.AddRange(report.Progress.Select(p => (Describe(p), p.Kind == ProgressKind.WaitingForBuilding ? Palette.Warn : Palette.Good)));
        if (report.CapturedCity is { } city)
            lines.Add(($"{city.Name} is captured.", Palette.Good));

        for (var i = 0; i < lines.Count && i < LogSize; i++)
            canvas.Text(1, i, lines[i].Text, lines[i].Color);
    }

    private string Describe(UnitProgress progress) => progress.Kind switch
    {
        ProgressKind.LeveledUp => $"{progress.Unit.Name} reached level {progress.Unit.Level}.",
        ProgressKind.Upgraded => $"{progress.PreviousName} became {progress.Unit.Name}.",
        _ => $"{progress.Unit.Name} needs {_session.Content.BuildingName(progress.Building ?? "")} in the capital to grow."
    };

    private IEnumerable<(string, Color)> Describe(BattleEvent e, Color text)
    {
        switch (e.Kind)
        {
            case BattleEventKind.RoundStarted:
                yield return ($"── Round {e.Round} ──", Palette.Dim);
                break;
            case BattleEventKind.Hit:
                yield return Name(e.Actor);
                yield return (" hits ", text);
                yield return Name(e.Target);
                yield return ($" for {e.Amount}", text);
                break;
            case BattleEventKind.Miss:
                yield return Name(e.Actor);
                yield return (" misses ", text);
                yield return Name(e.Target);
                break;
            case BattleEventKind.Immune:
                yield return Name(e.Target);
                yield return (" is immune to ", text);
                yield return Name(e.Actor);
                break;
            case BattleEventKind.Warded:
                yield return Name(e.Target);
                yield return (" wards off ", text);
                yield return Name(e.Actor);
                break;
            case BattleEventKind.Healed:
                yield return Name(e.Actor);
                yield return (" heals ", text);
                yield return Name(e.Target);
                yield return (" for ", text);
                yield return (e.Amount.ToString(), Palette.Good);
                break;
            case BattleEventKind.Defended:
                yield return Name(e.Actor);
                yield return (" defends", text);
                break;
            case BattleEventKind.Waited:
                yield return Name(e.Actor);
                yield return (" waits", text);
                break;
            case BattleEventKind.Killed:
                yield return Name(e.Target);
                yield return (" dies", Palette.Bad);
                break;
            case BattleEventKind.Retreated:
                yield return ("The party retreats", Palette.Warn);
                break;
            default:
                yield return (e.Kind.ToString(), text);
                break;
        }
    }

    private (string, Color) Name(Unit? unit) => unit == null ? ("", Palette.Text) : (unit.Name, SideColor(unit));

    private IEnumerable<(string, string)> Hints()
    {
        if (_report != null)
        {
            yield return ("Any key", "continue");
            yield break;
        }

        var enter = _battle.Current?.Definition.AttackType switch
        {
            AttackType.Heal => "heal",
            AttackType.AllEnemies => "attack all",
            _ => "attack"
        };
        yield return ("←↑→↓", "target");
        yield return ("Enter", enter);
        yield return ("D", "defend");
        if (_battle.CanWait)
            yield return ("W", "wait");
        yield return ("A", "auto");
        yield return ("X", "retreat (end battle)");
    }
}
