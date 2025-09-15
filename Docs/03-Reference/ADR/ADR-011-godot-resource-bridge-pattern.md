# ADR-011: Godot Resource Bridge Pattern

**Status**: Proposed
**Date**: 2025-09-08
**Author**: Tech Lead
**Deciders**: Tech Lead, Dev Engineer
**Updated**: 2025-09-16 - Updated file paths for ADR-021 project separation  

## Context

Darklands needs a data-driven approach for defining game content (actors, items, abilities, levels) that allows:
- Designers to modify game balance without recompiling
- Easy content creation and iteration
- Resource hot-reloading during development
- Type-safe access from C# code

Godot's Resource system (.tres/.tscn files) provides excellent built-in support for this, but poses an architectural challenge: How do we leverage Godot Resources without violating Clean Architecture principles where Domain and Application layers must remain framework-agnostic?

### The Architectural Challenge

```
Domain Layer (Pure C#)          ← Must NOT know about Godot
    ↑
Application Layer (CQRS)        ← Must NOT know about Godot  
    ↑
Infrastructure Layer            ← CAN know about both Godot AND Domain
    ↑
Presentation Layer (Godot)      ← Full Godot integration
```

## Decision

We will implement a **Resource Bridge Pattern** where:

1. **Godot Resources** are used as external data sources for game content
2. **Infrastructure Layer** acts as an Anti-Corruption Layer (ACL) that bridges between Godot Resources and Domain models
3. **Domain Layer** defines interfaces for the data it needs, unaware of storage implementation
4. **Resource definitions** are loaded, validated, and cached at the Infrastructure boundary

### Core Pattern

```csharp
// Domain Layer - Pure C#, no Godot knowledge
namespace Darklands.Domain.Definitions
{
    public interface IActorDefinitionRepository
    {
        Fin<ActorDefinition> GetById(string definitionId);
        Fin<IEnumerable<ActorDefinition>> GetAllByType(ActorType type);
        Fin<ActorDefinition> GetRandom(ActorType type);
    }

    public sealed record ActorDefinition(
        string Id,
        string Name,
        Health BaseHealth,
        Speed BaseSpeed,
        Attack BaseAttack,
        Defense BaseDefense,
        IReadOnlyList<AbilityId> Abilities
    );
}

// Infrastructure Layer - Bridges Godot to Domain
namespace Darklands.Core.Infrastructure.Resources
{
    public sealed class GodotActorDefinitionRepository : IActorDefinitionRepository
    {
        private readonly IResourceLoader _resourceLoader;
        private readonly IDefinitionCache<ActorDefinition> _cache;
        private readonly ILogger _logger;

        public Fin<ActorDefinition> GetById(string definitionId)
        {
            return _cache.GetOrLoad(definitionId, () => LoadAndConvert(definitionId));
        }

        private Fin<ActorDefinition> LoadAndConvert(string definitionId)
        {
            var resourcePath = $"res://Resources/Definitions/Actors/{definitionId}.tres";
            
            return _resourceLoader.Load<ActorDefinitionResource>(resourcePath)
                .Bind(ValidateResource)
                .Map(ConvertToDomainModel);
        }

        private Fin<ActorDefinitionResource> ValidateResource(ActorDefinitionResource resource)
        {
            if (string.IsNullOrEmpty(resource.DefinitionId))
                return Error.New("Actor definition missing ID");
            
            if (resource.MaxHealth <= 0)
                return Error.New($"Invalid health value: {resource.MaxHealth}");
            
            return resource;
        }

        private ActorDefinition ConvertToDomainModel(ActorDefinitionResource resource)
        {
            // Convert Godot Resource to pure domain model
            var health = Health.Create(resource.MaxHealth, resource.MaxHealth)
                .Match(h => h, _ => Health.Default);
            
            var speed = Speed.Create(resource.Speed)
                .Match(s => s, _ => Speed.Default);

            return new ActorDefinition(
                resource.DefinitionId,
                resource.ActorName,
                health,
                speed,
                Attack.Create(resource.AttackPower),
                Defense.Create(resource.DefenseRating),
                resource.Abilities?.Select(a => new AbilityId(a)).ToList() ?? new List<AbilityId>()
            );
        }
    }
}

// Godot Resource Definition - Lives with Godot project
namespace Darklands.Resources
{
    [Tool]
    public partial class ActorDefinitionResource : Resource
    {
        [Export] public string DefinitionId { get; set; } = "";
        [Export] public string ActorName { get; set; } = "Unnamed";
        [Export] public int MaxHealth { get; set; } = 100;
        [Export] public int Speed { get; set; } = 5;
        [Export] public int AttackPower { get; set; } = 10;
        [Export] public int DefenseRating { get; set; } = 5;
        [Export] public string[] Abilities { get; set; } = Array.Empty<string>();
        
        // Visual/Godot-specific properties
        [Export] public Texture2D Sprite { get; set; }
        [Export] public Color TintColor { get; set; } = Colors.White;
        [Export] public AudioStream AttackSound { get; set; }
    }
}
```

### Resource Loading Abstraction

```csharp
// Infrastructure abstraction for testability
public interface IResourceLoader
{
    Fin<T> Load<T>(string path) where T : Resource;
    Fin<bool> Exists(string path);
    IObservable<ResourceChanged> WatchForChanges(string path); // For hot-reload
}

public sealed class GodotResourceLoader : IResourceLoader
{
    public Fin<T> Load<T>(string path) where T : Resource
    {
        try
        {
            if (!ResourceLoader.Exists(path))
                return Error.New($"Resource not found: {path}");
            
            var resource = GD.Load<T>(path);
            if (resource == null)
                return Error.New($"Failed to load resource as {typeof(T).Name}: {path}");
            
            return resource;
        }
        catch (Exception ex)
        {
            return Error.New($"Resource loading error: {ex.Message}");
        }
    }

    public Fin<bool> Exists(string path)
    {
        try { return ResourceLoader.Exists(path); }
        catch (Exception ex) { return Error.New($"Exists failed: {ex.Message}"); }
    }

    public IObservable<ResourceChanged> WatchForChanges(string path)
    {
        // Development-only simplistic watcher: polls ResourceLoader.Exists and version tag.
        // In production builds, return an empty observable to avoid overhead.
        return Observable.Empty<ResourceChanged>();
    }
}
```

### Caching Strategy

```csharp
public interface IDefinitionCache<T> where T : class
{
    Fin<T> GetOrLoad(string key, Func<Fin<T>> loader);
    void Invalidate(string key);
    void Clear();
}

public sealed class DefinitionCache<T> : IDefinitionCache<T> where T : class
{
    private readonly Dictionary<string, T> _cache = new();
    private readonly bool _enableHotReload;
    
    public Fin<T> GetOrLoad(string key, Func<Fin<T>> loader)
    {
        // In development, always reload for hot-reload support
        if (_enableHotReload)
        {
            var result = loader();
            if (result.IsSucc)
            {
                result.IfSucc(value => _cache[key] = value);
            }
            return result;
        }
        
        // In production, cache indefinitely
        if (_cache.TryGetValue(key, out var cached))
            return cached;
        
        var loaded = loader();
        loaded.IfSucc(value => _cache[key] = value);
        return loaded;
    }
}
```

## Implementation Guidelines

### 1. File Organization (Updated for ADR-021 Project Separation)

```
darklands/
├── src/
│   ├── Darklands.Domain/
│   │   └── Definitions/
│   │       ├── IActorDefinitionRepository.cs
│   │       ├── ActorDefinition.cs
│   │       └── ItemDefinition.cs
│   │
│   ├── Darklands.Core/
│   │   ├── Application/
│   │   │   └── Commands/
│   │   │       └── CreateActorFromDefinitionCommand.cs
│   │   └── Infrastructure/
│   │       └── Resources/
│   │           ├── GodotActorDefinitionRepository.cs
│   │           ├── GodotItemDefinitionRepository.cs
│   │           ├── GodotResourceLoader.cs
│   │           └── DefinitionCache.cs
│   │
│   └── Darklands.Presentation/
│       └── Presenters/
│           └── ActorPresenter.cs           # Uses definition repository
│
├── Resources/                              # Godot project root
│   ├── Definitions/
│   │   ├── Actors/
│   │   │   ├── player.tres
│   │   │   ├── goblin.tres
│   │   │   └── combat_dummy.tres
│   │   └── Items/
│   │       ├── sword.tres
│   │       └── health_potion.tres
│   └── ResourceTypes/
│       ├── ActorDefinitionResource.cs
│       └── ItemDefinitionResource.cs
│
└── tests/
    └── Darklands.Core.Tests/
        └── Infrastructure/
            └── Resources/
                └── ResourceBridgeTests.cs
```

### 2. Usage in Application Layer

```csharp
public sealed class CreateActorFromDefinitionCommandHandler : ICommandHandler<CreateActorFromDefinitionCommand>
{
    private readonly IActorDefinitionRepository _definitions;
    private readonly IActorStateService _actorState;
    
    public async Task<Fin<Unit>> Handle(CreateActorFromDefinitionCommand command, CancellationToken ct)
    {
        // Application layer uses repository interface, unaware of Godot
        return await _definitions.GetById(command.DefinitionId)
            .BindAsync(async definition => 
            {
                var actor = Actor.CreateFromDefinition(
                    ActorId.NewId(),
                    definition,
                    command.Position
                );
                
                return await _actorState.AddActor(actor);
            });
    }
}
```

### 3. Dependency Injection Setup (Updated for Project Separation)

```csharp
// In Darklands.Presentation/ServiceConfiguration.cs
public static class ServiceConfiguration
{
    public static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Resource loading infrastructure
        services.AddSingleton<IResourceLoader, GodotResourceLoader>();
        services.AddSingleton<IDefinitionCache<ActorDefinition>, DefinitionCache<ActorDefinition>>();

        // Repository implementations
        services.AddSingleton<IActorDefinitionRepository, GodotActorDefinitionRepository>();
        services.AddSingleton<IItemDefinitionRepository, GodotItemDefinitionRepository>();

        // Presenters that use repositories
        services.AddScoped<IActorPresenter, ActorPresenter>();

        return services.BuildServiceProvider();
    }
}
```

## Consequences

### Positive

1. **Clean Architecture Preserved**: Domain remains pure C# with no framework dependencies
2. **Data-Driven Design**: Game balance and content easily tweakable via .tres files
3. **Designer-Friendly**: Godot's inspector for editing resources
4. **Type Safety**: Compile-time checking for resource properties
5. **Hot-Reload Support**: Resources can reload during development
6. **Testability**: Domain/Application tests mock repository interfaces
7. **Performance**: Caching prevents repeated disk I/O
8. **Validation**: Resources validated before domain conversion
9. **Extensible**: Easy to add new definition types

### Negative

1. **Additional Layer**: Infrastructure bridge adds complexity
2. **Dual Maintenance**: Both Resource classes and Domain models need updates
3. **Runtime Errors**: Missing or invalid resources only caught at runtime
4. **Memory Usage**: Caching all definitions could be significant for large games
5. **Learning Curve**: Developers must understand the bridge pattern

## Alternatives Considered

### 1. JSON/YAML Files
- **Pros**: Framework-agnostic, human-readable
- **Cons**: No Godot inspector, manual parsing, no type safety
- **Rejected**: Loses Godot's excellent tooling

### 2. Direct Godot Resource Usage in Domain
- **Pros**: Simpler, no conversion needed
- **Cons**: Violates Clean Architecture, couples domain to Godot
- **Rejected**: Architectural integrity more important

### 3. Code-Only Definitions
- **Pros**: Type-safe, no I/O, simple
- **Cons**: Requires recompilation for balance changes
- **Rejected**: Designers can't iterate quickly

### 4. ScriptableObject Pattern (Unity-style)
- **Pros**: Similar to Unity developers' experience
- **Cons**: Godot Resources are already this pattern
- **Rejected**: Unnecessary abstraction

## Migration Strategy

For existing hardcoded data (like TD_013's test actors):

1. Create .tres files for existing actor types
2. Implement repository and cache infrastructure
3. Update IActorFactory to use definition repository
4. Remove hardcoded values from code
5. Test hot-reload in development

## Example Resource File

```gdresource
[gd_resource type="Resource" script_class="ActorDefinitionResource" load_steps=3 format=3]

[ext_resource type="Texture2D" path="res://sprites/goblin.png" id="1"]
[ext_resource type="AudioStream" path="res://sounds/goblin_attack.wav" id="2"]

[resource]
script = ExtResource("res://Resources/ResourceTypes/ActorDefinitionResource.cs")
DefinitionId = "goblin_warrior"
ActorName = "Goblin Warrior"
MaxHealth = 30
Speed = 6
AttackPower = 8
DefenseRating = 3
Abilities = ["quick_strike", "dodge"]
Sprite = ExtResource("1")
TintColor = Color(0.8, 1.0, 0.8, 1.0)
AttackSound = ExtResource("2")
```

## Testing Strategy

```csharp
// Domain tests - mock repository
[Fact]
public async Task CreateActor_WithDefinition_UsesDefinitionValues()
{
    var mockRepo = new Mock<IActorDefinitionRepository>();
    mockRepo.Setup(r => r.GetById("test_actor"))
        .Returns(Fin<ActorDefinition>.Succ(TestDefinitions.StandardActor));
    
    // Test uses mock, no Godot dependency
}

// Infrastructure tests - use test resources
[Fact] 
public void GodotRepository_LoadsResource_ConvertsCorrectly()
{
    var loader = new TestResourceLoader(); // Returns test data
    var repo = new GodotActorDefinitionRepository(loader, cache, logger);
    
    var result = repo.GetById("test_actor");
    
    result.IsSucc.Should().BeTrue();
    result.IfSucc(def => def.Name.Should().Be("Test Actor"));
}
```

## Security Considerations

- Resource paths should be validated to prevent directory traversal
- Resources from untrusted sources (mods) need sandboxing
- Cache size limits to prevent memory exhaustion

## References

- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html) - Robert C. Martin
- [Anti-Corruption Layer Pattern](https://docs.microsoft.com/en-us/azure/architecture/patterns/anti-corruption-layer) - Microsoft
- [Repository Pattern](https://martinfowler.com/eaaCatalog/repository.html) - Martin Fowler
- [Godot Resources Documentation](https://docs.godotengine.org/en/stable/tutorials/scripting/resources.html)