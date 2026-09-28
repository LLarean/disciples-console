using Disciples.ConsoleApp.Rendering;
using Disciples.Core.Battles;
using Disciples.Core.Map;
using Disciples.Core.Session;
using Disciples.Core.Squads;
using Disciples.Core.Units;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Disciples.ConsoleApp.Screens;

public sealed class BattleScreen : IScreen
{
    private const int QueueWidth = 22;
    private const int LogSize = 7;

    private static readonly Color ActorColor = Color.DeepSkyBlue1;
    private static readonly Color TargetColor = Color.Red1;
    private static readonly Color HealColor = Color.Green3;

    private readonly GameSession _session;
    private readonly NeutralSquad _neutral;
    private readonly ScreenStack _screens;
    private readonly Battle _battle;
    private readonly SimpleBattleAi _ai;
    private int _targetIndex;

    public BattleScreen(GameSession session, NeutralSquad neutral, ScreenStack screens)
    {
        _session = session;
        _neutral = neutral;
        _screens = screens;
        _battle = session.StartBattle(neutral);
        _ai = new SimpleBattleAi(session.Random);
        RunEnemyTurns();
    }

    private IReadOnlyList<Unit> Targets => _battle.ValidTargets();
    private Unit? SelectedTarget => Targets.Count == 0 ? null : Targets[Math.Min(_targetIndex, Targets.Count - 1)];

    public IRenderable Render(int width, int height)
    {
        var field = new Grid()
            .AddColumn(new GridColumn().NoWrap())
            .AddColumn(new GridColumn().Width(QueueWidth))
            .AddColumn(new GridColumn().NoWrap())
            .AddRow(PlayerView().Render(), RenderQueue(), EnemyView().Render());

        return new Rows(RenderHeader(), field, RenderLog(), RenderHints());
    }

    public void HandleKey(ConsoleKeyInfo key)
    {
        if (_battle.IsOver)
        {
            Finish();
            return;
        }

        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
            case ConsoleKey.LeftArrow:
                CycleTarget(-1);
                break;
            case ConsoleKey.DownArrow:
            case ConsoleKey.RightArrow:
                CycleTarget(1);
                break;
            case ConsoleKey.Enter when SelectedTarget is { } target:
                _battle.Act(target);
                AfterPlayerAction();
                break;
            case ConsoleKey.D:
                _battle.Defend();
                AfterPlayerAction();
                break;
            case ConsoleKey.A:
                _ai.Act(_battle);
                AfterPlayerAction();
                break;
            case ConsoleKey.X:
                _battle.Retreat();
                break;
        }
    }

    private void CycleTarget(int delta)
    {
        var count = Targets.Count;
        if (count > 0)
            _targetIndex = (Math.Min(_targetIndex, count - 1) + delta + count) % count;
    }

    private void AfterPlayerAction()
    {
        RunEnemyTurns();
        _targetIndex = 0;
    }

    private void RunEnemyTurns()
    {
        while (!_battle.IsOver && !_battle.IsAttackersTurn)
            _ai.Act(_battle);
    }

    private void Finish()
    {
        _session.FinishBattle(_battle, _neutral);
        if (_session.IsGameOver)
            _screens.Replace(new GameOverScreen(_screens));
        else
            _screens.Pop();
    }

    private SquadView PlayerView()
    {
        return new SquadView(_battle.Attackers)
        {
            Title = _session.Party.Name,
            IsFocused = _battle.IsAttackersTurn,
            Cursor = CursorIn(_battle.Attackers),
            Highlight = HighlightOf
        };
    }

    private SquadView EnemyView()
    {
        return new SquadView(_battle.Defenders)
        {
            Title = _neutral.Name,
            FacesLeft = true,
            Cursor = CursorIn(_battle.Defenders),
            Highlight = HighlightOf
        };
    }

    private SquadSlot? CursorIn(Squad squad) =>
        SelectedTarget is { } target && squad.Contains(target) ? squad.SlotOf(target) : null;

    private Color? HighlightOf(Unit unit)
    {
        if (unit == _battle.Current)
            return ActorColor;

        if (!Targets.Contains(unit))
            return null;

        return _battle.Current?.Definition.AttackType == AttackType.Heal ? HealColor : TargetColor;
    }

    private Markup RenderHeader()
    {
        var status = _battle.Outcome switch
        {
            BattleOutcome.Victory => $"[bold green3]Victory![/] [gold1]+{_neutral.Reward} gold[/]",
            BattleOutcome.Defeat => "[bold red]Defeat. Your party has fallen.[/]",
            BattleOutcome.Retreat => "[yellow]You retreat from the battle.[/]",
            _ => $"[grey]round[/] {_battle.Round}   [grey]acting[/] [bold]{Markup.Escape(_battle.Current?.Name ?? "")}[/]"
        };
        return new Markup($"[bold gold1]Battle[/] [grey]vs[/] [bold red]{Markup.Escape(_neutral.Name)}[/]   {status}");
    }

    private Panel RenderQueue()
    {
        var lines = _battle.Queue.Select((unit, i) =>
        {
            var color = _battle.Attackers.Contains(unit) ? "deepskyblue1" : "red1";
            var marker = i == 0 ? "[gold1]›[/]" : " ";
            var name = Markup.Escape(unit.Name);
            return new Markup(i == 0 ? $"{marker} [bold {color}]{name}[/]" : $"{marker} [{color}]{name}[/]");
        });

        return new Panel(new Rows(lines))
            .Header(" Turn order ")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Grey50)
            .Expand();
    }

    private Panel RenderLog()
    {
        var events = _battle.Log.Skip(Math.Max(0, _battle.Log.Count - LogSize)).ToList();
        var lines = events.Select((e, i) => new Markup(Describe(e), new Style(i == events.Count - 1 ? Color.Grey93 : Color.Grey62)));
        return new Panel(new Rows(lines)).Header(" Log ").Border(BoxBorder.Rounded).BorderColor(Color.Grey35).Expand();
    }

    private string Describe(BattleEvent e)
    {
        var actor = Name(e.Actor);
        var target = Name(e.Target);
        return e.Kind switch
        {
            BattleEventKind.RoundStarted => $"[grey]── Round {e.Round} ──[/]",
            BattleEventKind.Hit => $"{actor} hits {target} for [bold]{e.Amount}[/]",
            BattleEventKind.Miss => $"{actor} misses {target}",
            BattleEventKind.Healed => $"{actor} heals {target} for [green3]{e.Amount}[/]",
            BattleEventKind.Defended => $"{actor} defends",
            BattleEventKind.Killed => $"{target} [red]dies[/]",
            BattleEventKind.Retreated => "[yellow]The party retreats[/]",
            _ => e.Kind.ToString()
        };
    }

    private string Name(Unit? unit)
    {
        if (unit == null)
            return "";

        var color = _battle.Attackers.Contains(unit) ? "deepskyblue1" : "red1";
        return $"[{color}]{Markup.Escape(unit.Name)}[/]";
    }

    private Markup RenderHints()
    {
        if (_battle.IsOver)
            return new Markup("[gold1]Any key[/] [grey]continue[/]");

        var enter = _battle.Current?.Definition.AttackType switch
        {
            AttackType.Heal => "heal",
            AttackType.AllEnemies => "attack all",
            _ => "attack"
        };
        return new Markup(
            $"[gold1]←↑→↓[/] [grey]target[/]   [gold1]Enter[/] [grey]{enter}[/]   [gold1]D[/] [grey]defend[/]   " +
            "[gold1]A[/] [grey]auto[/]   [gold1]X[/] [grey]retreat (end battle)[/]");
    }
}
