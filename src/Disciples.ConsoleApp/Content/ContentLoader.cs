using System.Text.Json;
using System.Text.Json.Serialization;
using Disciples.Core;
using Disciples.Core.Cities;
using Disciples.Core.Map;
using Disciples.Core.Session;
using Disciples.Core.Squads;
using Disciples.Core.Units;

namespace Disciples.ConsoleApp.Content;

public sealed class ContentLoader(string contentRoot)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public GameSession LoadSession(string mapId)
    {
        var terrains = Read<List<TerrainData>>("terrains.json")
            .ToDictionary(t => t.Id, t => new Terrain(t.Id, t.Name, t.MoveCost));
        var units = Read<List<UnitData>>("units.json")
            .ToDictionary(u => u.Id, ToDefinition);
        var buildings = Read<List<BuildingData>>("buildings.json");
        var mapData = Read<MapData>(Path.Combine("maps", mapId + ".json"));

        var cities = mapData.Cities.Select(c => ToCity(c, units, buildings));
        var neutrals = mapData.Neutrals.Select(n => ToNeutral(n, units));
        var map = new WorldMap(mapData.Name, BuildTiles(mapData, terrains), cities, neutrals);

        var partyData = mapData.Party;
        var party = new Party(new Unit(Find(units, partyData.Leader)), new Position(partyData.X, partyData.Y), partyData.MovementPoints);
        return new GameSession(map, party, mapData.Gold, new SystemRandom());
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

    private static UnitDefinition ToDefinition(UnitData d) =>
        new(d.Id, d.Name, d.Hp, d.Armor, d.Initiative, d.Power, d.Accuracy, d.Attack, d.Size, d.Cost, d.Leadership);

    private static City ToCity(CityData data, IReadOnlyDictionary<string, UnitDefinition> units, IEnumerable<BuildingData> buildings)
    {
        var cityBuildings = data.Buildings
            ? buildings.Select(b => new Building(b.Id, b.Name, b.Branch, b.Cost, b.Description, b.Requires))
            : null;
        var city = new City(data.Name, new Position(data.X, data.Y), data.Capital, data.Income,
            data.Recruits.Select(id => Find(units, id)), cityBuildings);
        Fill(city.Garrison, data.Garrison, units, data.Name);
        return city;
    }

    private static NeutralSquad ToNeutral(NeutralData data, IReadOnlyDictionary<string, UnitDefinition> units)
    {
        var squad = new Squad();
        Fill(squad, data.Units, units, data.Name);
        return new NeutralSquad(data.Name, new Position(data.X, data.Y), squad, data.Reward);
    }

    private static void Fill(Squad squad, IEnumerable<SlotData> slots, IReadOnlyDictionary<string, UnitDefinition> units, string owner)
    {
        foreach (var slot in slots)
        {
            if (!squad.TryPlace(new Unit(Find(units, slot.Id)), new SquadSlot(slot.Line, slot.Column)))
                throw new InvalidDataException($"'{owner}': cannot place {slot.Id} at {slot.Line} {slot.Column}.");
        }
    }

    private static UnitDefinition Find(IReadOnlyDictionary<string, UnitDefinition> units, string id) =>
        units.TryGetValue(id, out var unit) ? unit : throw new InvalidDataException($"Unknown unit '{id}'.");

    private T Read<T>(string relativePath)
    {
        var json = File.ReadAllText(Path.Combine(contentRoot, relativePath));
        return JsonSerializer.Deserialize<T>(json, JsonOptions)
               ?? throw new InvalidDataException($"Empty content file '{relativePath}'.");
    }

    private sealed record TerrainData(string Id, string Name, int? MoveCost);

    private sealed record UnitData(
        string Id, string Name, int Hp, int Armor, int Initiative, int Power, int Accuracy,
        AttackType Attack, UnitSize Size, int Cost, int Leadership);

    private sealed record BuildingData(string Id, string Name, string Branch, int Cost, string Description, string? Requires);

    private sealed record SlotData(string Id, SquadLine Line, int Column);

    private sealed record CityData(
        string Name, int X, int Y, bool Capital, int Income, List<string> Recruits, bool Buildings, List<SlotData>? Garrison)
    {
        public List<SlotData> Garrison { get; init; } = Garrison ?? [];
    }

    private sealed record NeutralData(string Name, int X, int Y, int Reward, List<SlotData> Units);

    private sealed record PartyData(string Leader, int X, int Y, int MovementPoints);

    private sealed record MapData(
        string Name, int Gold, Dictionary<string, string> Legend, List<string> Rows,
        List<CityData> Cities, List<NeutralData> Neutrals, PartyData Party);
}
