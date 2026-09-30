using System.Linq;
using Disciples.Core.Cities;

namespace Disciples.Core.Map
{
    /// <summary>Land around cities belongs to their owner; where claims overlap, the nearest city wins.</summary>
    public sealed class Territory
    {
        private readonly WorldMap _map;
        private readonly int _capitalRadius;
        private readonly int _cityRadius;

        public Territory(WorldMap map, int capitalRadius, int cityRadius)
        {
            _map = map;
            _capitalRadius = capitalRadius;
            _cityRadius = cityRadius;
        }

        public Owner OwnerAt(Position position)
        {
            var claim = _map.Cities
                .Select(c => (City: c, Distance: DistanceSquared(c.Position, position)))
                .Where(x => Covers(x.City, x.Distance))
                .OrderBy(x => x.Distance)
                .FirstOrDefault();
            return claim.City?.Owner ?? Owner.Neutral;
        }

        private bool Covers(City city, int distanceSquared)
        {
            var radius = city.IsCapital ? _capitalRadius : _cityRadius;
            return distanceSquared <= radius * (radius + 1);
        }

        private static int DistanceSquared(Position a, Position b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            return dx * dx + dy * dy;
        }
    }
}
