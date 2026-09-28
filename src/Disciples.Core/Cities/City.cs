using System.Collections.Generic;
using System.Linq;
using Disciples.Core.Map;
using Disciples.Core.Squads;
using Disciples.Core.Units;

namespace Disciples.Core.Cities
{
    public sealed class City
    {
        private readonly List<UnitDefinition> _recruits;
        private readonly List<Building> _buildings;

        public City(
            string name,
            Position position,
            bool isCapital,
            int income,
            IEnumerable<UnitDefinition>? recruits = null,
            IEnumerable<Building>? buildings = null)
        {
            Name = name;
            Position = position;
            IsCapital = isCapital;
            Income = income;
            _recruits = recruits?.ToList() ?? new List<UnitDefinition>();
            _buildings = buildings?.ToList() ?? new List<Building>();
        }

        public string Name { get; }
        public Position Position { get; }
        public bool IsCapital { get; }

        /// <summary>Gold per turn.</summary>
        public int Income { get; }

        public Squad Garrison { get; } = new Squad();
        public IReadOnlyList<UnitDefinition> Recruits => _recruits;
        public IReadOnlyList<Building> Buildings => _buildings;

        public Building? FindBuilding(string id) => _buildings.FirstOrDefault(b => b.Id == id);
    }
}
