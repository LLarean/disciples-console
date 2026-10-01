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

    public enum BuildResult
    {
        Built,
        AlreadyBuilt,
        NotEnoughGold,
        RequirementMissing
    }
}
