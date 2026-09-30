using System;
using System.Collections.Generic;
using System.Linq;

namespace Disciples.Core.Map
{
    /// <summary>A planned path with the running movement cost of each step.</summary>
    public sealed class Route
    {
        public static readonly Route None = new Route(Array.Empty<Position>(), Array.Empty<int>());

        private readonly IReadOnlyList<int> _totals;

        private Route(IReadOnlyList<Position> steps, IReadOnlyList<int> totals)
        {
            Steps = steps;
            _totals = totals;
        }

        public IReadOnlyList<Position> Steps { get; }
        public bool IsEmpty => Steps.Count == 0;
        public int Cost => IsEmpty ? 0 : _totals[_totals.Count - 1];

        public int CostTo(int step) => _totals[step];

        /// <summary>Number of leading steps affordable with the given movement points.</summary>
        public int Reachable(int movementPoints) => _totals.TakeWhile(t => t <= movementPoints).Count();

        public static Route Along(WorldMap map, IReadOnlyList<Position> steps)
        {
            var total = 0;
            var totals = steps.Select(s => total += map.TerrainAt(s).MoveCost.GetValueOrDefault()).ToList();
            return new Route(steps, totals);
        }
    }
}
