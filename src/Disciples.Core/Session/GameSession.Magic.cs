using System.Collections.Generic;
using System.Linq;
using Disciples.Core.Magic;
using Disciples.Core.Map;
using Disciples.Core.Squads;
using Disciples.Core.Units;

namespace Disciples.Core.Session
{
    /// <summary>A place on the map a spell can be cast at and the squads it affects there.</summary>
    public sealed class SpellTarget
    {
        internal SpellTarget(Position at, string name, IEnumerable<Squad> squads)
        {
            At = at;
            Name = name;
            Squads = squads.ToList();
        }

        public Position At { get; }
        public string Name { get; }
        public IReadOnlyList<Squad> Squads { get; }
    }

    public sealed partial class GameSession
    {
        public Spellbook Spellbook { get; }

        /// <summary>Spells are researched once the capital has a building that allows it.</summary>
        public bool CanResearch => Content.Buildings.Any(b => b.AllowsResearch && HasCapitalBuilding(b.Id));

        /// <summary>Spells the player's race can research.</summary>
        public IEnumerable<SpellDefinition> Spells => Content.SpellsOf(Race);

        /// <summary>Learns a spell of the player's race for its research cost; one spell per turn.</summary>
        public ResearchResult Research(SpellDefinition spell)
        {
            if (!Race.Has(spell.Race))
                return ResearchResult.Unavailable;

            if (Spellbook.Knows(spell))
                return ResearchResult.AlreadyKnown;

            if (!CanResearch)
                return ResearchResult.NoTower;

            if (Spellbook.ResearchedThisTurn)
                return ResearchResult.AlreadyResearched;

            if (!Mana.Covers(spell.ResearchCost))
                return ResearchResult.NotEnoughMana;

            Mana = Mana.Minus(spell.ResearchCost);
            Spellbook.Learn(spell);
            return ResearchResult.Researched;
        }

        /// <summary>Explored places the spell can be cast at: hostile squads for damage, the player's wounded squads for healing.</summary>
        public IReadOnlyList<SpellTarget> SpellTargets(SpellDefinition spell) =>
            (spell.Kind == SpellKind.Heal ? WoundedTargets() : HostileTargets()).Where(t => Fog.IsExplored(t.At)).ToList();

        /// <summary>Casts a known spell for its cast cost; each spell once per turn.</summary>
        public CastResult Cast(SpellDefinition spell, Position at)
        {
            if (!Spellbook.Knows(spell))
                return CastResult.Unknown;

            if (Spellbook.WasCast(spell))
                return CastResult.AlreadyCast;

            if (!Mana.Covers(spell.CastCost))
                return CastResult.NotEnoughMana;

            var target = SpellTargets(spell).FirstOrDefault(t => t.At == at);
            if (target == null)
                return CastResult.NoTarget;

            Mana = Mana.Minus(spell.CastCost);
            Spellbook.MarkCast(spell);
            var amount = target.Squads.SelectMany(s => s.AliveUnits).ToList().Sum(u => Apply(spell, u));
            _events.Add(new GameEvent(GameEventKind.SpellCast, spell.Name, target.Name, amount) { Spell = spell, At = at });
            if (spell.Kind == SpellKind.Damage)
                BuryTheDead(target, spell);

            return CastResult.Cast;
        }

        private IEnumerable<SpellTarget> HostileTargets() =>
            Map.Neutrals.Select(n => n.Position)
                .Concat(Map.Enemies.Select(e => e.Position))
                .Concat(Map.Cities.Where(c => !c.IsPlayerOwned).Select(c => c.Position))
                .Distinct()
                .Select(position => (Position: position, Encounter: EncounterAt(position)))
                .Where(t => t.Encounter != null)
                .Select(t => new SpellTarget(t.Position, t.Encounter!.Name, new[] { t.Encounter.Defenders }));

        private IEnumerable<SpellTarget> WoundedTargets()
        {
            var inCities = Map.Cities.Where(c => c.IsPlayerOwned)
                .Select(c => new SpellTarget(c.Position, c.Name, new[] { c.Garrison, PartyAt(c.Position)?.Squad }.OfType<Squad>()));
            var inTheField = _parties.Where(p => Map.CityAt(p.Position) == null)
                .Select(p => new SpellTarget(p.Position, p.Name, new[] { p.Squad }));
            return inCities.Concat(inTheField).Where(t => t.Squads.Any(s => s.Units.Any(u => u.IsWounded)));
        }

        /// <summary>Spell damage ignores armor; a ward stops it like an immunity, since a spell is a single hit.</summary>
        private static int Apply(SpellDefinition spell, Unit unit)
        {
            if (spell.Kind == SpellKind.Heal)
                return unit.Heal(spell.Amount);

            var resists = unit.Definition.Immunities.Contains(spell.Source) || unit.Definition.Wards.Contains(spell.Source);
            return resists ? 0 : unit.TakeDamage(spell.Amount);
        }

        /// <summary>A neutral squad with nobody left or an enemy party without its leader leaves the map.</summary>
        private void BuryTheDead(SpellTarget target, SpellDefinition spell)
        {
            var neutral = Map.NeutralAt(target.At);
            var enemy = Map.EnemyAt(target.At);
            var destroyed = neutral?.Squad.IsDefeated == true || enemy?.Leader.IsAlive == false;

            foreach (var squad in target.Squads)
                squad.RemoveDead();

            if (!destroyed)
                return;

            if (neutral != null)
                Map.RemoveNeutral(neutral);
            if (enemy != null)
                Map.RemoveEnemy(enemy);

            _events.Add(new GameEvent(GameEventKind.SquadDestroyed, target.Name, spell.Name) { Spell = spell, Party = enemy, At = target.At });
            CheckVictory();
        }
    }
}
