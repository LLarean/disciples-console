namespace Disciples.Core.Session
{
    public enum MoveResult
    {
        Moved,
        OutOfBounds,
        Occupied,
        Impassable,
        NotEnoughMovement,
        EnemyEncountered,
        CityCaptured,
        SiteVisited
    }

    public enum GameStatus
    {
        Playing,
        Won,
        Lost
    }

    public enum HireResult
    {
        HiredToParty,
        HiredToGarrison,
        LeaderHired,
        NotEnoughGold,
        NoRoom,
        Unavailable
    }

    public enum ItemResult
    {
        Used,
        NoEffect,
        Unavailable
    }

    public enum BuyResult
    {
        Bought,
        NotEnoughGold,
        Unavailable
    }

    public enum UpgradeResult
    {
        Upgraded,
        TopTier,
        NotEnoughGold,
        Unavailable
    }

    public enum ResearchResult
    {
        Researched,
        AlreadyKnown,
        NoTower,
        AlreadyResearched,
        NotEnoughMana
    }

    public enum CastResult
    {
        Cast,
        Unknown,
        AlreadyCast,
        NotEnoughMana,
        NoTarget
    }

    public enum RodResult
    {
        Planted,
        NotARodBearer,
        Occupied,
        NotEnoughGold
    }

    public enum ThiefResult
    {
        Done,
        Caught,
        NotAThief,
        Exhausted,
        NoTarget,
        Pointless
    }

    public enum BuildResult
    {
        Built,
        AlreadyBuilt,
        NotEnoughGold,
        RequirementMissing
    }
}
