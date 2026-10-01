using System;
using System.Collections.Generic;
using System.Linq;
using Disciples.Core.Cities;
using Disciples.Core.Squads;

namespace Disciples.Core.Map
{
    public sealed class WorldMap
    {
        private readonly Terrain[,] _tiles;
        private readonly List<City> _cities;
        private readonly List<NeutralSquad> _neutrals;
        private readonly List<Party> _enemies;
        private readonly List<Site> _sites;

        public WorldMap(string name, Terrain[,] tiles, IEnumerable<City> cities, IEnumerable<NeutralSquad>? neutrals = null, IEnumerable<Party>? enemies = null, IEnumerable<Site>? sites = null)
        {
            Name = name;
            _tiles = tiles;
            _cities = cities.ToList();
            _neutrals = neutrals?.ToList() ?? new List<NeutralSquad>();
            _enemies = enemies?.ToList() ?? new List<Party>();
            _sites = sites?.ToList() ?? new List<Site>();

            var outside = _cities.Select(c => (c.Name, c.Position))
                .Concat(_neutrals.Select(n => (n.Name, n.Position)))
                .Concat(_enemies.Select(e => (e.Name, e.Position)))
                .Concat(_sites.Select(s => (s.Name, s.Position)))
                .FirstOrDefault(o => !Contains(o.Position));
            if (outside.Name != null)
                throw new ArgumentException($"'{outside.Name}' is outside the map.");
        }

        public string Name { get; }
        public int Width => _tiles.GetLength(0);
        public int Height => _tiles.GetLength(1);
        public IReadOnlyList<City> Cities => _cities;
        public IReadOnlyList<NeutralSquad> Neutrals => _neutrals;

        /// <summary>Hostile leaders with their squads, moved by the AI.</summary>
        public IReadOnlyList<Party> Enemies => _enemies;

        public IReadOnlyList<Site> Sites => _sites;

        public bool Contains(Position position)
        {
            return position.X >= 0 && position.X < Width && position.Y >= 0 && position.Y < Height;
        }

        public Terrain TerrainAt(Position position) => _tiles[position.X, position.Y];

        public City? CityAt(Position position) => _cities.FirstOrDefault(c => c.Position == position);

        public NeutralSquad? NeutralAt(Position position) => _neutrals.FirstOrDefault(n => n.Position == position);

        public Party? EnemyAt(Position position) => _enemies.FirstOrDefault(e => e.Position == position);

        public Site? SiteAt(Position position) => _sites.FirstOrDefault(s => s.Position == position);

        internal void RemoveNeutral(NeutralSquad neutral) => _neutrals.Remove(neutral);

        internal void AddEnemy(Party enemy) => _enemies.Add(enemy);

        internal void RemoveEnemy(Party enemy) => _enemies.Remove(enemy);

        internal void RemoveSite(Site site) => _sites.Remove(site);
    }
}
