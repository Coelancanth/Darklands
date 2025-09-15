# ADR-021: Minimal Project Separation for Domain Purity

## Status
**Status**: Accepted
**Date**: 2025-09-15
**Decision Makers**: Tech Lead, Dev Engineer, DevOps Engineer
**Supersedes**: ADR-019 (rejected as over-engineered)
**Complements**: ADR-020 (feature-based organization)

## Context

### Current State
We currently have:
- `Darklands.csproj` - Godot entry point (references Darklands.Core)
- `Darklands.Core.csproj` - All business logic (Domain, Application, Infrastructure, Presentation)
- `Darklands.Core.Tests.csproj` - All tests

This structure already provides separation between Godot and our business logic, but the Domain layer can still accidentally reference infrastructure concerns.

### The Problem
While namespace organization (ADR-020) solves our collision issues, we still lack compile-time enforcement of domain purity. The Domain layer should never depend on:
- Godot types
- File I/O
- Database access
- External services
- Infrastructure concerns

### Post-TD_042 Constraints
After removing DDD over-engineering, any solution must be:
- Simple and elegant
- Minimal friction
- Quick to implement (hours, not days)
- No complex tooling required

## Decision

Extract only the Domain layer into a separate project, creating a minimal three-project structure that enforces the most critical architectural boundary.

### Project Structure

```
Darklands.sln
├── Darklands.csproj              # Godot entry point & Views (unchanged)
│   └── References: Darklands.Presentation ONLY (enforces MVP pattern)
│
├── src/
│   ├── Darklands.Domain.csproj   # Pure domain logic (NEW)
│   │   └── No project references
│   │
│   ├── Darklands.Core.csproj     # Application & Infrastructure (modified)
│   │   └── References: Darklands.Domain
│   │
│   └── Darklands.Presentation.csproj  # Presenters & View Interfaces (NEW)
│       └── References: Darklands.Domain, Darklands.Core
│
└── tests/
    └── Darklands.Core.Tests.csproj
        └── References: All projects
```

**Note**: We need 4 projects total to maintain proper dependency inversion. Presenters need Core (for commands), but Core shouldn't know about Presenters.

### Dependency Injection Composition Root

**CRITICAL**: The DI composition root lives in `Darklands.Presentation.csproj`, NOT in the Godot project.

```csharp
// src/Darklands.Presentation/ServiceConfiguration.cs
public static class ServiceConfiguration
{
    public static IServiceCollection ConfigureServices(IServiceCollection services)
    {
        // Register Domain services (if any)

        // Register Application handlers
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Darklands.Core.CoreMarker).Assembly));

        // Register Infrastructure services
        services.AddSingleton<IDeterministicRandom, DeterministicRandom>();
        services.AddSingleton<IGridRepository, InMemoryGridRepository>();

        // Register Presenters
        services.AddSingleton<GridPresenter>();
        services.AddSingleton<ActorPresenter>();

        return services;
    }
}
```

The Godot entry point (`GameManager.cs`) only needs to:
1. Create the service container
2. Call `ServiceConfiguration.ConfigureServices()`
3. Resolve the root presenter

This ensures Views never directly access Core services, maintaining MVP integrity.

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

### Migration Plan (3 Hours Total)

#### Phase 1: Create Projects (30 minutes)
1. Create `src/Darklands.Domain/Darklands.Domain.csproj`
2. Create `src/Darklands.Presentation/Darklands.Presentation.csproj`
3. Add to solution file
4. Set up project references correctly
5. Update test project references

#### Phase 2: Extract Domain (1 hour)
1. Move `src/Domain/` folder to `src/Darklands.Domain/`
2. Update namespaces from `Darklands.Core.Domain.*` to `Darklands.Domain.*`
3. Keep feature organization (World, Characters, Combat, Vision)
4. Fix all domain imports

#### Phase 3: Extract Presentation (1 hour)
1. Move `src/Presentation/` folder to `src/Darklands.Presentation/`
2. Update namespaces from `Darklands.Core.Presentation.*` to `Darklands.Presentation.*`
3. Ensure Presenters reference Core for commands/queries
4. Update Darklands.csproj to reference Presentation project

#### Phase 4: Fix Imports and Verify (30 minutes)
1. Update all `using` statements across projects
2. Run build to catch any dependency violations
3. Verify no Godot references in Core, Domain, or Presentation
4. Run all tests to ensure nothing broke

### Enforcement and Validation

#### Architecture Tests
```csharp
[Fact]
public void Domain_Should_Not_Have_External_Dependencies()
{
    var domainAssembly = typeof(Darklands.Domain.DomainMarker).Assembly;

    var result = Types.InAssembly(domainAssembly)
        .Should()
        .NotHaveDependencyOnAny(
            "Darklands.Core",
            "GodotSharp",
            "System.IO",
            "System.Data",
            "Microsoft.EntityFrameworkCore")
        .GetResult();

    result.IsSuccessful.Should().BeTrue();
}
```

**Note**: Architecture tests require maintenance as the project evolves. When adding new infrastructure dependencies, update the forbidden dependency list. Consider this test as part of the architectural documentation that must evolve with the system.

## Consequences

### Positive
- **Domain purity enforced**: Compile-time prevention of infrastructure leakage
- **Clear boundary**: Most important architectural boundary is protected
- **Simple structure**: Only 3 projects, easy to understand
- **Quick migration**: 2-hour implementation
- **No tooling required**: Standard .NET project references
- **Preserves existing structure**: Darklands.csproj remains unchanged

### Negative
- **Cognitive overhead**: Increased mental load navigating between projects during development
- **Navigation friction**: Cross-project code navigation is slower than within single project
- **One more project**: Slightly more complex solution structure
- **Import updates**: One-time cost to update using statements
- **Build order**: Must build Domain before Core
- **Debugging complexity**: Stepping through code across project boundaries

### Neutral
- Application/Infrastructure/Presentation remain in single project (acceptable trade-off)
- No impact on runtime performance
- No impact on Godot integration

## Alternatives Considered

### Alternative 1: Keep Everything in Darklands.Core
- ✅ Simplest possible structure
- ❌ No compile-time enforcement of domain purity
- ❌ Easy to accidentally violate Clean Architecture

### Alternative 2: Full Layer Separation (Original ADR-019)
- ✅ Complete compile-time enforcement
- ❌ Over-engineered (10+ projects)
- ❌ Weeks of migration effort
- ❌ Requires extensive tooling

### Alternative 3: Feature-Based Projects
- ✅ Team ownership boundaries
- ❌ Not needed for small team
- ❌ Complex inter-feature communication
- ❌ Many projects to manage

## Decision Rationale

This minimal separation provides:
1. **Maximum value**: Protects the most critical boundary (domain purity)
2. **Minimum complexity**: Only one additional project
3. **Quick win**: 2-hour implementation vs weeks
4. **Future-proof**: Can add more separation later if needed
5. **Aligned with simplification**: Post-TD_042 philosophy maintained

The Domain layer is the heart of our application. Keeping it pure is worth the minimal additional complexity of one extra project.

## Implementation Checklist

- [ ] Create Darklands.Domain project file
- [ ] Move Domain folder to new project
- [ ] Update namespaces to Darklands.Domain.*
- [ ] Update Core project to reference Domain
- [ ] Fix all using statements
- [ ] Run build and fix any violations
- [ ] Run all tests
- [ ] Add architecture test for domain purity
- [ ] Update solution documentation

## Success Metrics

- Zero external dependencies in Domain project
- All existing tests pass
- Build succeeds without warnings
- Architecture tests enforce domain purity
- No accidental Godot references in domain logic

## References

- TD_042: Architectural simplification initiative
- ADR-019: Over-engineered project separation (rejected)
- ADR-020: Feature-based namespace organization (complementary)
- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)