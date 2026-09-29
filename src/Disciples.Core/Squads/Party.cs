using System;
using Disciples.Core.Map;
using Disciples.Core.Units;

namespace Disciples.Core.Squads
{
    /// <summary>A leader with its squad travelling on the world map.</summary>
    public sealed class Party
    {
        public Party(Unit leader, Position position, int maxMovementPoints)
            : this(SquadFor(leader), position, maxMovementPoints)
        {
        }

        /// <summary>Restores a party from a squad that already contains its leader.</summary>
        public Party(Squad squad, Position position, int maxMovementPoints, int? movementPoints = null)
        {
            Leader = squad.Leader ?? throw new ArgumentException("A party squad needs a leader.", nameof(squad));
            Squad = squad;
            Position = position;
            MaxMovementPoints = maxMovementPoints;
            MovementPoints = movementPoints ?? maxMovementPoints;
        }

        public Unit Leader { get; }
        public Squad Squad { get; }
        public string Name => Leader.Name;
        public Position Position { get; private set; }
        public int MovementPoints { get; private set; }
        public int MaxMovementPoints { get; }

        public bool CanAfford(int cost) => MovementPoints >= cost;

        internal void MoveTo(Position position, int cost)
        {
            Position = position;
            MovementPoints -= cost;
        }

        internal void RestoreMovement() => MovementPoints = MaxMovementPoints;

        private static Squad SquadFor(Unit leader)
        {
            if (!leader.IsLeader)
                throw new ArgumentException($"{leader.Name} cannot lead a party.", nameof(leader));

            var squad = new Squad(leader.Definition.Leadership);
            squad.TryPlace(leader, new SquadSlot(SquadLine.Front, 1));
            return squad;
        }
    }
}
