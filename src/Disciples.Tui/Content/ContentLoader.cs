using System.Text.Json;
using System.Text.Json.Serialization;
using Disciples.Core.Cities;
using Disciples.Core.Content;
using Disciples.Core.Items;
using Disciples.Core.Map;
using Disciples.Core.Persistence;
using Disciples.Core.Units;

namespace Disciples.Tui.Content;

public sealed class ContentLoader(string contentRoot)
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public GameContent LoadContent()
    {
        var rules = Read<GameRules>("rules.json");
        var terrains = Read<List<TerrainData>>("terrains.json").Select(t => new Terrain(t.Id, t.Name, t.MoveCost, t.Symbol));
        var units = Read<List<UnitData>>("units.json").Select(u => ToDefinition(u, rules));
        var buildings = Read<List<BuildingData>>("buildings.json")
            .Select(b => new Building(b.Id, b.Name, b.Branch, b.Cost, b.Description, b.Requires, b.HealBonusPercent));
        var items = Read<List<ItemData>>("items.json")
            .Select(i => new ItemDefinition(i.Id, i.Name, i.Kind, i.Cost, i.Heal, new StatBonus(i.Armor, i.PowerPercent, i.Initiative, i.Accuracy)));
        return new GameContent(units, terrains, buildings, rules, Read<ContentAliases>("aliases.json"), items);
    }

    public GameSnapshot LoadScenario(string id) => Read<GameSnapshot>(Path.Combine("maps", id + ".json"));

    private static UnitDefinition ToDefinition(UnitData d, GameRules rules) =>
        new(d.Id, d.Name, d.Hp, d.Armor, d.Initiative, d.Power, d.Accuracy, d.Attack, d.Size, d.Cost, d.Leadership,
            d.ExperienceToLevel ?? 100, d.ExperienceValue, d.LevelGrowthPercent ?? rules.LevelGrowthPercent,
            d.UpgradesTo, d.UpgradeBuilding, d.Movement, d.Source ?? AttackSource.Weapon, d.Immunities, d.Wards, d.Effect ?? AttackEffect.None, d.Guardian);

    private T Read<T>(string relativePath)
    {
        var json = File.ReadAllText(Path.Combine(contentRoot, relativePath));
        return JsonSerializer.Deserialize<T>(json, JsonOptions)
               ?? throw new ContentException($"Empty content file '{relativePath}'.");
    }

    private sealed record TerrainData(string Id, string Name, int? MoveCost, char? Symbol);

    private sealed record UnitData(
        string Id, string Name, int Hp, int Armor, int Initiative, int Power, int Accuracy,
        AttackType Attack, UnitSize Size, int Cost, int Leadership, int Movement,
        int? ExperienceToLevel, int ExperienceValue, int? LevelGrowthPercent, string? UpgradesTo, string? UpgradeBuilding,
        AttackSource? Source, List<AttackSource>? Immunities, List<AttackSource>? Wards, AttackEffect? Effect, bool Guardian);

    private sealed record ItemData(string Id, string Name, ItemKind Kind, int Cost, int Heal, int Armor, int PowerPercent, int Initiative, int Accuracy);

    private sealed record BuildingData(string Id, string Name, string Branch, int Cost, string Description, string? Requires, int HealBonusPercent);
}
