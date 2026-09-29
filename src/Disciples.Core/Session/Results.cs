namespace Disciples.Core.Session
{
    public enum MoveResult
    {
        Moved,
        OutOfBounds,
        Impassable,
        NotEnoughMovement,
        EnemyEncountered,
        CityCaptured
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
        NotEnoughGold,
        NoRoom
    }

    public enum BuildResult
    {
        Built,
        AlreadyBuilt,
        NotEnoughGold,
        RequirementMissing
    }
}
