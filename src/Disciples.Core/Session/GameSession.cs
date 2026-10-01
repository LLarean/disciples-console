using System;
using System.Collections.Generic;
using System.Linq;
using Disciples.Core.Battles;
using Disciples.Core.Cities;
using Disciples.Core.Content;
using Disciples.Core.Map;
using Disciples.Core.Squads;
using Disciples.Core.Units;

namespace Disciples.Core.Session
{
    /// <summary>Command facade over the game state; front-ends call its methods and drain <see cref="TakeEvents"/>.</summary>
    public sealed class GameSession
    {
        private readonly List<GameEvent> _events = new List<GameEvent>();

        private readonly List<Party> _parties;

        public GameSession(GameContent content, WorldMap map, Party party, int gold, IRandom random, int turn = 1, FogOfWar? fog = null)
            : this(content, map, new[] { party }, gold, random, turn, fog)
        {
        }

        public GameSession(
            GameContent content, WorldMap map, IEnumerable<Party> parties, int gold, IRandom random, int turn = 1, FogOfWar? fog = null, int active = 0)
        {
            _parties = parties.ToList();
            if (active < 0 || active >= _parties.Count)
                throw new ArgumentException("The active party is not among the player's parties.", nameof(active));

            Content = content;
            Map = map;
            Party = _parties[active];
            Gold = gold;
            Random = random;
            Turn = turn;
            Fog = fog ?? new FogOfWar(map.Width, map.Height);
            Territory = new Territory(map, Rules.CapitalTerritoryRadius, Rules.CityTerritoryRadius);
            foreach (var city in map.Cities.Where(c => c.IsPlayerOwned))
                Fog.Reveal(city.Position, Rules.SightRadius);
            foreach (var party in _parties)
                RevealAround(party);
        }

        public GameContent Content { get; }
        public GameRules Rules => Content.Rules;
        public WorldMap Map { get; }
        public FogOfWar Fog { get; }
        public Territory Territory { get; }
        public IReadOnlyList<Party> Parties => _parties;

        /// <summary>The active party: movement, battles, perks and camp hiring act on it.</summary>
        public Party Party { get; private set; }

        public IRandom Random { get; }
        public int Gold { get; private set; }
        public int Turn { get; private set; }
        public GameStatus Status { get; private set; }
        public City? CurrentCity => Map.CityAt(Party.Position);
        public Site? CurrentSite => Map.SiteAt(Party.Position);
        public City? Capital => Map.Cities.FirstOrDefault(c => c.IsCapital && c.IsPlayerOwned);

        /// <summary>An enemy leader attacked a party during the enemy turn; that party becomes active and the front-end must fight it.</summary>
        public Encounter? IncomingAttack { get; private set; }

        public IReadOnlyList<GameEvent> TakeEvents()
        {
            var events = _events.ToList();
            _events.Clear();
            return events;
        }

        public Party? PartyAt(Position position) => _parties.FirstOrDefault(p => p.Position == position);

        public bool Select(Party party)
        {
            if (!_parties.Contains(party))
                return false;

            Party = party;
            return true;
        }

        /// <summary>The hostile squad guarding the tile, if any.</summary>
        public Encounter? EncounterAt(Position position)
        {
            var neutral = Map.NeutralAt(position);
            if (neutral != null)
                return Encounter.With(neutral);

            var enemy = Map.EnemyAt(position);
            if (enemy != null)
                return Encounter.With(enemy);

            var city = Map.CityAt(position);
            return city != null && !city.IsPlayerOwned && !city.Garrison.IsDefeated ? Encounter.With(city) : null;
        }

        public MoveResult TryMove(Direction direction)
        {
            var target = Party.Position.Step(direction);

            if (!Map.Contains(target))
                return MoveResult.OutOfBounds;

            if (PartyAt(target) != null)
                return MoveResult.Occupied;

            if (EncounterAt(target) != null)
                return MoveResult.EnemyEncountered;

            if (Map.TerrainAt(target).MoveCost is not int cost)
                return MoveResult.Impassable;

            if (!Party.CanAfford(cost))
                return MoveResult.NotEnoughMovement;

            Party.MoveTo(target, cost);
            RevealAround(Party);

            if (Map.SiteAt(target) is { } site)
            {
                Visit(site);
                return MoveResult.SiteVisited;
            }

            var city = Map.CityAt(target);
            if (city == null || city.IsPlayerOwned)
                return MoveResult.Moved;

            Capture(city);
            return MoveResult.CityCaptured;
        }

        /// <summary>
        /// Cheapest route for the party to the target. Hostile tiles are only allowed as the target, where the walk ends in a battle.
        /// Tiles held by other parties are never entered.
        /// </summary>
        public Route PlanRoute(Position target)
        {
            if (PartyAt(target) != null || !Map.Contains(target))
                return Route.None;

            var path = Pathfinder.FindPath(Map, Party.Position, p => p == target, p => EncounterAt(p) != null || PartyAt(p) != null);
            return Route.Along(Map, path);
        }

        /// <summary>Enemy leaders move, then a new turn starts. Check <see cref="IncomingAttack"/> afterwards.</summary>
        public void EndTurn()
        {
            MoveEnemies();

            Turn++;
            foreach (var party in _parties)
                party.RestoreMovement();
            ClaimMines();
            Gold += Map.Cities.Where(c => c.IsPlayerOwned).Sum(c => c.Income)
                    + Map.Sites.Where(s => s.Kind == SiteKind.Mine && s.Owner == Owner.Player).Sum(s => s.Gold);

            foreach (var city in Map.Cities.Where(c => c.IsPlayerOwned))
                HealSquad(city.Garrison);

            foreach (var party in _parties.Where(p => Map.CityAt(p.Position)?.IsPlayerOwned == true))
                HealSquad(party.Squad);

            _events.Add(new GameEvent(GameEventKind.TurnStarted, amount: Turn));
        }

        /// <summary>Hires into the party visiting the city, or into the garrison when there is no party or no room in it.</summary>
        public HireResult Hire(City city, UnitDefinition definition)
        {
            if (Gold < definition.Cost)
                return HireResult.NotEnoughGold;

            var unit = new Unit(definition);
            HireResult result;
            if (PartyAt(city.Position)?.Squad.TryAdd(unit) == true)
                result = HireResult.HiredToParty;
            else if (city.Garrison.TryAdd(unit))
                result = HireResult.HiredToGarrison;
            else
                return HireResult.NoRoom;

            Gold -= definition.Cost;
            return result;
        }

        /// <summary>
        /// Hires a leader of one of the leader classes in the capital. The new party appears there and becomes active,
        /// so the capital must have no visiting party.
        /// </summary>
        public HireResult HireLeader(City city, UnitDefinition leader)
        {
            if (!city.IsCapital || !city.IsPlayerOwned || !Rules.LeaderClasses.Contains(leader.Id))
                return HireResult.Unavailable;

            if (PartyAt(city.Position) != null)
                return HireResult.NoRoom;

            if (Gold < leader.Cost)
                return HireResult.NotEnoughGold;

            Gold -= leader.Cost;
            Party = new Party(new Unit(leader), city.Position, rules: Rules);
            _parties.Add(Party);
            return HireResult.LeaderHired;
        }

        /// <summary>Hires a mercenary straight into the party standing at the camp.</summary>
        public HireResult HireMercenary(Site camp, UnitDefinition definition)
        {
            if (Party.Position != camp.Position || !camp.Mercenaries.Contains(definition))
                return HireResult.Unavailable;

            if (Gold < definition.Cost)
                return HireResult.NotEnoughGold;

            if (!Party.Squad.TryAdd(new Unit(definition)))
                return HireResult.NoRoom;

            Gold -= definition.Cost;
            return HireResult.HiredToParty;
        }

        public bool Dismiss(Squad squad, Unit unit)
        {
            if (unit.IsLeader || !squad.Contains(unit))
                return false;

            squad.Remove(unit);
            return true;
        }

        public bool TakePerk(LeaderPerk perk)
        {
            if (!Party.CanTake(perk))
                return false;

            Party.Take(perk);
            return true;
        }

        public BuildResult Build(City city, Building building)
        {
            if (city.HasBuilt(building.Id))
                return BuildResult.AlreadyBuilt;

            if (building.Requires != null && !city.HasBuilt(building.Requires))
                return BuildResult.RequirementMissing;

            if (Gold < building.Cost)
                return BuildResult.NotEnoughGold;

            Gold -= building.Cost;
            city.MarkBuilt(building);
            return BuildResult.Built;
        }

        public Battle StartBattle(Encounter encounter) => new Battle(Party.Squad, encounter.Defenders, Random, Rules);

        /// <summary>
        /// Applies battle results: reward, shared experience and capture on victory. Losing the battle or the leader ends the game.
        /// Retreating costs the party the rest of its movement.
        /// </summary>
        public BattleReport FinishBattle(Battle battle, Encounter encounter)
        {
            if (!battle.IsOver)
                throw new InvalidOperationException("The battle is still going on.");

            IncomingAttack = null;
            var outcome = battle.Outcome;
            var gold = 0;
            var experience = 0;
            IReadOnlyList<UnitProgress> progress = Array.Empty<UnitProgress>();
            City? captured = null;

            if (outcome == BattleOutcome.Victory && Party.Leader.IsAlive)
            {
                gold = encounter.Reward;
                Gold += gold;
                experience = encounter.Defenders.Units.Sum(u => u.Definition.ExperienceValue);
                progress = Progression.Share(Party.Squad.Units, experience, Content.Unit, HasCapitalBuilding);
                _events.Add(new GameEvent(GameEventKind.BattleWon, encounter.Name, amount: gold));
                Report(progress);

                if (encounter.Neutral != null)
                    Map.RemoveNeutral(encounter.Neutral);
            }

            Party.Squad.RemoveDead();
            encounter.Defenders.RemoveDead();

            if (encounter.Enemy != null && !encounter.Enemy.Leader.IsAlive)
                Map.RemoveEnemy(encounter.Enemy);

            if (outcome == BattleOutcome.Retreat)
                Party.Exhaust();

            if (outcome == BattleOutcome.Defeat || !Party.Leader.IsAlive)
            {
                Status = GameStatus.Lost;
                _events.Add(new GameEvent(GameEventKind.GameLost, Party.Name));
            }
            else if (outcome == BattleOutcome.Victory && encounter.City != null)
            {
                captured = encounter.City;
                Party.MoveTo(captured.Position, 0);
                RevealAround(Party);
                Capture(captured);
            }
            else
            {
                CheckVictory();
            }

            return new BattleReport(outcome, gold, experience, progress, captured);
        }

        /// <summary>Mines on owned land pass to the land's owner.</summary>
        private void ClaimMines()
        {
            foreach (var mine in Map.Sites.Where(s => s.Kind == SiteKind.Mine))
            {
                var owner = Territory.OwnerAt(mine.Position);
                if (owner == Owner.Neutral || owner == mine.Owner)
                    continue;

                mine.Capture(owner);
                _events.Add(new GameEvent(owner == Owner.Player ? GameEventKind.MineCaptured : GameEventKind.MineLost, mine.Name, amount: mine.Gold));
            }
        }

        private void Visit(Site site)
        {
            switch (site.Kind)
            {
                case SiteKind.Treasure:
                    Gold += site.Gold;
                    Map.RemoveSite(site);
                    _events.Add(new GameEvent(GameEventKind.TreasureFound, site.Name, amount: site.Gold));
                    break;
                case SiteKind.Mine when site.Owner != Owner.Player:
                    site.Capture(Owner.Player);
                    _events.Add(new GameEvent(GameEventKind.MineCaptured, site.Name, amount: site.Gold));
                    break;
            }
        }

        private void RevealAround(Party party) => Fog.Reveal(party.Position, Rules.SightRadius);

        private void Capture(City city)
        {
            city.Capture(Owner.Player);
            _events.Add(new GameEvent(GameEventKind.CityCaptured, city.Name));
            CheckVictory();
        }

        private void CheckVictory()
        {
            if (Status == GameStatus.Playing && Map.Neutrals.Count == 0 && Map.Enemies.Count == 0 && Map.Cities.All(c => c.IsPlayerOwned))
            {
                Status = GameStatus.Won;
                _events.Add(new GameEvent(GameEventKind.GameWon, Party.Name));
            }
        }

        private void MoveEnemies()
        {
            foreach (var enemy in Map.Enemies.ToList())
            {
                enemy.RestoreMovement();
                MoveEnemy(enemy);
            }
        }

        /// <summary>Walks towards the nearest target: a player party or a city it can capture. The capital is never a target.</summary>
        private void MoveEnemy(Party enemy)
        {
            var path = Pathfinder.FindPath(Map, enemy.Position, IsEnemyTarget, IsBlockedForEnemy);
            for (var i = 0; i < path.Count; i++)
            {
                var step = path[i];
                if (i == path.Count - 1)
                {
                    Strike(enemy, step);
                    return;
                }

                var cost = Map.TerrainAt(step).MoveCost.GetValueOrDefault();
                if (!enemy.CanAfford(cost))
                    return;

                enemy.MoveTo(step, cost);
            }
        }

        private bool IsEnemyTarget(Position position)
        {
            if (PartyAt(position) != null)
                return IncomingAttack == null;

            return Map.CityAt(position) is { IsCapital: false } city && city.Owner != Owner.Enemy;
        }

        private bool IsBlockedForEnemy(Position position) =>
            PartyAt(position) != null
            || Map.NeutralAt(position) != null
            || Map.EnemyAt(position) != null
            || Map.CityAt(position) is { } city && city.Owner != Owner.Enemy;

        private void Strike(Party enemy, Position target)
        {
            if (PartyAt(target) is { } attacked)
            {
                Party = attacked;
                IncomingAttack = Encounter.With(enemy);
                _events.Add(new GameEvent(GameEventKind.EnemyAttacks, enemy.Name));
                return;
            }

            var city = Map.CityAt(target)!;
            if (city.Garrison.IsDefeated)
            {
                var cost = Map.TerrainAt(target).MoveCost.GetValueOrDefault();
                if (enemy.CanAfford(cost))
                    Occupy(enemy, city, cost);
            }
            else if (AutoBattle(enemy.Squad, city.Garrison) == BattleOutcome.Victory && enemy.Leader.IsAlive)
            {
                Occupy(enemy, city, 0);
            }
            else
            {
                _events.Add(new GameEvent(GameEventKind.CityHeld, city.Name, enemy.Name));
            }

            if (!enemy.Leader.IsAlive)
            {
                Map.RemoveEnemy(enemy);
                CheckVictory();
            }
        }

        private BattleOutcome AutoBattle(Squad attackers, Squad defenders)
        {
            var battle = new Battle(attackers, defenders, Random, Rules);
            var ai = new SimpleBattleAi(Random);
            while (!battle.IsOver)
                ai.Act(battle);

            attackers.RemoveDead();
            defenders.RemoveDead();
            return battle.Outcome;
        }

        private void Occupy(Party enemy, City city, int cost)
        {
            enemy.MoveTo(city.Position, cost);
            city.Capture(Owner.Enemy);
            _events.Add(new GameEvent(GameEventKind.CityFell, city.Name, enemy.Name));
        }

        private bool HasCapitalBuilding(string buildingId) => Capital?.HasBuilt(buildingId) == true;

        private void Report(IEnumerable<UnitProgress> progress)
        {
            foreach (var p in progress)
            {
                var gameEvent = p.Kind switch
                {
                    ProgressKind.LeveledUp => new GameEvent(GameEventKind.UnitLeveledUp, p.Unit.Name, amount: p.Unit.Level),
                    ProgressKind.Upgraded => new GameEvent(GameEventKind.UnitUpgraded, p.PreviousName, p.Unit.Name),
                    _ => new GameEvent(GameEventKind.UnitAwaitsBuilding, p.Unit.Name, Content.BuildingName(p.Building ?? ""))
                };
                _events.Add(gameEvent);
            }
        }

        private void HealSquad(Squad squad)
        {
            foreach (var unit in squad.AliveUnits)
                unit.Heal(unit.MaxHp * HealPercent / 100);
        }

        private int HealPercent =>
            Rules.CityHealPercent + Content.Buildings.Where(b => HasCapitalBuilding(b.Id)).Sum(b => b.HealBonusPercent);
    }
}
