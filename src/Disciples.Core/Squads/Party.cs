using System;
using Disciples.Core.Map;
using Disciples.Core.Units;

namespace Disciples.Core.Squads
{
    /// <summary>A leader with its squad travelling on the world map.</summary>
    public sealed class Party
    {
        public Party(Unit leader, Position position, int maxMovementPoints)
        {
            if (!leader.IsLeader)
                throw new ArgumentException($"{leader.Name} cannot lead a party.", nameof(leader));

            Leader = leader;
            Squad = new Squad(leader.Definition.Leadership);
            Squad.TryPlace(leader, new SquadSlot(SquadLine.Front, 1));
            Position = position;
            MaxMovementPoints = maxMovementPoints;
            MovementPoints = maxMovementPoints;
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
    }
}
