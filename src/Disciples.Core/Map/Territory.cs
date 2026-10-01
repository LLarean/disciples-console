using System.Linq;
using Disciples.Core.Cities;

namespace Disciples.Core.Map
{
    /// <summary>Land around cities and rods belongs to their owner; where claims overlap, the nearest one wins.</summary>
    public sealed class Territory
    {
        private readonly WorldMap _map;
        private readonly int _capitalRadius;
        private readonly int _cityRadius;
        private readonly int _rodRadius;

        public Territory(WorldMap map, int capitalRadius, int cityRadius, int rodRadius = 0)
        {
            _map = map;
            _capitalRadius = capitalRadius;
            _cityRadius = cityRadius;
            _rodRadius = rodRadius;
        }

        public Owner OwnerAt(Position position)
        {
            return _map.Cities
                .Select(c => (c.Owner, Radius: c.IsCapital ? _capitalRadius : _cityRadius, Distance: DistanceSquared(c.Position, position)))
                .Concat(_map.Rods.Select(r => (r.Owner, Radius: _rodRadius, Distance: DistanceSquared(r.Position, position))))
                .Where(x => x.Distance <= x.Radius * (x.Radius + 1))
                .OrderBy(x => x.Distance)
                .Select(x => x.Owner)
                .FirstOrDefault();
        }

        private static int DistanceSquared(Position a, Position b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            return dx * dx + dy * dy;
        }
    }
}
