using System;

namespace Disciples.Core.Units
{
    public sealed class Unit
    {
        public Unit(UnitDefinition definition, int level = 1, int experience = 0, int? hp = null)
        {
            Definition = definition;
            Level = Math.Max(1, level);
            Experience = Math.Max(0, experience);
            Hp = hp.HasValue ? Math.Min(Math.Max(0, hp.Value), MaxHp) : MaxHp;
        }

        public UnitDefinition Definition { get; private set; }
        public string Name => Definition.Name;
        public int Level { get; private set; }
        public int Experience { get; private set; }
        public int Hp { get; private set; }

        public int MaxHp => Grow(Definition.MaxHp);
        public int Power => Grow(Definition.Power);
        public int Armor => Definition.Armor;
        public int Accuracy => Definition.Accuracy;
        public int Initiative => Definition.Initiative;

        public bool IsAlive => Hp > 0;
        public bool IsLarge => Definition.Size == UnitSize.Large;
        public bool IsLeader => Definition.IsLeader;
        public bool IsGuardian => Definition.IsGuardian;
        public bool IsWounded => IsAlive && Hp < MaxHp;

        public int TakeDamage(int amount)
        {
            var dealt = Math.Min(Hp, Math.Max(0, amount));
            Hp -= dealt;
            return dealt;
        }

        public int Heal(int amount)
        {
            if (!IsAlive)
                return 0;

            var healed = Math.Min(MaxHp - Hp, Math.Max(0, amount));
            Hp += healed;
            return healed;
        }

        internal void AddExperience(int amount) => Experience += Math.Max(0, amount);

        internal void CapExperience() => Experience = Math.Min(Experience, Definition.ExperienceToLevel);

        internal void LevelUp()
        {
            Experience -= Definition.ExperienceToLevel;
            Level++;
            Hp = MaxHp;
        }

        /// <summary>Moves the unit to the next tier of its tree; the new tier starts from level 1.</summary>
        internal void Upgrade(UnitDefinition next)
        {
            Experience -= Definition.ExperienceToLevel;
            Definition = next;
            Level = 1;
            Hp = MaxHp;
        }

        private int Grow(int value) => value * (100 + (Level - 1) * Definition.LevelGrowthPercent) / 100;
    }
}
