using System.Collections.Generic;
using System.Linq;

namespace Disciples.Core.Units
{
    /// <summary>Static unit data shared by all instances (Type Object).</summary>
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
            int leadership = 0,
            int experienceToLevel = 100,
            int experienceValue = 0,
            int levelGrowthPercent = 0,
            string? upgradesTo = null,
            string? upgradeBuilding = null,
            int movement = 0,
            AttackSource source = AttackSource.Weapon,
            IEnumerable<AttackSource>? immunities = null,
            IEnumerable<AttackSource>? wards = null,
            AttackEffect effect = AttackEffect.None,
            bool guardian = false,
            bool plantsRods = false,
            bool thief = false)
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
            ExperienceToLevel = experienceToLevel;
            ExperienceValue = experienceValue;
            LevelGrowthPercent = levelGrowthPercent;
            UpgradesTo = upgradesTo;
            UpgradeBuilding = upgradeBuilding;
            Movement = movement;
            Source = source;
            Immunities = immunities?.ToList() ?? new List<AttackSource>();
            Wards = wards?.ToList() ?? new List<AttackSource>();
            Effect = effect;
            IsGuardian = guardian;
            PlantsRods = plantsRods;
            IsThief = thief;
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
        public AttackSource Source { get; }

        /// <summary>Sources that never harm the unit.</summary>
        public IReadOnlyList<AttackSource> Immunities { get; }

        /// <summary>Sources whose first hit in each battle is absorbed.</summary>
        public IReadOnlyList<AttackSource> Wards { get; }

        public AttackEffect Effect { get; }
        public UnitSize Size { get; }
        public int Cost { get; }

        /// <summary>Squad slots a leader can command, including itself. Zero for regular units.</summary>
        public int Leadership { get; }

        /// <summary>Experience needed for the next level or tier upgrade.</summary>
        public int ExperienceToLevel { get; }

        /// <summary>Experience granted to the winners for defeating this unit.</summary>
        public int ExperienceValue { get; }

        /// <summary>Percent added to HP and power per level above the first.</summary>
        public int LevelGrowthPercent { get; }

        /// <summary>Id of the next tier in the unit tree; null when the unit only gains levels.</summary>
        public string? UpgradesTo { get; }

        /// <summary>Capital building required for the upgrade to the next tier.</summary>
        public string? UpgradeBuilding { get; }

        /// <summary>Movement points of a leader's party per turn. Zero for regular units.</summary>
        public int Movement { get; }

        public bool IsLeader => Leadership > 0;

        /// <summary>Bound to its city: never leaves the garrison, cannot be dismissed and is fully restored every turn.</summary>
        public bool IsGuardian { get; }

        /// <summary>A leader that can plant rods to claim land.</summary>
        public bool PlantsRods { get; }

        /// <summary>A leader hired through the thieves guild that acts against adjacent hostile squads.</summary>
        public bool IsThief { get; }

        public int SlotCount => Size == UnitSize.Large ? 2 : 1;
    }
}
