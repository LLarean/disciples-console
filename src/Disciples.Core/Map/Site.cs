using System.Collections.Generic;
using System.Linq;
using Disciples.Core.Cities;
using Disciples.Core.Units;

namespace Disciples.Core.Map
{
    public enum SiteKind
    {
        Treasure,
        Mine,
        Camp
    }

    /// <summary>A passable map object visited by stepping on it: treasure, gold mine or mercenary camp.</summary>
    public sealed class Site
    {
        public Site(string name, SiteKind kind, Position position, int gold = 0, IEnumerable<UnitDefinition>? mercenaries = null, Owner owner = Owner.Neutral)
        {
            Name = name;
            Kind = kind;
            Position = position;
            Gold = gold;
            Mercenaries = mercenaries?.ToList() ?? new List<UnitDefinition>();
            Owner = owner;
        }

        public string Name { get; }
        public SiteKind Kind { get; }
        public Position Position { get; }

        /// <summary>Treasure amount, or mine income per turn.</summary>
        public int Gold { get; }

        public IReadOnlyList<UnitDefinition> Mercenaries { get; }
        public Owner Owner { get; private set; }

        internal void Capture(Owner owner) => Owner = owner;
    }
}
