# ADR-015: Namespace Organization Strategy

## Status
**Status**: Proposed  
**Date**: 2025-09-11  
**Decision Makers**: Tech Lead, Dev Engineer  

## Context

### The Problem
Our codebase suffers from namespace-class naming collisions that force verbose, confusing code:
- `Domain.Grid.Grid` - Grid record in Grid namespace
- `Domain.Actor.Actor` - Actor record in Actor namespace  
- Developers must use fully qualified names like `Darklands.Core.Domain.Grid.Grid`
- This pattern emerged from "feature folder" organization without namespace guidelines

### Current State Analysis
```
src/
├── Domain/
│   ├── Actor/        → namespace Domain.Actor
│   │   └── Actor.cs  → class Actor (COLLISION!)
│   ├── Grid/         → namespace Domain.Grid
│   │   └── Grid.cs   → record Grid (COLLISION!)
│   ├── Combat/       → namespace Domain.Combat (no Combat class - OK)
│   ├── Vision/       → namespace Domain.Vision (no Vision class - OK)
│   └── Services/     → namespace Domain.Services (interfaces only - OK)
├── Application/
│   ├── Actor/        → namespace Application.Actor (commands/queries - OK)
│   ├── Grid/         → namespace Application.Grid (commands/queries - OK)
│   └── Combat/       → namespace Application.Combat (commands/queries - OK)
└── Presentation/
    └── Views/        → namespace Presentation.Views (interfaces - OK)
```

### Why This Happened
1. **Folder-First Thinking**: Created folders for features, IDE auto-generated matching namespaces
2. **DDD Misunderstanding**: Confused "organize by domain" with "folder per entity"
3. **Missing Guidelines**: No documented namespace strategy
4. **Copy-Paste Proliferation**: Pattern spread without review

### Impact on Architecture
- **Violates Clean Architecture**: Unclear boundaries between aggregates
- **Hinders VSA**: Vertical slices become harder to identify
- **Blocks Refactoring**: Changing names requires extensive updates
- **Reduces Readability**: Code reviews slower, onboarding harder

## Decision

We will adopt a **Bounded Context + Aggregate** namespace strategy that:
1. Groups related domain concepts into logical contexts
2. Eliminates namespace-class collisions
3. Aligns with our Vertical Slice Architecture
4. Maintains Clean Architecture boundaries

### New Namespace Structure

```csharp
// Domain Layer - Organized by Bounded Contexts
namespace Darklands.Core.Domain.Spatial
{
    public sealed record WorldGrid { }     // Was: Grid.Grid
    public sealed record Position { }      // Stays in spatial context
    public sealed record Tile { }          // Related to grid
    public sealed record TerrainType { }   // Spatial concept
}

namespace Darklands.Core.Domain.Entities  
{
    public sealed record Actor { }         // Was: Actor.Actor
    public sealed record ActorId { }       // Entity identifiers
    public sealed record Health { }        // Entity attributes
    public sealed record DummyActor { }    // Test entities
}

namespace Darklands.Core.Domain.TurnBased
{
    public sealed record TimeUnit { }      // Turn mechanics
    public sealed record CombatAction { }  // Action system
    public interface ISchedulable { }      // Scheduling contracts
    public static class TimeUnitCalculator { } // Turn calculations
}

namespace Darklands.Core.Domain.Perception
{
    public sealed record VisionRange { }   // Sensory mechanics
    public sealed record VisionState { }   // Visibility tracking
    public static class ShadowcastingFOV { } // FOV algorithms
}

namespace Darklands.Core.Domain.Rules
{
    public static class Movement { }       // Movement rules
    public static class AttackValidation { } // Combat rules
}

// Application Layer - Organized by Feature Slices (unchanged)
namespace Darklands.Core.Application.Grid.Commands { }    // Grid operations
namespace Darklands.Core.Application.Actor.Queries { }    // Actor queries
namespace Darklands.Core.Application.Combat.Handlers { }  // Combat handlers

// Infrastructure Layer - Organized by Technical Concerns
namespace Darklands.Core.Infrastructure.Persistence { }   // Save/Load
namespace Darklands.Core.Infrastructure.Services { }      // External services
namespace Darklands.Core.Infrastructure.Godot { }         // Godot bridges
```

### Naming Conventions

#### Domain Layer Rules
1. **Namespace = Bounded Context**: Groups related concepts
2. **No Repeated Names**: Class name never matches namespace
3. **Descriptive Names**: `WorldGrid` not `Grid`, `Actor` stays unique
4. **Static Helpers**: Use static classes for pure functions

#### Application Layer Rules  
1. **Feature.Operation**: `Grid.Commands`, `Actor.Queries`
2. **Vertical Alignment**: Matches domain contexts
3. **CQRS Structure**: Commands/Queries/Handlers sub-namespaces

#### Infrastructure Layer Rules
1. **Technical Grouping**: By infrastructure concern
2. **Bridge Pattern**: Godot adapters in `.Godot` namespace
3. **Service Suffix**: Implementation classes use Service suffix

## Consequences

### Positive
- **No More Collisions**: Eliminates `Grid.Grid`, `Actor.Actor` patterns
- **Clearer Boundaries**: Bounded contexts visible in code structure
- **Better IntelliSense**: IDE suggestions more meaningful
- **Easier Refactoring**: Less coupling between namespaces
- **VSA Alignment**: Vertical slices map cleanly to contexts

### Negative  
- **Large Refactoring**: ~50+ files need namespace updates
- **Test Updates**: All tests need new imports
- **Learning Curve**: Team needs to understand new structure
- **Git History**: One large commit for namespace changes

### Neutral
- **Import Statements**: Change but not increase in complexity
- **Documentation**: ADRs and guides need updates
- **Build Time**: No significant impact expected

## Implementation Plan

### Phase 1: Domain Layer (2 hours)
```bash
# 1. Rename Grid → WorldGrid
src/Domain/Grid/Grid.cs → src/Domain/Spatial/WorldGrid.cs

# 2. Move to new namespaces
src/Domain/Grid/* → src/Domain/Spatial/*
src/Domain/Actor/* → src/Domain/Entities/*
src/Domain/Combat/* → src/Domain/TurnBased/*
src/Domain/Vision/* → src/Domain/Perception/*

# 3. Update namespace declarations and imports
```

### Phase 2: Application Layer (1 hour)
```bash
# Update imports only - structure stays same
# Fix references to Domain.Grid.Grid → Domain.Spatial.WorldGrid
# Fix references to Domain.Actor.Actor → Domain.Entities.Actor
```

### Phase 3: Tests (1 hour)
```bash
# Update all test imports
# No logic changes required
```

### Phase 4: Validation
- Run all tests: `./scripts/core/build.ps1 test`
- Architecture fitness tests must pass
- No warnings about ambiguous references

## Alternatives Considered

### Alternative 1: Rename Classes Only
Keep namespace structure, rename classes: `Grid` → `GridState`
- ✅ Minimal changes
- ❌ Doesn't fix organizational issues
- ❌ Perpetuates folder-first thinking

### Alternative 2: Flat Domain Namespace
Put all domain classes in `Domain` namespace
- ✅ Simple, no collisions
- ❌ Loses logical grouping
- ❌ 100+ classes in one namespace

### Alternative 3: Feature Folders Throughout
Organize everything by features: `Features.Grid`, `Features.Combat`
- ✅ Aligns with VSA
- ❌ Breaks Clean Architecture layers
- ❌ Couples domain to features

## Migration Strategy

1. **Create new structure** alongside old (temporary duplication)
2. **Update one context at a time** with tests passing
3. **Single PR** for entire refactoring (atomic change)
4. **Update documentation** immediately after

## Verification

Success criteria:
- [ ] No namespace-class collisions remain
- [ ] All tests pass without warnings
- [ ] Architecture fitness tests validate structure
- [ ] IntelliSense shows clear, unique suggestions
- [ ] Code review confirms improved readability

## References

- [Clean Architecture principles](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [DDD Bounded Contexts](https://martinfowler.com/bliki/BoundedContext.html)
- [C# Namespace Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/names-of-namespaces)
- ADR-001: Strict Model View Separation (layer boundaries)
- ADR-002: Phased Implementation Protocol (affects refactoring approach)

## Decision Log

- 2025-09-11: Initial proposal by Tech Lead after identifying Grid.Grid collision