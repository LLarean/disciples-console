using Disciples.Core.Units;

namespace Disciples.Core.Magic
{
    public enum SpellKind
    {
        /// <summary>Hits every unit of a hostile squad on the map.</summary>
        Damage,

        /// <summary>Heals every unit of the player's squad on the map.</summary>
        Heal
    }

    public sealed class SpellDefinition
    {
        public SpellDefinition(
            string id, string name, SpellKind kind, int amount, Mana? researchCost = null, Mana? castCost = null,
            AttackSource source = AttackSource.Weapon)
        {
            Id = id;
            Name = name;
            Kind = kind;
            Amount = amount;
            ResearchCost = researchCost ?? Mana.None;
            CastCost = castCost ?? Mana.None;
            Source = source;
        }

        public string Id { get; }
        public string Name { get; }
        public SpellKind Kind { get; }

        /// <summary>HP taken from or restored to each unit.</summary>
        public int Amount { get; }

        public Mana ResearchCost { get; }
        public Mana CastCost { get; }

        /// <summary>Units immune or warded against the source take no damage.</summary>
        public AttackSource Source { get; }
    }
}
