using System.Collections.Generic;
using System.Linq;
using Disciples.Core.Cities;
using Disciples.Core.Items;
using Disciples.Core.Magic;
using Disciples.Core.Units;

namespace Disciples.Core.Map
{
    public enum SiteKind
    {
        Treasure,
        Mine,
        Camp,
        Merchant,
        ManaSource,
        Trainer
    }

    /// <summary>A passable map object visited by stepping on it: treasure, gold mine, mana source, mercenary camp, merchant or trainer.</summary>
    public sealed class Site
    {
        private readonly List<ItemDefinition> _items;

        public Site(
            string name, SiteKind kind, Position position, int gold = 0, IEnumerable<UnitDefinition>? mercenaries = null,
            Owner owner = Owner.Neutral, IEnumerable<ItemDefinition>? items = null, Mana? mana = null)
        {
            Name = name;
            Kind = kind;
            Position = position;
            Gold = gold;
            Mercenaries = mercenaries?.ToList() ?? new List<UnitDefinition>();
            Owner = owner;
            _items = items?.ToList() ?? new List<ItemDefinition>();
            Mana = mana ?? Mana.None;
        }

        public string Name { get; }
        public SiteKind Kind { get; }
        public Position Position { get; }

        /// <summary>Treasure amount, or mine income per turn.</summary>
        public int Gold { get; }

        public IReadOnlyList<UnitDefinition> Mercenaries { get; }

        /// <summary>Items lying in a treasure, or a merchant's stock.</summary>
        public IReadOnlyList<ItemDefinition> Items => _items;

        /// <summary>Mana a source yields per turn.</summary>
        public Mana Mana { get; }

        /// <summary>Mines and mana sources pay their owner every turn and change hands.</summary>
        public bool IsResource => Kind == SiteKind.Mine || Kind == SiteKind.ManaSource;

        public Owner Owner { get; private set; }

        internal void Capture(Owner owner) => Owner = owner;

        internal bool Take(ItemDefinition item) => _items.Remove(item);
    }
}
