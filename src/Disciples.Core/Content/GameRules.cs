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
    }
}
