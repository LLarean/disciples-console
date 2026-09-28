using Disciples.Core.Map;

namespace Disciples.Core.Squads
{
    public sealed class Leader
    {
        public Leader(string name, Position position, int maxMovementPoints)
        {
            Name = name;
            Position = position;
            MaxMovementPoints = maxMovementPoints;
            MovementPoints = maxMovementPoints;
        }

        public string Name { get; }
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
