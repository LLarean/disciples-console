using System;
using System.Collections.Generic;
using System.Linq;

namespace Disciples.Core.Units
{
    public enum ProgressKind
    {
        LeveledUp,
        Upgraded,
        WaitingForBuilding
    }

    public sealed class UnitProgress
    {
        public UnitProgress(ProgressKind kind, Unit unit, string previousName, string? building = null)
        {
            Kind = kind;
            Unit = unit;
            PreviousName = previousName;
            Building = building;
        }

        public ProgressKind Kind { get; }
        public Unit Unit { get; }
        public string PreviousName { get; }

        /// <summary>Id of the missing building for <see cref="ProgressKind.WaitingForBuilding"/>.</summary>
        public string? Building { get; }
    }

    /// <summary>
    /// Experience rules: on reaching the threshold a unit upgrades to the next tier when the capital has the required building,
    /// waits at the threshold when it does not, and gains a level when its tree has ended.
    /// </summary>
    public static class Progression
    {
        public static IReadOnlyList<UnitProgress> Gain(
            Unit unit, int experience, Func<string, UnitDefinition> findUnit, Func<string, bool> hasBuilding)
        {
            var progress = new List<UnitProgress>();
            unit.AddExperience(experience);

            while (unit.IsAlive && unit.Experience >= unit.Definition.ExperienceToLevel)
            {
                var definition = unit.Definition;
                var previousName = unit.Name;

                if (definition.UpgradesTo == null)
                {
                    unit.LevelUp();
                    progress.Add(new UnitProgress(ProgressKind.LeveledUp, unit, previousName));
                }
                else if (definition.UpgradeBuilding == null || hasBuilding(definition.UpgradeBuilding))
                {
                    unit.Upgrade(findUnit(definition.UpgradesTo));
                    progress.Add(new UnitProgress(ProgressKind.Upgraded, unit, previousName));
                }
                else
                {
                    unit.CapExperience();
                    progress.Add(new UnitProgress(ProgressKind.WaitingForBuilding, unit, previousName, definition.UpgradeBuilding));
                    break;
                }
            }

            return progress;
        }

        /// <summary>Splits experience evenly between the living units, rounding up.</summary>
        public static IReadOnlyList<UnitProgress> Share(
            IEnumerable<Unit> units, int experience, Func<string, UnitDefinition> findUnit, Func<string, bool> hasBuilding)
        {
            var alive = units.Where(u => u.IsAlive).ToList();
            if (alive.Count == 0 || experience <= 0)
                return Array.Empty<UnitProgress>();

            var share = (experience + alive.Count - 1) / alive.Count;
            return alive.SelectMany(u => Gain(u, share, findUnit, hasBuilding)).ToList();
        }
    }
}
