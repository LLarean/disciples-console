using System.Collections.Generic;
using System.Linq;
using System.Text;
using Disciples.Core.Cities;
using Disciples.Core.Content;
using Disciples.Core.Map;
using Disciples.Core.Session;
using Disciples.Core.Squads;
using Disciples.Core.Units;

namespace Disciples.Core.Persistence
{
    public static class SnapshotMapper
    {
        private const string SpareSymbols = "0123456789abcdefghijklmnopqrstuvwxyz";

        public static GameSnapshot Capture(GameSession session)
        {
            return new GameSnapshot
            {
                Map = CaptureMap(session.Map),
                Turn = session.Turn,
                Gold = session.Gold,
                Party = CaptureParty(session.Party),
                Cities = session.Map.Cities.Select(CaptureCity).ToList(),
                Neutrals = session.Map.Neutrals.Select(n => new NeutralSnapshot
                {
                    Name = n.Name,
                    X = n.Position.X,
                    Y = n.Position.Y,
                    Reward = n.Reward,
                    Units = CaptureSquad(n.Squad)
                }).ToList(),
                Enemies = session.Map.Enemies.Select(CaptureParty).ToList()
            };
        }

        public static GameSession Restore(GameSnapshot snapshot, GameContent content, IRandom random)
        {
            if (snapshot.Version != GameSnapshot.CurrentVersion)
                throw new ContentException($"Unsupported save version {snapshot.Version}.");

            var cities = snapshot.Cities.Select(c => RestoreCity(c, content));
            var neutrals = snapshot.Neutrals.Select(n => new NeutralSquad(
                n.Name, new Position(n.X, n.Y), RestoreSquad(n.Units, new Squad(), content, n.Name), n.Reward));
            var enemies = snapshot.Enemies.Select(e => RestoreParty(e, content, "Enemy"));
            var map = new WorldMap(snapshot.Map.Name, RestoreTiles(snapshot.Map, content), cities, neutrals, enemies);
            var party = RestoreParty(snapshot.Party, content, "Party");

            return new GameSession(content, map, party, snapshot.Gold, random, snapshot.Turn);
        }

        private static PartySnapshot CaptureParty(Party party) => new PartySnapshot
        {
            X = party.Position.X,
            Y = party.Position.Y,
            MaxMovementPoints = party.MaxMovementPoints,
            MovementPoints = party.MovementPoints,
            Units = CaptureSquad(party.Squad)
        };

        private static Party RestoreParty(PartySnapshot data, GameContent content, string owner)
        {
            var leader = data.Units.Select(u => content.Unit(u.Id)).FirstOrDefault(d => d.IsLeader)
                         ?? throw new ContentException($"'{owner}' has no leader.");
            var squad = RestoreSquad(data.Units, new Squad(leader.Leadership), content, owner);
            return new Party(squad, new Position(data.X, data.Y), data.MaxMovementPoints, data.MovementPoints);
        }

        private static MapSnapshot CaptureMap(WorldMap map)
        {
            var symbols = new Dictionary<string, char>();
            var rows = new List<string>();

            for (var y = 0; y < map.Height; y++)
            {
                var row = new StringBuilder(map.Width);
                for (var x = 0; x < map.Width; x++)
                    row.Append(SymbolFor(map.TerrainAt(new Position(x, y)).Id, symbols));
                rows.Add(row.ToString());
            }

            return new MapSnapshot
            {
                Name = map.Name,
                Legend = symbols.ToDictionary(s => s.Value.ToString(), s => s.Key),
                Rows = rows
            };
        }

        /// <summary>Uses the first letter of the terrain id when free, so saved maps stay readable.</summary>
        private static char SymbolFor(string terrainId, Dictionary<string, char> symbols)
        {
            if (symbols.TryGetValue(terrainId, out var symbol))
                return symbol;

            var candidates = new[] { char.ToLowerInvariant(terrainId[0]), char.ToUpperInvariant(terrainId[0]) }.Concat(SpareSymbols);
            symbol = candidates.First(c => !symbols.ContainsValue(c));
            symbols.Add(terrainId, symbol);
            return symbol;
        }

        private static Terrain[,] RestoreTiles(MapSnapshot data, GameContent content)
        {
            if (data.Rows.Count == 0)
                throw new ContentException($"Map '{data.Name}' has no rows.");

            var height = data.Rows.Count;
            var width = data.Rows[0].Length;
            var tiles = new Terrain[width, height];

            for (var y = 0; y < height; y++)
            {
                var row = data.Rows[y];
                if (row.Length != width)
                    throw new ContentException($"Map '{data.Name}': row {y} has length {row.Length}, expected {width}.");

                for (var x = 0; x < width; x++)
                {
                    if (!data.Legend.TryGetValue(row[x].ToString(), out var terrainId))
                        throw new ContentException($"Map '{data.Name}': unknown tile '{row[x]}' at ({x}, {y}).");
                    tiles[x, y] = content.Terrain(terrainId);
                }
            }

            return tiles;
        }

        private static CitySnapshot CaptureCity(City city) => new CitySnapshot
        {
            Name = city.Name,
            X = city.Position.X,
            Y = city.Position.Y,
            Capital = city.IsCapital,
            Owner = city.Owner,
            Income = city.Income,
            Recruits = city.Recruits.Select(r => r.Id).ToList(),
            Buildings = city.Buildings.Count > 0,
            Built = city.BuiltIds.ToList(),
            Garrison = CaptureSquad(city.Garrison)
        };

        private static City RestoreCity(CitySnapshot data, GameContent content)
        {
            var city = new City(
                data.Name,
                new Position(data.X, data.Y),
                data.Capital,
                data.Income,
                data.Recruits.Select(content.Unit),
                data.Buildings ? content.Buildings : null,
                data.Owner,
                data.Built);
            RestoreSquad(data.Garrison, city.Garrison, content, data.Name);
            return city;
        }

        private static List<UnitSnapshot> CaptureSquad(Squad squad) =>
            squad.Units.Select(u =>
            {
                var slot = squad.SlotOf(u);
                return new UnitSnapshot
                {
                    Id = u.Definition.Id,
                    Line = slot.Line,
                    Column = slot.Column,
                    Level = u.Level,
                    Experience = u.Experience,
                    Hp = u.Hp == u.MaxHp ? (int?)null : u.Hp
                };
            }).ToList();

        private static Squad RestoreSquad(IEnumerable<UnitSnapshot> units, Squad squad, GameContent content, string owner)
        {
            foreach (var data in units)
            {
                var unit = new Unit(content.Unit(data.Id), data.Level, data.Experience, data.Hp);
                if (!squad.TryPlace(unit, new SquadSlot(data.Line, data.Column)))
                    throw new ContentException($"'{owner}': cannot place {data.Id} at {data.Line} {data.Column}.");
            }

            return squad;
        }
    }
}
