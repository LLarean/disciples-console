using System;
using System.Collections.Generic;
using System.Linq;
using Disciples.Core.Cities;
using Disciples.Core.Map;
using Disciples.Core.Units;

namespace Disciples.Core.Session
{
    public enum ThiefAction
    {
        /// <summary>Wounds every unit of the squad without killing anyone.</summary>
        Poison,
        /// <summary>Kills the weakest unit that is neither a leader nor a guardian.</summary>
        Assassinate,
        /// <summary>Takes gold from the enemy treasury through its party or city.</summary>
        Steal
    }

    public sealed partial class GameSession
    {
        public bool IsThief => Party.Leader.Definition.IsThief;

        /// <summary>Thief leaders are hired once the capital has a building that allows it.</summary>
        public bool CanHireThieves => Content.Buildings.Any(b => b.AllowsThieves && HasCapitalBuilding(b.Id));

        /// <summary>Tiles next to the active party that hold a hostile squad.</summary>
        public IReadOnlyList<Position> ThiefTargets() =>
            Enum.GetValues(typeof(Direction)).Cast<Direction>()
                .Select(d => Party.Position.Step(d))
                .Where(p => Map.Contains(p) && EncounterAt(p) != null)
                .ToList();

        /// <summary>
        /// The active thief acts against the hostile squad on an adjacent tile. Any attempt takes the rest of the party's movement;
        /// a failed one wounds the thief.
        /// </summary>
        public ThiefResult Infiltrate(ThiefAction action, Position at)
        {
            if (!IsThief)
                return ThiefResult.NotAThief;

            if (Party.MovementPoints == 0)
                return ThiefResult.Exhausted;

            if (!ThiefTargets().Contains(at))
                return ThiefResult.NoTarget;

            var target = EncounterAt(at)!;
            if (!IsWorthwhile(action, target))
                return ThiefResult.Pointless;

            var thief = Party;
            thief.Exhaust();
            if (Random.Next(0, 100) >= Rules.ThiefSuccessPercent)
            {
                var damage = thief.Leader.TakeDamage(Rules.ThiefFailureDamage);
                _events.Add(new GameEvent(GameEventKind.ThiefCaught, thief.Name, target.Name, damage) { Party = thief, At = at });
                if (!thief.Leader.IsAlive)
                    Disband(thief);
                return ThiefResult.Caught;
            }

            switch (action)
            {
                case ThiefAction.Poison:
                    var harm = target.Defenders.AliveUnits.ToList().Sum(u => u.TakeDamage(Math.Min(Rules.ThiefPoisonDamage, u.Hp - 1)));
                    _events.Add(new GameEvent(GameEventKind.ThiefPoisoned, thief.Name, target.Name, harm) { Party = thief, At = at });
                    break;
                case ThiefAction.Assassinate:
                    Assassinate(target, at);
                    break;
                default:
                    var gold = Math.Min(EnemyGold, Rules.ThiefStealGold);
                    EnemyGold -= gold;
                    Gold += gold;
                    _events.Add(new GameEvent(GameEventKind.ThiefStole, thief.Name, target.Name, gold) { Party = thief, At = at });
                    break;
            }

            return ThiefResult.Done;
        }

        private bool IsWorthwhile(ThiefAction action, Encounter target) => action switch
        {
            ThiefAction.Assassinate => VictimIn(target) != null,
            ThiefAction.Steal => EnemyGold > 0 && (target.Enemy != null || target.City?.Owner == Owner.Enemy),
            _ => target.Defenders.AliveUnits.Any(u => u.Hp > 1)
        };

        private static Unit? VictimIn(Encounter target) =>
            target.Defenders.AliveUnits.Where(u => !u.IsLeader && !u.IsGuardian).OrderBy(u => u.Hp).FirstOrDefault();

        private void Assassinate(Encounter target, Position at)
        {
            var victim = VictimIn(target)!;
            victim.TakeDamage(victim.Hp);
            _events.Add(new GameEvent(GameEventKind.ThiefAssassinated, Party.Name, victim.Name) { Party = Party, Unit = victim, At = at });

            var wiped = target.Neutral != null && target.Defenders.IsDefeated;
            target.Defenders.RemoveDead();
            if (!wiped)
                return;

            Map.RemoveNeutral(target.Neutral!);
            _events.Add(new GameEvent(GameEventKind.SquadDestroyed, target.Name, Party.Name) { Party = Party, At = at });
            CheckVictory();
        }
    }
}
