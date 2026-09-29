using System.Collections.Generic;
using System.Linq;
using Disciples.Core.Map;
using Disciples.Core.Squads;
using Disciples.Core.Units;

namespace Disciples.Core.Cities
{
    public enum Owner
    {
        Neutral,
        Player
    }

    public sealed class City
    {
        private readonly List<UnitDefinition> _recruits;
        private readonly List<Building> _buildings;
        private readonly HashSet<string> _built;

        public City(
            string name,
            Position position,
            bool isCapital,
            int income,
            IEnumerable<UnitDefinition>? recruits = null,
            IEnumerable<Building>? buildings = null,
            Owner owner = Owner.Player,
            IEnumerable<string>? built = null)
        {
            Name = name;
            Position = position;
            IsCapital = isCapital;
            Income = income;
            Owner = owner;
            _recruits = recruits?.ToList() ?? new List<UnitDefinition>();
            _buildings = buildings?.ToList() ?? new List<Building>();
            _built = new HashSet<string>(built ?? Enumerable.Empty<string>());
        }

        public string Name { get; }
        public Position Position { get; }
        public bool IsCapital { get; }
        public Owner Owner { get; private set; }
        public bool IsPlayerOwned => Owner == Owner.Player;

        /// <summary>Gold per turn while owned by the player.</summary>
        public int Income { get; }

        public Squad Garrison { get; } = new Squad();
        public IReadOnlyList<UnitDefinition> Recruits => _recruits;
        public IReadOnlyList<Building> Buildings => _buildings;
        public IEnumerable<string> BuiltIds => _built;

        public Building? FindBuilding(string id) => _buildings.FirstOrDefault(b => b.Id == id);

        public bool HasBuilt(string buildingId) => _built.Contains(buildingId);

        internal void MarkBuilt(Building building) => _built.Add(building.Id);

        internal void Capture() => Owner = Owner.Player;
    }
}
