using System;
using System.Collections.Generic;
using System.Linq;
using Disciples.Core.Cities;

namespace Disciples.Core.Map
{
    public sealed class WorldMap
    {
        private readonly Terrain[,] _tiles;
        private readonly List<City> _cities;
        private readonly List<NeutralSquad> _neutrals;

        public WorldMap(string name, Terrain[,] tiles, IEnumerable<City> cities, IEnumerable<NeutralSquad>? neutrals = null)
        {
            Name = name;
            _tiles = tiles;
            _cities = cities.ToList();
            _neutrals = neutrals?.ToList() ?? new List<NeutralSquad>();

            var outside = _cities.Select(c => (c.Name, c.Position))
                .Concat(_neutrals.Select(n => (n.Name, n.Position)))
                .FirstOrDefault(o => !Contains(o.Position));
            if (outside.Name != null)
                throw new ArgumentException($"'{outside.Name}' is outside the map.");
        }

        public string Name { get; }
        public int Width => _tiles.GetLength(0);
        public int Height => _tiles.GetLength(1);
        public IReadOnlyList<City> Cities => _cities;
        public IReadOnlyList<NeutralSquad> Neutrals => _neutrals;

        public bool Contains(Position position)
        {
            return position.X >= 0 && position.X < Width && position.Y >= 0 && position.Y < Height;
        }

        public Terrain TerrainAt(Position position) => _tiles[position.X, position.Y];

        public City? CityAt(Position position) => _cities.FirstOrDefault(c => c.Position == position);

        public NeutralSquad? NeutralAt(Position position) => _neutrals.FirstOrDefault(n => n.Position == position);

        internal void RemoveNeutral(NeutralSquad neutral) => _neutrals.Remove(neutral);
    }
}
