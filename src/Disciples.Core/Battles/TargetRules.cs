using System;
using System.Collections.Generic;
using System.Linq;
using Disciples.Core.Squads;
using Disciples.Core.Units;

namespace Disciples.Core.Battles
{
    public static class TargetRules
    {
        /// <param name="present">Units still taking part in the battle; living units by default.</param>
        public static IReadOnlyList<Unit> ValidTargets(Unit actor, Squad own, Squad enemies, Func<Unit, bool>? present = null)
        {
            present ??= u => u.IsAlive;
            switch (actor.Definition.AttackType)
            {
                case AttackType.Melee:
                    return MeleeTargets(actor, own, enemies, present);
                case AttackType.Heal:
                    var allies = own.Units.Where(present).ToList();
                    var wounded = allies.Where(u => u.IsWounded).ToList();
                    return wounded.Count > 0 ? wounded : allies;
                default:
                    return enemies.Units.Where(present).ToList();
            }
        }

        /// <summary>
        /// Melee reaches only the nearest enemy line with units still fighting,
        /// and a back-line attacker is blocked while its own front line has such units.
        /// </summary>
        private static IReadOnlyList<Unit> MeleeTargets(Unit actor, Squad own, Squad enemies, Func<Unit, bool> present)
        {
            var actorInBack = !actor.IsLarge && own.SlotOf(actor).Line == SquadLine.Back;
            if (actorInBack && own.UnitsInLine(SquadLine.Front).Any(present))
                return new List<Unit>();

            var front = enemies.UnitsInLine(SquadLine.Front).Where(present).Distinct().ToList();
            return front.Count > 0 ? front : enemies.UnitsInLine(SquadLine.Back).Where(present).ToList();
        }
    }
}
