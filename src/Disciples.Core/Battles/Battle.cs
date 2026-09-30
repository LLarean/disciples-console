using System;
using System.Collections.Generic;
using System.Linq;
using Disciples.Core.Content;
using Disciples.Core.Squads;
using Disciples.Core.Units;

namespace Disciples.Core.Battles
{
    public enum BattleOutcome
    {
        Ongoing,
        Victory,
        Defeat,
        Retreat
    }

    /// <summary>Turn-based battle; outcome is from the attacker's point of view.</summary>
    public sealed class Battle
    {
        private readonly IRandom _random;
        private readonly GameRules _rules;
        private readonly List<Unit> _queue = new List<Unit>();
        private readonly HashSet<Unit> _defending = new HashSet<Unit>();
        private readonly HashSet<Unit> _waited = new HashSet<Unit>();
        private readonly HashSet<(Unit, AttackSource)> _spentWards = new HashSet<(Unit, AttackSource)>();
        private readonly List<BattleEvent> _log = new List<BattleEvent>();

        public Battle(Squad attackers, Squad defenders, IRandom random, GameRules? rules = null)
        {
            Attackers = attackers;
            Defenders = defenders;
            _random = random;
            _rules = rules ?? new GameRules();
            StartRound();
        }

        public Squad Attackers { get; }
        public Squad Defenders { get; }
        public int Round { get; private set; }
        public BattleOutcome Outcome { get; private set; }
        public bool IsOver => Outcome != BattleOutcome.Ongoing;
        public Unit? Current => IsOver || _queue.Count == 0 ? null : _queue[0];
        public bool IsAttackersTurn => Current != null && Attackers.Contains(Current);
        public IReadOnlyList<Unit> Queue => _queue;
        public IReadOnlyList<BattleEvent> Log => _log;

        /// <summary>A unit may postpone its turn to the end of the queue once per round.</summary>
        public bool CanWait => Current != null && _queue.Count > 1 && !_waited.Contains(Current);

        public bool IsDefending(Unit unit) => _defending.Contains(unit);

        public Squad SquadOf(Unit unit) => Attackers.Contains(unit) ? Attackers : Defenders;

        public Squad OpponentsOf(Unit unit) => Attackers.Contains(unit) ? Defenders : Attackers;

        public IReadOnlyList<Unit> ValidTargets()
        {
            var actor = Current;
            return actor == null ? Array.Empty<Unit>() : TargetRules.ValidTargets(actor, SquadOf(actor), OpponentsOf(actor));
        }

        /// <summary>Attacks or heals the target. Units hitting all enemies ignore the target choice.</summary>
        public bool Act(Unit target)
        {
            var actor = Current;
            if (actor == null || !ValidTargets().Contains(target))
                return false;

            _defending.Remove(actor);
            switch (actor.Definition.AttackType)
            {
                case AttackType.Heal:
                    var healed = target.Heal(actor.Power);
                    _log.Add(new BattleEvent(BattleEventKind.Healed, Round, actor, target, healed));
                    break;
                case AttackType.AllEnemies:
                    foreach (var enemy in OpponentsOf(actor).AliveUnits.ToList())
                        Strike(actor, enemy);
                    break;
                default:
                    Strike(actor, target);
                    break;
            }

            EndAction();
            return true;
        }

        public void Defend()
        {
            var actor = Current;
            if (actor == null)
                return;

            _defending.Add(actor);
            _log.Add(new BattleEvent(BattleEventKind.Defended, Round, actor));
            EndAction();
        }

        public bool Wait()
        {
            var actor = Current;
            if (actor == null || !CanWait)
                return false;

            _waited.Add(actor);
            _queue.RemoveAt(0);
            _queue.Add(actor);
            _log.Add(new BattleEvent(BattleEventKind.Waited, Round, actor));
            return true;
        }

        public void Retreat()
        {
            if (IsOver)
                return;

            Outcome = BattleOutcome.Retreat;
            _log.Add(new BattleEvent(BattleEventKind.Retreated, Round));
        }

        private void Strike(Unit actor, Unit target)
        {
            var source = actor.Definition.Source;
            if (target.Definition.Immunities.Contains(source))
            {
                _log.Add(new BattleEvent(BattleEventKind.Immune, Round, actor, target));
                return;
            }

            if (target.Definition.Wards.Contains(source) && _spentWards.Add((target, source)))
            {
                _log.Add(new BattleEvent(BattleEventKind.Warded, Round, actor, target));
                return;
            }

            if (_random.Next(0, 100) >= actor.Accuracy)
            {
                _log.Add(new BattleEvent(BattleEventKind.Miss, Round, actor, target));
                return;
            }

            var dealt = target.TakeDamage(Damage(actor, target));
            _log.Add(new BattleEvent(BattleEventKind.Hit, Round, actor, target, dealt));

            if (!target.IsAlive)
                _log.Add(new BattleEvent(BattleEventKind.Killed, Round, actor, target));
        }

        private int Damage(Unit actor, Unit target)
        {
            var power = actor.Power;
            var spread = power * _rules.DamageSpreadPercent / 100;
            var damage = power + _random.Next(-spread, spread + 1);
            damage = damage * (100 - target.Armor) / 100;

            if (IsDefending(target))
                damage = damage * _rules.DefendDamagePercent / 100;

            return Math.Max(1, damage);
        }

        private void EndAction()
        {
            _queue.RemoveAt(0);
            _queue.RemoveAll(u => !u.IsAlive);

            if (Defenders.IsDefeated)
                Outcome = BattleOutcome.Victory;
            else if (Attackers.IsDefeated)
                Outcome = BattleOutcome.Defeat;
            else if (_queue.Count == 0)
                StartRound();
        }

        private void StartRound()
        {
            if (Round >= _rules.MaxBattleRounds)
            {
                Outcome = BattleOutcome.Retreat;
                _log.Add(new BattleEvent(BattleEventKind.Retreated, Round));
                return;
            }

            Round++;
            _waited.Clear();
            _log.Add(new BattleEvent(BattleEventKind.RoundStarted, Round));

            var order = Attackers.AliveUnits.Concat(Defenders.AliveUnits)
                .Select(u => (unit: u, roll: u.Initiative + _random.Next(0, _rules.InitiativeSpread)))
                .OrderByDescending(x => x.roll)
                .Select(x => x.unit);
            _queue.AddRange(order);
        }
    }
}
