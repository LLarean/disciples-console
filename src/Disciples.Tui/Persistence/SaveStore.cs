using System.Text.Json;
using Disciples.Core.Content;
using Disciples.Core.Persistence;
using Disciples.Tui.Content;

namespace Disciples.Tui.Persistence;

public sealed record SaveSlotInfo(int Slot, DateTime? SavedAt, string Summary, bool IsReadable)
{
    public bool IsEmpty => SavedAt == null && IsReadable;
}

/// <summary>Numbered JSON save slots in a directory.</summary>
public sealed class SaveStore(string directory)
{
    public const int SlotCount = 3;

    public static string DefaultDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DisciplesConsole", "saves");

    public IReadOnlyList<SaveSlotInfo> List() => Enumerable.Range(1, SlotCount).Select(Describe).ToList();

    public bool HasAny() => List().Any(s => s.SavedAt != null);

    public void Save(int slot, GameSnapshot game)
    {
        Directory.CreateDirectory(directory);
        var json = JsonSerializer.Serialize(new SaveFile(DateTime.Now, game), ContentLoader.JsonOptions);
        var path = PathOf(slot);
        var temp = path + ".tmp";
        File.WriteAllText(temp, json);
        File.Move(temp, path, overwrite: true);
    }

    public GameSnapshot Load(int slot) => Read(slot).Game;

    private SaveSlotInfo Describe(int slot)
    {
        if (!File.Exists(PathOf(slot)))
            return new SaveSlotInfo(slot, null, "empty", true);

        try
        {
            var save = Read(slot);
            return new SaveSlotInfo(slot, save.SavedAt, $"{save.Game.Map.Name}, turn {save.Game.Turn}, {save.Game.Gold} gold", true);
        }
        catch (Exception e) when (e is JsonException or ContentException or IOException)
        {
            return new SaveSlotInfo(slot, null, "unreadable", false);
        }
    }

    private SaveFile Read(int slot)
    {
        var json = File.ReadAllText(PathOf(slot));
        return JsonSerializer.Deserialize<SaveFile>(json, ContentLoader.JsonOptions)
               ?? throw new ContentException($"Save slot {slot} is empty.");
    }

    private string PathOf(int slot) => Path.Combine(directory, $"slot{slot}.json");

    private sealed record SaveFile(DateTime SavedAt, GameSnapshot Game);
}
