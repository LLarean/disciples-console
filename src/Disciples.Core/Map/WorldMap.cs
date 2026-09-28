using System;
using System.Collections.Generic;
using System.Linq;

namespace Disciples.Core.Map
{
    public sealed class WorldMap
    {
        private readonly Terrain[,] _tiles;
        private readonly List<City> _cities;

        public WorldMap(string name, Terrain[,] tiles, IEnumerable<City> cities)
        {
            Name = name;
            _tiles = tiles;
            _cities = cities.ToList();

            var outside = _cities.FirstOrDefault(c => !Contains(c.Position));
            if (outside != null)
                throw new ArgumentException($"City '{outside.Name}' is outside the map.", nameof(cities));
        }

        public string Name { get; }
        public int Width => _tiles.GetLength(0);
        public int Height => _tiles.GetLength(1);
        public IReadOnlyList<City> Cities => _cities;

        public bool Contains(Position position)
        {
            return position.X >= 0 && position.X < Width && position.Y >= 0 && position.Y < Height;
        }

        public Terrain TerrainAt(Position position) => _tiles[position.X, position.Y];

        public City? CityAt(Position position) => _cities.FirstOrDefault(c => c.Position == position);
    }
}
