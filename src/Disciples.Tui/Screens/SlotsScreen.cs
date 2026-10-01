using System.Text.Json;
using Disciples.Core.Content;
using Disciples.Core.Session;
using Disciples.Tui.Persistence;
using Disciples.Tui.Widgets;

namespace Disciples.Tui.Screens;

/// <summary>Save slot picker: saves the given session, or loads a game when there is none.</summary>
public sealed class SlotsScreen(GameFlow flow, GameSession? session) : MenuScreen
{
    private bool IsSaving => session != null;

    protected override IReadOnlyList<string> Heading => [IsSaving ? "Save game" : "Load game", "choose a slot"];

    protected override IReadOnlyList<MenuItem> Items =>
        flow.Saves.List().Select(slot => new MenuItem(
            $"Slot {slot.Slot}",
            () => Choose(slot),
            IsSaving || slot.SavedAt != null,
            slot.SavedAt is { } savedAt ? $"{savedAt:yyyy-MM-dd HH:mm}  {slot.Summary}" : slot.Summary)).ToList();

    protected override void Back() => Shell.Pop();

    private void Choose(SaveSlotInfo slot)
    {
        if (IsSaving && !slot.IsEmpty)
            Shell.Push(new ConfirmScreen($"Overwrite slot {slot.Slot}?", slot.Summary, () => Use(slot)));
        else
            Use(slot);
    }

    private void Use(SaveSlotInfo slot)
    {
        try
        {
            if (session != null)
            {
                flow.SaveGame(session, slot.Slot);
                Show($"Saved to slot {slot.Slot}.", Palette.Good);
            }
            else
            {
                flow.LoadGame(slot.Slot);
            }
        }
        catch (Exception e) when (e is JsonException or ContentException or IOException or UnauthorizedAccessException)
        {
            Show(e.Message, Palette.Bad);
        }
    }
}
