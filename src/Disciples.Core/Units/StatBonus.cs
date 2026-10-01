using System;

namespace Disciples.Core.Units
{
    /// <summary>Additions to a unit's battle stats from equipped items.</summary>
    public sealed class StatBonus
    {
        public static readonly StatBonus None = new StatBonus();

        public StatBonus(int armor = 0, int powerPercent = 0, int initiative = 0, int accuracy = 0)
        {
            Armor = armor;
            PowerPercent = powerPercent;
            Initiative = initiative;
            Accuracy = accuracy;
        }

        public int Armor { get; }
        public int PowerPercent { get; }
        public int Initiative { get; }
        public int Accuracy { get; }
        public bool IsEmpty => Armor == 0 && PowerPercent == 0 && Initiative == 0 && Accuracy == 0;

        public StatBonus Plus(StatBonus other) => new StatBonus(
            Armor + other.Armor, PowerPercent + other.PowerPercent, Initiative + other.Initiative, Accuracy + other.Accuracy);

        public int PowerOf(Unit unit) => unit.Power * (100 + PowerPercent) / 100;

        public int ArmorOf(Unit unit) => Math.Min(100, unit.Armor + Armor);

        public int InitiativeOf(Unit unit) => unit.Initiative + Initiative;

        public int AccuracyOf(Unit unit) => unit.Accuracy + Accuracy;
    }
}
