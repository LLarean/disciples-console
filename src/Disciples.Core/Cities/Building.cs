namespace Disciples.Core.Cities
{
    public sealed class Building
    {
        public Building(string id, string name, string branch, int cost, string description, string? requires, int healBonusPercent = 0, bool allowsResearch = false, bool allowsThieves = false, string? race = null)
        {
            Id = id;
            Name = name;
            Branch = branch;
            Cost = cost;
            Description = description;
            Requires = requires;
            HealBonusPercent = healBonusPercent;
            AllowsResearch = allowsResearch;
            AllowsThieves = allowsThieves;
            Race = race;
        }

        public string Id { get; }
        public string Name { get; }
        public string Branch { get; }
        public int Cost { get; }
        public string Description { get; }

        /// <summary>Id of the building that must be built first.</summary>
        public string? Requires { get; }

        /// <summary>Extra healing per turn in player cities once built in the capital.</summary>
        public int HealBonusPercent { get; }

        /// <summary>Spells can be researched once it is built in the capital.</summary>
        public bool AllowsResearch { get; }

        /// <summary>Thief leaders can be hired once it is built in the capital.</summary>
        public bool AllowsThieves { get; }

        /// <summary>Id of the race that builds it; null for a building of every race.</summary>
        public string? Race { get; }
    }
}
