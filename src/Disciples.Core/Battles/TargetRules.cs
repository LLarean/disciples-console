using System.Collections.Generic;
using System.Linq;
using Disciples.Core.Squads;
using Disciples.Core.Units;

namespace Disciples.Core.Battles
{
    public static class TargetRules
    {
        public static IReadOnlyList<Unit> ValidTargets(Unit actor, Squad own, Squad enemies)
        {
            switch (actor.Definition.AttackType)
            {
                case AttackType.Melee:
                    return MeleeTargets(actor, own, enemies);
                case AttackType.Heal:
                    var wounded = own.AliveUnits.Where(u => u.IsWounded).ToList();
                    return wounded.Count > 0 ? wounded : own.AliveUnits.ToList();
                default:
                    return enemies.AliveUnits.ToList();
            }
        }

        /// <summary>
        /// Melee reaches only the nearest enemy line with living units,
        /// and a back-line attacker is blocked while its own front line has living units.
        /// </summary>
        private static IReadOnlyList<Unit> MeleeTargets(Unit actor, Squad own, Squad enemies)
        {
            var actorInBack = !actor.IsLarge && own.SlotOf(actor).Line == SquadLine.Back;
            if (actorInBack && own.UnitsInLine(SquadLine.Front).Any(u => u.IsAlive))
                return new List<Unit>();

            var front = enemies.UnitsInLine(SquadLine.Front).Where(u => u.IsAlive).Distinct().ToList();
            return front.Count > 0 ? front : enemies.UnitsInLine(SquadLine.Back).Where(u => u.IsAlive).ToList();
        }
    }
}
