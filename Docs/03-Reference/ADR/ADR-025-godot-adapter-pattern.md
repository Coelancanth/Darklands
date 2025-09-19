# ADR-025: Godot Adapter Pattern for Clean Architecture Boundaries

**Status**: Accepted
**Date**: 2025-09-19
**Decision Makers**: Tech Lead
**Tags**: `architecture` `adapter-pattern` `clean-architecture` `godot-integration` `foundational`

## Context

Clean Architecture requires that business logic layers (Domain, Application) have NO knowledge of external frameworks like Godot. This is fundamental to maintaining testability, portability, and separation of concerns. However, we need to leverage Godot's powerful features (nodes, signals, resources, scene tree) for our game implementation.

This creates a fundamental impedance mismatch:
- **Business Logic**: Pure C#, framework-agnostic, testable without Godot
- **Godot Engine**: Node-based, scene tree lifecycle, requires inheriting from Godot types

Throughout our codebase, multiple ADRs independently discovered the same solution: the Adapter Pattern. This pattern has proven so fundamental that it appears in:
- ADR-010 (UI Event Bus) - Adapting MediatR events to Godot nodes
- ADR-011 (Resource Bridge) - Adapting Godot resources to domain models
- ADR-018 (DI Lifecycle) - Adapting MS.DI to scene tree lifecycle
- ADR-024 (GameLoop) - Adapting frame updates to game logic

This ADR formalizes the Adapter Pattern as our standard approach for Godot integration.

## Decision

We establish the **Adapter Pattern** as the canonical approach for bridging Clean Architecture boundaries with Godot features. Any integration with Godot MUST follow this pattern to maintain architectural integrity.

### Core Principle

**"Godot at the edges, pure C# at the core"**

For any Godot feature that business logic needs:
1. **Define an interface** in Application/Domain layer (pure C#)
2. **Implement business logic** in Application/Domain layer (no Godot)
3. **Create thin adapter** in Godot project that translates between worlds

### Pattern Structure

```
┌─────────────────────────────────────────────────────┐
│                  Godot Project                      │
│  ┌───────────────────────────────────────────────┐ │
│  │         XxxAdapter : GodotNode                │ │  ← Thin adapter layer
│  │  - Inherits from Node/Node2D/Control/etc     │ │  ← ONLY place with Godot types
│  │  - Contains NO business logic                │ │  ← Just translation/delegation
│  │  - Converts Godot types ↔ Pure C# types     │ │  ← Type mapping only
│  │  - Handles thread marshalling (CallDeferred) │ │  ← Godot-specific concerns
│  └────────────────────┬──────────────────────────┘ │
└──────────────────────┼──────────────────────────────┘
                       │ Delegates to
                       ↓
┌─────────────────────────────────────────────────────┐
│           Application/Domain Layer                  │
│  ┌───────────────────────────────────────────────┐ │
│  │      XxxService/Coordinator/Handler           │ │  ← Pure business logic
│  │  - Implements IXxx interface                  │ │  ← Contract-based
│  │  - Zero Godot dependencies                    │ │  ← No "using Godot;"
│  │  - Fully unit testable                        │ │  ← Tests run without engine
│  │  - Uses only C# types and abstractions        │ │  ← Portable code
│  └───────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────┘
```

### Standard Adapter Implementations

| Godot Feature | Interface (Pure C#) | Implementation (Pure C#) | Adapter (Godot) | Purpose |
|--------------|-------------------|------------------------|----------------|----------|
| Frame Updates | `IGameClock` | `GameLoopCoordinator` | `GameLoop : Node` | Game timing |
| Scene Events | `IUIEventBus` | `UIEventBus` | `UIDispatcher : Node` | Event routing |
| Resources | `IResourceLoader` | `ResourceCache` | `GodotResourceLoader` | Asset loading |
| DI Scopes | `IScopeManager` | `ScopeManager` | `GodotScopeManager` | Lifecycle mgmt |
| Audio | `IAudioService` | `AudioManager` | `GodotAudioPlayer : Node2D` | Sound playback |
| Input | `IInputService` | `InputProcessor` | `GodotInputAdapter : Node` | Input handling |
| Saves | `ISaveService` | `SaveManager` | `GodotFileAccess` | File I/O |
| Settings | `ISettingsService` | `SettingsManager` | `GodotProjectSettings` | Configuration |

### Implementation Rules

#### 1. Keep Adapters Thin (< 50 lines typical)
```csharp
// ✅ GOOD: Thin adapter, just delegation
public partial class GameLoop : Node
{
    private IGameLoopCoordinator? _coordinator;

    public override void _Ready()
    {
        _coordinator = this.GetService<IGameLoopCoordinator>();
    }

    public override void _Process(double delta)
    {
        _coordinator?.ProcessTick((float)delta); // Just delegate
    }
}

// ❌ BAD: Business logic in adapter
public partial class GameLoop : Node
{
    public override void _Process(double delta)
    {
        // ❌ NO! Business logic doesn't belong here!
        if (_actors.Any(a => a.Health <= 0))
        {
            EndCombat();
        }
    }
}
```

#### 2. Type Conversion at Boundary
```csharp
// Adapter converts Godot types to pure C# types
public partial class InputAdapter : Node
{
    private IInputService? _inputService;

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            // Convert Godot type to pure C# type
            var position = new CoreVector2(
                mouseEvent.Position.X,
                mouseEvent.Position.Y
            );
            _inputService?.HandleClick(position);
        }
    }
}
```

#### 3. Service Locator Pattern (Necessary Evil)
```csharp
// Service locator ONLY in adapters at the boundary
public override void _Ready()
{
    // Godot can't use constructor injection, so service locator is acceptable here
    _presenter = this.GetService<IMyPresenter>();
    _presenter.AttachView(this);
}
```

#### 4. Thread Safety via CallDeferred
```csharp
public class UIDispatcher : Node
{
    public void DispatchToMainThread(Action action)
    {
        // Godot UI must update on main thread
        CallDeferred(nameof(ExecuteOnMainThread), action);
    }

    private void ExecuteOnMainThread(Action action)
    {
        action.Invoke();
    }
}
```

### Testing Strategy

#### Pure Implementations (Unit Tests)
```csharp
// Can test without Godot running
[Fact]
public void GameLoopCoordinator_ProcessesTick_AdvancesTime()
{
    var coordinator = new GameLoopCoordinator(mockScheduler, mockMovement);
    coordinator.ProcessTick(0.02f);

    Assert.Equal(TimeUnit.Create(1), coordinator.CurrentTime);
}
```

#### Adapters (Integration Tests)
```csharp
// Requires Godot context
[GodotTest]
public async Task GameLoop_CallsCoordinator_OnProcess()
{
    var scene = GD.Load<PackedScene>("res://Tests/GameLoopTest.tscn");
    var instance = scene.Instantiate<GameLoop>();

    // Verify delegation happens
    await instance.WaitForProcess();
    mockCoordinator.Verify(c => c.ProcessTick(It.IsAny<float>()), Times.AtLeastOnce);
}
```

## Consequences

### Positive
- ✅ **Pure Business Logic**: Core game logic has zero Godot dependencies
- ✅ **100% Unit Testable**: Business logic tests run in milliseconds without engine
- ✅ **Engine Portability**: Could theoretically port to Unity/MonoGame by replacing adapters
- ✅ **Clear Boundaries**: Obvious where Godot-specific code lives
- ✅ **Consistent Pattern**: Same approach for all Godot integrations
- ✅ **Maintainability**: Changes to Godot API only affect adapters

### Negative
- ❌ **Additional Layer**: Extra abstraction between engine and logic
- ❌ **Code Duplication**: Interfaces + implementations + adapters
- ❌ **Service Locator**: Anti-pattern required at Godot boundaries
- ❌ **Type Conversion Overhead**: Converting between Godot and C# types
- ❌ **Learning Curve**: Developers must understand the pattern

### Neutral
- ➖ More files to manage (but better organized)
- ➖ Indirection can make debugging slightly harder
- ➖ Not all Godot features need adapters (per ADR-006)

## Examples

### Example 1: Frame Update Adapter (from ADR-024)
```csharp
// GodotIntegration/GameLoop.cs (Godot adapter)
public partial class GameLoop : Node
{
    private IGameLoopCoordinator? _coordinator;

    public override void _Ready()
    {
        _coordinator = this.GetService<IGameLoopCoordinator>();
    }

    public override void _Process(double delta)
    {
        _coordinator?.ProcessTick((float)delta);
    }
}

// Application/GameLoopCoordinator.cs (Pure logic)
public class GameLoopCoordinator : IGameLoopCoordinator
{
    public void ProcessTick(float deltaSeconds)
    {
        // Pure C# game logic, no Godot knowledge
        AdvanceTime(deltaSeconds);
        ProcessScheduledActors();
        UpdateMovements();
    }
}
```

### Example 2: Event Bus Adapter (from ADR-010)
```csharp
// Presentation/UIDispatcher.cs (Godot adapter)
public partial class UIDispatcher : Node
{
    private readonly Queue<Action> _eventQueue = new();

    public void QueueEvent(Action action)
    {
        lock (_eventQueue)
        {
            _eventQueue.Enqueue(action);
        }
        CallDeferred(nameof(ProcessEvents));
    }

    private void ProcessEvents()
    {
        // Process on main thread
        while (_eventQueue.TryDequeue(out var action))
        {
            action();
        }
    }
}
```

### Example 3: Resource Loading Adapter (from ADR-011)
```csharp
// Infrastructure/GodotResourceLoader.cs (Godot adapter)
public class GodotResourceLoader : IResourceLoader
{
    public Fin<T> Load<T>(string path) where T : class
    {
        try
        {
            // Godot-specific loading
            var resource = GD.Load(path);
            if (resource == null)
                return Error.New($"Resource not found: {path}");

            // Convert to domain model
            var domainModel = ConvertToDomain<T>(resource);
            return Fin<T>.Succ(domainModel);
        }
        catch (Exception ex)
        {
            return Error.New($"Failed to load resource: {ex.Message}");
        }
    }
}
```

## Implementation Checklist

When creating a new Godot integration:

- [ ] Define interface in Application/Domain layer
- [ ] Implement business logic with no Godot dependencies
- [ ] Create adapter class inheriting from appropriate Godot node
- [ ] Keep adapter under 50 lines (just delegation)
- [ ] Handle type conversion at the boundary
- [ ] Use CallDeferred for thread safety if needed
- [ ] Write unit tests for pure implementation
- [ ] Write integration tests for adapter (if critical)
- [ ] Document the adapter in relevant ADR

## Ideal Project Organization

### Recommended Folder Structure for Clean Architecture + Godot

```
YourGame.sln
│
├── src/                                # Pure C# Business Logic (NO Godot dependencies)
│   ├── YourGame.Domain/                # Core business rules and entities
│   │   ├── Combat/
│   │   │   ├── Damage.cs               # Value object
│   │   │   ├── AttackResult.cs         # Value object
│   │   │   └── CombatCalculator.cs     # Pure domain logic
│   │   ├── Actors/
│   │   │   ├── Actor.cs                # Entity (as record for save-ready)
│   │   │   ├── ActorId.cs              # Value object
│   │   │   ├── Health.cs               # Value object
│   │   │   └── Stats.cs                # Value object
│   │   ├── World/
│   │   │   ├── Position.cs             # Value object
│   │   │   ├── Tile.cs                 # Entity
│   │   │   └── Grid.cs                 # Aggregate root
│   │   └── Common/
│   │       ├── TimeUnit.cs             # Universal time currency
│   │       ├── IDeterministicRandom.cs # Abstraction for deterministic RNG
│   │       └── Result.cs               # Fin<T> for error handling
│   │
│   ├── YourGame.Application/           # Use cases and application services
│   │   ├── Combat/
│   │   │   ├── Commands/
│   │   │   │   ├── ExecuteAttackCommand.cs
│   │   │   │   └── ExecuteAttackCommandHandler.cs
│   │   │   ├── Coordination/
│   │   │   │   └── GameLoopCoordinator.cs  # Pure game loop logic (NO Node inheritance)
│   │   │   └── Services/
│   │   │       ├── ICombatService.cs
│   │   │       └── CombatService.cs
│   │   ├── Movement/
│   │   │   ├── Commands/
│   │   │   │   └── MoveActorCommand.cs
│   │   │   └── Services/
│   │   │       └── MovementService.cs
│   │   └── Common/
│   │       ├── IGameClock.cs           # Time abstraction
│   │       └── ISchedulerService.cs    # Turn scheduling interface
│   │
│   ├── YourGame.Presentation/          # MVP presenters and view interfaces
│   │   ├── ViewInterfaces/             # Contracts that views must implement
│   │   │   ├── ICombatView.cs
│   │   │   ├── IActorView.cs
│   │   │   └── IWorldMapView.cs
│   │   ├── Presenters/                 # MVP presenters (orchestration logic)
│   │   │   ├── CombatPresenter.cs
│   │   │   ├── ActorPresenter.cs
│   │   │   └── WorldMapPresenter.cs
│   │   ├── EventBus/                   # Domain→UI event routing
│   │   │   └── UIEventBus.cs
│   │   └── DI/                         # Dependency injection setup
│   │       └── ServiceConfiguration.cs
│   │
│   └── YourGame.Infrastructure/        # External system integrations
│       ├── Randomization/
│       │   └── DeterministicRandom.cs  # PCG implementation
│       ├── Persistence/
│       │   └── SaveManager.cs
│       └── Services/
│           └── GameClock.cs
│
├── YourGame.csproj                     # GODOT PROJECT (ALL Godot code here)
│   ├── GodotIntegration/              # ADAPTER LAYER - Bridges to pure C#
│   │   ├── GameLoop/
│   │   │   └── GameLoop.cs            # : Node - Thin adapter for frame timing
│   │   ├── Audio/
│   │   │   └── GodotAudioService.cs   # : Node2D - Implements IAudioService
│   │   ├── Input/
│   │   │   └── GodotInputAdapter.cs   # : Node - Implements IInputService
│   │   ├── Resources/
│   │   │   └── GodotResourceLoader.cs # Implements IResourceLoader
│   │   └── Core/
│   │       ├── GameManager.cs         # : Node - Bootstrap and DI setup
│   │       └── ServiceLocator.cs      # : Node - Autoload for DI access
│   │
│   ├── Views/                         # Godot view implementations
│   │   ├── Combat/
│   │   │   ├── CombatView.cs          # : Control - Implements ICombatView
│   │   │   └── CombatView.tscn        # Godot scene file
│   │   ├── Actors/
│   │   │   ├── ActorView.cs           # : Node2D - Implements IActorView
│   │   │   ├── ActorView.tscn         # Godot scene file
│   │   │   └── ActorSprite.tres       # Godot resource
│   │   ├── World/
│   │   │   ├── WorldMapView.cs        # : Node2D
│   │   │   ├── WorldMapView.tscn
│   │   │   └── TileMapView.cs         # : TileMap
│   │   └── UI/
│   │       ├── InventoryView.cs       # : Control
│   │       ├── InventoryView.tscn
│   │       ├── HealthBar.cs           # : ProgressBar
│   │       └── HealthBar.tscn
│   │
│   ├── Scenes/                        # Composed Godot scenes
│   │   ├── MainMenu.tscn
│   │   ├── WorldMap.tscn
│   │   ├── CombatArena.tscn
│   │   └── GameOver.tscn
│   │
│   ├── Resources/                     # Godot assets (sprites, audio, data)
│   │   ├── Sprites/
│   │   │   ├── Characters/
│   │   │   ├── Terrain/
│   │   │   └── UI/
│   │   ├── Audio/
│   │   │   ├── Music/
│   │   │   └── SFX/
│   │   ├── Fonts/
│   │   └── Data/                      # .tres resource files
│   │       ├── Actors/
│   │       │   ├── Knight.tres
│   │       │   └── Archer.tres
│   │       └── Items/
│   │           ├── Sword.tres
│   │           └── Bow.tres
│   │
│   └── project.godot                  # Godot project settings
│
└── tests/
    └── YourGame.Tests/
        ├── Domain/                     # Pure unit tests (no Godot)
        ├── Application/                # Handler tests with mocks
        └── Integration/                # Tests requiring Godot context
```

### Key Organizational Principles

1. **src/ folder**: Contains ONLY pure C# with zero Godot dependencies
2. **GodotIntegration/ folder**: All adapter classes that bridge Godot to pure C#
3. **Views/ folder**: Godot nodes that implement view interfaces
4. **Paired files**: Each view has .cs and .tscn files together
5. **Resources/**: Organized by type (Sprites, Audio, Data)

### Dependency Rules

```
Godot Project → Presentation → Application → Domain
      ↓              ↓              ↓           ↓
 (Can see all)  (Can see 2)    (Can see 1)  (Sees nothing)
```

### File Naming Conventions

- **Domain**: Business terms (`Actor.cs`, `Health.cs`, `Position.cs`)
- **Application**: Command/Query pattern (`ExecuteAttackCommand.cs`, `GetActorQuery.cs`)
- **Presentation**: MVP pattern (`CombatPresenter.cs`, `ICombatView.cs`)
- **Adapters**: Descriptive (`GodotAudioService.cs`, `GameLoop.cs`)
- **Views**: Paired naming (`ActorView.cs` + `ActorView.tscn`)

## When NOT to Use Adapters

Per ADR-006 (Selective Abstraction), not everything needs an adapter:
- ✅ Direct Godot use in Views for UI elements
- ✅ Direct particle system usage in presentation
- ✅ Direct animation/tween usage in views
- ✅ Direct scene loading for UI-only concerns

The Adapter Pattern is for when business logic needs to interact with Godot features.

## Related ADRs

- **ADR-001**: Strict Model-View Separation (motivation for adapters)
- **ADR-006**: Selective Abstraction (when to use adapters)
- **ADR-010**: UI Event Bus (adapter example)
- **ADR-011**: Resource Bridge (adapter example)
- **ADR-018**: DI Lifecycle (adapter example)
- **ADR-021**: Project Separation (enforces adapter boundaries)
- **ADR-024**: GameLoop Architecture (adapter example)

## References

- [Adapter Pattern - Gang of Four](https://refactoring.guru/design-patterns/adapter)
- [Hexagonal Architecture (Ports and Adapters)](https://alistair.cockburn.us/hexagonal-architecture/)
- [Clean Architecture - Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)

## Decision Record

This pattern has been proven through multiple independent implementations in our codebase. It's not theoretical - it's the practical solution that makes Clean Architecture work with Godot.

The Adapter Pattern is now our standard approach for ALL Godot integrations that require business logic interaction.