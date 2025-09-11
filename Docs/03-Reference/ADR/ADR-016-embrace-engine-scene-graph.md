# ADR-016: Embrace Engine Scene Graph for UI Composition

**Status**: Proposed  
**Date**: 2025-09-11  
**Decision Makers**: Tech Lead, Dev Engineer  
**Complexity**: 🔴 High (90/100) - Fundamental architectural principle  

## Context

Our current architecture attempts to maintain strict separation between all views, even when Godot's scene graph naturally handles parent-child relationships. This has led to:

1. **Split-brain architecture**: HealthView manages phantom nodes while ActorView owns the actual UI
2. **Bridge patterns**: Complex routing to coordinate naturally coupled elements  
3. **Duplicated logic**: Multiple systems tracking positions and visibility
4. **Lost engine benefits**: Not leveraging Godot's automatic transformations

The BR_003 incident revealed we have 790 lines of code in HealthView managing UI elements that don't actually exist, while the working solution (health bars as child nodes) is simple and elegant.

## Architectural Scope

**This ADR applies WITHIN Godot's presentation layer only.** It addresses how to structure UI components when operating entirely within Godot's composition root and scene tree system.

**This ADR does NOT apply to:**
- Communication between domain and presentation layers (see ADR-010)
- Bridging between DI container and Godot worlds (see ADR-010)
- Service abstractions at architectural boundaries (see ADR-006)

## The Problem

**We're fighting the engine instead of embracing it.**

When we create health bars as child nodes of actors:
- ✅ They move automatically with the parent
- ✅ They hide/show with parent visibility  
- ✅ They're destroyed when parent is freed
- ✅ Transform inheritance handles scaling/rotation
- ✅ Z-ordering is automatic

When we try to maintain separate views:
- ❌ Manual position synchronization required
- ❌ Separate visibility tracking needed
- ❌ Complex lifecycle management
- ❌ Bridge patterns for coordination
- ❌ Race conditions from parallel updates

## Decision

**Within Godot's presentation layer, embrace the scene graph for UI composition. Use parent-child relationships when UI elements are naturally coupled.**

### Core Principles

1. **Godot IS the Presentation Layer**
   - Don't abstract what the engine does well
   - Scene graph is a feature, not a coupling to avoid
   - Let Godot handle spatial relationships
   - **BUT**: Still need bridges at architectural boundaries (ADR-010)

2. **Composite Views Are Natural (Within Godot)**
   - Actor + HealthBar + StatusEffects = One visual unit
   - Grid + Tiles + Overlays = One visual space
   - Don't artificially separate coupled UI elements
   - Use Godot signals for node-to-node communication

3. **Clean Architecture Still Applies**
   - Domain layer remains pure C# (no Godot references)
   - Application layer uses commands/queries
   - **Boundary complexity is justified** (UIEventBus for domain→UI)
   - **Within-layer complexity is not** (HealthView phantom nodes)

4. **One Presenter Per Composite**
   - ActorPresenter manages actor + health + effects
   - GridPresenter manages grid + tiles + fog
   - Don't split presenters for coupled UI
   - Presenters still bridge to domain via proper patterns

### The Context Rule

**Ask**: "Am I working within one world or bridging between worlds?"
- **Within Godot**: Embrace simplicity, use engine features
- **Between worlds**: Accept necessary complexity for proper bridging

## Implementation Guidelines

### ✅ EMBRACE Scene Graph When:

```gdscript
# Good: Natural parent-child relationship
ActorNode (Node2D)
  ├── Sprite
  ├── HealthBar (ProgressBar)
  ├── StatusEffectIcons (HBoxContainer)
  └── SelectionIndicator (ColorRect)
```

**Benefits**: 
- Free movement synchronization
- Automatic visibility inheritance
- Natural lifecycle management
- Transform inheritance (scale, rotation)

### ❌ DON'T Over-Abstract When:

```csharp
// Bad: Fighting the engine
class HealthView : IHealthView {
    // 790 lines managing positions separately
    // Complex synchronization with ActorView
    // Bridge patterns to coordinate
}

class ActorView : IActorView {
    // Duplicate position tracking
    // Manual health bar coordination
}
```

### ✅ DO Abstract When (per ADR-006):

```csharp
// Good: Abstract external dependencies
interface IAudioService { }     // Platform differences
interface ISaveService { }      // Serialization boundary  
interface IInputService { }     // Remapping/replay needs
interface IRandomService { }    // Determinism requirement
```

### ⚠️ DON'T Oversimplify Architectural Boundaries:

```csharp
// GOOD: UIEventBus for crossing composition roots (ADR-010)
public class UIEventBus : IUIEventBus 
{
    // This complexity IS justified - bridging two worlds
    private readonly Dictionary<Type, List<WeakSubscription>> _subscriptions;
    // WeakReferences needed - different lifecycles
}

// BAD: Creating abstractions within Godot world
public class HealthView : IHealthView 
{
    // This complexity is NOT justified - same world
    // Just make health bars children of actors!
}
```

**The Rule**: 
- Complex bridges between worlds = GOOD (necessary)
- Complex abstractions within Godot = BAD (unnecessary)

## Consequences

### Positive
- **Massive complexity reduction**: Eliminate phantom views and bridge patterns
- **Better performance**: Leverage engine optimizations
- **Fewer bugs**: No synchronization issues
- **Faster development**: Use engine features instead of reimplementing
- **Clearer code**: UI relationships visible in scene structure
- **Free features**: Transformations, culling, batching

### Negative  
- **Tighter Godot coupling**: UI layer deeply uses engine features
- **Harder unit testing**: Can't test UI without engine
- **Scene file complexity**: More logic in .tscn files
- **Refactoring challenges**: Changing parent-child relationships

### Mitigations
- **Keep logic in presenters**: Scenes are structure, not behavior
- **Use GDUnit for UI tests**: Test with the engine, not against it
- **Document scene contracts**: Clear ownership and responsibilities
- **Version scene files**: Track .tscn changes carefully

## Examples

### Example 1: Actor Composite (GOOD)
```csharp
public partial class ActorView : Node2D, IActorView 
{
    // Single view manages entire actor composite
    public void DisplayActor(ActorId id, Position pos) 
    {
        var actorNode = new ColorRect();
        
        // Health bar is child - moves/hides automatically!
        var healthBar = CreateHealthBar();
        actorNode.AddChild(healthBar);
        
        // Status effects also children
        var statusContainer = CreateStatusContainer();
        actorNode.AddChild(statusContainer);
        
        AddChild(actorNode);
    }
    
    public void UpdateHealth(ActorId id, int current, int max) 
    {
        // Direct update - no coordination needed
        _healthBars[id].Value = current;
    }
}
```

### Example 2: Separate Views (BAD)
```csharp
// DON'T DO THIS - Fighting the engine
public class HealthView : IHealthView 
{
    // Separate position tracking
    Dictionary<ActorId, Vector2> _healthBarPositions;
    
    public void MoveHealthBar(ActorId id, Position to) 
    {
        // Manual synchronization - error prone!
        _healthBarPositions[id] = GridToPixel(to);
        _healthBars[id].Position = _healthBarPositions[id];
    }
}

public class ActorView : IActorView 
{
    public void MoveActor(ActorId id, Position to) 
    {
        // Must notify health view separately!
        _healthPresenter.NotifyMove(id, to);
    }
}
```

### Example 3: Grid Composite (GOOD)
```csharp
public partial class GridView : Node2D, IGridView 
{
    // Grid owns all spatial elements
    Node2D _tileLayer;      // Child node
    Node2D _fogLayer;       // Child node  
    Node2D _overlayLayer;   // Child node
    
    public void SetTileVisibility(Position pos, VisibilityState state) 
    {
        // All layers update together naturally
        var tile = _tileLayer.GetChild(GetTileIndex(pos));
        tile.Modulate = GetFogColor(state);
    }
}
```

## Relationship to Other ADRs

### ADR-010: UI Event Bus Architecture
**Different Problem Space**: ADR-010 solves bridging between two incompatible composition roots (DI container ↔ Godot). That complexity is justified because it's crossing architectural boundaries. This ADR (016) applies WITHIN Godot's world where we should embrace simplicity.

**When to use which:**
- **Use ADR-010 patterns**: When crossing from domain/application layer to presentation
- **Use ADR-016 patterns**: When working entirely within Godot's presentation layer

### ADR-006: Selective Abstraction Strategy
**Complementary**: ADR-006 defines WHAT to abstract (services at boundaries), while this ADR defines HOW to structure UI within the presentation layer.

### ADR-001: Model-View Separation
**Still Valid**: Domain models remain pure C#. This ADR only affects how views are structured within Godot, not the domain-view boundary.

## Architectural Layers and Complexity

```
Domain Layer (Pure C#)
    │
    │ ← ADR-010: Complex bridge justified (different worlds)
    ▼
═══════════════════════════ Composition Root Boundary
    │
Presentation Layer (Godot)
    │
    │ ← ADR-016: Embrace simplicity (same world)
    ▼
Scene Tree & UI Components
```

**Key Principle**: Complexity at boundaries, simplicity within layers.

## Status Transitions

- 2025-09-11: Created based on BR_003 learnings and TD_034 analysis
- [Awaiting approval]

## References

- BR_003 Post-Mortem: Split-brain HealthView with no actual UI
- TD_034: View consolidation opportunity analysis
- Godot Best Practices: Scene composition patterns
- Unity DOTS: Similar "embrace the engine" philosophy

## Decision Record

**Proposed by**: Tech Lead  
**Key Insight**: "Godot IS our presentation layer - embrace it"  
**Approval**: [Pending]

---

*"Don't fight your tools. A scene graph is a powerful feature, not a coupling to avoid."*