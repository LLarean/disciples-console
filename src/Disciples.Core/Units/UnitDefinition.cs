namespace Disciples.Core.Units
{
    public sealed class UnitDefinition
    {
        public UnitDefinition(
            string id,
            string name,
            int maxHp,
            int armor,
            int initiative,
            int power,
            int accuracy,
            AttackType attackType,
            UnitSize size,
            int cost,
            int leadership = 0)
        {
            Id = id;
            Name = name;
            MaxHp = maxHp;
            Armor = armor;
            Initiative = initiative;
            Power = power;
            Accuracy = accuracy;
            AttackType = attackType;
            Size = size;
            Cost = cost;
            Leadership = leadership;
        }

        public string Id { get; }
        public string Name { get; }
        public int MaxHp { get; }

        /// <summary>Percent of incoming damage absorbed.</summary>
        public int Armor { get; }

        public int Initiative { get; }

        /// <summary>Damage dealt, or HP restored for healers.</summary>
        public int Power { get; }

        /// <summary>Hit chance in percent.</summary>
        public int Accuracy { get; }

        public AttackType AttackType { get; }
        public UnitSize Size { get; }
        public int Cost { get; }

        /// <summary>Squad slots a leader can command, including itself. Zero for regular units.</summary>
        public int Leadership { get; }

        public bool IsLeader => Leadership > 0;
        public int SlotCount => Size == UnitSize.Large ? 2 : 1;
    }
}
