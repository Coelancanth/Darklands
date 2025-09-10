using System.Collections.Immutable;
using Darklands.Core.Domain.Common;

namespace Darklands.Core.Domain.SaveSystem;

/// <summary>
/// Versioned container for save data with metadata and integrity checks.
/// This is the top-level structure written to save files.
/// </summary>
public sealed record SaveContainer(
    int Version,
    DateTimeOffset Timestamp,
    string GameVersion,
    SaveMetadata Metadata,
    byte[] CompressedState,
    string Checksum,
    ImmutableHashSet<string> ModIds
) : IPersistentEntity
{
    /// <summary>
    /// Current save format version.
    /// Increment when making breaking changes to save structure.
    /// </summary>
    public const int CurrentVersion = 1;

    /// <summary>
    /// Unique identifier for this save container.
    /// </summary>
    public SaveContainerId Id { get; init; } = SaveContainerId.NewId();

    /// <summary>
    /// IPersistentEntity implementation.
    /// </summary>
    IEntityId IPersistentEntity.Id => Id;

    /// <summary>
    /// Creates a new save container with current timestamp and version.
    /// </summary>
    /// <param name="gameVersion">Current game version</param>
    /// <param name="metadata">Save metadata for display</param>
    /// <param name="compressedState">Compressed game state data</param>
    /// <param name="checksum">Data integrity checksum</param>
    /// <param name="modIds">Active mod identifiers</param>
    /// <returns>New save container</returns>
    public static SaveContainer Create(
        string gameVersion,
        SaveMetadata metadata,
        byte[] compressedState,
        string checksum,
        ImmutableHashSet<string>? modIds = null)
    {
        return new SaveContainer(
            CurrentVersion,
            DateTimeOffset.UtcNow,
            gameVersion,
            metadata,
            compressedState,
            checksum,
            modIds ?? ImmutableHashSet<string>.Empty
        );
    }
}

/// <summary>
/// Unique identifier for save containers.
/// </summary>
public readonly record struct SaveContainerId(Guid Value) : IEntityId
{
    public static SaveContainerId NewId() => new(Guid.NewGuid());
    public static SaveContainerId Empty => new(Guid.Empty);
    public bool IsEmpty => Value == Guid.Empty;
    public override string ToString() => Value.ToString("N")[..8];
}

/// <summary>
/// Display metadata for save slots without loading full save data.
/// Used for save/load menu display.
/// </summary>
public sealed record SaveMetadata(
    ulong CurrentTurn,
    GamePhase Phase,
    int ActorCount,
    TimeSpan PlayTime,
    string CampaignName,
    string? ScreenshotBase64
);

/// <summary>
/// Available save slots for the save/load system.
/// </summary>
public enum SaveSlot
{
    /// <summary>F5 quicksave slot</summary>
    QuickSave,

    /// <summary>Automatic save triggered by game events</summary>
    AutoSave,

    /// <summary>Manual save slot 1</summary>
    Manual1,

    /// <summary>Manual save slot 2</summary>
    Manual2,

    /// <summary>Manual save slot 3</summary>
    Manual3,

    /// <summary>Manual save slot 4</summary>
    Manual4,

    /// <summary>Manual save slot 5</summary>
    Manual5,

    /// <summary>Iron Man mode single save</summary>
    IronMan,

    /// <summary>Debug/development save</summary>
    Debug
}

/// <summary>
/// Current phase of the game for display purposes.
/// </summary>
public enum GamePhase
{
    MainMenu,
    CampaignMap,
    TacticalCombat,
    CharacterSheet,
    Inventory,
    Settings,
    Cutscene
}

/// <summary>
/// Extensions for SaveSlot enum.
/// </summary>
public static class SaveSlotExtensions
{
    /// <summary>
    /// Gets the filename for a save slot.
    /// </summary>
    /// <param name="slot">Save slot</param>
    /// <returns>Filename without extension</returns>
    public static string GetFileName(this SaveSlot slot)
    {
        return slot switch
        {
            SaveSlot.QuickSave => "quicksave",
            SaveSlot.AutoSave => "autosave",
            SaveSlot.Manual1 => "save_001",
            SaveSlot.Manual2 => "save_002",
            SaveSlot.Manual3 => "save_003",
            SaveSlot.Manual4 => "save_004",
            SaveSlot.Manual5 => "save_005",
            SaveSlot.IronMan => "ironman",
            SaveSlot.Debug => "debug",
            _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, null)
        };
    }

    /// <summary>
    /// Gets display name for UI.
    /// </summary>
    /// <param name="slot">Save slot</param>
    /// <returns>Human-readable name</returns>
    public static string GetDisplayName(this SaveSlot slot)
    {
        return slot switch
        {
            SaveSlot.QuickSave => "Quick Save (F5)",
            SaveSlot.AutoSave => "Auto Save",
            SaveSlot.Manual1 => "Save Slot 1",
            SaveSlot.Manual2 => "Save Slot 2",
            SaveSlot.Manual3 => "Save Slot 3",
            SaveSlot.Manual4 => "Save Slot 4",
            SaveSlot.Manual5 => "Save Slot 5",
            SaveSlot.IronMan => "Iron Man",
            SaveSlot.Debug => "Debug Save",
            _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, null)
        };
    }

    /// <summary>
    /// Checks if this slot supports overwrite operations.
    /// </summary>
    /// <param name="slot">Save slot to check</param>
    /// <returns>True if slot can be overwritten</returns>
    public static bool CanOverwrite(this SaveSlot slot)
    {
        return slot switch
        {
            SaveSlot.QuickSave => true,
            SaveSlot.AutoSave => true,
            SaveSlot.Manual1 => true,
            SaveSlot.Manual2 => true,
            SaveSlot.Manual3 => true,
            SaveSlot.Manual4 => true,
            SaveSlot.Manual5 => true,
            SaveSlot.IronMan => true,
            SaveSlot.Debug => true,
            _ => false
        };
    }
}
