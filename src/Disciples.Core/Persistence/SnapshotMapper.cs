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
                Parties = session.Parties.Select(CaptureParty).ToList(),
                Active = session.Parties.ToList().IndexOf(session.Party),
                Cities = session.Map.Cities.Select(CaptureCity).ToList(),
                Neutrals = session.Map.Neutrals.Select(n => new NeutralSnapshot
                {
                    Name = n.Name,
                    X = n.Position.X,
                    Y = n.Position.Y,
                    Reward = n.Reward,
                    Units = CaptureSquad(n.Squad)
                }).ToList(),
                Enemies = session.Map.Enemies.Select(CaptureParty).ToList(),
                Sites = session.Map.Sites.Select(s => new SiteSnapshot
                {
                    Name = s.Name,
                    Kind = s.Kind,
                    X = s.Position.X,
                    Y = s.Position.Y,
                    Gold = s.Gold,
                    Owner = s.Owner,
                    Mercenaries = s.Mercenaries.Select(m => m.Id).ToList()
                }).ToList(),
                Explored = session.Fog.ToRows()
            };
        }

        public static GameSession Restore(GameSnapshot snapshot, GameContent content, IRandom random)
        {
            SnapshotMigrator.Default.Upgrade(snapshot);

            var cities = snapshot.Cities.Select(c => RestoreCity(c, content));
            var neutrals = snapshot.Neutrals.Select(n => new NeutralSquad(
                n.Name, new Position(n.X, n.Y), RestoreSquad(n.Units, new Squad(), content, n.Name), n.Reward));
            var enemies = snapshot.Enemies.Select(e => RestoreParty(e, content, "Enemy"));
            var sites = snapshot.Sites.Select(s => new Site(
                s.Name, s.Kind, new Position(s.X, s.Y), s.Gold, s.Mercenaries.Select(content.Unit), s.Owner));
            var map = new WorldMap(snapshot.Map.Name, RestoreTiles(snapshot.Map, content), cities, neutrals, enemies, sites);
            if (snapshot.Active < 0 || snapshot.Active >= snapshot.Parties.Count)
                throw new ContentException("The game has no active player party.");

            var parties = snapshot.Parties.Select(p => RestoreParty(p, content, "Party"));
            var fog = FogOfWar.FromRows(map.Width, map.Height, snapshot.Explored);

            return new GameSession(content, map, parties, snapshot.Gold, random, snapshot.Turn, fog, snapshot.Active);
        }

        private static PartySnapshot CaptureParty(Party party) => new PartySnapshot
        {
            X = party.Position.X,
            Y = party.Position.Y,
            MovementPoints = party.MovementPoints,
            Units = CaptureSquad(party.Squad),
            Perks = party.Perks.ToList(),
            Items = party.Items.Select(i => i.Id).ToList()
        };

        private static Party RestoreParty(PartySnapshot data, GameContent content, string owner)
        {
            var squad = RestoreSquad(data.Units, new Squad(), content, owner);
            if (squad.Leader == null)
                throw new ContentException($"'{owner}' has no leader.");

            return new Party(squad, new Position(data.X, data.Y), data.MovementPoints, data.Perks, content.Rules, data.Items.Select(content.Item));
        }

        /// <summary>Replaces the leader of the starting party of a new game with the chosen leader class.</summary>
        public static void ChooseLeader(GameSnapshot snapshot, UnitDefinition leader, GameContent content)
        {
            SnapshotMigrator.Default.Upgrade(snapshot);

            var current = snapshot.Parties.FirstOrDefault()?.Units.FirstOrDefault(u => content.Unit(u.Id).IsLeader)
                          ?? throw new ContentException("The party has no leader.");
            current.Id = leader.Id;
        }

        private static MapSnapshot CaptureMap(WorldMap map)
        {
            var symbols = new Dictionary<string, char>();
            var rows = new List<string>();

            for (var y = 0; y < map.Height; y++)
            {
                var row = new StringBuilder(map.Width);
                for (var x = 0; x < map.Width; x++)
                    row.Append(SymbolFor(map.TerrainAt(new Position(x, y)), symbols));
                rows.Add(row.ToString());
            }

            return new MapSnapshot
            {
                Name = map.Name,
                Legend = symbols.ToDictionary(s => s.Value.ToString(), s => s.Key),
                Rows = rows
            };
        }

        /// <summary>Uses the terrain's own symbol or the first letter of its id when free, so saved maps stay readable.</summary>
        private static char SymbolFor(Terrain terrain, Dictionary<string, char> symbols)
        {
            if (symbols.TryGetValue(terrain.Id, out var symbol))
                return symbol;

            var candidates = new[] { terrain.Symbol ?? terrain.Id[0], char.ToLowerInvariant(terrain.Id[0]), char.ToUpperInvariant(terrain.Id[0]) }
                .Concat(SpareSymbols);
            symbol = candidates.First(c => !symbols.ContainsValue(c));
            symbols.Add(terrain.Id, symbol);
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
                data.Built.Select(content.BuildingId));
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
