using System;

namespace Disciples.Core.Map
{
    public readonly struct Position : IEquatable<Position>
    {
        public Position(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }

        public Position Step(Direction direction)
        {
            return direction switch
            {
                Direction.North => new Position(X, Y - 1),
                Direction.NorthEast => new Position(X + 1, Y - 1),
                Direction.East => new Position(X + 1, Y),
                Direction.SouthEast => new Position(X + 1, Y + 1),
                Direction.South => new Position(X, Y + 1),
                Direction.SouthWest => new Position(X - 1, Y + 1),
                Direction.West => new Position(X - 1, Y),
                Direction.NorthWest => new Position(X - 1, Y - 1),
                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
            };
        }

        public bool Equals(Position other) => X == other.X && Y == other.Y;
        public override bool Equals(object? obj) => obj is Position other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Y);
        public override string ToString() => $"({X}, {Y})";

        public static bool operator ==(Position left, Position right) => left.Equals(right);
        public static bool operator !=(Position left, Position right) => !left.Equals(right);
    }
}
