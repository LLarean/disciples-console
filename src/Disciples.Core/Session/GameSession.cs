using System.Linq;
using Disciples.Core.Battles;
using Disciples.Core.Cities;
using Disciples.Core.Map;
using Disciples.Core.Squads;
using Disciples.Core.Units;

namespace Disciples.Core.Session
{
    public sealed class GameSession
    {
        private const int CityHealPercent = 25;

        public GameSession(WorldMap map, Party party, int gold, IRandom random)
        {
            Map = map;
            Party = party;
            Gold = gold;
            Random = random;
            Turn = 1;
        }

        public WorldMap Map { get; }
        public Party Party { get; }
        public IRandom Random { get; }
        public int Gold { get; private set; }
        public int Turn { get; private set; }
        public bool IsGameOver { get; private set; }
        public City? CurrentCity => Map.CityAt(Party.Position);

        public MoveResult TryMove(Direction direction)
        {
            var target = Party.Position.Step(direction);

            if (!Map.Contains(target))
                return MoveResult.OutOfBounds;

            if (Map.NeutralAt(target) != null)
                return MoveResult.EnemyEncountered;

            if (Map.TerrainAt(target).MoveCost is not int cost)
                return MoveResult.Impassable;

            if (!Party.CanAfford(cost))
                return MoveResult.NotEnoughMovement;

            Party.MoveTo(target, cost);
            return MoveResult.Moved;
        }

        public void EndTurn()
        {
            Turn++;
            Party.RestoreMovement();
            Gold += Map.Cities.Sum(c => c.Income);

            foreach (var city in Map.Cities)
                HealSquad(city.Garrison);

            if (CurrentCity != null)
                HealSquad(Party.Squad);
        }

        public HireResult Hire(City city, UnitDefinition definition)
        {
            if (Gold < definition.Cost)
                return HireResult.NotEnoughGold;

            var unit = new Unit(definition);
            HireResult result;
            if (Party.Position == city.Position && Party.Squad.TryAdd(unit))
                result = HireResult.HiredToParty;
            else if (city.Garrison.TryAdd(unit))
                result = HireResult.HiredToGarrison;
            else
                return HireResult.NoRoom;

            Gold -= definition.Cost;
            return result;
        }

        public bool Dismiss(Squad squad, Unit unit)
        {
            if (unit.IsLeader || !squad.Contains(unit))
                return false;

            squad.Remove(unit);
            return true;
        }

        public BuildResult Build(City city, Building building)
        {
            if (building.IsBuilt)
                return BuildResult.AlreadyBuilt;

            if (building.Requires != null && city.FindBuilding(building.Requires)?.IsBuilt != true)
                return BuildResult.RequirementMissing;

            if (Gold < building.Cost)
                return BuildResult.NotEnoughGold;

            Gold -= building.Cost;
            building.IsBuilt = true;
            return BuildResult.Built;
        }

        public Battle StartBattle(NeutralSquad neutral) => new Battle(Party.Squad, neutral.Squad, Random);

        /// <summary>Applies battle results. A fallen leader is revived with 1 HP until leader death rules are implemented.</summary>
        public void FinishBattle(Battle battle, NeutralSquad neutral)
        {
            if (battle.Outcome == BattleOutcome.Defeat)
            {
                IsGameOver = true;
                return;
            }

            if (battle.Outcome == BattleOutcome.Victory)
            {
                Map.RemoveNeutral(neutral);
                Gold += neutral.Reward;
            }

            if (!Party.Leader.IsAlive)
                Party.Leader.Revive(1);

            Party.Squad.RemoveDead();
            neutral.Squad.RemoveDead();
        }

        private static void HealSquad(Squad squad)
        {
            foreach (var unit in squad.AliveUnits)
                unit.Heal(unit.MaxHp * CityHealPercent / 100);
        }
    }
}
