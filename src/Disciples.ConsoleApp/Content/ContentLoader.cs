using System.Text.Json;
using Disciples.Core.Map;
using Disciples.Core.Session;
using Disciples.Core.Squads;

namespace Disciples.ConsoleApp.Content;

public sealed class ContentLoader(string contentRoot)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public GameSession LoadSession(string mapId)
    {
        var terrains = Read<List<TerrainData>>("terrains.json")
            .ToDictionary(t => t.Id, t => new Terrain(t.Id, t.Name, t.MoveCost));
        var mapData = Read<MapData>(Path.Combine("maps", mapId + ".json"));

        var map = new WorldMap(mapData.Name, BuildTiles(mapData, terrains), mapData.Cities.Select(ToCity));
        var leader = new Leader(mapData.Leader.Name, new Position(mapData.Leader.X, mapData.Leader.Y), mapData.Leader.MovementPoints);
        return new GameSession(map, leader);
    }

    private static Terrain[,] BuildTiles(MapData data, IReadOnlyDictionary<string, Terrain> terrains)
    {
        var height = data.Rows.Count;
        var width = data.Rows[0].Length;
        var tiles = new Terrain[width, height];

        for (var y = 0; y < height; y++)
        {
            var row = data.Rows[y];
            if (row.Length != width)
                throw new InvalidDataException($"Map '{data.Name}': row {y} has length {row.Length}, expected {width}.");

            for (var x = 0; x < width; x++)
            {
                if (!data.Legend.TryGetValue(row[x].ToString(), out var terrainId) || !terrains.TryGetValue(terrainId, out var terrain))
                    throw new InvalidDataException($"Map '{data.Name}': unknown tile '{row[x]}' at ({x}, {y}).");
                tiles[x, y] = terrain;
            }
        }

        return tiles;
    }

    private static City ToCity(CityData data) => new(data.Name, new Position(data.X, data.Y), data.Capital);

    private T Read<T>(string relativePath)
    {
        var json = File.ReadAllText(Path.Combine(contentRoot, relativePath));
        return JsonSerializer.Deserialize<T>(json, JsonOptions)
               ?? throw new InvalidDataException($"Empty content file '{relativePath}'.");
    }

    private sealed record TerrainData(string Id, string Name, int? MoveCost);

    private sealed record MapData(string Name, Dictionary<string, string> Legend, List<string> Rows, List<CityData> Cities, LeaderData Leader);

    private sealed record CityData(string Name, int X, int Y, bool Capital);

    private sealed record LeaderData(string Name, int X, int Y, int MovementPoints);
}
