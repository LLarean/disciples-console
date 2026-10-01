using Disciples.Core;
using Disciples.Core.Content;
using Disciples.Core.Persistence;
using Disciples.Core.Session;
using Disciples.Core.Units;
using Disciples.Tui.Content;
using Disciples.Tui.Persistence;
using Disciples.Tui.Screens;

namespace Disciples.Tui;

/// <summary>Application coordinator: owns content and saves, switches between menu and game screens.</summary>
public sealed class GameFlow(Shell shell, ContentLoader loader, SaveStore saves)
{
    private const string Scenario = "test-valley";

    private GameContent? _content;

    public SaveStore Saves => saves;
    private GameContent Content => _content ??= loader.LoadContent();

    public void ShowMainMenu() => shell.Reset(new MainMenuScreen(this));

    public IReadOnlyList<Race> Races => Content.Races;

    public IEnumerable<UnitDefinition> LeadersOf(Race race) => Content.LeadersOf(race);

    public IEnumerable<UnitDefinition> RecruitsOf(Race race) => Content.RecruitsOf(race);

    public void StartNewGame(Race race, UnitDefinition leader)
    {
        var snapshot = loader.LoadScenario(Scenario);
        SnapshotMapper.StartAs(snapshot, race, leader, Content);
        Play(snapshot);
    }

    /// <exception cref="ContentException">The save does not match the current content.</exception>
    public void LoadGame(int slot) => Play(saves.Load(slot));

    public void SaveGame(GameSession session, int slot) => saves.Save(slot, SnapshotMapper.Capture(session));

    public void EndGame(GameSession session) =>
        shell.Reset(new GameEndScreen(session.Status == GameStatus.Won, session.Turn, ShowMainMenu));

    public void Quit() => shell.Quit();

    private void Play(GameSnapshot snapshot)
    {
        var session = SnapshotMapper.Restore(snapshot, Content, new SystemRandom());
        shell.Reset(new MapScreen(this, session));
    }
}
