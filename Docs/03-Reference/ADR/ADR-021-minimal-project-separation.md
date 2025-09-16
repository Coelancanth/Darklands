# ADR-021: Minimal Project Separation for Domain Purity

## Status
**Status**: Accepted (Updated)
**Date**: 2025-09-15 (Updated: 2025-09-15)
**Decision Makers**: Tech Lead, Dev Engineer, DevOps Engineer
**Supersedes**: ADR-019 (rejected as over-engineered)
**Complements**: ADR-020 (feature-based organization), ADR-006 (selective abstraction), ADR-010 (UI event bus), ADR-018 (DI lifecycle)

## Context

### Current State
We currently have:
- `Darklands.csproj` - Godot entry point (references Darklands.Core)
- `Darklands.Core.csproj` - All business logic (Domain, Application, Infrastructure, Presentation)
- `Darklands.Core.Tests.csproj` - All tests

This structure already provides separation between Godot and our business logic, but has critical architectural weaknesses:

1. **Domain Purity**: Domain layer can accidentally reference infrastructure concerns
2. **MVP Enforcement**: Views can bypass Presenters and directly access Commands/Repositories
3. **Layer Boundaries**: No compile-time enforcement of Clean Architecture layers

### The Problem

#### 1. Domain Purity (ADR-006 Alignment)
The Domain layer should never depend on:
- Godot types
- File I/O or external services
- Infrastructure implementations
- Framework-specific concerns

#### 2. MVP Pattern Enforcement (ADR-010/ADR-018 Critical Requirement)
**CRITICAL**: ADR-010 and ADR-018 require strict MVP separation:
- Views MUST ONLY resolve Presenter interfaces
- Views MUST NOT access Commands, Handlers, or Infrastructure services
- This prevents architectural decay and maintains testability

**Current Problem**: Without separate projects, nothing prevents Views from bypassing Presenters:
```csharp
// This should be IMPOSSIBLE but currently isn't:
public partial class BadView : Control
{
    public override void _Ready()
    {
        var mediator = this.GetService<IMediator>();     // ❌ Bypasses Presenter!
        var repo = this.GetService<IRepository>();       // ❌ Direct data access!
    }
}
```

### Post-TD_042 Constraints
After removing DDD over-engineering, any solution must be:
- **Simple and elegant**: No unnecessary abstraction layers
- **Minimal friction**: Quick development, not bureaucratic
- **Quick to implement**: 5 hours max (realistic estimate)
- **Architecturally sound**: Prevents corruption over time

## Decision

Implement a **4-project structure** that enforces both Domain purity and MVP pattern boundaries through compile-time project references, while using NetArchTest for internal layer enforcement within projects.

**Key Insight**: The 4th project (Presentation) is not over-engineering—it's the **architectural firewall** that prevents Views from bypassing Presenters and directly accessing Core services.

### Complete Project Structure

```
Darklands.sln
│
├── Darklands.csproj                        # 🎮 GODOT PROJECT
│   ├── Views/                              # Godot View Implementations
│   │   ├── Combat/
│   │   │   ├── CombatView.cs             # : Control, ICombatView
│   │   │   └── CombatView.tscn           # Godot scene file
│   │   ├── Grid/
│   │   │   ├── GridView.cs               # : TileMap, IGridView
│   │   │   └── GridView.tscn             # Godot scene file
│   │   ├── Actor/
│   │   │   ├── ActorView.cs              # : Node2D, IActorView
│   │   │   └── ActorView.tscn            # Godot scene file
│   │   └── UI/
│   │       ├── InventoryView.cs          # : Control, IInventoryView
│   │       └── InventoryView.tscn        # Godot scene file
│   ├── Scenes/                            # Composed scenes
│   │   ├── MainMenu.tscn
│   │   ├── GameWorld.tscn
│   │   └── CombatArena.tscn
│   ├── Resources/                         # Godot assets
│   │   ├── Sprites/
│   │   ├── Audio/
│   │   ├── Themes/
│   │   └── Fonts/
│   ├── Bootstrap/                         # Minimal wiring layer
│   │   ├── GameManager.cs                # Entry point
│   │   ├── ServiceLocator.cs             # Autoload for DI
│   │   └── SceneManager.cs               # Scene transitions with DI
│   ├── project.godot                      # Godot project settings
│   └── 📦 References: Darklands.Presentation ONLY
│
├── src/
│   ├── Darklands.Domain/                  # 🧠 PURE BUSINESS LOGIC
│   │   ├── Darklands.Domain.csproj
│   │   ├── World/
│   │   │   ├── Grid.cs                   # Entity
│   │   │   ├── Tile.cs                   # Value Object
│   │   │   └── Position.cs               # Value Object
│   │   ├── Characters/
│   │   │   ├── Actor.cs                  # Entity
│   │   │   ├── ActorId.cs                # Value Object
│   │   │   └── Health.cs                 # Value Object
│   │   ├── Combat/
│   │   │   ├── Damage.cs                 # Value Object
│   │   │   └── CombatResult.cs           # Value Object
│   │   ├── Vision/
│   │   │   └── ShadowcastingFOV.cs       # Pure Algorithm
│   │   └── Common/
│   │       ├── IDeterministicRandom.cs   # Interface
│   │       └── Result.cs                 # Functional types
│   │   └── 📦 References: NONE (LanguageExt.Core only)
│   │
│   ├── Darklands.Application/             # 📦 APPLICATION + INFRASTRUCTURE
│   │   ├── Darklands.Application.csproj  # RENAMED from Darklands.Core.csproj
│   │   ├── Application/                   # Use Cases (depends on interfaces only)
│   │   │   ├── Commands/
│   │   │   │   ├── AttackCommand.cs
│   │   │   │   └── MoveCommand.cs
│   │   │   ├── Queries/
│   │   │   │   └── GetVisibleTilesQuery.cs
│   │   │   └── Handlers/
│   │   │       ├── AttackCommandHandler.cs
│   │   │       └── MoveCommandHandler.cs
│   │   └── Infrastructure/                # External Concerns (implements interfaces)
│   │       ├── Services/
│   │       │   ├── DeterministicRandom.cs
│   │       │   ├── GodotAudioService.cs  # Implements IAudioService
│   │       │   └── SaveService.cs
│   │       └── Repositories/
│   │           ├── ActorRepository.cs
│   │           └── GridRepository.cs
│   │   └── 📦 References: Darklands.Domain
│   │
│   └── Darklands.Presentation/            # 🎯 MVP PRESENTERS & FIREWALL
│       ├── Darklands.Presentation.csproj
│       ├── ViewInterfaces/                # View Contracts
│       │   ├── ICombatView.cs
│       │   ├── IGridView.cs
│       │   ├── IActorView.cs
│       │   └── IInventoryView.cs
│       ├── Presenters/                    # Orchestration Layer
│       │   ├── CombatPresenter.cs        # Implements ICombatPresenter
│       │   ├── GridPresenter.cs          # MVP pattern coordination
│       │   ├── ActorPresenter.cs
│       │   └── InventoryPresenter.cs
│       ├── EventBus/                      # UI Event System (ADR-010)
│       │   ├── UIEventBus.cs
│       │   └── UIDispatcher.cs
│       ├── DI/                            # Dependency Injection (ADR-018)
│       │   ├── ServiceConfiguration.cs    # DI Composition Root
│       │   ├── GodotScopeManager.cs      # Scope Management
│       │   └── NodeServiceExtensions.cs   # Extension Methods
│       └── Abstractions/                  # Service Interfaces (ADR-006)
│           ├── IAudioService.cs
│           ├── IInputService.cs
│           ├── ISaveService.cs
│           └── ISettingsService.cs
│       └── 📦 References: Darklands.Domain, Darklands.Application
│
└── tests/
    └── Darklands.Tests/                   # Comprehensive Testing
        ├── Darklands.Tests.csproj
        ├── Architecture/                   # NetArchTest Enforcement
        │   ├── DomainPurityTests.cs      # Domain has no dependencies
        │   ├── MVPEnforcementTests.cs    # Views only access Presenters
        │   ├── LayerBoundaryTests.cs     # Application ↛ Infrastructure
        │   └── NamingConventionTests.cs  # Enforce naming standards
        ├── Domain/
        ├── Application/
        ├── Infrastructure/
        └── Integration/
        └── 📦 References: ALL projects + NetArchTest
```

**Critical Architectural Boundaries**:
- **Compile-Time**: Darklands.csproj → Presentation ONLY (MVP firewall)
- **Compile-Time**: Domain → NONE (purity enforcement)
- **Test-Time**: NetArchTest rules for internal boundaries
- **Review-Time**: Pull request checks for violations

### Dependency Injection Strategy (ADR-018 Alignment)

**CRITICAL**: The DI composition root lives in `Darklands.Presentation.csproj`, with Godot project containing only minimal bootstrapping code.

```csharp
// src/Darklands.Presentation/DI/ServiceConfiguration.cs
public static class ServiceConfiguration
{
    public static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // SCOPE MANAGEMENT (ADR-018)
        services.AddSingleton<IScopeManager>(provider => new GodotScopeManager(provider));

        // PRESENTERS - The ONLY services Views should resolve (MVP Firewall)
        services.AddScoped<ICombatPresenter, CombatPresenter>();
        services.AddScoped<IGridPresenter, GridPresenter>();
        services.AddScoped<IActorPresenter, ActorPresenter>();
        services.AddScoped<IInventoryPresenter, InventoryPresenter>();

        // UI EVENT BUS (ADR-010)
        services.AddSingleton<IUIEventBus, UIEventBus>();
        services.AddTransient(typeof(INotificationHandler<>), typeof(UIEventForwarder<>));

        // SERVICE ABSTRACTIONS (ADR-006 - Selective Abstraction)
        services.AddSingleton<IAudioService, GodotAudioService>();
        services.AddSingleton<IInputService, GodotInputService>();
        services.AddSingleton<ISaveService, SaveService>();
        services.AddSingleton<IDeterministicRandom, DeterministicRandom>();
        services.AddSingleton<ISettingsService, GodotSettingsService>();

        // APPLICATION SERVICES (Used by Presenters)
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(
            typeof(Darklands.Application.ApplicationMarker).Assembly));

        // INFRASTRUCTURE SERVICES
        services.AddScoped<IActorRepository, ActorRepository>();
        services.AddScoped<IGridRepository, GridRepository>();

        return services.BuildServiceProvider();
    }
}

// Bootstrap/GameManager.cs (Darklands.csproj - minimal only)
public partial class GameManager : Node
{
    public override void _Ready()
    {
        // Bootstrap DI through Presentation layer
        var serviceProvider = ServiceConfiguration.ConfigureServices();

        // Initialize ServiceLocator autoload (ADR-018 pattern)
        var serviceLocator = GetNode<ServiceLocator>("/root/ServiceLocator");
        serviceLocator.Initialize(serviceProvider.GetRequiredService<IScopeManager>());
    }
}
```

**MVP Pattern Enforcement**: Views resolve ONLY Presenters. Presenters handle ALL Core interactions.

## Implementation Clarifications

**CRITICAL**: The following implementation details were identified during architectural review and MUST be addressed during implementation to prevent production issues.

### 1. Scope Lifecycle Management (ADR-018 Clarification)

**Definition**: A scope corresponds to a major scene transition in Godot's scene tree.

**🚨 CRITICAL RULE**: Direct use of `GetTree().ChangeScene*` methods is **STRICTLY FORBIDDEN**. All scene transitions MUST go through the injected `ISceneManager` service to maintain scope lifecycle integrity.

#### Scope Boundaries
```csharp
// Scene-Level Scopes (Major Boundaries)
MainMenuScene     → Creates MainMenuScope
GameWorldScene    → Creates GameWorldScope
CombatArenaScene  → Creates CombatScope
InventoryScene    → Creates InventoryScope (modal overlay)

// Scope Lifecycle - ONLY through SceneManager
SceneManager.LoadScene("GameWorld")
  → Disposes previous scope
  → Creates new scope
  → All scoped services reset
  → Scene loaded with proper DI context

// ❌ FORBIDDEN - Bypasses scope management
GetTree().ChangeSceneToFile("res://scenes/game.tscn")  // NEVER DO THIS!
GetTree().ChangeSceneToPacked(scene)                   // NEVER DO THIS!

// ✅ CORRECT - Maintains scope lifecycle
_sceneManager.LoadScene(SceneType.GameWorld);          // Always use this
```

#### Service Lifetimes by Scope
```csharp
// SINGLETON (Application Root - Never Disposed)
services.AddSingleton<ILogger>()           // Cross-scene logging
services.AddSingleton<IAudioService>()     // Hardware interface
services.AddSingleton<ISettingsService>()  // Global configuration
services.AddSingleton<IUIEventBus>()       // Cross-scene events

// SCOPED (Scene-Level - Disposed on Scene Change)
services.AddScoped<IGameState>()          // Current game state
services.AddScoped<IActorRepository>()    // Scene-specific actors
services.AddScoped<ICombatPresenter>()    // UI state for scene
services.AddScoped<IGridPresenter>()      // Visual state

// TRANSIENT (Per-Request - Short-Lived)
services.AddTransient<AttackCommand>()    // Single-use commands
services.AddTransient<MoveCommandHandler>() // Stateless handlers
```

### 2. Thread Safety Protocol (Godot Main Thread Requirement)

**CRITICAL**: All UI updates MUST happen on Godot's main thread to prevent random crashes.

#### UIDispatcher Singleton Implementation (Godot Autoload)
```csharp
// src/Darklands.Presentation/EventBus/UIDispatcher.cs
// IMPORTANT: This MUST be registered as an Autoload in project.godot
public sealed partial class UIDispatcher : Node
{
    private static UIDispatcher? _instance;
    public static UIDispatcher Instance => _instance ?? throw new InvalidOperationException("UIDispatcher not initialized");

    private readonly ConcurrentQueue<Action> _actionQueue = new();

    public override void _Ready()
    {
        _instance = this;
        // This node must persist across scene changes
        ProcessMode = ProcessModeEnum.Always;
    }

    /// <summary>
    /// Thread-safe way to marshal actions to Godot main thread
    /// MUST be used for all UI updates from background threads
    /// </summary>
    public void DispatchToMainThread(Action action)
    {
        _actionQueue.Enqueue(action);
        CallDeferred(nameof(ProcessQueuedActions));
    }

    private void ProcessQueuedActions()
    {
        while (_actionQueue.TryDequeue(out var action))
        {
            try
            {
                action.Invoke();
            }
            catch (Exception ex)
            {
                GD.PrintErr($"UI dispatch error: {ex.Message}");
            }
        }
    }
}

// DI Registration in ServiceConfiguration.cs
public static class ServiceConfiguration
{
    public static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Register UIDispatcher singleton from Godot autoload
        var uiDispatcher = (UIDispatcher)Engine.GetMainLoop().GetRoot().GetNode("/root/UIDispatcher");
        services.AddSingleton<UIDispatcher>(uiDispatcher);
        services.AddSingleton<IUIDispatcher>(uiDispatcher); // Interface for testing

        // ... other service registrations
        return services.BuildServiceProvider();
    }
}

// Usage in Command Handlers (Background Threads)
public class AttackCommandHandler : IRequestHandler<AttackCommand>
{
    private readonly IUIDispatcher _dispatcher;

    public AttackCommandHandler(IUIDispatcher dispatcher) // Injected via DI
    {
        _dispatcher = dispatcher;
    }

    public async Task Handle(AttackCommand request)
    {
        // Background work can happen here
        var result = await CalculateAttackResult(request);

        // UI update MUST be marshalled to main thread
        _dispatcher.DispatchToMainThread(() =>
            UpdateCombatUI(result));
    }
}
```

#### Thread Safety Rules (Mandatory)
1. **Command Handlers**: May run on background threads
2. **UI Updates**: MUST use UIDispatcher.DispatchToMainThread()
3. **Presenter Methods**: Always called on main thread (via UIDispatcher)
4. **View Updates**: Always on main thread (Godot requirement)

### 3. Cross-Presenter Communication Protocol

**Problem**: Presenters may need data from other features (e.g., Combat needs Inventory data).

#### Solution: MediatR-Only Communication
```csharp
// FORBIDDEN: Direct presenter injection
public class CombatPresenter : ICombatPresenter
{
    // ❌ NEVER DO THIS - Creates coupling
    private readonly IInventoryPresenter _inventory;
}

// CORRECT: MediatR notification pattern
public class CombatPresenter : ICombatPresenter
{
    private readonly IMediator _mediator;

    public async Task OnAttackRequested()
    {
        // Query other features through MediatR
        var weapon = await _mediator.Send(new GetActiveWeaponQuery());

        // Send command through MediatR
        await _mediator.Send(new AttackCommand(weapon));

        // Listen to events from other features
        _eventBus.Subscribe<InventoryChangedEvent>(this, OnInventoryChanged);
    }
}
```

#### Inter-Presenter Communication Rules
1. **NO direct presenter references** - prevents coupling
2. **Use MediatR queries** for data from other features
3. **Use MediatR commands** for actions affecting other features
4. **Use UIEventBus** for notifications between presenters
5. **Share state through scoped services** for complex scenarios

### 4. MVP Enforcement with Service Locator Pattern

**Reality Check**: Godot instantiates nodes through scene files (`.tscn`) using parameterless constructors. Constructor injection is IMPOSSIBLE for Views.

**Pragmatic Acceptance**: While Service Locator is generally an anti-pattern, it's a **necessary evil** for Godot Views. We limit its use to:
- **ONLY in View `_Ready()` methods** - Never in business logic
- **ONLY to resolve Presenter interfaces** - Never Core services
- **ONLY once per View lifecycle** - Cache the presenter reference

#### Solution: Service Locator with Property Injection (ADR-018 Aligned)
```csharp
// CORRECT VIEW PATTERN: Service locator as shown in ADR-018
public partial class CombatView : Control, ICombatView
{
    private ICombatPresenter? _presenter;

    public override void _Ready()
    {
        // Service locator pattern - the ONLY viable option in Godot
        _presenter = this.GetService<ICombatPresenter>();

        if (_presenter == null)
        {
            GD.PrintErr($"Failed to resolve presenter for {GetType().Name}");
            return;
        }

        _presenter.AttachView(this);
        _presenter.Initialize();
    }

    public override void _ExitTree()
    {
        _presenter?.Dispose();
    }
}

// Alternative: Property Injection for Testing
public partial class TestableView : Control, ITestableView
{
    // Property injection for unit tests only
    [Export] // Godot's attribute for inspector-visible properties
    public ICombatPresenter? Presenter { get; set; }

    public override void _Ready()
    {
        // Try property injection first (for tests), fall back to service locator
        _presenter = Presenter ?? this.GetService<ICombatPresenter>();
        _presenter?.AttachView(this);
        _presenter?.Initialize();
    }
}
```

#### Architecture Tests for Service Locator Pattern
```csharp
// tests/Architecture/MVPEnforcementTests.cs
[Fact]
public void Views_Should_Only_GetService_Presenter_Interfaces()
{
    var viewAssembly = typeof(GameManager).Assembly;
    var viewTypes = viewAssembly.GetTypes()
        .Where(t => t.IsSubclassOf(typeof(Node)) && t.Name.EndsWith("View"));

    foreach (var viewType in viewTypes)
    {
        var methods = viewType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var method in methods)
        {
            // This is admittedly complex but necessary for service locator pattern
            var methodBody = method.GetMethodBody();
            if (methodBody == null) continue;

            // Scan for GetService<T> calls in IL
            // Alternative: Use Roslyn analyzers for compile-time checking
            // For now: Enforce through code review and documentation
        }
    }
}

// Simpler Alternative: Convention-based testing
[Fact]
public void Views_Should_Have_Presenter_Field_Only()
{
    var viewTypes = typeof(GameManager).Assembly.GetTypes()
        .Where(t => t.IsSubclassOf(typeof(Node)) && t.Name.EndsWith("View"));

    foreach (var viewType in viewTypes)
    {
        var serviceFields = viewType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(f => f.Name.Contains("presenter", StringComparison.OrdinalIgnoreCase));

        serviceFields.Should().NotBeEmpty($"{viewType.Name} should have a presenter field");

        var otherServiceFields = viewType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(f => f.FieldType.Name.Contains("Service") ||
                       f.FieldType.Name.Contains("Repository") ||
                       f.FieldType.Name == "IMediator");

        otherServiceFields.Should().BeEmpty(
            $"{viewType.Name} should not have direct service dependencies");
    }
}
```

### What Goes Where

#### Darklands.Domain.csproj (Pure Business Logic)
```
src/Darklands.Domain/
├── World/
│   ├── Grid.cs              # Entity
│   ├── Tile.cs              # Value Object
│   ├── Position.cs          # Value Object
│   └── TerrainType.cs       # Enum
├── Characters/
│   ├── Actor.cs             # Entity
│   ├── ActorId.cs           # Value Object
│   ├── Health.cs            # Value Object
│   └── ActorState.cs        # Enum
├── Combat/
│   ├── AttackAction.cs      # Value Object
│   ├── Damage.cs            # Value Object
│   ├── TimeUnit.cs          # Value Object
│   └── CombatResult.cs      # Value Object
├── Vision/
│   ├── VisionRange.cs       # Value Object
│   ├── VisionState.cs       # Value Object
│   └── ShadowcastingFOV.cs  # Pure Algorithm
└── Common/
    ├── IDeterministicRandom.cs  # Domain Interface
    └── Result.cs                # Domain Result Type
```

#### Darklands.Core.csproj (Application & Infrastructure)
```
src/Darklands.Core/
├── Application/              # Use Cases (depends on interfaces only)
│   ├── World/
│   │   ├── Commands/
│   │   └── Queries/
│   ├── Characters/
│   │   ├── Commands/
│   │   └── Handlers/
│   └── Combat/
│       └── Handlers/
└── Infrastructure/           # External Concerns (implements interfaces)
    ├── Persistence/
    ├── Random/
    └── Services/
```

**Internal Boundary Enforcement**: Within `Darklands.Core`, we maintain Clean Architecture principles through Dependency Inversion:
- Application layer defines interfaces (e.g., `IGridRepository`, `IDeterministicRandom`)
- Infrastructure layer provides implementations
- Handlers depend only on interfaces, never concrete implementations
- This is enforced through code review and namespace conventions

#### Darklands.Presentation.csproj (MVP Layer - NEW)
```
src/Darklands.Presentation/
├── Views/                    # View Interfaces
│   ├── IActorView.cs
│   ├── IGridView.cs
│   └── IGameView.cs
└── Presenters/              # Presenters (orchestrate between Views and Core)
    ├── World/
    │   └── GridPresenter.cs
    ├── Characters/
    │   └── ActorPresenter.cs
    └── Combat/
        └── CombatPresenter.cs
```

#### Darklands.csproj (Godot Integration - Unchanged)
```
/                            # Root project folder
├── Views/                   # Godot View Implementations
│   ├── ActorView.cs        # : Node2D, IActorView
│   ├── GridView.cs         # : TileMap, IGridView
│   └── GameView.cs         # : Control, IGameView
├── Scenes/                  # Godot scenes
├── Resources/               # Godot resources
└── GameManager.cs          # Entry point

### Project Configuration

#### Darklands.Domain.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <RootNamespace>Darklands.Domain</RootNamespace>
  </PropertyGroup>

  <!-- NO project references - Domain must be pure -->

  <ItemGroup>
    <!-- Only pure .NET packages allowed -->
    <!-- LanguageExt.Core is explicitly allowed as it provides pure functional
         programming constructs (Option<T>, Fin<T>) that enhance domain modeling
         capabilities without introducing infrastructure dependencies -->
    <PackageReference Include="LanguageExt.Core" Version="4.4.9" />
  </ItemGroup>
</Project>
```

#### Darklands.Core.csproj (Modified)
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <RootNamespace>Darklands.Core</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <!-- Reference Domain project -->
    <ProjectReference Include="..\Darklands.Domain\Darklands.Domain.csproj" />
  </ItemGroup>

  <ItemGroup>
    <!-- All other packages -->
    <PackageReference Include="GodotSharp" Version="4.3.0" />
    <PackageReference Include="MediatR" Version="12.4.1" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
    <!-- etc -->
  </ItemGroup>
</Project>
```

### Migration Plan (2-3 Days Total - Realistic Estimate)

**CRITICAL**: This is a high-risk architectural refactoring affecting 662+ tests. Execute with pair programming and NO parallel development during migration.

#### Phase 1: Create Projects and References (2 hours)
1. Create `src/Darklands.Domain/Darklands.Domain.csproj` (pure domain)
2. Create `src/Darklands.Application/Darklands.Application.csproj` (renamed from Core)
3. Create `src/Darklands.Presentation/Darklands.Presentation.csproj` (MVP firewall)
4. Set up project references:
   - Domain → NONE
   - Application → Domain
   - Presentation → Domain + Application
   - Darklands.csproj → Presentation ONLY (critical firewall)
5. Update solution file and test project references
6. Verify initial build configuration

#### Phase 2: Extract and Move Domain (3-4 hours)
1. Move `src/Core/Domain/` to `src/Darklands.Domain/`
2. Update namespaces: `Darklands.Core.Domain.*` → `Darklands.Domain.*`
3. Fix hundreds of using statements across all files
4. Update all domain imports across solution
5. Resolve circular dependency issues that emerge
6. Verify build success and no external dependencies

#### Phase 3: Rename Core → Application (4-6 hours)
**Most time-consuming phase due to widespread references**
1. Rename `Darklands.Core.csproj` → `Darklands.Application.csproj`
2. Update namespaces: `Darklands.Core.*` → `Darklands.Application.*`
3. Update 600+ using statements across entire codebase
4. Update assembly references in all projects
5. Update project.godot autoload references
6. Fix test project references and namespaces
7. Resolve any namespace collision issues

#### Phase 4: Extract Presentation Layer (3-4 hours)
1. Move `src/Core/Presentation/` to `src/Darklands.Presentation/`
2. Update namespaces: `Darklands.Core.Presentation.*` → `Darklands.Presentation.*`
3. Move service abstractions (IAudioService, etc.) to Presentation/Abstractions
4. Move DI configuration to Presentation/DI/ServiceConfiguration.cs
5. Update Darklands.csproj to reference Presentation only
6. Fix all View imports and presenter references

#### Phase 5: Implement Critical Clarifications (4-5 hours)
**Critical for production stability**
1. Set up UIDispatcher as Godot autoload singleton
2. Update all Views to use service locator pattern (NOT constructor injection)
3. Implement ISceneManager to enforce scope lifecycle
4. Add scene transition enforcement (block direct GetTree().ChangeScene)
5. Update all command handlers for thread-safe UI updates
6. Verify MediatR-based cross-presenter communication

#### Phase 6: Testing and Validation (3-4 hours)
1. Add NetArchTest package and create architecture tests
2. Create scene lifecycle integration tests
3. Add thread safety verification tests
4. Fix the inevitable 50-100 broken tests
5. Run full test suite multiple times
6. Performance validation and memory leak checks
7. Manual testing of scene transitions and UI updates

### Enforcement and Validation Strategy

#### 1. Compile-Time Enforcement (Project References)
```xml
<!-- Critical: Views can ONLY see Presentation layer -->
<!-- Darklands.csproj (Views) -->
<ProjectReference Include="src\Darklands.Presentation\Darklands.Presentation.csproj" />
<!-- NO reference to Application or Domain - compile-time safety -->
```

#### 2. Architecture Tests (NetArchTest)
```csharp
// tests/Architecture/DomainPurityTests.cs
[Fact]
public void Domain_Should_Have_No_External_Dependencies()
{
    var domainAssembly = typeof(Darklands.Domain.DomainMarker).Assembly;

    var result = Types.InAssembly(domainAssembly)
        .Should()
        .NotHaveDependencyOnAny(
            "Darklands.Application",
            "Darklands.Presentation",
            "GodotSharp",
            "System.IO",
            "System.Data")
        .GetResult();

    result.IsSuccessful.Should().BeTrue("Domain must remain pure");
}

// tests/Architecture/MVPEnforcementTests.cs
[Fact]
public void Views_Should_Only_Resolve_Presenter_Interfaces()
{
    var viewTypes = typeof(GameManager).Assembly
        .GetTypes()
        .Where(t => t.IsSubclassOf(typeof(Node)) && t.Name.EndsWith("View"));

    foreach (var viewType in viewTypes)
    {
        var methods = viewType.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

        // Check GetService<T> calls resolve only IPresenter types
        // Implementation scans method body for GetService calls
        // Ensures T always implements IPresenter interface
    }
}

// tests/Architecture/LayerBoundaryTests.cs
[Fact]
public void Application_Should_Not_Reference_Infrastructure_Implementations()
{
    var result = Types.InNamespace("Darklands.Application.Application")
        .Should()
        .NotHaveDependencyOn("Darklands.Application.Infrastructure")
        .GetResult();

    result.IsSuccessful.Should().BeTrue(
        "Application layer should depend on interfaces only");
}
```

#### 3. Review-Time Enforcement
- **Pull Request Checks**: Scan for GetService<IMediator>() in Views
- **Naming Conventions**: All View dependencies must end with "Presenter"
- **Project Reference Audits**: Ensure firewall integrity maintained

## Consequences

### Positive
- **Domain Purity Enforced**: Compile-time prevention of infrastructure leakage
- **MVP Pattern Guaranteed**: Views CANNOT access Core services directly (architectural firewall)
- **ADR Consistency**: Aligns perfectly with ADR-006, ADR-010, and ADR-018
- **Selective Complexity**: Only adds boundaries where they provide architectural value
- **Future-Proof**: Can evolve without breaking existing patterns
- **Clear Responsibilities**: Each project has single, well-defined purpose
- **Testable Architecture**: NetArchTest prevents architectural decay
- **Battle-Tested Pattern**: Proven approach from enterprise applications

### Negative
- **Navigation Complexity**: 4 projects require more mental overhead than 2
- **Build Dependencies**: Must build in correct order (Domain → Application → Presentation)
- **Import Updates**: One-time migration cost for namespace changes
- **Project Management**: Slightly more complex solution structure
- **Debug Navigation**: Stepping through code crosses project boundaries
- **Learning Curve**: Developers must understand MVP pattern and project boundaries

### Neutral
- **Runtime Performance**: No impact on game performance
- **Godot Integration**: No changes to Godot development workflow
- **Development Tools**: Standard .NET project structure, works with all IDEs
- **CI/CD Impact**: Minimal changes to build pipeline

## Alternatives Considered

### Alternative 1: Keep Everything in Darklands.Core (Status Quo)
- ✅ Simplest possible structure
- ❌ No compile-time enforcement of domain purity
- ❌ Views can bypass MVP pattern and access Core directly
- ❌ Easy to accidentally violate Clean Architecture
- **Rejected**: Doesn't solve the architectural problems we face

### Alternative 2: 3-Project Structure (Chinese Review Suggestion)
- ✅ Domain purity enforced (Domain separate)
- ✅ Application/Infrastructure consolidated with NetArchTest
- ✅ Fewer projects to manage than 4-project approach
- ❌ **CRITICAL**: Views can access Commands/Repositories directly
- ❌ Breaks MVP pattern compile-time enforcement (ADR-010/ADR-018)
- ❌ NetArchTest cannot prevent compile-time violations
- **Rejected**: Creates architectural vulnerability that defeats MVP pattern

### Alternative 3: Full Layer Separation (Original ADR-019)
- ✅ Complete compile-time enforcement for all boundaries
- ❌ Over-engineered (10+ projects)
- ❌ Weeks of migration effort
- ❌ Requires extensive tooling and complex build system
- **Rejected**: Over-engineering for our current needs

### Alternative 4: Feature-Based Projects (Vertical Slice Projects)
- ✅ Team ownership boundaries
- ✅ Domain-driven organization
- ❌ Not needed for small team
- ❌ Complex inter-feature communication
- ❌ Many projects to manage (one per feature)
- **Rejected**: Wrong abstraction level for current team size

## Decision Rationale

The 4-project structure provides optimal balance of architectural safety and complexity:

### 1. **Critical Boundary Protection**
- **Domain Purity**: Compile-time guarantee of no infrastructure leakage
- **MVP Firewall**: Views CANNOT bypass Presenters to access Core services

### 2. **ADR Consistency**
- **ADR-006**: Maintains selective abstraction boundaries
- **ADR-010**: Enforces UI event bus pattern through MVP
- **ADR-018**: Supports DI lifecycle management through proper layering

### 3. **Selective Complexity**
- Only adds project boundaries where they provide architectural value
- Each additional project solves a specific, measurable problem
- No "just in case" abstractions

### 4. **Future Evolution**
- Can add more separation later if team grows
- Can consolidate if needs change
- Provides foundation for scaling patterns

### 5. **Battle-Tested Approach**
- Proven pattern from enterprise applications
- Balances pragmatism with architectural rigor
- Prevents common architectural decay patterns

**Core Insight**: The 4th project isn't over-engineering—it's the architectural firewall that prevents MVP pattern violations. This small additional complexity prevents major architectural problems.

## Implementation Checklist

### Phase 1: Project Setup
- [ ] Create `src/Darklands.Domain/Darklands.Domain.csproj`
- [ ] Create `src/Darklands.Application/Darklands.Application.csproj`
- [ ] Create `src/Darklands.Presentation/Darklands.Presentation.csproj`
- [ ] Set up project references (Domain→NONE, Application→Domain, Presentation→Domain+Application)
- [ ] Update Darklands.csproj to reference ONLY Presentation

### Phase 2: Domain Extraction
- [ ] Move `src/Core/Domain/` to `src/Darklands.Domain/`
- [ ] Update namespaces: `Darklands.Core.Domain.*` → `Darklands.Domain.*`
- [ ] Update all domain imports across solution
- [ ] Verify zero external dependencies in Domain

### Phase 3: Core Rename
- [ ] Rename `Darklands.Core.csproj` → `Darklands.Application.csproj`
- [ ] Update namespaces: `Darklands.Core.*` → `Darklands.Application.*`
- [ ] Update all assembly references and using statements
- [ ] Update project.godot references

### Phase 4: Presentation Extraction
- [ ] Move `src/Core/Presentation/` to `src/Darklands.Presentation/`
- [ ] Move service abstractions to Presentation/Abstractions/
- [ ] Move DI configuration to Presentation/DI/ServiceConfiguration.cs
- [ ] Update Godot Views to reference only Presentation

### Phase 5: Testing and Validation
- [ ] Add NetArchTest package to test project
- [ ] Create architecture tests for all boundaries
- [ ] Run complete build (should succeed without warnings)
- [ ] Run all 662+ tests (should pass)
- [ ] Verify MVP firewall: Views can only resolve Presenters

## Success Metrics

### Core Architecture
- **Compile-Time Safety**: Views cannot see Application/Domain types
- **Domain Purity**: Zero external dependencies in Domain project
- **Test Suite**: All existing tests pass without modification
- **Build Success**: Clean build with zero warnings
- **MVP Enforcement**: Views can ONLY resolve Presenter interfaces

### Implementation Clarifications
- **Thread Safety**: UIDispatcher correctly marshals all UI updates to main thread
- **Scope Lifecycle**: Scene transitions properly dispose and recreate scoped services
- **Cross-Presenter Communication**: No direct presenter-to-presenter references (MediatR only)
- **Constructor Injection**: All Views use constructor injection instead of service locator
- **Architecture Tests**: Simplified constructor parameter validation passes
- **Scene Scope Testing**: Memory profiler shows proper scope disposal on scene changes

### Integration Validation
- **ADR Consistency**: No conflicts with ADR-006, ADR-010, ADR-018
- **Thread Crash Prevention**: No random crashes during scene transitions or background operations
- **Memory Leak Prevention**: Scoped services properly disposed, no memory growth over time
- **Performance**: Scene transition times remain acceptable (<2 seconds for major scenes)

## Critical Updates from Review

**Based on ADR-018 alignment and architectural review, the following corrections were made**:

1. **Service Locator Pattern Required**: Constructor injection is impossible in Godot. Views MUST use service locator in `_Ready()`.
2. **UIDispatcher as Singleton**: Must be Godot autoload, registered in DI container.
3. **Scene Management Enforcement**: Direct `GetTree().ChangeScene*` is forbidden to maintain scope lifecycle.
4. **Realistic Timeline**: 2-3 days for migration, not 6 hours.
5. **Property Injection for Testing**: Test views can use property injection as alternative.

## References

### Related ADRs
- **ADR-006**: Selective Abstraction Strategy - Service abstraction decisions
- **ADR-010**: UI Event Bus Architecture - MVP pattern enforcement requirements
- **ADR-018**: Godot-Aligned DI Lifecycle - Dependency injection integration (CRITICAL ALIGNMENT)
- **ADR-019**: Over-engineered project separation (rejected for complexity)
- **ADR-020**: Feature-based namespace organization (complementary)

### Related Technical Debt
- **TD_042**: Architectural simplification initiative (context)
- **TD_046**: Clean Architecture migration implementation (this ADR)

### External References
- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Onion Architecture](https://jeffreypalermo.com/2008/07/the-onion-architecture-part-1/)
- [MVP Pattern in Game Development](https://unity.com/how-to/architect-game-code-scriptable-objects)
- [NetArchTest Documentation](https://github.com/BenMorris/NetArchTest)