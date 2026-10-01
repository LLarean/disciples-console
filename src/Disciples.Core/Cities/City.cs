using System;
using System.Collections.Generic;
using System.Linq;
using Disciples.Core.Content;
using Disciples.Core.Map;
using Disciples.Core.Squads;
using Disciples.Core.Units;

namespace Disciples.Core.Cities
{
    public enum Owner
    {
        Neutral,
        Player,
        Enemy
    }

    public sealed class City
    {
        private readonly List<UnitDefinition> _recruits;
        private readonly List<Building> _buildings;
        private readonly HashSet<string> _built;
        private readonly GameRules _rules;

        public City(
            string name,
            Position position,
            bool isCapital,
            int income,
            IEnumerable<UnitDefinition>? recruits = null,
            IEnumerable<Building>? buildings = null,
            Owner owner = Owner.Player,
            IEnumerable<string>? built = null,
            int tier = 1,
            GameRules? rules = null,
            Squad? garrison = null)
        {
            Name = name;
            Position = position;
            IsCapital = isCapital;
            Income = income;
            Owner = owner;
            _recruits = recruits?.ToList() ?? new List<UnitDefinition>();
            _buildings = buildings?.ToList() ?? new List<Building>();
            _built = new HashSet<string>(built ?? Enumerable.Empty<string>());
            _rules = rules ?? new GameRules();
            Tier = isCapital ? _rules.MaxCityTier : Math.Max(1, Math.Min(tier, _rules.MaxCityTier));
            Garrison = garrison ?? new Squad();
            Garrison.SetCapacity(_rules.GarrisonSlots(Tier));
        }

        public string Name { get; }
        public Position Position { get; }
        public bool IsCapital { get; }
        public Owner Owner { get; private set; }
        public bool IsPlayerOwned => Owner == Owner.Player;

        /// <summary>Gold per turn while owned by the player.</summary>
        public int Income { get; }

        /// <summary>Sets the garrison size and the healing rate; a capital is always at the top tier.</summary>
        public int Tier { get; private set; }

        public int HealPercent => _rules.CityHealPercentAt(Tier);

        /// <summary>Gold for the next tier, or null at the top tier.</summary>
        public int? UpgradeCost => Tier < _rules.MaxCityTier ? _rules.CityUpgradeCosts[Tier - 1] : (int?)null;

        public Squad Garrison { get; }
        public IReadOnlyList<UnitDefinition> Recruits => _recruits;
        public IReadOnlyList<Building> Buildings => _buildings;
        public IEnumerable<string> BuiltIds => _built;

        public Building? FindBuilding(string id) => _buildings.FirstOrDefault(b => b.Id == id);

        public bool HasBuilt(string buildingId) => _built.Contains(buildingId);

        internal void MarkBuilt(Building building) => _built.Add(building.Id);

        internal void Capture(Owner owner) => Owner = owner;

        internal void Upgrade()
        {
            Tier++;
            Garrison.SetCapacity(_rules.GarrisonSlots(Tier));
        }
    }
}
