using Disciples.Core.Units;

namespace Disciples.Core.Battles
{
    public enum BattleEventKind
    {
        RoundStarted,
        Hit,
        Miss,
        Immune,
        Warded,
        Afflicted,
        Drained,
        PoisonDamage,
        TurnLost,
        Healed,
        Defended,
        Waited,
        Killed,
        Retreated
    }

    public sealed class BattleEvent
    {
        public BattleEvent(BattleEventKind kind, int round, Unit? actor = null, Unit? target = null, int amount = 0, AttackEffect effect = AttackEffect.None)
        {
            Kind = kind;
            Round = round;
            Actor = actor;
            Target = target;
            Amount = amount;
            Effect = effect;
        }

        public BattleEventKind Kind { get; }
        public int Round { get; }
        public Unit? Actor { get; }
        public Unit? Target { get; }
        public int Amount { get; }
        public AttackEffect Effect { get; }
    }
}
