# ADR-001: Strict Model-View Separation

**Status**: Approved
**Date**: 2025-08-29
**Decision Makers**: Product Owner (proposed), Tech Lead (approved 2025-08-28)
**Updated**: 2025-09-16 - Updated for ADR-021 MVP enforcement with project separation

## Context

Darklands requires extensive modding support from day one. Modders should be able to:
- Add new items, enemies, skills without touching Godot
- Create new game mechanics in pure C#
- Test their mods without running Godot

## Decision

Enforce STRICT separation between game logic and presentation using MVP (Model-View-Presenter) pattern:

### Architecture Layers (Updated per ADR-021)

```
src/
├── Darklands.Domain.csproj     (Pure business logic - NO dependencies)
│   ├── World/                  (Grid, Tile, Position)
│   ├── Characters/             (Actor, Health, ActorState)
│   ├── Combat/                 (Damage, TimeUnit, AttackAction)
│   └── Vision/                 (VisionRange, ShadowcastingFOV)
│
├── Darklands.Core.csproj       (Application & Infrastructure)
│   ├── Application/            (Commands, Queries, Handlers - MediatR)
│   └── Infrastructure/         (Services, repositories)
│
└── Darklands.Presentation.csproj (MVP Presenters & View Interfaces)
    ├── Views/                  (View interfaces: ICombatView, IGridView)
    ├── Presenters/             (MVP Presenters - orchestrate logic)
    └── ServiceConfiguration.cs (DI composition root)

tests/
└── Darklands.Core.Tests.csproj (Unit tests for all projects)

Darklands.csproj                (Godot project - references ONLY Presentation)
├── Views/                      (Godot view implementations)
│   ├── Combat/
│   │   ├── CombatView.cs      (implements ICombatView)
│   │   └── CombatView.tscn
│   └── Grid/
│       ├── GridView.cs        (implements IGridView)
│       └── GridView.tscn
├── Scenes/                     (Composed scenes)
├── Resources/                  (Art, sounds, Godot assets)
└── ServiceLocator.cs           (DI bridge autoload)
```

### MVP Pattern Implementation

**Critical**: Presenters live in Darklands.Presentation and coordinate between Views and Core services

```csharp
// ✅ CORRECT - In Darklands.Presentation
namespace Darklands.Presentation.Presenters;

public class CombatPresenter : IPresenter<ICombatView>
{
    private readonly IMediator _mediator;
    private readonly ICombatStateService _combatState;
    private ICombatView? _view;

    public CombatPresenter(IMediator mediator, ICombatStateService combatState)
    {
        _mediator = mediator;
        _combatState = combatState;
    }

    public void AttachView(ICombatView view)
    {
        _view = view;
    }

    public void Initialize()
    {
        // Subscribe to view events
        if (_view != null)
        {
            // Handle view interactions by sending commands through MediatR
        }
    }

    public void ProcessAttack(GridCoordinates from, GridCoordinates to)
    {
        // Presenter orchestrates: View → Commands → Domain → View updates
        var command = new ExecuteAttackCommand(from, to);
        _mediator.Send(command).ContinueWith(result =>
        {
            // Update view based on result
            _view?.UpdateTimeDisplay(result.Result.TimeUnits);
        });
    }
}

// View interface in Darklands.Presentation
public interface ICombatView
{
    void UpdateTimeDisplay(int timeUnits);
    void ShowAttackAnimation();
    event System.Action<GridCoordinates, GridCoordinates> AttackRequested;
}

// ✅ CORRECT - In Darklands.csproj (Godot project)
namespace Darklands.Views.Combat;

public partial class CombatView : Control, ICombatView
{
    private ICombatPresenter? _presenter;

    public override void _Ready()
    {
        // View ONLY resolves its presenter
        _presenter = this.GetService<ICombatPresenter>();
        _presenter.AttachView(this);
        _presenter.Initialize();
    }

    // ICombatView implementation - pure UI updates
    public void UpdateTimeDisplay(int timeUnits)
    {
        GetNode<Label>("TimeLabel").Text = $"Time: {timeUnits}";
    }

    public void ShowAttackAnimation()
    {
        GetNode<AnimationPlayer>("AttackAnimation").Play("attack");
    }

    // UI events delegate to presenter
    public event System.Action<GridCoordinates, GridCoordinates>? AttackRequested;

    private void _on_AttackButton_pressed()
    {
        var from = GetSelectedFrom();
        var to = GetSelectedTo();
        AttackRequested?.Invoke(from, to);
    }
}
```

### Enforcement via Project Separation (Per ADR-021)

```xml
<!-- src/Darklands.Domain/Darklands.Domain.csproj -->
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
    <PackageReference Include="LanguageExt.Core" Version="4.4.9" />
  </ItemGroup>
</Project>

<!-- src/Darklands.Core/Darklands.Core.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <RootNamespace>Darklands.Core</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Darklands.Domain\Darklands.Domain.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="LanguageExt.Core" Version="4.4.9" />
    <PackageReference Include="MediatR" Version="12.4.1" />
  </ItemGroup>
</Project>

<!-- src/Darklands.Presentation/Darklands.Presentation.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <RootNamespace>Darklands.Presentation</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Darklands.Domain\Darklands.Domain.csproj" />
    <ProjectReference Include="..\Darklands.Core\Darklands.Core.csproj" />
  </ItemGroup>
</Project>

<!-- Darklands.csproj (Root Godot Project) -->
<Project Sdk="Godot.NET.Sdk/4.3.0">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <EnableDynamicLoading>true</EnableDynamicLoading>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
  <ItemGroup>
    <!-- CRITICAL: Only reference Presentation, NOT Core -->
    <ProjectReference Include="src\Darklands.Presentation\Darklands.Presentation.csproj" />
  </ItemGroup>
  <ItemGroup>
    <!-- Exclude src folder from Godot compilation -->
    <Compile Remove="src\**" />
    <EmbeddedResource Remove="src\**" />
    <None Remove="src\**" />
  </ItemGroup>
</Project>

<!-- tests/Darklands.Core.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\src\Darklands.Core.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="FluentAssertions" Version="8.6.0" />
    <PackageReference Include="FsCheck.Xunit" Version="3.3.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
  </ItemGroup>
</Project>

<!-- CRITICAL: Husky.NET auto-installation in src/Darklands.Core.csproj -->
<Target Name="husky" BeforeTargets="Restore;CollectPackageReferences" Condition="'$(HUSKY)' != '0'">
  <Exec Command="dotnet tool restore" StandardOutputImportance="Low" StandardErrorImportance="High" />
  <Exec Command="dotnet husky install" StandardOutputImportance="Low" StandardErrorImportance="High" WorkingDirectory=".." />
</Target>
```

## CI/CD Strategy

### What Gets Tested in CI
- **GitHub Actions**: Only tests `Darklands.Core.csproj` (pure C# tests)
- **No Godot in CI**: Integration tests run locally only
- **Fast feedback**: Core tests run in seconds, not minutes

### Why This Works
1. **Core logic is 80% of bugs** - Most issues are in game logic, not rendering
2. **Modders trust CI** - They see green checkmarks for Core logic
3. **Fast iteration** - PRs get feedback in <30 seconds
4. **No Godot setup pain** - No export templates, no cache issues

## Consequences

### Positive
- Mods can be pure C# DLLs without Godot dependency
- Core game logic is unit testable in CI without Godot
- MVP pattern makes UI changes independent of logic
- Clean architecture enforced by build system
- Can switch from Godot to another engine if needed
- CI runs in seconds, not minutes

### Negative  
- More initial setup complexity (three projects)
- Presenter/View separation requires discipline
- Must pass data between layers (no direct Godot node access)
- Some boilerplate for view interfaces

## Alternatives Considered

1. **Mixed Architecture** - Rejected: Makes modding require Godot knowledge
2. **Scripting Language** - Rejected: C# mods are more powerful
3. **Single Project** - Rejected: Can't enforce separation

## Implementation Notes

### Critical Setup Steps (VERIFIED from Proven Production Code)

1. **Create folder structure first**:
   ```
   /src/Darklands.Core.csproj
   /tests/Darklands.Core.Tests.csproj  
   /Darklands.csproj (root - Godot project)
   ```

2. **MANDATORY .csproj configurations (copy from proven architecture)**:
   - `<Compile Remove="src\**" />` in Godot project prevents dual compilation
   - `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` enforces quality
   - `<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>` consistent style
   - `<Nullable>enable</Nullable>` prevents null reference bugs
   - `<ValidateOnBuild>true</ValidateOnBuild>` in DI container setup

3. **REQUIRED Tech Stack (proven architecture)**:
   - **Microsoft.Extensions.DependencyInjection** (9.0.8): Full DI container
   - **Microsoft.Extensions.Logging** abstractions in Core; Serilog provider in host: Fallback-safe logging that never crashes
   - **LanguageExt.Core** (4.4.9): Fin<T>, Option<T> for error handling
   - **MediatR** (13.0.0): Command/query/notification pipeline
   - **FsCheck.Xunit** (3.3.0): Property-based testing for time-units
   - **FluentAssertions** (8.6.0): Readable test assertions
   - **Husky.NET**: Git hooks for safety (auto-installs, prevents errors)

### Build Commands

```bash
# Build just the core (fast, no Godot)
dotnet build src/Darklands.Core.csproj

# Run tests without Godot
dotnet test tests/Darklands.Core.Tests.csproj

# Build everything (slower, includes Godot)
dotnet build Darklands.csproj
```

**For Tech Lead**: This setup allows modders to reference only `Darklands.Core.dll` without needing Godot installed

### Git Hook Setup (CRITICAL for Safety)

1. **Copy .husky folder from proven setup** with all hooks
2. **Add Husky.NET to .config/dotnet-tools.json**:
```json
{
  "version": 1,
  "isRoot": true,
  "tools": {
    "husky": {
      "version": "0.7.1",
      "commands": ["husky"]
    }
  }
}
```
3. **Hooks auto-install on first build** via Target in .csproj

## Tech Lead Review (2025-08-28)

### ✅ APPROVED - Architecture is sound and appropriate

**Technical Assessment**:
- **Feasibility**: Standard .NET multi-project solution (Complexity: 3/10)
- **Modding**: Genuine support for pure C# mods without Godot (Score: 9/10)  
- **CI Speed**: 15-30 seconds vs 3-5 minutes with Godot (Score: 10/10)
- **Not Over-engineered**: Justified by explicit modding requirement and time-unit complexity
- **MVP Pattern**: Clean separation with minor gaps to address (Score: 7/10)

### CRITICAL Implementation Requirements (from Proven Analysis)

**MUST HAVE from Day 1**:

1. **Full DI Container** (not just MediatR):
   - Use Microsoft.Extensions.DependencyInjection
   - GameStrapper.cs pattern with ValidateOnBuild = true
   - Service lifetimes: Singleton for state, Transient for handlers
   
2. **Serilog with Fallback Safety**:
   - Never crash app if logging fails
   - Category-based log levels (Commands, Queries, Combat, etc.)
   - LogCategory.cs for well-defined contexts

3. **ADR-006 Phased Implementation**:
   - Phase 1: Domain → Phase 2: Application → Phase 3: Infrastructure → Phase 4: Presentation
   - Hard gates between phases (tests must be GREEN)
   - Commit messages: `feat(x): description [Phase X/4]`

4. **Error Handling Pattern**:
   - LanguageExt's Fin<T> for all operations that can fail
   - No exceptions crossing boundaries
   - Result pattern for Godot→Core communication

**First Implementation Steps**:
1. Create 3-project structure with exact .csproj configurations
2. Add core dependencies (LanguageExt, MediatR, xUnit)
3. Implement "walking skeleton" feature to validate pattern

**Success Metrics**:
- Core tests complete in <20 seconds
- Zero Godot references in Darklands.Core.csproj
- Modders can reference only Core.dll

### Reviewer Addendum (2025-09-08)
- Standardize logging on `Microsoft.Extensions.Logging` within Core. Configure Serilog as the logging provider in the Godot host project.
- Provide a synchronous command bus adapter for any async MediatR handlers to avoid deadlocks (see ADR-009). Presenters should call the adapter, not `.GetAwaiter().GetResult()` directly.
- Add architecture tests (see ADR-006 examples) to enforce: no `Godot.*` references in Core/Application; no references from Core/Application to Presentation assemblies.

## Status

✅ **APPROVED BY TECH LEAD** - Ready for implementation. Product Owner can now create VS_001 for initial setup.