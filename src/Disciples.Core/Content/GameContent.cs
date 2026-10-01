using System.Collections.Generic;
using System.Linq;
using Disciples.Core.Cities;
using Disciples.Core.Items;
using Disciples.Core.Magic;
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
        private readonly Dictionary<string, ItemDefinition> _items;
        private readonly Dictionary<string, SpellDefinition> _spells;
        private readonly List<Race> _races;
        private readonly ContentAliases _aliases;

        public GameContent(IEnumerable<UnitDefinition> units, IEnumerable<Terrain> terrains, IEnumerable<Building> buildings, GameRules rules, ContentAliases? aliases = null,
            IEnumerable<ItemDefinition>? items = null, IEnumerable<SpellDefinition>? spells = null, IEnumerable<Race>? races = null)
        {
            _units = units.ToDictionary(u => u.Id);
            _terrains = terrains.ToDictionary(t => t.Id);
            _buildings = buildings.ToList();
            _items = (items ?? Enumerable.Empty<ItemDefinition>()).ToDictionary(i => i.Id);
            _spells = (spells ?? Enumerable.Empty<SpellDefinition>()).ToDictionary(s => s.Id);
            _races = (races ?? Enumerable.Empty<Race>()).ToList();
            if (_races.Count == 0)
                _races.Add(new Race("default", "Default"));
            _aliases = aliases ?? new ContentAliases();
            Rules = rules;

            var deadAlias = _aliases.Units.Where(a => !_units.ContainsKey(a.Value))
                .Concat(_aliases.Terrains.Where(a => !_terrains.ContainsKey(a.Value)))
                .Concat(_aliases.Buildings.Where(a => _buildings.All(b => b.Id != a.Value)))
                .Concat(_aliases.Items.Where(a => !_items.ContainsKey(a.Value)))
                .Concat(_aliases.Spells.Where(a => !_spells.ContainsKey(a.Value)))
                .Select(a => a.Key)
                .FirstOrDefault();
            if (deadAlias != null)
                throw new ContentException($"Alias '{deadAlias}' points to unknown content.");

            var brokenLink = _units.Values.FirstOrDefault(u => u.UpgradesTo != null && !_units.ContainsKey(u.UpgradesTo));
            if (brokenLink != null)
                throw new ContentException($"Unit '{brokenLink.Id}' upgrades to unknown unit '{brokenLink.UpgradesTo}'.");

            var immobile = _units.Values.FirstOrDefault(u => u.IsLeader && u.Movement <= 0);
            if (immobile != null)
                throw new ContentException($"Leader '{immobile.Id}' has no movement.");

            var badClass = _races.SelectMany(r => r.Leaders).Concat(rules.EnemyLeaderClasses)
                .FirstOrDefault(id => !_units.TryGetValue(id, out var unit) || !unit.IsLeader);
            if (badClass != null)
                throw new ContentException($"Leader class '{badClass}' is not a leader unit.");

            var strayUnit = _races.SelectMany(r => r.Guardian == null ? r.Recruits : r.Recruits.Append(r.Guardian))
                .FirstOrDefault(id => !_units.ContainsKey(id));
            if (strayUnit != null)
                throw new ContentException($"A race refers to unknown unit '{strayUnit}'.");

            var strayRace = _buildings.Select(b => b.Race).Concat(_spells.Values.Select(s => s.Race))
                .FirstOrDefault(id => id != null && _races.All(r => r.Id != id));
            if (strayRace != null)
                throw new ContentException($"Unknown race '{strayRace}'.");
        }

        public GameRules Rules { get; }
        public IReadOnlyList<Building> Buildings => _buildings;
        public IEnumerable<ItemDefinition> Items => _items.Values;
        public IEnumerable<SpellDefinition> Spells => _spells.Values;
        public IReadOnlyList<Race> Races => _races;

        /// <summary>The race of a game that names none: saves made before races, content without races.</summary>
        public Race DefaultRace => _races[0];
        public IEnumerable<UnitDefinition> EnemyLeaderClasses => Rules.EnemyLeaderClasses.Select(Unit);

        public Race Race(string? id) =>
            id == null ? DefaultRace : _races.FirstOrDefault(r => r.Id == id) ?? throw new ContentException($"Unknown race '{id}'.");

        public IEnumerable<UnitDefinition> LeadersOf(Race race) => race.Leaders.Select(Unit);

        public IEnumerable<UnitDefinition> RecruitsOf(Race race) => race.Recruits.Select(Unit);

        public IEnumerable<Building> BuildingsOf(Race race) => _buildings.Where(b => race.Has(b.Race));

        public IEnumerable<SpellDefinition> SpellsOf(Race race) => _spells.Values.Where(s => race.Has(s.Race));

        public UnitDefinition Unit(string id) =>
            _units.TryGetValue(Current(id, _aliases.Units), out var unit) ? unit : throw new ContentException($"Unknown unit '{id}'.");

        public Terrain Terrain(string id) =>
            _terrains.TryGetValue(Current(id, _aliases.Terrains), out var terrain) ? terrain : throw new ContentException($"Unknown terrain '{id}'.");

        public ItemDefinition Item(string id) =>
            _items.TryGetValue(Current(id, _aliases.Items), out var item) ? item : throw new ContentException($"Unknown item '{id}'.");

        public SpellDefinition Spell(string id) =>
            _spells.TryGetValue(Current(id, _aliases.Spells), out var spell) ? spell : throw new ContentException($"Unknown spell '{id}'.");

        /// <summary>Current id of a building that may be stored under a retired id.</summary>
        public string BuildingId(string id) => Current(id, _aliases.Buildings);

        public Building? FindBuilding(string id) => _buildings.FirstOrDefault(b => b.Id == id);

        public string BuildingName(string id) => FindBuilding(id)?.Name ?? id;

        private static string Current(string id, Dictionary<string, string> aliases) =>
            aliases.TryGetValue(id, out var current) ? current : id;
    }
}
