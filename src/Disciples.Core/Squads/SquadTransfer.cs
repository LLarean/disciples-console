namespace Disciples.Core.Squads
{
    public static class SquadTransfer
    {
        /// <summary>
        /// Moves the unit at <paramref name="fromSlot"/> to <paramref name="toSlot"/>, swapping with the unit there if any.
        /// Works within one squad or between two. Leaves both squads unchanged when the result would not fit.
        /// </summary>
        public static bool Move(Squad from, SquadSlot fromSlot, Squad to, SquadSlot toSlot)
        {
            var moving = from.UnitAt(fromSlot);
            if (moving == null)
                return false;

            var displaced = to.UnitAt(toSlot);
            if (displaced == moving)
                return false;

            var leaderChangesSquad = from != to && (moving.IsLeader || displaced?.IsLeader == true);
            if (leaderChangesSquad)
                return false;

            var movingOrigin = from.SlotOf(moving);
            var displacedOrigin = displaced == null ? default : to.SlotOf(displaced);

            from.Remove(moving);
            if (displaced != null)
                to.Remove(displaced);

            if (to.TryPlace(moving, toSlot) && (displaced == null || from.TryPlace(displaced, movingOrigin)))
                return true;

            to.Remove(moving);
            if (displaced != null)
            {
                from.Remove(displaced);
                to.TryPlace(displaced, displacedOrigin);
            }
            from.TryPlace(moving, movingOrigin);
            return false;
        }
    }
}
