using Disciples.Core.Units;

namespace Disciples.Core.Items
{
    public enum ItemKind
    {
        Potion,
        Artifact,
        Banner
    }

    /// <summary>Static item data (Type Object); items have no instances and are carried as definitions.</summary>
    public sealed class ItemDefinition
    {
        public ItemDefinition(string id, string name, ItemKind kind, int cost, int heal = 0, StatBonus? bonus = null)
        {
            Id = id;
            Name = name;
            Kind = kind;
            Cost = cost;
            Heal = heal;
            Bonus = bonus ?? StatBonus.None;
        }

        public string Id { get; }
        public string Name { get; }
        public ItemKind Kind { get; }
        public int Cost { get; }

        /// <summary>HP a potion restores.</summary>
        public int Heal { get; }

        /// <summary>Applies while equipped: an artifact to the leader, a banner to the whole squad.</summary>
        public StatBonus Bonus { get; }

        public bool IsEquipment => Kind != ItemKind.Potion;
    }
}
