using System;
using System.Collections.Generic;
using System.Linq;
using Disciples.Core.Battles;
using Disciples.Core.Cities;
using Disciples.Core.Content;
using Disciples.Core.Items;
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
            GameContent content, WorldMap map, IEnumerable<Party> parties, int gold, IRandom random, int turn = 1, FogOfWar? fog = null, int active = 0,
            int enemyGold = 0)
        {
            _parties = parties.ToList();
            if (active < 0 || active >= _parties.Count)
                throw new ArgumentException("The active party is not among the player's parties.", nameof(active));

            Content = content;
            Map = map;
            Party = _parties[active];
            Gold = gold;
            EnemyGold = enemyGold;
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

        /// <summary>The active party: movement, battles, perks and camp hiring act on it. Once the game is lost it is the last fallen party.</summary>
        public Party Party { get; private set; }

        public IRandom Random { get; }
        public int Gold { get; private set; }
        public int EnemyGold { get; private set; }
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

        /// <summary>The enemy takes its turn, then a new turn starts. Check <see cref="IncomingAttack"/> afterwards.</summary>
        public void EndTurn()
        {
            SupplyEnemies();
            MoveEnemies();
            HireEnemyLeader();

            Turn++;
            foreach (var party in _parties)
                party.RestoreMovement();
            ClaimMines();
            Gold += IncomeOf(Owner.Player);

            foreach (var city in Map.Cities.Where(c => c.IsPlayerOwned))
            {
                HealSquad(city.Garrison, HealPercentIn(city));
                if (PartyAt(city.Position) is { } visitor)
                    HealSquad(visitor.Squad, HealPercentIn(city));
            }

            foreach (var guardian in Map.Cities.SelectMany(c => c.Garrison.AliveUnits).Where(u => u.IsGuardian))
                guardian.Heal(guardian.MaxHp);

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

        /// <summary>Buys an item from the merchant's stock into the bag of the party standing there.</summary>
        public BuyResult BuyItem(Site merchant, ItemDefinition item)
        {
            if (merchant.Kind != SiteKind.Merchant || Party.Position != merchant.Position || !merchant.Items.Contains(item))
                return BuyResult.Unavailable;

            if (Gold < item.Cost)
                return BuyResult.NotEnoughGold;

            Gold -= item.Cost;
            merchant.Take(item);
            Party.Give(item);
            return BuyResult.Bought;
        }

        public bool Dismiss(Squad squad, Unit unit)
        {
            if (unit.IsLeader || unit.IsGuardian || !squad.Contains(unit))
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

        /// <summary>Raises a player city to the next tier: a larger garrison and faster healing.</summary>
        public UpgradeResult UpgradeCity(City city)
        {
            if (!city.IsPlayerOwned)
                return UpgradeResult.Unavailable;

            if (city.UpgradeCost is not int cost)
                return UpgradeResult.TopTier;

            if (Gold < cost)
                return UpgradeResult.NotEnoughGold;

            Gold -= cost;
            city.Upgrade();
            return UpgradeResult.Upgraded;
        }

        /// <summary>Percent of max HP units in the city regain per turn.</summary>
        public int HealPercentIn(City city) =>
            city.HealPercent + Content.Buildings.Where(b => HasCapitalBuilding(b.Id)).Sum(b => b.HealBonusPercent);

        /// <summary>The active party's leader uses a potion from the bag on a unit of its squad.</summary>
        public ItemResult UseItem(ItemDefinition item, Unit target)
        {
            if (item.Kind != ItemKind.Potion || !Party.Items.Contains(item) || !Party.Squad.Contains(target))
                return ItemResult.Unavailable;

            if (target.Heal(item.Heal) == 0)
                return ItemResult.NoEffect;

            Party.Take(item);
            return ItemResult.Used;
        }

        /// <summary>The active party's leader puts on an artifact or banner from the bag, replacing the worn one of that kind.</summary>
        public bool Equip(ItemDefinition item) => Party.Equip(item);

        public bool Unequip(ItemDefinition item) => Party.Unequip(item);

        public Battle StartBattle(Encounter encounter) =>
            new Battle(Party.Squad, encounter.Defenders, Random, Rules, BonusesOf(Party, encounter.Enemy));

        /// <summary>
        /// Applies battle results: reward, shared experience and capture on victory. Losing the battle or the leader disbands the party;
        /// the game is lost when it was the last one.
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
                _events.Add(new GameEvent(GameEventKind.BattleWon, encounter.Name, amount: gold) { Party = Party, City = encounter.City, At = Party.Position });
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
                Disband(Party);
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

        /// <summary>A party that lost its leader is gone with its survivors; the next party takes over, or the game is lost.</summary>
        private void Disband(Party party)
        {
            _parties.Remove(party);
            _events.Add(new GameEvent(GameEventKind.PartyLost, party.Name) { Party = party, At = party.Position });

            if (_parties.Count > 0)
            {
                Party = _parties[0];
                return;
            }

            Status = GameStatus.Lost;
            _events.Add(new GameEvent(GameEventKind.GameLost, party.Name) { Party = party });
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
                var kind = owner == Owner.Player ? GameEventKind.MineCaptured : GameEventKind.MineLost;
                _events.Add(new GameEvent(kind, mine.Name, amount: mine.Gold) { Site = mine, At = mine.Position });
            }
        }

        private void Visit(Site site)
        {
            switch (site.Kind)
            {
                case SiteKind.Treasure:
                    Gold += site.Gold;
                    Map.RemoveSite(site);
                    _events.Add(new GameEvent(GameEventKind.TreasureFound, site.Name, amount: site.Gold) { Site = site, Party = Party, At = site.Position });
                    foreach (var item in site.Items)
                    {
                        Party.Give(item);
                        _events.Add(new GameEvent(GameEventKind.ItemFound, item.Name, site.Name) { Item = item, Site = site, Party = Party, At = site.Position });
                    }
                    break;
                case SiteKind.Mine when site.Owner != Owner.Player:
                    site.Capture(Owner.Player);
                    _events.Add(new GameEvent(GameEventKind.MineCaptured, site.Name, amount: site.Gold) { Site = site, Party = Party, At = site.Position });
                    break;
            }
        }

        private void RevealAround(Party party) => Fog.Reveal(party.Position, Rules.SightRadius);

        private void Capture(City city)
        {
            city.Capture(Owner.Player);
            _events.Add(new GameEvent(GameEventKind.CityCaptured, city.Name) { City = city, Party = Party, At = city.Position });
            CheckVictory();
        }

        private void CheckVictory()
        {
            if (Status == GameStatus.Playing && Map.Neutrals.Count == 0 && Map.Enemies.Count == 0 && Map.Cities.All(c => c.IsPlayerOwned))
            {
                Status = GameStatus.Won;
                _events.Add(new GameEvent(GameEventKind.GameWon, Party.Name) { Party = Party });
            }
        }

        private int IncomeOf(Owner owner) =>
            Map.Cities.Where(c => c.Owner == owner).Sum(c => c.Income)
            + Map.Sites.Where(s => s.Kind == SiteKind.Mine && s.Owner == owner).Sum(s => s.Gold);

        /// <summary>The enemy collects income, heals in its cities and fills the squads of the leaders standing there.</summary>
        private void SupplyEnemies()
        {
            EnemyGold += IncomeOf(Owner.Enemy);

            foreach (var city in Map.Cities.Where(c => c.Owner == Owner.Enemy))
            {
                HealSquad(city.Garrison, city.HealPercent);
                if (Map.EnemyAt(city.Position) is not { } visitor)
                    continue;

                HealSquad(visitor.Squad, city.HealPercent);
                Reinforce(visitor, city);
            }
        }

        /// <summary>Buys the dearest recruits the treasury affords while the squad has room.</summary>
        private void Reinforce(Party enemy, City city)
        {
            foreach (var recruit in city.Recruits.Where(r => !r.IsLeader).OrderByDescending(r => r.Cost))
                while (EnemyGold >= recruit.Cost && enemy.Squad.TryAdd(new Unit(recruit)))
                    EnemyGold -= recruit.Cost;
        }

        /// <summary>A new leader appears in the enemy capital once it is vacant; it gets its squad on the following turns.</summary>
        private void HireEnemyLeader()
        {
            var capital = Map.Cities.FirstOrDefault(c => c.IsCapital && c.Owner == Owner.Enemy);
            if (capital == null || Map.Enemies.Count >= Rules.EnemyLeaderLimit || Map.EnemyAt(capital.Position) != null)
                return;

            var leader = Content.EnemyLeaderClasses.Where(l => l.Cost <= EnemyGold).OrderByDescending(l => l.Cost).FirstOrDefault();
            if (leader == null)
                return;

            EnemyGold -= leader.Cost;
            var party = new Party(new Unit(leader), capital.Position, rules: Rules);
            Map.AddEnemy(party);
            _events.Add(new GameEvent(GameEventKind.EnemyAppeared, party.Name, capital.Name) { Party = party, City = capital, At = capital.Position });
        }

        private void MoveEnemies()
        {
            foreach (var enemy in Map.Enemies.ToList())
            {
                enemy.RestoreMovement();
                MoveEnemy(enemy);
            }
        }

        /// <summary>
        /// Walks towards the nearest target: a player party, a city it can capture, a treasure or a mine.
        /// The capital is never a target. Sites on the way are plundered too.
        /// </summary>
        private void MoveEnemy(Party enemy)
        {
            foreach (var step in Pathfinder.FindPath(Map, enemy.Position, IsEnemyTarget, IsBlockedForEnemy))
            {
                if (PartyAt(step) != null || Map.CityAt(step) is { } city && city.Owner != Owner.Enemy)
                {
                    Strike(enemy, step);
                    return;
                }

                var cost = Map.TerrainAt(step).MoveCost.GetValueOrDefault();
                if (!enemy.CanAfford(cost))
                    return;

                Step(enemy, step, cost);
                if (Map.SiteAt(step) is { } site)
                    Plunder(enemy, site);
            }
        }

        private bool IsEnemyTarget(Position position)
        {
            if (PartyAt(position) != null)
                return IncomingAttack == null;

            if (Map.CityAt(position) is { } city)
                return !city.IsCapital && city.Owner != Owner.Enemy;

            return Map.SiteAt(position) is { } site && IsLoot(site) && Map.NeutralAt(position) == null && Map.EnemyAt(position) == null;
        }

        /// <summary>A mine on the player's land is not worth taking: the land claims it back.</summary>
        private bool IsLoot(Site site) =>
            site.Kind == SiteKind.Treasure
            || site.Kind == SiteKind.Mine && site.Owner != Owner.Enemy && Territory.OwnerAt(site.Position) != Owner.Player;

        private void Plunder(Party enemy, Site site)
        {
            if (!IsLoot(site))
                return;

            if (site.Kind == SiteKind.Mine)
            {
                site.Capture(Owner.Enemy);
                _events.Add(new GameEvent(GameEventKind.MineLost, site.Name, enemy.Name, site.Gold) { Site = site, Party = enemy, At = site.Position });
                return;
            }

            EnemyGold += site.Gold;
            Map.RemoveSite(site);
            foreach (var item in site.Items)
            {
                enemy.Give(item);
                if (enemy.Equipped.All(e => e.Kind != item.Kind))
                    enemy.Equip(item);
            }

            _events.Add(new GameEvent(GameEventKind.TreasureLost, site.Name, enemy.Name, site.Gold) { Site = site, Party = enemy, At = site.Position });
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
                _events.Add(new GameEvent(GameEventKind.EnemyAttacks, enemy.Name, attacked.Name) { Party = enemy, From = enemy.Position, At = target });
                return;
            }

            var city = Map.CityAt(target)!;
            if (city.Garrison.IsDefeated)
            {
                var cost = Map.TerrainAt(target).MoveCost.GetValueOrDefault();
                if (enemy.CanAfford(cost))
                    Occupy(enemy, city, cost);
            }
            else if (AutoBattle(enemy, city.Garrison) == BattleOutcome.Victory && enemy.Leader.IsAlive)
            {
                Occupy(enemy, city, 0);
            }
            else
            {
                _events.Add(new GameEvent(GameEventKind.CityHeld, city.Name, enemy.Name) { City = city, Party = enemy, At = city.Position });
            }

            if (!enemy.Leader.IsAlive)
            {
                Map.RemoveEnemy(enemy);
                CheckVictory();
            }
        }

        private static Func<Unit, StatBonus> BonusesOf(Party attacker, Party? defender = null) =>
            unit => attacker.BonusFor(unit).Plus(defender?.BonusFor(unit) ?? StatBonus.None);

        private BattleOutcome AutoBattle(Party attacker, Squad defenders)
        {
            var attackers = attacker.Squad;
            var battle = new Battle(attackers, defenders, Random, Rules, BonusesOf(attacker));
            var ai = new SimpleBattleAi(Random);
            while (!battle.IsOver)
                ai.Act(battle);

            attackers.RemoveDead();
            defenders.RemoveDead();
            return battle.Outcome;
        }

        private void Step(Party enemy, Position target, int cost)
        {
            var from = enemy.Position;
            enemy.MoveTo(target, cost);
            _events.Add(new GameEvent(GameEventKind.EnemyMoved, enemy.Name) { Party = enemy, From = from, At = target });
        }

        private void Occupy(Party enemy, City city, int cost)
        {
            Step(enemy, city.Position, cost);
            city.Capture(Owner.Enemy);
            _events.Add(new GameEvent(GameEventKind.CityFell, city.Name, enemy.Name) { City = city, Party = enemy, At = city.Position });
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
                gameEvent.Unit = p.Unit;
                _events.Add(gameEvent);
            }
        }

        private static void HealSquad(Squad squad, int percent)
        {
            foreach (var unit in squad.AliveUnits)
                unit.Heal(unit.MaxHp * percent / 100);
        }
    }
}
