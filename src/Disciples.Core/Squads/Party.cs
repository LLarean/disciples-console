using System;
using System.Collections.Generic;
using System.Linq;
using Disciples.Core.Content;
using Disciples.Core.Items;
using Disciples.Core.Map;
using Disciples.Core.Units;

namespace Disciples.Core.Squads
{
    /// <summary>Leadership and Movement can be taken repeatedly; the rest are abilities taken once.</summary>
    public enum LeaderPerk
    {
        Leadership,
        Movement,
        Might,
        NaturalArmor,
        FirstStrike,
        Accuracy,
        NaturalHealing,
        WeaponMaster
    }

    /// <summary>A leader with its squad travelling on the world map.</summary>
    public sealed class Party
    {
        private readonly List<LeaderPerk> _perks;
        private readonly List<ItemDefinition> _items;
        private readonly List<ItemDefinition> _equipped = new List<ItemDefinition>();
        private readonly GameRules _rules;

        public Party(Unit leader, Position position, int? movementPoints = null, GameRules? rules = null)
            : this(SquadFor(leader), position, movementPoints, rules: rules)
        {
        }

        /// <summary>Restores a party from a squad that already contains its leader; squad capacity follows the leadership.</summary>
        public Party(Squad squad, Position position, int? movementPoints = null, IEnumerable<LeaderPerk>? perks = null, GameRules? rules = null,
            IEnumerable<ItemDefinition>? items = null, IEnumerable<ItemDefinition>? equipped = null)
        {
            Leader = squad.Leader ?? throw new ArgumentException("A party squad needs a leader.", nameof(squad));
            Squad = squad;
            Position = position;
            _perks = perks?.ToList() ?? new List<LeaderPerk>();
            _items = items?.ToList() ?? new List<ItemDefinition>();
            _rules = rules ?? new GameRules();
            Squad.SetCapacity(Leader.Definition.Leadership + Count(LeaderPerk.Leadership));
            MovementPoints = movementPoints ?? MaxMovementPoints;

            foreach (var item in equipped ?? Enumerable.Empty<ItemDefinition>())
            {
                // Equip takes the first copy from the bag: put the worn one in front so the copies already there keep their places.
                _items.Insert(0, item);
                Equip(item);
            }
        }

        public Unit Leader { get; }
        public Squad Squad { get; }
        public string Name => Leader.Name;
        public Position Position { get; private set; }
        public int MovementPoints { get; private set; }
        public int MaxMovementPoints => Leader.Definition.Movement + Count(LeaderPerk.Movement) * MovementPerk;
        public int MovementPerk => _rules.MovementPerk;
        public IReadOnlyList<LeaderPerk> Perks => _perks;

        /// <summary>Items the leader carries; the bag has no size limit.</summary>
        public IReadOnlyList<ItemDefinition> Items => _items;

        /// <summary>Worn items, at most one of each kind; they are not in the bag.</summary>
        public IReadOnlyList<ItemDefinition> Equipped => _equipped;

        /// <summary>The banner strengthens every unit of the squad; the artifact and the leader's abilities only the leader.</summary>
        public StatBonus BonusFor(Unit unit)
        {
            if (!Squad.Contains(unit))
                return StatBonus.None;

            var worn = _equipped.Where(i => i.Kind == ItemKind.Banner || unit == Leader).Aggregate(StatBonus.None, (sum, i) => sum.Plus(i.Bonus));
            return unit == Leader ? worn.Plus(AbilityBonus) : worn;
        }

        /// <summary>Percent of the battle experience the squad receives.</summary>
        public int ExperiencePercent => 100 + (Has(LeaderPerk.WeaponMaster) ? _rules.ExperiencePerkPercent : 0);

        /// <summary>Percent of max HP the leader regains every turn wherever it stands.</summary>
        public int NaturalHealPercent => Has(LeaderPerk.NaturalHealing) ? _rules.HealingPerkPercent : 0;

        public bool Has(LeaderPerk perk) => _perks.Contains(perk);

        /// <summary>Each leader level above the first grants one perk.</summary>
        public int UnspentPerks => Math.Max(0, Leader.Level - 1 - _perks.Count);

        public bool CanAfford(int cost) => MovementPoints >= cost;

        public bool CanTake(LeaderPerk perk)
        {
            if (UnspentPerks == 0)
                return false;

            return perk switch
            {
                LeaderPerk.Leadership => Squad.Capacity < Squad.MaxSlots,
                LeaderPerk.Movement => true,
                _ => !Has(perk)
            };
        }

        internal void Take(LeaderPerk perk)
        {
            _perks.Add(perk);
            if (perk == LeaderPerk.Leadership)
                Squad.SetCapacity(Squad.Capacity + 1);
            else if (perk == LeaderPerk.Movement)
                MovementPoints += MovementPerk;
        }

        internal void Give(ItemDefinition item) => _items.Add(item);

        /// <summary>Removes one such item from the bag.</summary>
        internal bool Take(ItemDefinition item) => _items.Remove(item);

        /// <summary>Moves an artifact or banner from the bag to its slot; the item worn there returns to the bag.</summary>
        internal bool Equip(ItemDefinition item)
        {
            if (!item.IsEquipment || !_items.Remove(item))
                return false;

            var worn = _equipped.FirstOrDefault(e => e.Kind == item.Kind);
            if (worn != null)
                Unequip(worn);

            _equipped.Add(item);
            return true;
        }

        internal bool Unequip(ItemDefinition item)
        {
            if (!_equipped.Remove(item))
                return false;

            _items.Add(item);
            return true;
        }

        internal void MoveTo(Position position, int cost)
        {
            Position = position;
            MovementPoints -= cost;
        }

        internal void RestoreMovement() => MovementPoints = MaxMovementPoints;

        internal void Exhaust() => MovementPoints = 0;

        private StatBonus AbilityBonus => new StatBonus(
            Has(LeaderPerk.NaturalArmor) ? _rules.ArmorPerk : 0,
            Has(LeaderPerk.Might) ? _rules.MightPerkPercent : 0,
            Has(LeaderPerk.FirstStrike) ? _rules.InitiativePerk : 0,
            Has(LeaderPerk.Accuracy) ? _rules.AccuracyPerk : 0);

        private int Count(LeaderPerk perk) => _perks.Count(p => p == perk);

        private static Squad SquadFor(Unit leader)
        {
            if (!leader.IsLeader)
                throw new ArgumentException($"{leader.Name} cannot lead a party.", nameof(leader));

            var squad = new Squad();
            squad.TryPlace(leader, new SquadSlot(SquadLine.Front, 1));
            return squad;
        }
    }
}
