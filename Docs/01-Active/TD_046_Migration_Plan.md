# TD_046: MVP-Compliant Project Separation - Migration Plan

## Current State Analysis

### Existing Structure
```
src/
├── Presentation/Presenters/    # Currently in Core project
│   ├── ActorPresenter.cs
│   ├── AttackPresenter.cs
│   └── GridPresenter.cs
├── Infrastructure/DependencyInjection/
│   └── GameStrapper.cs         # DI configuration in Core
└── Domain/                      # Mixed with Application/Infrastructure
```

### Key Violations Found
1. **EventAwareNode** directly resolves IUIEventBus and ICategoryLogger
2. **Presenters** are in Core project, not separate Presentation project
3. **GameStrapper** allows any service resolution from any node
4. **No compile-time enforcement** of MVP boundaries

## Migration Strategy

### Phase 1: Create Project Structure (1 hour)

#### 1.1 Create New Projects
```bash
# Create Domain project
dotnet new classlib -n Darklands.Domain -o src/Darklands.Domain
dotnet sln add src/Darklands.Domain/Darklands.Domain.csproj

# Create Presentation project
dotnet new classlib -n Darklands.Presentation -o src/Darklands.Presentation
dotnet sln add src/Darklands.Presentation/Darklands.Presentation.csproj
```

#### 1.2 Update Project References
```xml
<!-- Darklands.Domain.csproj -->
<ItemGroup>
  <PackageReference Include="LanguageExt.Core" Version="4.4.9" />
</ItemGroup>

<!-- Darklands.Core.csproj -->
<ItemGroup>
  <ProjectReference Include="..\Darklands.Domain\Darklands.Domain.csproj" />
</ItemGroup>

<!-- Darklands.Presentation.csproj -->
<ItemGroup>
  <ProjectReference Include="..\Darklands.Domain\Darklands.Domain.csproj" />
  <ProjectReference Include="..\Darklands.Core\Darklands.Core.csproj" />
</ItemGroup>

<!-- Darklands.csproj (Root Godot) -->
<ItemGroup>
  <!-- CRITICAL: Only reference Presentation, NOT Core -->
  <ProjectReference Include="src\Darklands.Presentation\Darklands.Presentation.csproj" />
</ItemGroup>
```

### Phase 2: Move Domain Types (1.5 hours)

#### 2.1 Move Pure Domain Logic
```
FROM: src/Domain/
TO:   src/Darklands.Domain/

Files to move:
- Combat/         (AttackAction, Damage, TimeUnit, etc.)
- Common/         (ActorId, Position, etc.)
- Grid/           (Grid, Tile, TerrainType)
- Vision/         (VisionRange, ShadowcastingFOV)
- Determinism/    (IDeterministicRandom, DeterministicRandom)
- Services/       (Domain interfaces like IAudioService)
```

#### 2.2 Update Namespaces
```csharp
// OLD
namespace Darklands.Core.Domain.Combat;

// NEW
namespace Darklands.Domain.Combat;
```

### Phase 3: Extract Presentation Layer (2 hours)

#### 3.1 Move Presenters
```
FROM: src/Presentation/Presenters/
TO:   src/Darklands.Presentation/Presenters/

Files:
- ActorPresenter.cs
- AttackPresenter.cs
- GridPresenter.cs
```

#### 3.2 Create View Interfaces
```csharp
// src/Darklands.Presentation/Views/IActorView.cs
namespace Darklands.Presentation.Views;

public interface IActorView
{
    void UpdatePosition(Position position);
    void UpdateHealth(int current, int max);
    void ShowDamage(int damage);
}
```

#### 3.3 Create Base Presenter
```csharp
// src/Darklands.Presentation/Presenters/IPresenter.cs
namespace Darklands.Presentation.Presenters;

public interface IPresenter<TView> where TView : class
{
    void AttachView(TView view);
    void Initialize();
    void Dispose();
}
```

#### 3.4 Create EventAwarePresenter Base
```csharp
// src/Darklands.Presentation/Presenters/EventAwarePresenter.cs
namespace Darklands.Presentation.Presenters;

/// <summary>
/// Base presenter that handles event subscriptions for Views
/// </summary>
public abstract class EventAwarePresenter<TView> : IPresenter<TView>
    where TView : class
{
    protected readonly IUIEventBus _eventBus;
    protected readonly ICategoryLogger _logger;
    protected TView? _view;

    protected EventAwarePresenter(IUIEventBus eventBus, ICategoryLogger logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    public virtual void AttachView(TView view)
    {
        _view = view;
    }

    public virtual void Initialize()
    {
        SubscribeToEvents();
    }

    public virtual void Dispose()
    {
        _eventBus.UnsubscribeAll(this);
    }

    protected abstract void SubscribeToEvents();
}
```

### Phase 4: Fix EventAwareNode (1 hour)

#### 4.1 Create IEventAwarePresenter Interface
```csharp
// src/Darklands.Presentation/Presenters/IEventAwarePresenter.cs
namespace Darklands.Presentation.Presenters;

public interface IEventAwarePresenter : IDisposable
{
    void Initialize();
}
```

#### 4.2 Update EventAwareNode
```csharp
// Presentation/UI/EventAwareNode.cs
public abstract partial class EventAwareNode : Node2D
{
    protected IEventAwarePresenter? Presenter { get; private set; }

    public override void _Ready()
    {
        try
        {
            // Views ONLY resolve their presenter
            Presenter = ResolvePresenter();
            Presenter?.Initialize();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Failed to initialize presenter: {ex.Message}");
        }
    }

    public override void _ExitTree()
    {
        Presenter?.Dispose();
        Presenter = null;
    }

    /// <summary>
    /// Subclasses override to specify their presenter type
    /// </summary>
    protected abstract IEventAwarePresenter? ResolvePresenter();
}
```

#### 4.3 Example View Implementation
```csharp
public partial class CombatView : EventAwareNode, ICombatView
{
    protected override IEventAwarePresenter? ResolvePresenter()
    {
        return this.GetService<ICombatPresenter>();
    }

    // ICombatView implementation
    public void UpdateHealth(int current, int max)
    {
        GetNode<ProgressBar>("HealthBar").Value = (float)current / max;
    }
}
```

### Phase 5: Move DI Configuration (1.5 hours)

#### 5.1 Create ServiceConfiguration in Presentation
```csharp
// src/Darklands.Presentation/ServiceConfiguration.cs
namespace Darklands.Presentation;

public static class ServiceConfiguration
{
    public static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Presenters - The ONLY services Views can resolve
        services.AddScoped<IActorPresenter, ActorPresenter>();
        services.AddScoped<IGridPresenter, GridPresenter>();
        services.AddScoped<IAttackPresenter, AttackPresenter>();

        // Core services (for presenters to use)
        ConfigureCoreServices(services);

        return services.BuildServiceProvider();
    }

    private static void ConfigureCoreServices(IServiceCollection services)
    {
        // This method can reference Core project
        // But Views can't see these services directly
        services.AddSingleton<IMediator, Mediator>();
        services.AddSingleton<IUIEventBus, UIEventBus>();
        // etc...
    }
}
```

#### 5.2 Update GameManager Bootstrap
```csharp
// GameManager.cs
public partial class GameManager : Node
{
    public override void _Ready()
    {
        // Minimal bootstrap - use Presentation's configuration
        var serviceProvider = ServiceConfiguration.ConfigureServices();

        // Initialize ServiceLocator
        var serviceLocator = GetNode<ServiceLocator>("/root/ServiceLocator");
        serviceLocator.Initialize(serviceProvider.GetRequiredService<IScopeManager>());
    }
}
```

### Phase 6: Update All Imports (1 hour)

#### 6.1 Update Domain Imports
```csharp
// OLD
using Darklands.Core.Domain.Combat;

// NEW
using Darklands.Domain.Combat;
```

#### 6.2 Update Presentation Imports
```csharp
// OLD
using Darklands.Core.Presentation.Presenters;

// NEW
using Darklands.Presentation.Presenters;
```

## Breaking Changes

### For Existing Code

1. **All Domain imports must change**
   - Search/Replace: `Darklands.Core.Domain` → `Darklands.Domain`

2. **All Presenter imports must change**
   - Search/Replace: `Darklands.Core.Presentation` → `Darklands.Presentation`

3. **EventAwareNode subclasses must update**
   - Must implement `ResolvePresenter()` method
   - Can no longer resolve services directly

4. **GameStrapper references must update**
   - Views can't reference GameStrapper anymore
   - Use ServiceConfiguration instead

### For Tests

1. **Test project references must update**
   ```xml
   <ProjectReference Include="..\src\Darklands.Domain\Darklands.Domain.csproj" />
   <ProjectReference Include="..\src\Darklands.Presentation\Darklands.Presentation.csproj" />
   ```

2. **Mock presenters needed for View tests**

## Validation Checklist

- [ ] Domain project has NO external dependencies (except LanguageExt.Core)
- [ ] Darklands.csproj does NOT reference Core
- [ ] All Views only resolve Presenter interfaces
- [ ] ServiceConfiguration is in Presentation project
- [ ] All 673 tests still pass
- [ ] No compile errors
- [ ] Architecture tests validate boundaries

## Risk Mitigation

1. **Create branch before starting**: `git checkout -b feat/td046-mvp-separation`
2. **Commit after each phase**: Allows rollback if issues
3. **Run tests after each phase**: Catch breaks early
4. **Keep old structure temporarily**: Move, don't delete until confirmed working

## Success Criteria

1. **Compile-time MVP enforcement**: Views literally cannot reference Core services
2. **Clean project boundaries**: Domain → Core → Presentation → Godot
3. **All tests passing**: No regression in functionality
4. **No runtime errors**: Application runs as before
5. **IntelliSense improvement**: No more namespace collisions

## Estimated Timeline

- Phase 1: Create Projects (1 hour)
- Phase 2: Move Domain (1.5 hours)
- Phase 3: Extract Presentation (2 hours)
- Phase 4: Fix EventAwareNode (1 hour)
- Phase 5: Move DI Config (1.5 hours)
- Phase 6: Update Imports (1 hour)
- **Total: 8 hours** (slightly over original 7h estimate due to EventAwareNode refactoring)

## Next Steps

1. Review this plan with team
2. Create feature branch
3. Execute Phase 1
4. Validate compilation after each phase
5. Run full test suite
6. Create PR with detailed description