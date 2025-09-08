# ADR-005: Save-Ready Architecture

**Status**: Accepted  
**Date**: 2025-09-08  
**Author**: Tech Lead  
**Deciders**: Tech Lead, Dev Engineer  

## Context

Battle Brothers-scale tactical games require robust save/load systems for:
- **Player Progress**: Campaign state, character development, inventory
- **Mid-Battle Saves**: Complex tactical battles can take 30+ minutes  
- **Iron Man Mode**: Single save slot with no save-scumming
- **Version Migration**: Updates shouldn't break existing saves
- **Mod Compatibility**: Modded saves need special handling

Retrofitting save functionality to an existing codebase is extremely expensive. Every domain entity, every service, and every system must be designed with serialization in mind from the beginning.

### The Save System Evolution Problem

```csharp
// Early Development (Month 1) - Simple, but unsaveable
public class Actor : Node2D  // Godot node can't serialize
{
    public Actor Target;  // Circular reference
    public Action<int> OnDamage;  // Delegates can't save
    private static int _nextId = 1;  // Non-deterministic IDs
}

// Late Development (Month 12) - Trying to add saves = rewrite everything
// Too late! Entire codebase assumes the wrong patterns
```

## Decision

We will implement a **Save-Ready Architecture** with three stages:

1. **NOW: Save-Conscious Development** - Write all code to be serializable
2. **ALPHA: Debug Saves** - Simple JSON saves for development
3. **BETA: Production Saves** - Versioned, validated, compressed saves

### Stage 1: Save-Conscious Development (Implement Now)

```csharp
// Domain Layer - All entities must be save-ready
namespace Darklands.Core.Domain.Actors
{
    /// <summary>
    /// Core design principles for save-ready entities:
    /// 1. Use value types and records (immutable, serializable)
    /// 2. Reference by ID, not object references
    /// 3. No framework types (Godot nodes, Unity objects)
    /// 4. No delegates or events in domain
    /// 5. Separate persistent vs transient state
    /// </summary>
    public sealed record Actor(
        ActorId Id,                    // Stable, deterministic ID
        string DefinitionId,           // Reference to data definition
        string Name,
        Health Health,
        Position Position,
        ActorId? TargetId,            // ID reference, not object reference
        TurnTime NextTurn,
        ImmutableList<StatusEffectInstance> StatusEffects
    ) : IPersistentEntity
    {
        // Transient state is kept separate
        [JsonIgnore]
        public ITransientState? TransientState { get; init; }
    }
    
    /// <summary>
    /// Stable ID that survives saves and network transfer
    /// </summary>
    public readonly record struct ActorId(Guid Value) : IEntityId
    {
        public static ActorId NewId() => new(Guid.NewGuid());
        public static ActorId Empty => new(Guid.Empty);
        public override string ToString() => Value.ToString("N")[..8];
    }
    
    /// <summary>
    /// Marker interface for persistent entities
    /// </summary>
    public interface IPersistentEntity
    {
        IEntityId Id { get; }
    }
    
    /// <summary>
    /// Transient state that doesn't save (animations, cache, etc)
    /// </summary>
    public interface ITransientState
    {
        void Reset();
    }
    
    /// <summary>
    /// Example of properly structured status effect
    /// </summary>
    public sealed record StatusEffectInstance(
        string EffectId,               // Reference to definition
        int RemainingTurns,
        int StackCount,
        ActorId SourceId,             // Who applied it (ID reference)
        ulong AppliedAtTurn           // When it was applied
    );
}

// Domain Layer - Game state root
namespace Darklands.Core.Domain.GameState
{
    /// <summary>
    /// Root aggregate for entire game state.
    /// This is what gets saved/loaded.
    /// </summary>
    public sealed record GameState(
        GameSessionId SessionId,
        ulong Seed,                   // For deterministic random
        ulong CurrentTurn,
        GamePhase Phase,
        ImmutableDictionary<ActorId, Actor> Actors,
        ImmutableDictionary<GridId, GridState> Grids,
        ImmutableDictionary<string, CampaignVariable> Variables,
        InventoryState Inventory,
        QuestState Quests,
        RandomState RandomStates      // All RNG states
    ) : IPersistentEntity
    {
        public IEntityId Id => SessionId;
        
        /// <summary>
        /// Create save-safe deep copy
        /// </summary>
        public GameState CreateSaveSnapshot()
        {
            // Records give us immutability for free
            // This is already a safe snapshot!
            return this;
        }
    }
    
    /// <summary>
    /// Random generator states for save/load
    /// </summary>
    public sealed record RandomState(
        ulong MasterState,
        ulong CombatState,
        ulong LootState,
        ulong EventState
    );
}

// Infrastructure Layer - Serialization helpers
namespace Darklands.Core.Infrastructure.Serialization
{
    /// <summary>
    /// Ensures entities are save-ready at compile time
    /// </summary>
    public static class SaveReadyValidator
    {
        public static bool ValidateEntity<T>() where T : IPersistentEntity
        {
            var type = typeof(T);
            
            // Check for problematic types
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                if (IsProblematicType(field.FieldType))
                {
                    throw new InvalidOperationException(
                        $"Entity {type.Name} has unsaveable field {field.Name} of type {field.FieldType}");
                }
            }
            
            return true;
        }
        
        private static bool IsProblematicType(Type type)
        {
            // Delegates/Events can't serialize
            if (typeof(Delegate).IsAssignableFrom(type))
                return true;
            
            // Godot types can't serialize
            if (type.Namespace?.StartsWith("Godot") == true)
                return true;
            
            // Thread/Task types indicate wrong patterns
            if (type.Namespace?.StartsWith("System.Threading") == true)
                return true;
            
            return false;
        }
    }
}
```

### Stage 2: Debug Saves (Implement at Alpha)

```csharp
// Simple debug save system for development
namespace Darklands.Core.Infrastructure.Saves
{
    public sealed class DebugSaveService
    {
        private readonly ILogger _logger;
        private readonly JsonSerializerOptions _options;
        
        public DebugSaveService(ILogger logger)
        {
            _logger = logger;
            _options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = 
                {
                    new ActorIdJsonConverter(),
                    new FixedJsonConverter(),
                    new ImmutableDictionaryJsonConverter()
                }
            };
        }
        
        public Fin<Unit> QuickSave(GameState state)
        {
            try
            {
                var json = JsonSerializer.Serialize(state, _options);
                var path = Path.Combine(OS.GetUserDataDir(), "debug_saves", "quicksave.json");
                
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, json);
                
                _logger.Information("Quick saved to {Path} ({Size} bytes)", path, json.Length);
                return unit;
            }
            catch (Exception ex)
            {
                return Error.New($"Save failed: {ex.Message}");
            }
        }
        
        public Fin<GameState> QuickLoad()
        {
            try
            {
                var path = Path.Combine(OS.GetUserDataDir(), "debug_saves", "quicksave.json");
                if (!File.Exists(path))
                    return Error.New("No quicksave found");
                
                var json = File.ReadAllText(path);
                var state = JsonSerializer.Deserialize<GameState>(json, _options);
                
                if (state == null)
                    return Error.New("Failed to deserialize save");
                
                _logger.Information("Quick loaded from {Path}", path);
                return state;
            }
            catch (Exception ex)
            {
                return Error.New($"Load failed: {ex.Message}");
            }
        }
    }
}
```

### Stage 3: Production Saves (Implement at Beta)

```csharp
// Production-ready save system with versioning
namespace Darklands.Core.Infrastructure.Saves
{
    public sealed class SaveService : ISaveService
    {
        private const int CurrentSaveVersion = 1;
        private readonly ILogger _logger;
        private readonly ICompression _compression;
        private readonly ISaveValidator _validator;
        
        public async Task<Fin<Unit>> SaveGame(SaveSlot slot, GameState state)
        {
            try
            {
                // Create versioned save container
                var saveData = new SaveContainer
                {
                    Version = CurrentSaveVersion,
                    Timestamp = DateTimeOffset.UtcNow,
                    GameVersion = Application.Version,
                    ModIds = GetActiveModIds(),
                    
                    // Compress large data
                    CompressedState = await _compression.CompressAsync(state),
                    
                    // Metadata for save slot display
                    Metadata = new SaveMetadata
                    {
                        Turn = state.CurrentTurn,
                        Phase = state.Phase,
                        ActorCount = state.Actors.Count,
                        PlayTime = CalculatePlayTime(state),
                        Screenshot = await CaptureScreenshot()
                    },
                    
                    // Integrity check
                    Checksum = CalculateChecksum(state)
                };
                
                // Validate before writing
                var validation = _validator.ValidateSave(saveData);
                if (validation.IsFail)
                    return validation.Match(Succ: _ => unit, Fail: e => FinFail<Unit>(e));
                
                // Write atomically (temp file -> rename)
                var path = GetSavePath(slot);
                var tempPath = path + ".tmp";
                
                await File.WriteAllBytesAsync(tempPath, SerializeSave(saveData));
                File.Move(tempPath, path, true);
                
                _logger.Information("Saved game to slot {Slot} (v{Version})", slot, CurrentSaveVersion);
                return unit;
            }
            catch (Exception ex)
            {
                return Error.New($"Save failed: {ex.Message}");
            }
        }
        
        public async Task<Fin<GameState>> LoadGame(SaveSlot slot)
        {
            try
            {
                var path = GetSavePath(slot);
                if (!File.Exists(path))
                    return Error.New($"Save slot {slot} is empty");
                
                var bytes = await File.ReadAllBytesAsync(path);
                var saveData = DeserializeSave(bytes);
                
                // Validate checksum
                if (!_validator.ValidateChecksum(saveData))
                    return Error.New("Save file is corrupted");
                
                // Handle version migration
                if (saveData.Version != CurrentSaveVersion)
                {
                    var migrated = await MigrateSave(saveData);
                    if (migrated.IsFail)
                        return migrated;
                    saveData = migrated.Match(Succ: s => s, Fail: _ => saveData);
                }
                
                // Decompress state
                var state = await _compression.DecompressAsync<GameState>(saveData.CompressedState);
                
                // Restore transient state
                RestoreTransientState(state);
                
                _logger.Information("Loaded game from slot {Slot} (v{Version})", slot, saveData.Version);
                return state;
            }
            catch (Exception ex)
            {
                return Error.New($"Load failed: {ex.Message}");
            }
        }
        
        private async Task<Fin<SaveContainer>> MigrateSave(SaveContainer save)
        {
            return save.Version switch
            {
                0 => MigrateV0ToV1(save),
                _ => Error.New($"Unknown save version: {save.Version}")
            };
        }
    }
}
```

### Save-Ready Patterns to Follow

```csharp
// ✅ GOOD: Save-ready entity
public sealed record Item(
    ItemId Id,
    string DefinitionId,
    int StackCount,
    ItemRarity Rarity,
    ImmutableList<string> ModifierIds
);

// ❌ BAD: Unsaveable entity
public class Item
{
    public Texture2D Icon;  // Godot type!
    public Item Container;  // Circular ref!
    public event Action<Item> OnUsed;  // Event!
    private static int _counter;  // Static state!
}

// ✅ GOOD: Reference by ID
public sealed record Quest(
    QuestId Id,
    ActorId GiverId,  // ID reference
    LocationId TargetLocationId  // ID reference
);

// ❌ BAD: Direct object references
public class Quest
{
    public Actor Giver;  // Object reference!
    public Location TargetLocation;  // Object reference!
}

// ✅ GOOD: Immutable collections
public sealed record Inventory(
    ImmutableDictionary<ItemId, Item> Items,
    ImmutableList<ItemId> QuickSlots
);

// ❌ BAD: Mutable collections
public class Inventory
{
    public Dictionary<ItemId, Item> Items;  // Mutable!
    public List<ItemId> QuickSlots;  // Mutable!
}
```

## Consequences

### Positive

1. **No Retrofit Cost**: Save system can be added incrementally
2. **Clean Testing**: Domain entities serialize to JSON for tests
3. **Network Ready**: Same serialization works for multiplayer
4. **Mod Friendly**: JSON saves are moddable
5. **Version Migration**: Can update game without breaking saves
6. **Debugging**: Can inspect save files as JSON

### Negative

1. **Design Constraints**: Can't use certain C# features (events, delegates)
2. **ID Management**: Must track IDs instead of references
3. **Immutability Overhead**: More allocations with immutable collections
4. **No Godot Integration**: Can't save Godot nodes directly

## Implementation Checklist

### Immediate (Every entity from now on)
- [ ] Use records for domain entities
- [ ] Reference by ID, not object
- [ ] No Godot types in domain
- [ ] No events/delegates in domain
- [ ] Immutable collections
- [ ] Stable ID generation

### Alpha (When needed)
- [ ] Implement DebugSaveService
- [ ] Add F5/F9 quicksave/load
- [ ] Test save/load regularly
- [ ] Find serialization issues

### Beta (Production)
- [ ] Implement SaveService
- [ ] Add save versioning
- [ ] Add compression
- [ ] Add validation
- [ ] Add save slots UI
- [ ] Add cloud save support

## Testing Strategy

```csharp
[Test]
public void AllDomainEntities_AreSerializable()
{
    var domainAssembly = typeof(Actor).Assembly;
    var entityTypes = domainAssembly.GetTypes()
        .Where(t => typeof(IPersistentEntity).IsAssignableFrom(t))
        .Where(t => !t.IsInterface && !t.IsAbstract);
    
    foreach (var type in entityTypes)
    {
        var instance = CreateTestInstance(type);
        var json = JsonSerializer.Serialize(instance);
        var deserialized = JsonSerializer.Deserialize(json, type);
        
        deserialized.Should().BeEquivalentTo(instance);
    }
}

[Test]
public void GameState_WithCircularReferences_StillSerializes()
{
    var state = CreateGameStateWithCombat();
    
    var json = JsonSerializer.Serialize(state);
    var loaded = JsonSerializer.Deserialize<GameState>(json);
    
    loaded.Should().BeEquivalentTo(state);
}
```

## References

- [Game Programming Patterns - Game Loop](https://gameprogrammingpatterns.com/game-loop.html)
- [Save System Design](https://www.gamedeveloper.com/design/save-system-design)
- [Binary vs JSON Serialization](https://stackoverflow.com/questions/4143421/binary-vs-json-serialization)
- Battle Brothers saves use binary format with versioning