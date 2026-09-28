using Disciples.Core.Map;
using Disciples.Core.Squads;

namespace Disciples.Core.Session
{
    public sealed class GameSession
    {
        public GameSession(WorldMap map, Leader leader)
        {
            Map = map;
            Leader = leader;
            Turn = 1;
        }

        public WorldMap Map { get; }
        public Leader Leader { get; }
        public int Turn { get; private set; }

        public MoveResult TryMove(Direction direction)
        {
            var target = Leader.Position.Step(direction);

            if (!Map.Contains(target))
                return MoveResult.OutOfBounds;

            if (Map.TerrainAt(target).MoveCost is not int cost)
                return MoveResult.Impassable;

            if (!Leader.CanAfford(cost))
                return MoveResult.NotEnoughMovement;

            Leader.MoveTo(target, cost);
            return MoveResult.Moved;
        }

        public void EndTurn()
        {
            Turn++;
            Leader.RestoreMovement();
        }
    }
}
