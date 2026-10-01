using System;

namespace Disciples.Core.Magic
{
    public enum ManaType
    {
        Life,
        Death,
        Infernal,
        Runic
    }

    /// <summary>An amount of each mana type: a stock, an income per turn or a price.</summary>
    public sealed class Mana : IEquatable<Mana>
    {
        public static readonly Mana None = new Mana();

        public Mana(int life = 0, int death = 0, int infernal = 0, int runic = 0)
        {
            Life = life;
            Death = death;
            Infernal = infernal;
            Runic = runic;
        }

        public int Life { get; }
        public int Death { get; }
        public int Infernal { get; }
        public int Runic { get; }

        public int this[ManaType type] => type switch
        {
            ManaType.Life => Life,
            ManaType.Death => Death,
            ManaType.Infernal => Infernal,
            _ => Runic
        };

        public static Mana Of(ManaType type, int amount) => new Mana(
            type == ManaType.Life ? amount : 0,
            type == ManaType.Death ? amount : 0,
            type == ManaType.Infernal ? amount : 0,
            type == ManaType.Runic ? amount : 0);

        public Mana Plus(Mana other) =>
            new Mana(Life + other.Life, Death + other.Death, Infernal + other.Infernal, Runic + other.Runic);

        public Mana Minus(Mana other) =>
            new Mana(Life - other.Life, Death - other.Death, Infernal - other.Infernal, Runic - other.Runic);

        public bool Covers(Mana price) =>
            Life >= price.Life && Death >= price.Death && Infernal >= price.Infernal && Runic >= price.Runic;

        public bool Equals(Mana? other) =>
            other != null && Life == other.Life && Death == other.Death && Infernal == other.Infernal && Runic == other.Runic;

        public override bool Equals(object? obj) => Equals(obj as Mana);

        public override int GetHashCode() => (Life, Death, Infernal, Runic).GetHashCode();

        public override string ToString() => $"life {Life}, death {Death}, infernal {Infernal}, runic {Runic}";
    }
}
