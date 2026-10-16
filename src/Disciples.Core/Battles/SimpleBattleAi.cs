using System;
using System.Collections.Generic;
using System.Linq;
using Disciples.Core.Units;

namespace Disciples.Core.Battles
{
    /// <summary>
    /// Plays one side: healers mend the most wounded ally and defend when nobody is hurt;
    /// attackers finish off the strongest enemy one hit can kill, otherwise wear down the one closest to death.
    /// Units with a status effect prefer enemies not yet afflicted. Equal choices are random.
    /// </summary>
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
            var target = actor == null || targets.Count == 0 ? null
                : actor.Definition.AttackType == AttackType.Heal ? HealTarget(targets)
                : StrikeTarget(battle, actor, targets);

            if (target == null)
                battle.Defend();
            else
                battle.Act(target);
        }

        /// <summary>Plays both sides until the battle is over.</summary>
        public void Resolve(Battle battle)
        {
            while (!battle.IsOver)
                Act(battle);
        }

        private static Unit? HealTarget(IEnumerable<Unit> allies) =>
            allies.Where(u => u.Hp < u.MaxHp).OrderBy(u => (double)u.Hp / u.MaxHp).FirstOrDefault();

        private Unit StrikeTarget(Battle battle, Unit actor, List<Unit> targets)
        {
            if (actor.Definition.Effect is not (AttackEffect.None or AttackEffect.Drain))
            {
                var unaffected = targets.Where(t => battle.EffectOn(t) == AttackEffect.None).ToList();
                if (unaffected.Count > 0)
                    targets = unaffected;
            }

            var killable = targets.Where(t => battle.ExpectedDamage(actor, t) >= t.Hp).ToList();
            return killable.Count > 0
                ? PickBest(killable, t => -battle.PowerOf(t))
                : PickBest(targets, t => t.Hp - battle.ExpectedDamage(actor, t));
        }

        private Unit PickBest(List<Unit> units, Func<Unit, int> score)
        {
            var best = units.Min(score);
            var tied = units.Where(u => score(u) == best).ToList();
            return tied[_random.Next(0, tied.Count)];
        }

        private static bool CanHarm(Battle battle, Unit actor, Unit target) =>
            actor.Definition.AttackType == AttackType.Heal
            || !target.Definition.Immunities.Contains(actor.Definition.Source) && battle.EffectOn(target) != AttackEffect.Petrification;
    }
}
