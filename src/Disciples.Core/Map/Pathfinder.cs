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

            while (true)
            {
                var open = cost.Where(c => !done.Contains(c.Key)).ToList();
                if (open.Count == 0)
                    return Array.Empty<Position>();

                var current = open.OrderBy(c => c.Value).First().Key;
                if (current != start && isGoal(current))
                    return Trace(previous, start, current);

                done.Add(current);
                if (current != start && isBlocked(current))
                    continue;

                foreach (var next in Directions.Select(current.Step))
                {
                    if (!map.Contains(next) || done.Contains(next) || map.TerrainAt(next).MoveCost is not int step)
                        continue;

                    var total = cost[current] + step;
                    if (cost.TryGetValue(next, out var known) && known <= total)
                        continue;

                    cost[next] = total;
                    previous[next] = current;
                }
            }
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
