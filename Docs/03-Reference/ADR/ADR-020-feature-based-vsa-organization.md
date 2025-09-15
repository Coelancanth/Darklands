# ADR-020: Feature-Based VSA Organization

## Status
**Status**: Accepted
**Date**: 2025-09-15
**Decision Makers**: Tech Lead, Dev Engineer, DevOps Engineer
**Replaces**: ADR-015 (rejected as over-engineered)

## Context

### The Problem
- Namespace-class collisions (`Domain.Grid.Grid`, `Domain.Actor.Actor`) force verbose code
- Current organization doesn't align with Vertical Slice Architecture
- Need intuitive structure without DDD complexity

### Post-TD_042 Landscape
After removing over-engineered DDD patterns, we need organization that is:
- Simple and intuitive
- Aligned with VSA principles
- Free from architectural astronautics
- Based on concrete game concepts

## Decision

Adopt **Feature-Based Organization** aligned with Vertical Slice Architecture:

### Core Features (Game Concepts)
- **World**: Grid, tiles, terrain, spatial concepts
- **Characters**: Actors, health, states, identities
- **Combat**: Actions, damage, turn mechanics
- **Vision**: Sight, fog of war, visibility

### Namespace Structure

```csharp
// Domain Layer - Pure game logic
Darklands.Core.Domain.World
Darklands.Core.Domain.Characters
Darklands.Core.Domain.Combat
Darklands.Core.Domain.Vision

// Application Layer - Feature handlers
Darklands.Core.Application.World.Commands
Darklands.Core.Application.Characters.Queries
Darklands.Core.Application.Combat.Handlers
Darklands.Core.Application.Vision.Services

// Infrastructure Layer - Technical implementations
Darklands.Core.Infrastructure.World
Darklands.Core.Infrastructure.Characters
Darklands.Core.Infrastructure.Combat
Darklands.Core.Infrastructure.Vision

// Presentation Layer - UI/Views
Darklands.Core.Presentation.World
Darklands.Core.Presentation.Characters
Darklands.Core.Presentation.Combat
Darklands.Core.Presentation.Vision
```

## Consequences

### Positive
- **No collisions**: `World.Grid` and `Characters.Actor` are unique
- **Intuitive**: Game developers immediately understand the organization
- **VSA aligned**: Features slice vertically through all layers
- **Clean Architecture maintained**: Layer boundaries remain strict
- **Simple**: 4 clear features, not complex bounded contexts

### Negative
- **Migration effort**: 4 hours to reorganize (but one-time cost)
- **Subjective boundaries**: Some types could belong to multiple features
- **Git history**: One large refactoring commit

### Neutral
- Import statements change but remain simple
- Folder structure reflects namespace structure
- No impact on runtime performance

## Implementation

See TD_044 for detailed migration plan. Key phases:
1. Domain layer migration (1.5h)
2. Application layer alignment (1h)
3. Infrastructure layer alignment (1h)
4. Presentation layer alignment (30min)

## Alternatives Considered

### Alternative 1: Simple Renames (TD_032 original)
Just rename `Grid` → `WorldGrid`, `Actor` → `ActorEntity`
- ✅ Minimal change (30 minutes)
- ❌ Doesn't improve organization
- ❌ Misses VSA alignment opportunity

### Alternative 2: DDD Bounded Contexts (ADR-015)
Complex bounded context organization
- ❌ Over-engineered for our needs
- ❌ Reintroduces complexity we removed
- ❌ 4+ hour migration for theoretical benefits

### Alternative 3: Flat Domain
All domain types in single namespace
- ✅ Very simple
- ❌ Loses logical grouping
- ❌ 100+ types in one namespace

## Verification

Success criteria:
- [ ] No namespace-class collisions
- [ ] Consistent feature organization across all layers
- [ ] All tests pass (662 currently)
- [ ] IntelliSense provides clear suggestions
- [ ] VSA slices clearly identifiable

## References

- TD_042: Architectural simplification that removed DDD
- TD_044: Implementation plan for this migration
- ADR-001: Strict Model View Separation (layer boundaries)
- ADR-006: Selective Abstraction Strategy (pragmatic choices)
- [Vertical Slice Architecture](https://jimmybogard.com/vertical-slice-architecture/)

## Decision Log

- 2025-09-15: Accepted as replacement for over-engineered ADR-015
- Feature-based organization chosen as middle ground between flat and complex
- Aligns with VSA principles while maintaining simplicity