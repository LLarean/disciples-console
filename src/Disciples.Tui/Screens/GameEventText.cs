using Disciples.Core.Session;

namespace Disciples.Tui.Screens;

public static class GameEventText
{
    public static string Describe(GameEvent e) => e.Kind switch
    {
        GameEventKind.TurnStarted => $"Turn {e.Amount}.",
        GameEventKind.BattleWon => e.Amount > 0 ? $"Defeated {e.Subject}, +{e.Amount} gold." : $"Defeated {e.Subject}.",
        GameEventKind.CityCaptured => $"{e.Subject} is now yours.",
        GameEventKind.EnemyAttacks => $"{e.Subject} attacks you!",
        GameEventKind.CityFell => $"{e.Subject} falls to {e.Detail}.",
        GameEventKind.CityHeld => $"{e.Subject} holds against {e.Detail}.",
        GameEventKind.TreasureFound => e.Amount > 0 ? $"Found {e.Subject}: +{e.Amount} gold." : $"Found {e.Subject}.",
        GameEventKind.ItemFound => $"{e.Subject} goes into the bag.",
        GameEventKind.MineCaptured => $"{e.Subject} is yours: +{e.Amount} gold per turn.",
        GameEventKind.MineLost => $"{e.Subject} fell to enemy land.",
        GameEventKind.UnitLeveledUp => $"{e.Subject} reached level {e.Amount}.",
        GameEventKind.UnitUpgraded => $"{e.Subject} became {e.Detail}.",
        GameEventKind.UnitAwaitsBuilding => $"{e.Subject} needs {e.Detail} in the capital to grow.",
        GameEventKind.GameWon => "All enemies are defeated. Victory!",
        GameEventKind.PartyLost => $"{e.Subject} has fallen; the party is no more.",
        GameEventKind.GameLost => "No leader is left. Defeat.",
        _ => e.Kind.ToString()
    };
}
