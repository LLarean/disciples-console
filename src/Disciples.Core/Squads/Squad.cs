using System;
using System.Collections.Generic;
using System.Linq;
using Disciples.Core.Units;

namespace Disciples.Core.Squads
{
    /// <summary>
    /// 2×3 grid. A large unit occupies both lines of its column and is stored in both cells.
    /// </summary>
    public sealed class Squad
    {
        public const int MaxSlots = 6;

        private readonly Unit?[,] _cells = new Unit?[2, SquadSlot.Columns];

        public Squad(int capacity = MaxSlots)
        {
            Capacity = Math.Min(capacity, MaxSlots);
        }

        /// <summary>Maximum occupied slots; a large unit takes two.</summary>
        public int Capacity { get; private set; }

        public IReadOnlyList<Unit> Units => SquadSlot.All.Select(UnitAt).OfType<Unit>().Distinct().ToList();
        public IEnumerable<Unit> AliveUnits => Units.Where(u => u.IsAlive);
        public int UsedSlots => Units.Sum(u => u.Definition.SlotCount);
        public Unit? Leader => Units.FirstOrDefault(u => u.IsLeader);
        public bool IsDefeated => Units.All(u => !u.IsAlive);

        public Unit? UnitAt(SquadSlot slot) => _cells[(int)slot.Line, slot.Column];

        public bool Contains(Unit unit) => Units.Contains(unit);

        public SquadSlot SlotOf(Unit unit)
        {
            foreach (var slot in SquadSlot.All)
                if (UnitAt(slot) == unit)
                    return slot;

            throw new InvalidOperationException($"{unit.Name} is not in the squad.");
        }

        public IEnumerable<Unit> UnitsInLine(SquadLine line)
        {
            return Enumerable.Range(0, SquadSlot.Columns)
                .Select(column => UnitAt(new SquadSlot(line, column)))
                .OfType<Unit>();
        }

        public bool CanPlace(Unit unit, SquadSlot slot)
        {
            if (Contains(unit) || UsedSlots + unit.Definition.SlotCount > Capacity)
                return false;

            if (unit.IsLarge)
                return UnitAt(slot.WithLine(SquadLine.Front)) == null && UnitAt(slot.WithLine(SquadLine.Back)) == null;

            return UnitAt(slot) == null;
        }

        public bool TryPlace(Unit unit, SquadSlot slot)
        {
            if (!CanPlace(unit, slot))
                return false;

            if (unit.IsLarge)
            {
                SetCell(slot.WithLine(SquadLine.Front), unit);
                SetCell(slot.WithLine(SquadLine.Back), unit);
            }
            else
            {
                SetCell(slot, unit);
            }

            return true;
        }

        /// <summary>Places the unit into the first free slot, melee units preferring the front line.</summary>
        public bool TryAdd(Unit unit)
        {
            var preferFront = unit.IsLarge || unit.Definition.AttackType == AttackType.Melee;
            var slots = SquadSlot.All.OrderBy(s => (s.Line == SquadLine.Front) == preferFront ? 0 : 1);
            return slots.Any(slot => TryPlace(unit, slot));
        }

        public void Remove(Unit unit)
        {
            foreach (var slot in SquadSlot.All)
                if (UnitAt(slot) == unit)
                    SetCell(slot, null);
        }

        public void RemoveDead()
        {
            foreach (var unit in Units.Where(u => !u.IsAlive))
                Remove(unit);
        }

        internal void SetCapacity(int capacity) => Capacity = Math.Min(capacity, MaxSlots);

        private void SetCell(SquadSlot slot, Unit? unit) => _cells[(int)slot.Line, slot.Column] = unit;
    }
}
