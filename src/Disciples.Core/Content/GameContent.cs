using System.Collections.Generic;
using System.Linq;
using Disciples.Core.Cities;
using Disciples.Core.Map;
using Disciples.Core.Units;

namespace Disciples.Core.Content
{
    /// <summary>Immutable definitions referenced by id from scenarios and saves.</summary>
    public sealed class GameContent
    {
        private readonly Dictionary<string, UnitDefinition> _units;
        private readonly Dictionary<string, Terrain> _terrains;
        private readonly List<Building> _buildings;

        public GameContent(IEnumerable<UnitDefinition> units, IEnumerable<Terrain> terrains, IEnumerable<Building> buildings, GameRules rules)
        {
            _units = units.ToDictionary(u => u.Id);
            _terrains = terrains.ToDictionary(t => t.Id);
            _buildings = buildings.ToList();
            Rules = rules;

            var brokenLink = _units.Values.FirstOrDefault(u => u.UpgradesTo != null && !_units.ContainsKey(u.UpgradesTo));
            if (brokenLink != null)
                throw new ContentException($"Unit '{brokenLink.Id}' upgrades to unknown unit '{brokenLink.UpgradesTo}'.");

            var immobile = _units.Values.FirstOrDefault(u => u.IsLeader && u.Movement <= 0);
            if (immobile != null)
                throw new ContentException($"Leader '{immobile.Id}' has no movement.");

            var badClass = rules.LeaderClasses.FirstOrDefault(id => !_units.TryGetValue(id, out var unit) || !unit.IsLeader);
            if (badClass != null)
                throw new ContentException($"Leader class '{badClass}' is not a leader unit.");
        }

        public GameRules Rules { get; }
        public IReadOnlyList<Building> Buildings => _buildings;
        public IEnumerable<UnitDefinition> LeaderClasses => Rules.LeaderClasses.Select(Unit);

        public UnitDefinition Unit(string id) =>
            _units.TryGetValue(id, out var unit) ? unit : throw new ContentException($"Unknown unit '{id}'.");

        public Terrain Terrain(string id) =>
            _terrains.TryGetValue(id, out var terrain) ? terrain : throw new ContentException($"Unknown terrain '{id}'.");

        public Building? FindBuilding(string id) => _buildings.FirstOrDefault(b => b.Id == id);

        public string BuildingName(string id) => FindBuilding(id)?.Name ?? id;
    }
}
