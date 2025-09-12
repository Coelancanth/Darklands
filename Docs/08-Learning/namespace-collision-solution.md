# Namespace Collision Solution - Pluralization Strategy

**Date**: 2025-09-12  
**Source**: modular-monolith-with-ddd analysis  
**Impact**: TD_032 simplified from 4h to 2h

## 🎯 The Discovery

While analyzing modular-monolith-with-ddd for DDD patterns, discovered their elegant solution to namespace-class collisions:

**Use plural folder names, singular class names**

## ❌ The Problem We Had

```csharp
namespace Domain.Grid { public class Grid } // Domain.Grid.Grid - ugly!
namespace Domain.Actor { public class Actor } // Domain.Actor.Actor - confusing!
```

This forced verbose references and made code hard to read.

## ✅ The Simple Solution

```csharp
namespace Domain.Grids { public class Grid }   // Domain.Grids.Grid - clean!
namespace Domain.Actors { public class Actor } // Domain.Actors.Actor - clear!
```

**Pattern**:
- Folder: `Actors/` (plural)
- Class: `Actor.cs` (singular)
- Namespace: `Domain.Actors`
- Reference: `Actors.Actor` (no collision!)

## 📊 Impact on Implementation

**Before** (complex reorganization):
- 4 hours of work
- New bounded contexts: Spatial, Entities, TurnBased, Perception
- Rename classes: Grid → WorldGrid
- Complex namespace restructuring

**After** (simple pluralization):
- 2 hours of work
- Just rename folders: Actor → Actors, Grid → Grids
- Update namespace declarations
- No class renames needed

## 🏗️ Implementation Steps

1. **Rename Folders** (30 min):
   - `Domain/Actor/` → `Domain/Actors/`
   - `Domain/Grid/` → `Domain/Grids/`

2. **Update Namespaces** (60 min):
   - `namespace Domain.Actor` → `namespace Domain.Actors`
   - `namespace Domain.Grid` → `namespace Domain.Grids`

3. **Update Using Statements** (30 min):
   - Find/replace all import statements
   - Verify compilation

## 🎓 Key Learning

**Sometimes the best architectural solution is the simplest one.**

The modular-monolith project showed that:
- Industry convention solves common problems
- Simple naming conventions can eliminate architectural complexity
- Don't over-engineer when a simple pattern exists

## 📝 Updated Standards

All future aggregates in Darklands will use:
- **Plural folders**: `Actors/`, `Combat/`, `Items/`
- **Singular classes**: `Actor.cs`, `CombatSession.cs`, `Item.cs`
- **Clean references**: `Actors.Actor`, `Combat.CombatSession`, `Items.Item`

This pattern is now documented in:
- DDD Feature Implementation Protocol
- ADR-017 (enhanced)
- TD_032 (revised)