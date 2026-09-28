using System.Linq;

namespace Disciples.Core.Battles
{
    /// <summary>Placeholder AI: heals the most wounded ally or hits a random reachable enemy.</summary>
    public sealed class SimpleBattleAi
    {
        private readonly IRandom _random;

        public SimpleBattleAi(IRandom random)
        {
            _random = random;
        }

        public void Act(Battle battle)
        {
            var actor = battle.Current;
            var targets = battle.ValidTargets();
            if (actor == null || targets.Count == 0)
            {
                battle.Defend();
                return;
            }

            var target = actor.Definition.AttackType == Units.AttackType.Heal
                ? targets.OrderBy(u => (double)u.Hp / u.MaxHp).First()
                : targets[_random.Next(0, targets.Count)];
            battle.Act(target);
        }
    }
}
