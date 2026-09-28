namespace Disciples.Core.Session
{
    public enum MoveResult
    {
        Moved,
        OutOfBounds,
        Impassable,
        NotEnoughMovement,
        EnemyEncountered
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
