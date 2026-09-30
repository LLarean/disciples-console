using System.Collections.Generic;

namespace Disciples.Core.Content
{
    /// <summary>Global balance numbers; loaded from content, defaults are for tests.</summary>
    public sealed class GameRules
    {
        public int CityHealPercent { get; set; } = 25;

        /// <summary>Radius the party and player cities reveal through the fog of war.</summary>
        public int SightRadius { get; set; } = 4;

        /// <summary>Radius of land a capital claims for its owner.</summary>
        public int CapitalTerritoryRadius { get; set; } = 5;

        /// <summary>Radius of land any other city claims for its owner.</summary>
        public int CityTerritoryRadius { get; set; } = 3;
        public int InitiativeSpread { get; set; } = 10;
        public int DamageSpreadPercent { get; set; } = 10;

        /// <summary>Percent of damage a defending unit still takes.</summary>
        public int DefendDamagePercent { get; set; } = 50;

        /// <summary>Default <see cref="Units.UnitDefinition.LevelGrowthPercent"/> for units that do not set their own.</summary>
        public int LevelGrowthPercent { get; set; } = 10;

        /// <summary>Movement points added by the leader's movement perk.</summary>
        public int MovementPerk { get; set; } = 4;

        /// <summary>After this many rounds the attackers withdraw, so battles between mutually immune squads end.</summary>
        public int MaxBattleRounds { get; set; } = 50;

        /// <summary>Turns a poison, paralysis or petrification lasts.</summary>
        public int EffectTurns { get; set; } = 2;

        /// <summary>Poison damage per turn as a percent of the poisoner's power.</summary>
        public int PoisonPercent { get; set; } = 50;

        /// <summary>Percent of dealt damage a drainer restores to itself.</summary>
        public int DrainPercent { get; set; } = 50;

        /// <summary>Leader unit ids offered when starting a new game.</summary>
        public List<string> LeaderClasses { get; set; } = new List<string>();
    }
}
