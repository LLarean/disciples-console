using System;
using System.Collections.Generic;
using System.Linq;

namespace Disciples.Core.Map
{
    public static class Pathfinder
    {
        private static readonly Direction[] Directions = (Direction[])Enum.GetValues(typeof(Direction));

        /// <summary>
        /// Cheapest path by terrain cost to the nearest goal tile, excluding the start and including the goal.
        /// Goal tiles are reached even if blocked; empty when no goal is reachable.
        /// </summary>
        public static IReadOnlyList<Position> FindPath(
            WorldMap map, Position start, Func<Position, bool> isGoal, Func<Position, bool> isBlocked)
        {
            var cost = new Dictionary<Position, int> { [start] = 0 };
            var previous = new Dictionary<Position, Position>();
            var done = new HashSet<Position>();

            // Open tiles sorted by cost; equal costs go to the tile discovered first, which keeps the routes stable.
            var discovered = new List<Position> { start };
            var order = new Dictionary<Position, int> { [start] = 0 };
            var open = new SortedSet<(int Cost, int Order)> { (0, 0) };

            while (open.Count > 0)
            {
                var cheapest = open.Min;
                open.Remove(cheapest);

                var current = discovered[cheapest.Order];
                if (current != start && isGoal(current))
                    return Trace(previous, start, current);

                done.Add(current);
                if (current != start && isBlocked(current))
                    continue;

                foreach (var next in Directions.Select(current.Step))
                {
                    if (!map.Contains(next) || done.Contains(next) || map.TerrainAt(next).MoveCost is not int step)
                        continue;

                    var total = cheapest.Cost + step;
                    if (cost.TryGetValue(next, out var known))
                    {
                        if (known <= total)
                            continue;

                        open.Remove((known, order[next]));
                    }
                    else
                    {
                        order[next] = discovered.Count;
                        discovered.Add(next);
                    }

                    cost[next] = total;
                    previous[next] = current;
                    open.Add((total, order[next]));
                }
            }

            return Array.Empty<Position>();
        }

        private static IReadOnlyList<Position> Trace(Dictionary<Position, Position> previous, Position start, Position goal)
        {
            var path = new List<Position>();
            for (var p = goal; p != start; p = previous[p])
                path.Add(p);
            path.Reverse();
            return path;
        }
    }
}
