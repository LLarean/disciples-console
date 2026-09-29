namespace Disciples.Core.Session
{
    public enum GameEventKind
    {
        TurnStarted,
        BattleWon,
        CityCaptured,
        UnitLeveledUp,
        UnitUpgraded,
        UnitAwaitsBuilding,
        GameWon,
        GameLost
    }

    /// <summary>Something that happened in the session, for the front-end to report; subjects are display names.</summary>
    public sealed class GameEvent
    {
        public GameEvent(GameEventKind kind, string subject = "", string detail = "", int amount = 0)
        {
            Kind = kind;
            Subject = subject;
            Detail = detail;
            Amount = amount;
        }

        public GameEventKind Kind { get; }
        public string Subject { get; }
        public string Detail { get; }
        public int Amount { get; }
    }
}
