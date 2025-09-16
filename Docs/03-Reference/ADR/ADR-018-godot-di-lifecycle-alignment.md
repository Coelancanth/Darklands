# ADR-018: Godot-Aligned Dependency Injection Lifecycle

## Status
**ACCEPTED** - Implemented in TD_052 (2025-09-15)
**UPDATED** - 2025-09-15 - Aligned with ADR-021 MVP enforcement

## Context

Microsoft.Extensions.DependencyInjection (MS.DI) provides excellent service lifetime management (Singleton/Scoped/Transient), but it has no awareness of Godot's scene tree lifecycle. This creates several problems:

1. **Memory Leaks**: Services created for a scene outlive the scene itself
2. **State Pollution**: Scene-specific state carries over between scene loads
3. **Lifecycle Mismatch**: MS.DI scopes don't align with Godot node lifecycles
4. **Resolution Timing**: No clear pattern for when nodes should resolve dependencies
5. **Testing Friction**: Can't easily reset state between test scenes

Current state:
- `GameStrapper` creates a root ServiceProvider at startup
- All services are effectively singleton or transient
- No scene-level isolation or cleanup
- Nodes manually call ServiceProvider in various lifecycle methods

We need a lightweight bridge between MS.DI's scope model and Godot's scene lifecycle without abandoning our existing DI infrastructure or introducing complex abstractions.

### Critical Constraint: MVP Pattern Enforcement (per ADR-021)

**IMPORTANT**: This ADR must align with ADR-021's strict MVP separation:
- **Views (Godot nodes) can ONLY resolve their Presenter**
- **Views MUST NOT resolve Application services (IMediator, IRepository, etc.)**
- **Views MUST NOT resolve Infrastructure services (ILogger, IAudioService, etc.)**
- **Presenters are responsible for ALL service interactions**

This constraint is enforced at the project reference level:
- `Darklands.csproj` (Views) → references ONLY `Darklands.Presentation`
- `Darklands.csproj` has NO reference to `Darklands.Core`
- Therefore, Views cannot even see Core services at compile time

## Decision

We will implement an instance-based `IScopeManager` service that creates MS.DI scopes aligned with Godot's scene tree hierarchy. This allows both scene-level and nested scopes while maintaining testability and avoiding global state.

### Core Design

#### Chosen: Instance-Based Tree-Aligned Scopes

```csharp
// Interface for testability and dependency inversion
public interface IScopeManager
{
    IServiceScope CreateScope(Node node);
    void DisposeScope(Node node);
    IServiceProvider GetProviderForNode(Node node);
}

// Implementation registered as singleton in DI
public class GodotScopeManager : IScopeManager
{
    private readonly IServiceProvider _rootProvider;
    private readonly Dictionary<Node, IServiceScope> _nodeScopes = new();
    private readonly object _lock = new();

    public GodotScopeManager(IServiceProvider rootProvider)
    {
        _rootProvider = rootProvider ?? throw new ArgumentNullException(nameof(rootProvider));
    }

    /// <summary>
    /// Create a scope for a node, using parent scope if available
    /// </summary>
    public IServiceScope CreateScope(Node node)
    {
        lock (_lock)
        {
            if (_nodeScopes.ContainsKey(node))
            {
                GD.PrintErr($"[DI] Scope already exists for {node.Name}");
                return _nodeScopes[node];
            }

            // Find parent scope by walking up tree
            var parentProvider = FindParentProvider(node.GetParent());
            var scope = parentProvider.CreateScope();

            _nodeScopes[node] = scope;

            // Auto-cleanup on node exit
            if (!node.IsConnected("tree_exiting", Callable.From(() => DisposeScope(node))))
            {
                node.TreeExiting += () => DisposeScope(node);
            }

            GD.Print($"[DI] Scope created for {node.Name}");
            return scope;
        }
    }

    /// <summary>
    /// Dispose scope for a node and all child scopes
    /// </summary>
    public void DisposeScope(Node node)
    {
        lock (_lock)
        {
            if (!_nodeScopes.TryGetValue(node, out var scope))
                return;

            // Dispose child scopes first
            foreach (var child in node.GetChildren())
            {
                if (_nodeScopes.ContainsKey(child))
                    DisposeScope(child);
            }

            scope.Dispose();
            _nodeScopes.Remove(node);
            GD.Print($"[DI] Scope disposed for {node.Name}");
        }
    }

    /// <summary>
    /// Get provider for a node by walking up tree to find nearest scope
    /// </summary>
    public IServiceProvider GetProviderForNode(Node node)
    {
        lock (_lock)
        {
            var current = node;
            while (current != null)
            {
                if (_nodeScopes.TryGetValue(current, out var scope))
                    return scope.ServiceProvider;
                current = current.GetParent();
            }
            return _rootProvider;
        }
    }

    private IServiceProvider FindParentProvider(Node? parent)
    {
        while (parent != null)
        {
            if (_nodeScopes.TryGetValue(parent, out var scope))
                return scope.ServiceProvider;
            parent = parent.GetParent();
        }
        return _rootProvider;
    }
}
```

```csharp
// Better approach: Use Godot's autoload system to avoid static state
public partial class ServiceLocator : Node
{
    private IScopeManager? _scopeManager;

    // Singleton pattern using Godot's autoload
    private static ServiceLocator? _instance;
    public static ServiceLocator Instance => _instance ?? throw new InvalidOperationException("ServiceLocator not initialized");

    public override void _Ready()
    {
        _instance = this;
        // ScopeManager injected via constructor or property
    }

    public void Initialize(IScopeManager scopeManager)
    {
        _scopeManager = scopeManager ?? throw new ArgumentNullException(nameof(scopeManager));
    }

    public IScopeManager ScopeManager => _scopeManager ?? throw new InvalidOperationException("ScopeManager not initialized");
}

public static class NodeServiceExtensions
{
    /// <summary>
    /// Extension method to get services for any node using Godot's autoload
    /// </summary>
    public static T GetService<T>(this Node node) where T : class
    {
        // Access through Godot's autoload system - no static state in extension class
        var scopeManager = node.GetNode<ServiceLocator>("/root/ServiceLocator").ScopeManager;
        var provider = scopeManager.GetProviderForNode(node);
        return provider.GetRequiredService<T>();
    }

    /// <summary>
    /// Extension method to try get services for any node
    /// </summary>
    public static T? TryGetService<T>(this Node node) where T : class
    {
        var serviceLocator = node.GetNodeOrNull<ServiceLocator>("/root/ServiceLocator");
        if (serviceLocator?.ScopeManager == null)
            return null;

        var provider = serviceLocator.ScopeManager.GetProviderForNode(node);
        return provider.GetService<T>();
    }

    /// <summary>
    /// Create a scope for this node (usually for scene roots)
    /// </summary>
    public static IServiceScope CreateScope(this Node node)
    {
        var scopeManager = node.GetNode<ServiceLocator>("/root/ServiceLocator").ScopeManager;
        return scopeManager.CreateScope(node);
    }
}
```

### Service Registration

```csharp
// In Darklands.Presentation/ServiceConfiguration.cs (NOT in Darklands.csproj!)
public static class ServiceConfiguration
{
    public static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Register scope manager as singleton
        services.AddSingleton<IScopeManager>(provider =>
            new GodotScopeManager(provider));

        // PRESENTERS - The ONLY services Views should resolve
        services.AddScoped<IGridPresenter, GridPresenter>();
        services.AddScoped<IActorPresenter, ActorPresenter>();
        services.AddScoped<ICombatPresenter, CombatPresenter>();
        services.AddScoped<IInventoryPresenter, InventoryPresenter>();

        // SINGLETON - Lives entire application lifetime (used by Presenters)
        services.AddSingleton<ILogger, UnifiedLogger>();
        services.AddSingleton<IAudioService, GodotAudioService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IDeterministicRandom, DeterministicRandom>();
        services.AddSingleton<IUIEventBus, UIEventBus>();

        // SCOPED - Reset per scope (used by Presenters)
        services.AddScoped<IGameState, GameState>();
        services.AddScoped<ICombatState, CombatState>();
        services.AddScoped<IActorRepository, ActorRepository>();

        // TRANSIENT - New instance per resolution (used by Presenters)
        services.AddTransient<ICommand, Command>();
        services.AddTransient(typeof(IRequestHandler<,>), typeof(Handler<,>));

        // MediatR registration
        services.AddMediatR(cfg => {
            cfg.RegisterServicesFromAssembly(typeof(Darklands.Core.CoreMarker).Assembly);
        });

        var provider = services.BuildServiceProvider();

        // Initialize scope manager
        var scopeManager = provider.GetRequiredService<IScopeManager>();

        return provider;
    }
}

// In GameManager.cs (Darklands.csproj - minimal bootstrap only)
public partial class GameManager : Node
{
    public override void _Ready()
    {
        // Bootstrap DI through Presentation layer
        var serviceProvider = ServiceConfiguration.ConfigureServices();

        // Initialize ServiceLocator autoload
        var serviceLocator = GetNode<ServiceLocator>("/root/ServiceLocator");
        serviceLocator.Initialize(serviceProvider.GetRequiredService<IScopeManager>());
    }
}
```

### Node Resolution Pattern (MVP-Compliant)

```csharp
// CORRECT: View only resolves its Presenter
public partial class CombatView : Control, ICombatView
{
    private ICombatPresenter? _presenter;

    public override void _Ready()
    {
        // Views ONLY resolve their presenter
        _presenter = this.GetService<ICombatPresenter>();

        // Register view with presenter
        _presenter.AttachView(this);

        // Let presenter handle all initialization
        _presenter.Initialize();
    }

    public override void _ExitTree()
    {
        // Presenter handles cleanup
        _presenter?.Dispose();
    }

    // ICombatView implementation - UI updates only
    public void UpdateHealth(int current, int max)
    {
        GetNode<ProgressBar>("HealthBar").Value = (float)current / max;
    }

    // UI events delegate to presenter
    private void _on_AttackButton_pressed()
    {
        _presenter?.OnAttackRequested();
    }
}

// The Presenter handles ALL service interactions
public class CombatPresenter : ICombatPresenter
{
    private readonly IMediator _mediator;
    private readonly IUIEventBus _eventBus;
    private readonly ICombatState _combatState;
    private ICombatView? _view;

    // Presenter gets services through constructor injection
    public CombatPresenter(
        IMediator mediator,
        IUIEventBus eventBus,
        ICombatState combatState)
    {
        _mediator = mediator;
        _eventBus = eventBus;
        _combatState = combatState;
    }

    public void AttachView(ICombatView view)
    {
        _view = view;
    }

    public void Initialize()
    {
        // Presenter subscribes to domain events
        _eventBus.Subscribe<CombatStartedEvent>(OnCombatStarted);

        // Presenter updates view based on state
        var health = _combatState.PlayerHealth;
        _view?.UpdateHealth(health.Current, health.Max);
    }

    public void OnAttackRequested()
    {
        // Presenter sends commands through MediatR
        _mediator.Send(new AttackCommand());
    }

    public void Dispose()
    {
        _eventBus.Unsubscribe<CombatStartedEvent>(OnCombatStarted);
    }
}
```

### INCORRECT Patterns (DO NOT USE)

```csharp
// ❌ WRONG: View should NOT resolve Core services
public partial class BadView : Control
{
    public override void _Ready()
    {
        // These violate MVP - Views cannot see Core services!
        var mediator = this.GetService<IMediator>();       // ❌ COMPILE ERROR
        var repository = this.GetService<IRepository>();   // ❌ COMPILE ERROR
        var logger = this.GetService<ILogger>();          // ❌ COMPILE ERROR
    }
}
```

### Scene Loading Integration

```csharp
public partial class SceneManager : Node
{
    public void LoadScene(string scenePath)
    {
        // Load new scene
        var newScene = GD.Load<PackedScene>(scenePath).Instantiate();

        // Create scope for new scene using extension method
        newScene.CreateScope();

        // Switch scenes
        GetTree().Root.AddChild(newScene);
        GetTree().CurrentScene?.QueueFree();
        GetTree().CurrentScene = newScene;
    }

    public void LoadOverlay(string overlayPath, Node parent)
    {
        // Load overlay scene
        var overlay = GD.Load<PackedScene>(overlayPath).Instantiate();

        // Create nested scope for overlay
        overlay.CreateScope();

        // Add to parent
        parent.AddChild(overlay);
    }
}
```

### Testing Support

```csharp
[TestFixture]
public class CombatTests
{
    private IServiceProvider _provider;
    private IScopeManager _scopeManager;
    private Node _testNode;

    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IScopeManager, GodotScopeManager>();
        services.AddScoped<ICombatState, MockCombatState>();

        _provider = services.BuildServiceProvider();
        _scopeManager = _provider.GetRequiredService<IScopeManager>();

        // Initialize extension methods for test
        NodeServiceExtensions.Initialize(_scopeManager);

        // Create test scope
        _testNode = new Node();
        _scopeManager.CreateScope(_testNode);
    }

    [TearDown]
    public void Teardown()
    {
        _scopeManager.DisposeScope(_testNode);
        _provider?.Dispose();
    }

    [Test]
    public void CanRunTestsInParallel()
    {
        // Each test has its own scope manager instance
        // No static state interference
        var combatState = _testNode.GetService<ICombatState>();
        Assert.NotNull(combatState);
    }
}
```

## Implementation Update (TD_052 - September 2025)

The actual implementation exceeded the original design with several critical improvements:

### Production Enhancements Added

1. **Memory Safety (ConditionalWeakTable)**
   ```csharp
   private readonly ConditionalWeakTable<Node, IServiceScope> _nodeScopes = new();
   ```
   - Prevents memory leaks if nodes are freed without TreeExiting signal
   - Allows garbage collection of orphaned node references

2. **Performance Caching (WeakReference)**
   ```csharp
   private readonly ConcurrentDictionary<Node, WeakReference<IServiceProvider>> _providerCache = new();
   ```
   - O(1) service resolution after first lookup
   - Prevents repeated tree walking for same nodes

3. **Thread Safety (ReaderWriterLockSlim)**
   ```csharp
   private readonly ReaderWriterLockSlim _lock = new();
   ```
   - High-performance concurrent read access
   - Exclusive locks only for write operations (create/dispose)

4. **Performance Monitoring**
   ```csharp
   if (elapsedMs > 1.0)
   {
       _logger?.LogWarning("Slow scope resolution for {NodeName}: {ElapsedMs:F2}ms");
   }
   ```
   - Automatic detection of performance issues
   - Diagnostic information for troubleshooting

5. **Comprehensive Error Handling**
   - Graceful fallback to GameStrapper pattern
   - Never crashes node initialization
   - Detailed logging for troubleshooting

### File Structure (Actual Implementation)
```
src/Core/Domain/Services/IScopeManager.cs           # Interface with diagnostics
Presentation/Infrastructure/GodotScopeManager.cs    # Full implementation
Presentation/Infrastructure/NodeServiceExtensions.cs # Extension methods
ServiceLocator.cs                                   # Godot autoload
tests/Infrastructure/.../ScopeLifecycleIntegrationTests.cs # Comprehensive tests
```

### Service Migration Results
- **State Services → Scoped**: GridStateService, ActorStateService, CombatSchedulerService
- **Infrastructure → Singleton**: Logger, Audio, Settings, DeterministicRandom
- **EventAwareNode Updated**: Uses `this.GetService<T>()` with fallback
- **Zero Build Warnings**: 673 tests passing

### Key Achievements
- ✅ Memory leak prevention verified
- ✅ Test parallelization enabled
- ✅ Production-ready error handling
- ✅ Performance optimizations implemented
- ✅ Comprehensive testing suite added

## Consequences

### Positive
- **Memory Management**: Scoped services properly disposed when nodes exit tree
- **State Isolation**: Each scope gets fresh services, supports nested scopes
- **Multiple Concurrent Scopes**: Supports overlays, modals, HUD with separate scopes
- **Tree-Aligned**: Naturally follows Godot's node hierarchy pattern
- **Testability**: No static state, tests can run in parallel
- **Dependency Inversion**: Nodes depend on IScopeManager interface, not concrete class
- **Singleton Preservation**: App-level services persist across all scopes
- **Performance**: Single resolution per node lifecycle, no per-frame lookups
- **MS.DI Compatible**: Uses standard scope mechanism, all features preserved
- **Extension Method Ergonomics**: Clean syntax without inheritance constraints
- **Future-Proof**: Can handle any scene composition pattern

### Negative
- **Boilerplate Per Node**: Each node needs ~3 lines of resolution code
- **Manual Resolution**: Must explicitly call GetService in _Ready
- **No Attribute Injection**: More verbose than [Inject] patterns
- **Dictionary Overhead**: Small memory cost for scope tracking
- **Tree Walking**: GetProviderForNode walks up tree (usually shallow)
- **Thread Safety Overhead**: Lock contention possible with many concurrent scope operations
- **Memory Leak Risk**: If TreeExiting doesn't fire (e.g., node freed directly), scope won't be disposed
  - Mitigation: Consider WeakReference for node tracking or periodic cleanup

### Neutral
- **Explicit Over Magic**: Clear dependency flow, no hidden injection
- **Learning Curve**: Developers must understand tree-based scoping
- **Migration Work**: Existing nodes need _Ready updates
- **Service Registration**: Must categorize services by lifetime correctly

## Alternatives Considered

### Alternative 1: Static Single-Scope Manager
Initial approach using static class with single scene scope.
- **Pros**: Simpler mental model, fewer moving parts
- **Cons**: No parallel scopes, global state, can't test in parallel, violates DI principles
- **Reason not chosen**: Architectural review revealed fundamental flaws that would require major refactoring later

### Alternative 2: Fluentish.Godot.DependencyInjection
Existing library that creates new ServiceProvider per scene with attribute injection.
- **Pros**: Automatic injection, less boilerplate, existing solution
- **Cons**: No singleton sharing, attribute spread, performance overhead, architecture violation
- **Reason not chosen**: Incompatible with Clean Architecture, can't share app singletons

### Alternative 3: Custom DI Container
Build Godot-specific DI container from scratch.
- **Pros**: Perfect Godot integration, optimal performance
- **Cons**: Massive effort, lose MediatR compatibility, reinventing wheel
- **Reason not chosen**: Violates ADR-006 (selective abstraction), enormous maintenance burden

### Alternative 4: ServiceNode Base Class
Reduce boilerplate through inheritance.
- **Pros**: Less code repetition, centralized resolution logic
- **Cons**: Single inheritance limitation in C#, many nodes already have base classes
- **Reason not chosen**: Extension methods provide same benefit without inheritance constraints

### Alternative 5: No Scoping (Status Quo)
Keep current approach with only singleton/transient.
- **Pros**: Simple, no changes needed
- **Cons**: Memory leaks, state pollution, poor testing
- **Reason not chosen**: Doesn't solve the actual problems we're facing

## Implementation Notes

### Phase 1: Core Implementation (3 hours)
1. Create `IScopeManager` interface and `GodotScopeManager` implementation
2. Register as singleton in `GameStrapper`
3. Create `NodeServiceExtensions` with GetService methods
4. Initialize extension methods after DI setup
5. Test scope creation, disposal, and nesting

### Phase 2: Service Migration (2 hours)
1. Categorize existing services by lifetime (singleton/scoped/transient)
2. Update service registrations appropriately
3. Update nodes to use `this.GetService<T>()` extension method
4. Add scope creation to SceneManager
5. Verify proper disposal with scene transitions

### Phase 3: Testing & Validation (2 hours)
1. Create parallel test suite to verify no static interference
2. Add integration tests for nested scopes
3. Performance test tree walking in deep hierarchies
4. Add debug window showing active scopes and services

### Critical Rules (Updated for MVP Compliance)
1. **Views ONLY resolve Presenters**: Never resolve Core services directly
2. **Never resolve in _Process**: Only in _Ready or event handlers
3. **Cache resolved presenter**: Store in private field
4. **Presenters handle ALL service interactions**: Views are purely UI
5. **Dispose through presenter**: Call presenter.Dispose() in _ExitTree
6. **No attribute injection for services**: Only for node paths
7. **Test scope isolation**: Each test should begin/end scope
8. **Scene loading MUST use SceneManager**: Direct scene instantiation bypasses DI
9. **Compile-time enforcement**: Project references prevent violations

### Enforcement Mechanisms

```csharp
// Add to project settings or CI
public class DIEnforcementTests
{
    [Test]
    public void ViewsOnlyResolvePresenterInterfaces()
    {
        // Scan View classes for GetService<T> calls
        // Ensure T always ends with "Presenter"
        var viewTypes = Assembly.GetAssembly(typeof(GameManager))
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(Node)));

        foreach (var viewType in viewTypes)
        {
            // Check that GetService only resolves IPresenter types
        }
    }

    [Test]
    public void NoCoreReferencesInGodotProject()
    {
        // Ensure Darklands.csproj doesn't reference Darklands.Core
        // This is enforced at project level but double-check
    }

    [Test]
    public void AllScenesLoadedThroughSceneManager()
    {
        // Scan codebase for GetTree().ChangeScene* or PackedScene.Instantiate
        // outside of SceneManager
    }

    [Test]
    public void NoServiceResolutionInProcess()
    {
        // Scan for GetService calls in _Process methods
    }
}
```

### Service Lifetime Decision Matrix

| Service Type | Lifetime | Reasoning |
|-------------|----------|-----------|
| Logger | Singleton | App-wide, expensive to create |
| Audio/Input | Singleton | Hardware interfaces, stateless |
| Settings | Singleton | Global configuration |
| Event Bus | Singleton | Cross-scene communication |
| Game State | Scoped | Reset per level/scene |
| Repositories | Scoped | Fresh data per scene |
| Presenters | Scoped | UI state per scene |
| Commands | Transient | Stateless operations |
| Handlers | Transient | MediatR requirement |

## Godot Project Organization

### Where Godot Files Live (per ADR-021)

```
Darklands.csproj (Root - Godot Entry Point)
├── Views/                     # Godot View implementations
│   ├── Combat/
│   │   ├── CombatView.cs     # : Control, ICombatView
│   │   └── CombatView.tscn   # Godot scene file
│   ├── Grid/
│   │   ├── GridView.cs       # : TileMap, IGridView
│   │   └── GridView.tscn     # Godot scene file
│   └── Actor/
│       ├── ActorView.cs      # : Node2D, IActorView
│       └── ActorView.tscn    # Godot scene file
├── Scenes/                    # Composed scenes
│   ├── MainMenu.tscn
│   ├── GameWorld.tscn
│   └── CombatArena.tscn
├── Resources/                 # Godot resources
│   ├── Sprites/
│   ├── Audio/
│   └── Themes/
├── GameManager.cs            # Minimal bootstrap only
├── ServiceLocator.cs         # Autoload for DI
└── project.godot             # Godot project file

Darklands.Presentation.csproj
├── Views/                    # View INTERFACES
│   ├── ICombatView.cs
│   ├── IGridView.cs
│   └── IActorView.cs
├── Presenters/              # Presenters orchestrate
│   ├── CombatPresenter.cs
│   ├── GridPresenter.cs
│   └── ActorPresenter.cs
└── ServiceConfiguration.cs  # DI setup

Darklands.Core.csproj
├── Application/             # Commands, Handlers
└── Infrastructure/         # Services, Repositories

Darklands.Domain.csproj     # Pure business logic
```

### Key Points:
- **Godot scenes (.tscn)** stay in root project with Views
- **View interfaces** live in Presentation project
- **Presenters** live in Presentation and handle all logic
- **Views** only know about their Presenter interface
- **No Core references** allowed in Darklands.csproj

## References
- [Microsoft.Extensions.DependencyInjection Scopes](https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection#service-lifetimes)
- [Godot Scene Tree](https://docs.godotengine.org/en/stable/tutorials/scripting/scene_tree.html)
- ADR-006: Selective Abstraction Strategy
- ADR-010: UI Event Bus Architecture
- ADR-021: Minimal Project Separation for Domain Purity
- Unity's Zenject/VContainer scope model (inspiration)