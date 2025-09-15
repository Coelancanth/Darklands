# ADR-019: .csproj-Enforced Clean Architecture

**Status**: Proposed (Conditional Approval - See Conditions)
**Date**: 2025-09-15
**Decision Makers**: Tech Lead, Dev Engineer
**Last Updated**: 2025-09-15 (Post-Review Revision)

## Context

Our codebase follows Clean Architecture principles, but architectural violations can still creep in through code reviews. Currently, dependencies between layers are enforced only through convention and manual review. This creates risk of architectural decay over time.

We need compile-time enforcement of architectural boundaries to make violations impossible rather than merely discouraged.

## Decision

We will restructure our solution into separate .csproj files that enforce Clean Architecture dependency rules through project references. This makes architectural violations compile-time errors.

### Key Clarifications (Post-Review)

1. **Feature Slices are SINGLE projects**: Each feature is one .csproj, not multiple layers
2. **Features reference ONLY Application and Domain**: Never Infrastructure or other Features
3. **Migration requires analysis first**: Must quantify violations before estimating effort
4. **Developer tooling is mandatory**: Templates must exist before migration starts
5. **Gradual migration allowed**: If violations are extensive (>50), phase the migration

### Project Structure

```
Darklands.sln
├── Core Layer Projects
│   ├── Darklands.Domain.csproj           (No dependencies)
│   ├── Darklands.Application.csproj      (References: Domain)
│   ├── Darklands.Infrastructure.csproj   (References: Application, Domain)
│   └── Darklands.Presentation.csproj     (OPTIONAL - See "Presentation Layer Clarification")
│
├── Feature Slice Projects (VSA) - Each is a SINGLE project
│   ├── Darklands.Features.Block.csproj   (References: Application, Domain ONLY)
│   ├── Darklands.Features.Grid.csproj    (References: Application, Domain ONLY)
│   └── Darklands.Features.Combat.csproj  (References: Application, Domain ONLY)
│
├── Cross-Cutting Projects
│   ├── Darklands.SharedKernel.csproj     (No dependencies)
│   └── Darklands.Common.csproj           (References: SharedKernel only)
│
├── Test Projects
│   ├── Darklands.Domain.Tests.csproj
│   ├── Darklands.Application.Tests.csproj
│   ├── Darklands.Infrastructure.Tests.csproj
│   └── Darklands.Architecture.Tests.csproj
│
└── Main Godot Project
    └── Darklands.csproj                   (References: All necessary projects)
```

### Feature Slice Structure Clarification

**CRITICAL**: Each Feature Slice is a **single .csproj project**, not a mini Clean Architecture.

#### What a Feature Slice IS:
- A single project containing all code for one vertical slice
- Internally organized by responsibility (Commands, Handlers, Presenters)
- References ONLY Domain and Application from Core layers
- Cannot reference Infrastructure or other Feature Slices

#### What a Feature Slice IS NOT:
- NOT a replica of the 4-layer architecture internally
- NOT multiple projects per feature
- NOT allowed to directly use Infrastructure implementations

#### Correct Feature Slice Project Structure:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <!-- Feature Slices can ONLY reference these two -->
    <ProjectReference Include="..\..\Core\Domain\Darklands.Domain.csproj" />
    <ProjectReference Include="..\..\Core\Application\Darklands.Application.csproj" />
    <!-- NEVER reference Infrastructure or Presentation -->
    <!-- NEVER reference other Feature projects -->
  </ItemGroup>
</Project>
```

#### Internal Organization (Folders, not projects):
```
src/Features/Darklands.Features.Block/
├── Darklands.Features.Block.csproj   (The ONLY project file)
├── Commands/                          (Application commands)
├── Handlers/                          (Command handlers)
├── Models/                           (Feature-specific domain models)
├── Services/                         (Feature-specific services)
└── Presenters/                       (MVP presenters)
```

### Presentation Layer Clarification

#### Option A: No Shared Presentation Project (RECOMMENDED)
**Decision**: Remove `Darklands.Presentation.csproj` entirely. Each Feature contains its own presenters.

**Rationale**:
- Presenters are feature-specific by nature
- No shared presentation logic needed across features
- Simpler project structure
- Prevents "dumping ground" anti-pattern

**Implementation**:
```
src/Features/Darklands.Features.Block/
└── Presenters/
    ├── BlockPresenter.cs      (Feature-specific presenter)
    └── IBlockView.cs          (View interface)
```

#### Option B: Minimal Shared Presentation (IF NEEDED)
Keep `Darklands.Presentation.csproj` ONLY if you have:
- Shared presenter base classes
- Common view interfaces
- Shared UI value objects (e.g., `Color`, `UIPosition`)

**Contents if kept**:
```csharp
// Darklands.Presentation/PresenterBase.cs
public abstract class PresenterBase<TView> where TView : class
{
    protected IMediator Mediator { get; }
    // Minimal shared logic only
}

// Darklands.Presentation/ValueObjects/UIColor.cs
public record UIColor(byte R, byte G, byte B, byte A);
```

**Rule**: If less than 5 shared types, choose Option A

### Dependency Rules Enforced by Project References

#### Domain Layer (Darklands.Domain.csproj)
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <!-- NO project references - Domain is pure -->

  <ItemGroup>
    <!-- Only pure .NET packages allowed -->
    <PackageReference Include="LanguageExt.Core" Version="4.4.9" />
  </ItemGroup>
</Project>
```

#### Application Layer (Darklands.Application.csproj)
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <!-- Can only reference Domain -->
    <ProjectReference Include="..\Domain\Darklands.Domain.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="MediatR" Version="12.4.1" />
    <PackageReference Include="FluentValidation" Version="11.10.0" />
  </ItemGroup>
</Project>
```

#### Infrastructure Layer (Darklands.Infrastructure.csproj)
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <!-- Can reference Application and Domain -->
    <ProjectReference Include="..\Domain\Darklands.Domain.csproj" />
    <ProjectReference Include="..\Application\Darklands.Application.csproj" />
  </ItemGroup>

  <ItemGroup>
    <!-- Infrastructure-specific packages -->
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
    <PackageReference Include="Serilog" Version="4.0.2" />
  </ItemGroup>
</Project>
```

#### Presentation Layer (Darklands.Presentation.csproj)
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <!-- Can reference Application and Domain, NOT Infrastructure -->
    <ProjectReference Include="..\Domain\Darklands.Domain.csproj" />
    <ProjectReference Include="..\Application\Darklands.Application.csproj" />
  </ItemGroup>

  <ItemGroup>
    <!-- Godot packages for MVP presenters -->
    <PackageReference Include="GodotSharp" Version="4.3.0" />
  </ItemGroup>
</Project>
```

### Migration Strategy

#### Pre-Migration Analysis (REQUIRED)

##### Step 1: Quantitative Analysis
```bash
# Run architecture tests to identify violations
dotnet test --filter "FullyQualifiedName~Architecture"

# Generate dependency report
dotnet list package --include-transitive > dependency-report.txt

# Scan for cross-layer references
grep -r "using.*Infrastructure" src/Domain/ src/Application/
grep -r "using.*Presentation" src/Domain/ src/Application/ src/Infrastructure/
```

##### Step 2: Qualitative Violation Classification
Classify each violation by complexity level:

**Level 1 (Simple) - 5 minutes each**:
- Wrong `using` statement
- Misplaced file in wrong folder
- Simple type in wrong layer

Example:
```csharp
// In Domain layer - WRONG
using Darklands.Infrastructure.Logging; // L1: Just remove
```

**Level 2 (Medium) - 30 minutes each**:
- Requires interface extraction
- Move class to different layer
- Add abstraction for external dependency

Example:
```csharp
// In Application layer - WRONG
public class Handler {
    private readonly FileLogger _logger; // L2: Extract ILogger interface
}
```

**Level 3 (Complex) - 2-4 hours each**:
- Deep coupling requiring redesign
- Circular dependencies
- Business logic mixed with infrastructure

Example:
```csharp
// In Domain entity - WRONG
public class Actor {
    public void Save() {
        File.WriteAllText(...); // L3: Major refactor needed
    }
}
```

##### Step 3: Effort Estimation Formula
```
Total Hours = (L1_count × 0.1) + (L2_count × 0.5) + (L3_count × 3)

If Total Hours > 40: Consider phased migration
If Total Hours > 80: Requires dedicated refactoring sprint
```

##### Step 4: Migration Risk Matrix
| Violation Count | L1 Only | Mix L1/L2 | Has L3 |
|----------------|---------|-----------|---------|
| < 10           | Low Risk | Low Risk  | Medium Risk |
| 10-50          | Low Risk | Medium Risk | High Risk |
| > 50           | Medium Risk | High Risk | Critical Risk |

**Risk Mitigation**:
- **Low Risk**: Direct migration
- **Medium Risk**: Feature-by-feature migration
- **High Risk**: Create transition interfaces first
- **Critical Risk**: Consider incremental strangler pattern

#### Phase 1: Prepare and Analyze (2-5 days)
- Run violation analysis and document findings
- Estimate refactoring complexity
- Create violation fix plan with priorities
- Set up project templates (see Tooling section)

#### Phase 2: Create Project Structure (1 day)
- Create new .csproj files following structure
- Set up Directory.Build.props
- Configure shared properties
- Do NOT move code yet

#### Phase 3: Migrate Core Layers (3-5 days)
- Move Domain layer (should have zero violations)
- Move Application layer (fix interface violations)
- Move Infrastructure (fix circular dependencies)
- Move Presentation (fix direct infrastructure usage)
- Run tests after each layer migration

#### Phase 4: Migrate Feature Slices (1 day per feature)
- One feature at a time
- Fix cross-feature dependencies
- Ensure feature isolation
- Validate with architecture tests

#### Phase 5: Cleanup and Validation (1 day)
- Remove old project files
- Update CI/CD pipelines
- Run full test suite
- Update documentation

### Inter-Feature Slice Communication

**CRITICAL**: Features cannot reference each other directly, but they MUST be able to communicate.

#### Allowed Communication Patterns

##### 1. Commands/Queries via MediatR (Preferred)
Features communicate through the Application layer's MediatR pipeline:

```csharp
// In Darklands.Features.Combat
public class CombatHandler : IRequestHandler<AttackCommand, AttackResult>
{
    private readonly IMediator _mediator;

    public async Task<AttackResult> Handle(AttackCommand request, CancellationToken ct)
    {
        // Need position from Grid feature? Send a query!
        var position = await _mediator.Send(new GetActorPositionQuery(request.ActorId));

        // Process attack based on position...
        return new AttackResult();
    }
}

// In Darklands.Features.Grid
public class GetActorPositionHandler : IRequestHandler<GetActorPositionQuery, Position>
{
    // Handles the query from Combat feature
}
```

##### 2. Domain Events/Notifications (For Loose Coupling)
Features publish events that others can subscribe to:

```csharp
// In Darklands.Features.Block
public class BlockMatchedNotification : INotification
{
    public BlockMatchResult Result { get; init; }
}

public class BlockHandler : IRequestHandler<MatchBlocksCommand>
{
    public async Task Handle(MatchBlocksCommand request, CancellationToken ct)
    {
        // Process block matching...

        // Notify other features
        await _mediator.Publish(new BlockMatchedNotification { Result = result });
    }
}

// In Darklands.Features.Combat
public class BlockMatchedEffectHandler : INotificationHandler<BlockMatchedNotification>
{
    public async Task Handle(BlockMatchedNotification notification, CancellationToken ct)
    {
        // React to block match (e.g., trigger combat effects)
    }
}
```

##### 3. Shared Domain Models (In Domain Project)
Common entities and value objects live in the shared Domain project:

```csharp
// In Darklands.Domain/Entities/Actor.cs
public record Actor(ActorId Id, Health Health, Position Position);

// Both Combat and Grid features can use Actor
// But they cannot share feature-specific models
```

#### Forbidden Communication Patterns

- ❌ **Direct service references**: `GridService` cannot be injected into `CombatHandler`
- ❌ **Shared feature services**: No `ISharedFeatureService` that multiple features depend on
- ❌ **Cross-feature inheritance**: Features cannot inherit from each other's base classes
- ❌ **Static service locators**: No `ServiceLocator.Get<IGridService>()` patterns
- ❌ **God objects**: No central `GameState` that all features read/write

#### Communication Rules

1. **All communication goes through MediatR**: Single point of indirection
2. **Features are deployment-independent**: Could theoretically be microservices
3. **No compile-time coupling**: Features can be developed/tested in isolation
4. **Events are immutable**: Use records for all notifications
5. **Queries are idempotent**: Reading state never changes it

### Developer Tooling (CRITICAL for Reducing Friction)

#### Custom dotnet Templates (MUST HAVE)
Create templates to eliminate manual project setup:

```bash
# Install template
dotnet new install ./templates/darklands-feature

# Create new feature with single command
dotnet new darklands-feature --name Combat --output src/Features/

# Automatically creates:
# - Darklands.Features.Combat.csproj with correct references
# - Standard folder structure
# - Base command and handler examples
# - Presenter template
# - Architecture test for the feature
```

#### Template Implementation:
```json
// .template.config/template.json
{
  "author": "Darklands Team",
  "classifications": ["Feature", "VSA"],
  "identity": "Darklands.Feature.Template",
  "name": "Darklands Feature Slice",
  "shortName": "darklands-feature",
  "sourceName": "FeatureName",
  "symbols": {
    "name": {
      "type": "parameter",
      "replaces": "FeatureName",
      "isRequired": true
    }
  }
}
```

#### PowerShell Helper Scripts:
```powershell
# scripts/architecture/new-feature.ps1
param([string]$Name)

# Create feature project
dotnet new darklands-feature --name $Name --output "src/Features/Darklands.Features.$Name"

# Add to solution
dotnet sln add "src/Features/Darklands.Features.$Name/Darklands.Features.$Name.csproj"

# Create initial test project
dotnet new xunit --name "Darklands.Features.$Name.Tests" --output "tests/Features/$Name"

# Add test project references
dotnet add "tests/Features/$Name" reference "src/Features/Darklands.Features.$Name"

Write-Host "✅ Feature $Name created successfully!" -ForegroundColor Green
Write-Host "📁 Location: src/Features/Darklands.Features.$Name" -ForegroundColor Cyan
```

### Godot Integration Strategy (Composition Root)

**CRITICAL**: The main `Darklands.csproj` acts as the composition root, wiring Godot to our Clean Architecture.

#### DI Container Initialization

```csharp
// In Darklands.csproj - GameManager.cs (Godot Autoload Singleton)
public partial class GameManager : Node
{
    private IServiceProvider _serviceProvider;
    private IServiceScope _gameScope;

    public override void _Ready()
    {
        // Initialize DI container
        var services = new ServiceCollection();

        // Register Core services
        services.AddMediatR(cfg => {
            cfg.RegisterServicesFromAssembly(typeof(Application.ApplicationMarker).Assembly);
            // Register all feature assemblies
            cfg.RegisterServicesFromAssembly(typeof(Features.Block.BlockFeatureMarker).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(Features.Grid.GridFeatureMarker).Assembly);
        });

        // Register Infrastructure implementations
        services.AddSingleton<IGridStateService, InMemoryGridStateService>();
        services.AddScoped<IActorRepository, ActorRepository>();

        // Build provider
        _serviceProvider = services.BuildServiceProvider();
        _gameScope = _serviceProvider.CreateScope();

        // Make available to Godot nodes via ServiceLocator pattern
        ServiceLocator.Initialize(_gameScope.ServiceProvider);
    }

    public override void _ExitTree()
    {
        _gameScope?.Dispose();
        _serviceProvider?.Dispose();
    }
}
```

#### Service Access in Godot Nodes

```csharp
// ServiceLocator.cs (in Darklands.csproj)
public static class ServiceLocator
{
    private static IServiceProvider _provider;

    public static void Initialize(IServiceProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    public static T GetService<T>() where T : class
    {
        return _provider?.GetService<T>()
            ?? throw new InvalidOperationException($"Service {typeof(T).Name} not registered");
    }

    public static IMediator Mediator => GetService<IMediator>();
}

// In any Godot Node script
public partial class PlayerController : CharacterBody2D
{
    private IMediator _mediator;

    public override void _Ready()
    {
        // Get services from ServiceLocator
        _mediator = ServiceLocator.Mediator;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("attack"))
        {
            // Send command through MediatR
            _ = _mediator.Send(new AttackCommand { ActorId = PlayerId });
        }
    }
}
```

#### MVP Pattern Integration

```csharp
// Presenter base class (in Darklands.Presentation.csproj)
public abstract class PresenterBase<TView> where TView : Node
{
    protected TView View { get; }
    protected IMediator Mediator { get; }

    protected PresenterBase(TView view)
    {
        View = view;
        Mediator = ServiceLocator.Mediator;
        WireViewEvents();
    }

    protected abstract void WireViewEvents();
}

// Concrete presenter (in Feature project)
public class GridPresenter : PresenterBase<GridView>
{
    public GridPresenter(GridView view) : base(view) { }

    protected override void WireViewEvents()
    {
        // Connect Godot signals to commands
        View.TileClicked += OnTileClicked;
    }

    private async void OnTileClicked(Vector2I position)
    {
        // Transform UI event into application command
        var result = await Mediator.Send(new SelectTileCommand { Position = position });

        // Update view based on result
        if (result.IsSuccess)
        {
            View.HighlightTile(position);
        }
    }
}

// GridView.cs (Godot node)
public partial class GridView : Node2D
{
    [Signal]
    public delegate void TileClickedEventHandler(Vector2I position);

    private GridPresenter _presenter;

    public override void _Ready()
    {
        _presenter = new GridPresenter(this);
    }
}
```

#### Scope Management for Godot Lifecycle

```csharp
// Per-scene scope management
public partial class BattleScene : Node2D
{
    private IServiceScope _sceneScope;

    public override void _Ready()
    {
        // Create scene-specific scope
        _sceneScope = ServiceLocator.GetService<IServiceProvider>().CreateScope();

        // Scene-specific services available to children
        GetNode<BattleUI>("UI").Initialize(_sceneScope.ServiceProvider);
    }

    public override void _ExitTree()
    {
        _sceneScope?.Dispose();
    }
}
```

#### Key Integration Rules

1. **ServiceLocator is ONLY in Darklands.csproj**: Not accessible from Clean Architecture layers
2. **Presenters mediate between Godot and Application**: No direct Godot references in handlers
3. **Godot signals become MediatR commands**: Transform at the boundary
4. **Scene changes manage scopes**: Each major scene gets its own DI scope
5. **No Godot types in Application/Domain**: Keep them pure C#

### Enforcement Rules

#### Compile-Time Enforcement (Automatic)
- Domain cannot reference any other project
- Application can only reference Domain
- Infrastructure can reference Application and Domain
- Presentation can reference Application and Domain (not Infrastructure)
- Features can reference Application and Domain ONLY (not Infrastructure or other Features)

#### Analyzer Rules (Additional)
```xml
<!-- In Directory.Build.props -->
<ItemGroup>
  <PackageReference Include="NetArchTest.Rules" Version="1.3.2" />
  <PackageReference Include="ArchUnitNET" Version="0.10.6" />
</ItemGroup>
```

#### Build-Time Validation
```csharp
// In Darklands.Architecture.Tests
[Fact]
public void Domain_Should_Not_Reference_Other_Layers()
{
    var domainAssembly = typeof(Domain.DomainMarker).Assembly;

    var result = Types.InAssembly(domainAssembly)
        .Should()
        .NotHaveDependencyOn("Darklands.Application")
        .And()
        .NotHaveDependencyOn("Darklands.Infrastructure")
        .And()
        .NotHaveDependencyOn("Darklands.Presentation")
        .GetResult();

    result.IsSuccessful.Should().BeTrue();
}
```

## Benefits

### Immediate Benefits
- **Compile-time safety**: Violations become build errors
- **Clear boundaries**: Project structure mirrors architecture
- **Faster builds**: Parallel project compilation
- **Better IDE support**: IntelliSense only shows valid references
- **Easier onboarding**: Architecture is self-documenting

### Long-term Benefits
- **Prevents architectural decay**: Rules cannot be bypassed
- **Enables modular deployment**: Projects can be packaged separately
- **Supports team scaling**: Teams can own specific projects
- **Simplifies dependency management**: Each project declares its own dependencies
- **Improves testability**: Clear seams for testing

## Drawbacks

### Development Friction
- More complex solution structure
- Requires understanding of project references
- Initial setup time for new features
- Potential for circular reference issues

### Build Complexity
- Longer initial build times
- More complex CI/CD configuration
- NuGet package management across projects
- Potential version conflicts between projects

## Alternatives Considered

### 1. Convention-Only Enforcement
**Approach**: Rely on code reviews and team discipline
**Rejected because**: Violations still occur and accumulate over time

### 2. Analyzer-Only Enforcement
**Approach**: Use Roslyn analyzers without project separation
**Rejected because**: Can be suppressed or ignored, not as clear as project boundaries

### 3. Folder Structure Enforcement
**Approach**: Use folder conventions with namespace rules
**Rejected because**: Weaker enforcement, easier to violate accidentally

### 4. Single Project with Analyzers
**Approach**: Keep monolithic project but add strict analyzers
**Rejected because**: Loses benefits of parallel compilation and clear boundaries

## Conditions for Final Approval

This ADR has **conditional approval**. Before implementation, the following must be completed:

### 1. Pre-Implementation Requirements
- [ ] Run architectural violation analysis on current codebase
- [ ] Classify violations by L1/L2/L3 complexity levels
- [ ] Calculate effort using formula: (L1×0.1) + (L2×0.5) + (L3×3) hours
- [ ] Document migration risk level based on matrix
- [ ] Create detailed migration plan based on risk assessment

### 2. Tooling Requirements (MUST HAVE before Day 1)
- [ ] Create `dotnet new darklands-feature` template
- [ ] Test template creates correct project structure
- [ ] Create PowerShell helper script for feature creation
- [ ] Document template usage in developer guide

### 3. Critical Architecture Decisions
- [ ] Decide on Presentation layer (Option A or B)
- [ ] Confirm MediatR for inter-feature communication
- [ ] Document Godot integration approach in detail
- [ ] Create example of feature-to-feature communication

### 4. Documentation Updates
- [ ] Update this ADR with violation analysis results
- [ ] Document chosen Presentation layer approach
- [ ] Create migration runbook with specific commands
- [ ] Add troubleshooting guide for common issues

### 5. Risk Mitigation
- [ ] Identify features with highest L3 violation count
- [ ] Plan gradual migration if total hours > 40
- [ ] Create transition interfaces for L3 violations
- [ ] Document rollback strategy if migration blocks development

## Implementation Checklist (After Approval Conditions Met)

- [ ] Complete pre-implementation requirements above
- [ ] Create Directory.Build.props with shared configuration
- [ ] Set up project templates and tooling
- [ ] Split existing code into layer projects (phased)
- [ ] Fix dependency violations incrementally
- [ ] Update all tests to use new project structure
- [ ] Configure CI/CD for multi-project build
- [ ] Update documentation and onboarding guides
- [ ] Validate with architecture tests

## Example Implementation

### Directory.Build.props (Solution Root)
```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>

  <ItemGroup>
    <!-- Shared analyzers for all projects -->
    <PackageReference Include="StyleCop.Analyzers" Version="1.1.118" />
    <PackageReference Include="SonarAnalyzer.CSharp" Version="9.32.0.97167" />
  </ItemGroup>
</Project>
```


## Success Metrics

- Zero architectural violations in production code
- Build fails immediately on incorrect references
- New developers understand architecture from solution structure
- Feature development follows consistent patterns
- No accidental coupling between features

## References

- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Enforcing Architecture with .NET Project Dependencies](https://docs.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/architectural-principles)
- [Our ADR-001: Strict Model View Separation](./ADR-001-strict-model-view-separation.md)
- [Our ADR-002: Phased Implementation Protocol](./ADR-002-phased-implementation-protocol.md)