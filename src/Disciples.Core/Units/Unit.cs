using System;

namespace Disciples.Core.Units
{
    public sealed class Unit
    {
        public Unit(UnitDefinition definition)
        {
            Definition = definition;
            Hp = definition.MaxHp;
        }

        public UnitDefinition Definition { get; }
        public string Name => Definition.Name;
        public int Hp { get; private set; }
        public int MaxHp => Definition.MaxHp;
        public bool IsAlive => Hp > 0;
        public bool IsLarge => Definition.Size == UnitSize.Large;
        public bool IsLeader => Definition.IsLeader;
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

        public void Revive(int hp) => Hp = Math.Max(1, Math.Min(MaxHp, hp));
    }
}
