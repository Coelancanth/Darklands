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

### Reviewer Addendum (2025-09-08)

> Reviewer: Solid foundation. To make this bulletproof and aligned with ADR-004 and our handbook, apply these refinements (reflected in examples below):
> - Replace ad-hoc `Guid.NewGuid()` in domain IDs with an `IStableIdGenerator` (injected) to keep Core deterministic-friendly and testable.
> - Expand compile-time validation: scan properties and nested generic types; flag static mutable fields; flag events/delegates; detect Godot/System.Threading types recursively.
> - Persist deterministic RNG streams alongside states (for integrity checks and lockstep determinism) matching ADR-004.
> - Fix save validation return pattern; fail fast with the Fin result directly.
> - Add architecture tests: prohibit Godot types in Core assemblies and ensure entities are serializable.
> - Prefer GUIDv7/ULID for globally unique IDs in non-deterministic contexts (in Infra), and RNG-derived IDs for in-sim deterministic contexts (via the generator).
> - Define a clear World Hydration/Rehydration process to rebuild Godot scene graph from saved state without leaking Godot into Core.
> - Introduce a mod data extension point on core entities (`ModData`) and a registry to read/write mod payloads safely.
> - Standardize on a pluggable serialization provider; allow Newtonsoft.Json for advanced scenarios (polymorphism, `JsonExtensionData`, converters) while keeping Core free of concrete serializer dependencies.

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
        ImmutableList<StatusEffectInstance> StatusEffects,
        ImmutableDictionary<string, string> ModData
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
        // Deterministic-friendly: obtain IDs from injected generator
        public static ActorId NewId(IStableIdGenerator ids) => new(ids.NewGuid());
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

// Domain Layer - Stable ID generator contract
namespace Darklands.Core.Domain.Identity
{
    /// <summary>
    /// Provides stable ID creation without leaking framework specifics.
    /// Infra can implement via GUIDv7/ULID or deterministic RNG depending on context.
    /// </summary>
    public interface IStableIdGenerator
    {
        Guid NewGuid();
        string NewStringId(int length = 26); // e.g., ULID-like (base32/crockford)
    }
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
        RandomState RandomStates,     // All RNG states
        ImmutableDictionary<string, string> ModData  // Global mod payloads (serializer-agnostic)
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
        // States (PCG state)
        ulong MasterState,
        ulong CombatState,
        ulong LootState,
        ulong EventState,
        // Streams (PCG increment), for integrity checks (see ADR-004)
        ulong MasterStream,
        ulong CombatStream,
        ulong LootStream,
        ulong EventStream
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
            
            // Events are never allowed on persistent entities
            var events = type.GetEvents(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (events.Length > 0)
            {
                throw new InvalidOperationException($"Entity {type.Name} declares events (not save-ready)");
            }
            
            // Fields: reject static mutable and problematic types
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            foreach (var field in fields)
            {
                if (field.IsStatic && !field.IsInitOnly)
                {
                    throw new InvalidOperationException($"Entity {type.Name} has mutable static field {field.Name}");
                }
                if (IsProblematicTypeRecursive(field.FieldType))
                {
                    throw new InvalidOperationException(
                        $"Entity {type.Name} has unsaveable field {field.Name} of type {field.FieldType}");
                }
            }
            
            // Properties: scan types recursively
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var prop in properties)
            {
                if (IsProblematicTypeRecursive(prop.PropertyType))
                {
                    throw new InvalidOperationException(
                        $"Entity {type.Name} has unsaveable property {prop.Name} of type {prop.PropertyType}");
                }
            }
            
            return true;
        }
        
        private static bool IsProblematicTypeRecursive(Type type)
        {
            if (type == null) return false;
            
            if (typeof(Delegate).IsAssignableFrom(type)) return true;
            if (type.Namespace?.StartsWith("Godot") == true) return true;
            if (type.Namespace?.StartsWith("System.Threading") == true) return true;
            
            if (type.IsArray)
            {
                return IsProblematicTypeRecursive(type.GetElementType()!);
            }
            
            if (type.IsGenericType)
            {
                foreach (var arg in type.GetGenericArguments())
                {
                    if (IsProblematicTypeRecursive(arg)) return true;
                }
            }
            
            return false;
        }
    }
}

// Infrastructure Layer - Pluggable serialization provider
namespace Darklands.Core.Infrastructure.Serialization
{
    public interface ISerializationProvider
    {
        string Serialize<T>(T value);
        T? Deserialize<T>(string json);
        object? Deserialize(string json, Type type);
    }

    /// <summary>
    /// Abstracts filesystem access for saves to keep Core free of Godot APIs.
    /// Godot host provides an implementation that maps to OS.GetUserDataDir().
    /// </summary>
    public interface ISaveStorage
    {
        string GetUserDataDir();
        string Combine(params string[] parts);
        bool FileExists(string path);
        void EnsureDirectory(string path);
        void WriteAllText(string path, string contents);
        string ReadAllText(string path);
    }
    
    /// <summary>
    /// Newtonsoft.Json implementation for advanced scenarios (polymorphism, extension data, converters).
    /// </summary>
    public sealed class NewtonsoftSerializationProvider : ISerializationProvider
    {
        private readonly JsonSerializerSettings _settings;
        public NewtonsoftSerializationProvider(JsonSerializerSettings? settings = null)
        {
            _settings = settings ?? new JsonSerializerSettings
            {
                Formatting = Formatting.None,
                TypeNameHandling = TypeNameHandling.None,
                NullValueHandling = NullValueHandling.Ignore
            };
        }
        public string Serialize<T>(T value) => Newtonsoft.Json.JsonConvert.SerializeObject(value, _settings);
        public T? Deserialize<T>(string json) => Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json, _settings);
        public object? Deserialize(string json, Type type) => Newtonsoft.Json.JsonConvert.DeserializeObject(json, type, _settings);
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
        private readonly ISerializationProvider _serializer;  // Pluggable serializer
        private readonly ISaveStorage _storage;
        
        public DebugSaveService(ILogger logger, ISaveStorage storage, ISerializationProvider? serializer = null)
        {
            _logger = logger;
            _storage = storage;
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
            // Default to Newtonsoft for advanced scenarios; DI can supply another
            _serializer = serializer ?? new NewtonsoftSerializationProvider();
        }
        
        public Fin<Unit> QuickSave(GameState state)
        {
            try
            {
                var json = _serializer.Serialize(state);
                var path = _storage.Combine(_storage.GetUserDataDir(), "debug_saves", "quicksave.json");
                _storage.EnsureDirectory(Path.GetDirectoryName(path)!);
                _storage.WriteAllText(path, json);
                
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
                var path = _storage.Combine(_storage.GetUserDataDir(), "debug_saves", "quicksave.json");
                if (!_storage.FileExists(path))
                    return Error.New("No quicksave found");
                
                var json = _storage.ReadAllText(path);
                var state = _serializer.Deserialize<GameState>(json);
                
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
                    return validation;  // Fail fast with validator's Fin result
                
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
            // Scalable migration pipeline (Vn -> Vn+1 sequence)
            var current = save;
            while (current.Version != CurrentSaveVersion)
            {
                var step = _migrations.FirstOrDefault(m => m.FromVersion == current.Version);
                if (step is null)
                    return Error.New($"Missing migration from v{current.Version} -> v{CurrentSaveVersion}");
                var result = step.Apply(current);
                if (result.IsFail) return result.Match(Succ: _ => FinSucc(current), Fail: e => FinFail<SaveContainer>(e));
                current = result.Match(Succ: s => s, Fail: _ => current);
            }
            return current;
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
    ImmutableList<string> ModifierIds,
    ImmutableDictionary<string, string> ModData
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
- [ ] Add `ModData` extension dictionary on extensible entities

### Alpha (When needed)
- [ ] Implement DebugSaveService
- [ ] Add F5/F9 quicksave/load
- [ ] Test save/load regularly
- [ ] Find serialization issues
- [ ] Introduce serialization provider (default Newtonsoft.Json) with settings in DI
- [ ] Implement World Hydration: scene rebuild from state via hydrator/adapters

### Beta (Production)
- [ ] Implement SaveService
- [ ] Add save versioning
- [ ] Add compression
- [ ] Add validation
- [ ] Add save slots UI
- [ ] Add cloud save support
- [ ] Implement migration pipeline with discrete IMigration steps

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
        var instance = CreateTestInstance(type); // Use AutoFixture or builders
        var json = _serializer.Serialize(instance);
        var deserialized = _serializer.Deserialize(json, type);
        
        deserialized.Should().BeEquivalentTo(instance);
    }
}

[Test]
public void GameState_WithCircularReferences_StillSerializes()
{
    var state = CreateGameStateWithCombat();
    
    var json = _serializer.Serialize(state);
    var loaded = _serializer.Deserialize<GameState>(json);
    
    loaded.Should().BeEquivalentTo(state);
}
```

### World Hydration/Rehydration (Critical)

Define a dedicated hydrator that reconstructs the Godot scene graph from pure `GameState` after load. Keep Core/Application free of Godot. The hydrator and adapters live in Infrastructure/Presentation and bind domain IDs to views.

Key components:
- WorldHydrator (Infrastructure): traverses `GameState`, instantiates scenes, binds `ActorId` -> `Node2D` via a registry.
- TransientStateFactory (Application/Infra): creates `ITransientState` instances without polluting domain entities.
- Presenter/Adapters (Presentation): apply domain data to views; never reference Godot from Core.

Pitfalls:
- Never couple hydrator into save/deserialization; it is a separate step post-load.
- Never mutate domain records to attach transient state; use separate registries/factories.
- Ensure stable ordering when iterating collections during hydration (see ADR-004).

## References

- [Game Programming Patterns - Game Loop](https://gameprogrammingpatterns.com/game-loop.html)
- [Save System Design](https://www.gamedeveloper.com/design/save-system-design)
- [Binary vs JSON Serialization](https://stackoverflow.com/questions/4143421/binary-vs-json-serialization)
- Battle Brothers saves use binary format with versioning