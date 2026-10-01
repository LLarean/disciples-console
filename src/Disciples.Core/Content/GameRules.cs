using System.Collections.Generic;

namespace Disciples.Core.Content
{
    /// <summary>Global balance numbers; loaded from content, defaults are for tests.</summary>
    public sealed class GameRules
    {
        /// <summary>Percent of max HP units regain per turn in a tier-1 city.</summary>
        public int CityHealPercent { get; set; } = 25;

        /// <summary>Healing added by each city tier above the first.</summary>
        public int CityTierHealPercent { get; set; }

        /// <summary>Garrison slots of a tier-1 city; each tier adds one.</summary>
        public int CityGarrisonSlots { get; set; } = 6;

        /// <summary>Gold to raise a city to tier 2, 3 and so on; the list length sets the top tier. Capitals are always at the top tier.</summary>
        public List<int> CityUpgradeCosts { get; set; } = new List<int>();

        public int MaxCityTier => CityUpgradeCosts.Count + 1;

        /// <summary>Radius the party and player cities reveal through the fog of war.</summary>
        public int SightRadius { get; set; } = 4;

        /// <summary>Radius of land a capital claims for its owner.</summary>
        public int CapitalTerritoryRadius { get; set; } = 5;

        /// <summary>Radius of land any other city claims for its owner.</summary>
        public int CityTerritoryRadius { get; set; } = 3;

        /// <summary>Radius of land a rod claims for its owner.</summary>
        public int RodTerritoryRadius { get; set; } = 2;

        /// <summary>Gold a leader pays to plant a rod.</summary>
        public int RodCost { get; set; } = 150;

        public int InitiativeSpread { get; set; } = 10;
        public int DamageSpreadPercent { get; set; } = 10;

        /// <summary>Percent of damage a defending unit still takes.</summary>
        public int DefendDamagePercent { get; set; } = 50;

        /// <summary>Default <see cref="Units.UnitDefinition.LevelGrowthPercent"/> for units that do not set their own.</summary>
        public int LevelGrowthPercent { get; set; } = 10;

        /// <summary>Movement points added by the leader's movement perk.</summary>
        public int MovementPerk { get; set; } = 4;

        /// <summary>Percent added to the leader's damage by the Might ability.</summary>
        public int MightPerkPercent { get; set; } = 25;

        /// <summary>Armor added to the leader by the Natural Armor ability.</summary>
        public int ArmorPerk { get; set; } = 20;

        /// <summary>Initiative added to the leader by the First Strike ability.</summary>
        public int InitiativePerk { get; set; } = 20;

        /// <summary>Accuracy added to the leader by the Accuracy ability.</summary>
        public int AccuracyPerk { get; set; } = 15;

        /// <summary>Percent of max HP a leader with Natural Healing regains every turn.</summary>
        public int HealingPerkPercent { get; set; } = 15;

        /// <summary>Percent of extra battle experience for the squad of a Weapon Master.</summary>
        public int ExperiencePerkPercent { get; set; } = 25;

        /// <summary>After this many rounds the attackers withdraw, so battles between mutually immune squads end.</summary>
        public int MaxBattleRounds { get; set; } = 50;

        /// <summary>Turns a poison, paralysis, petrification or polymorph lasts.</summary>
        public int EffectTurns { get; set; } = 2;

        /// <summary>Poison damage per turn as a percent of the poisoner's power.</summary>
        public int PoisonPercent { get; set; } = 50;

        /// <summary>Percent of its power a polymorphed unit keeps.</summary>
        public int PolymorphPowerPercent { get; set; } = 25;

        /// <summary>Percent of dealt damage a drainer restores to itself.</summary>
        public int DrainPercent { get; set; } = 50;

        /// <summary>Gold a trainer takes for every point of experience.</summary>
        public int TrainerGoldPerExperience { get; set; } = 2;

        /// <summary>Chance in percent that a thief's action succeeds.</summary>
        public int ThiefSuccessPercent { get; set; } = 70;

        /// <summary>Damage a caught thief takes.</summary>
        public int ThiefFailureDamage { get; set; } = 40;

        /// <summary>Damage the thief's poison deals to every unit of a squad; it never kills.</summary>
        public int ThiefPoisonDamage { get; set; } = 25;

        /// <summary>Most gold a thief takes from the enemy treasury at once.</summary>
        public int ThiefStealGold { get; set; } = 100;

        /// <summary>Leader unit ids the enemy hires in its capital; none means it never hires leaders.</summary>
        public List<string> EnemyLeaderClasses { get; set; } = new List<string>();

        /// <summary>The enemy hires no new leader while it has this many.</summary>
        public int EnemyLeaderLimit { get; set; } = 3;

        public int GarrisonSlots(int tier) => CityGarrisonSlots + tier - 1;

        public int CityHealPercentAt(int tier) => CityHealPercent + (tier - 1) * CityTierHealPercent;
    }
}
