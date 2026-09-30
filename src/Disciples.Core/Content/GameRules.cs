using System.Collections.Generic;

namespace Disciples.Core.Content
{
    /// <summary>Global balance numbers; loaded from content, defaults are for tests.</summary>
    public sealed class GameRules
    {
        public int CityHealPercent { get; set; } = 25;
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

        /// <summary>Leader unit ids offered when starting a new game.</summary>
        public List<string> LeaderClasses { get; set; } = new List<string>();
    }
}
