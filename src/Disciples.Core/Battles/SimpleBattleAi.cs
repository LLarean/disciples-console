using System.Collections.Generic;
using System.Linq;
using Disciples.Core.Units;

namespace Disciples.Core.Battles
{
    /// <summary>Placeholder AI: heals the most wounded ally or hits a random reachable enemy that is not immune.</summary>
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
            var targets = actor == null ? new List<Unit>() : battle.ValidTargets().Where(t => CanHarm(battle, actor, t)).ToList();
            if (actor == null || targets.Count == 0)
            {
                battle.Defend();
                return;
            }

            var target = actor.Definition.AttackType == AttackType.Heal
                ? targets.OrderBy(u => (double)u.Hp / u.MaxHp).First()
                : targets[_random.Next(0, targets.Count)];
            battle.Act(target);
        }

        private static bool CanHarm(Battle battle, Unit actor, Unit target) =>
            actor.Definition.AttackType == AttackType.Heal
            || !target.Definition.Immunities.Contains(actor.Definition.Source) && battle.EffectOn(target) != AttackEffect.Petrification;
    }
}
