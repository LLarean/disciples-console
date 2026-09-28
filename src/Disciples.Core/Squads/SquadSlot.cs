using System;
using System.Collections.Generic;

namespace Disciples.Core.Squads
{
    public enum SquadLine
    {
        Front,
        Back
    }

    public readonly struct SquadSlot : IEquatable<SquadSlot>
    {
        public const int Columns = 3;

        public SquadSlot(SquadLine line, int column)
        {
            if (column < 0 || column >= Columns)
                throw new ArgumentOutOfRangeException(nameof(column), column, null);

            Line = line;
            Column = column;
        }

        public SquadLine Line { get; }
        public int Column { get; }

        public static IEnumerable<SquadSlot> All
        {
            get
            {
                foreach (SquadLine line in Enum.GetValues(typeof(SquadLine)))
                    for (var column = 0; column < Columns; column++)
                        yield return new SquadSlot(line, column);
            }
        }

        public SquadSlot WithLine(SquadLine line) => new SquadSlot(line, Column);

        public bool Equals(SquadSlot other) => Line == other.Line && Column == other.Column;
        public override bool Equals(object? obj) => obj is SquadSlot other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Line, Column);
        public override string ToString() => $"{Line} {Column}";

        public static bool operator ==(SquadSlot left, SquadSlot right) => left.Equals(right);
        public static bool operator !=(SquadSlot left, SquadSlot right) => !left.Equals(right);
    }
}
