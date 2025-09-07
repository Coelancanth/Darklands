# Post-Mortem: Health Bar Display Failure Due to Missing Presenter Coordination

**Date**: 2025-09-07  
**Author**: Debugger Expert  
**Severity**: Medium (User-visible feature completely broken)  
**Duration**: ~3 hours investigation and fix  
**Issue**: PM_001  

## Executive Summary

Health bars failed to display and track actor movement due to missing coordination between ActorPresenter and HealthPresenter, combined with incorrect Godot node initialization timing. This revealed a systemic gap in our presenter coordination architecture that would have affected all future UI features.

## Timeline of Events

1. **Initial State**: ActorView displayed blue square for test player at (0,0)
2. **Expected**: Health bar should appear above actor showing "100/100"
3. **Actual**: No health bar visible, no error messages
4. **Investigation**: Found three independent root causes across different architectural layers
5. **Resolution**: Implemented presenter coordination pattern and fixed Godot lifecycle usage

## Impact

### User Impact
- Health bars completely non-functional
- Core game mechanic (health visualization) broken
- Would block combat implementation (VS_002)

### Technical Impact
- Revealed architectural gap in MVP implementation
- Exposed Godot framework knowledge gap
- Found missing event propagation between presenters

## Root Cause Analysis (5 Whys)

### Primary Cause: Missing Presenter Coordination
1. **Why didn't health bars appear?**  
   HealthPresenter was never notified when actors were created
   
2. **Why wasn't it notified?**  
   ActorPresenter had no reference to HealthPresenter to send notifications
   
3. **Why no reference?**  
   Architecture assumed presenters would work independently through the model
   
4. **Why this assumption?**  
   Classic MVP pattern uses model events, but we're using MediatR command pattern
   
5. **Why does this matter?**  
   MediatR handles commands/queries but not real-time UI coordination events

### Secondary Cause: Godot Node Lifecycle Misuse
1. **Why didn't health bars render even when created?**  
   HealthBarNode created child nodes in constructor
   
2. **Why is this a problem?**  
   Godot requires nodes to be in scene tree before adding children
   
3. **Why wasn't it in scene tree?**  
   Constructor runs before node is added to parent
   
4. **Why not wait for scene tree?**  
   Didn't use `_Ready()` callback which fires after scene tree insertion
   
5. **Why this knowledge gap?**  
   Godot-specific pattern not obvious from C# perspective

### Tertiary Cause: Initialization Race Condition
1. **Why did HealthPresenter check for TestPlayerId?**  
   Tried to create initial health bars during Initialize()
   
2. **Why was TestPlayerId null?**  
   ActorPresenter hadn't created test player yet
   
3. **Why the timing issue?**  
   Both presenters initialized simultaneously
   
4. **Why simultaneously?**  
   GameManager called all Initialize() methods in sequence
   
5. **Why is this fragile?**  
   Created implicit temporal coupling between presenters

## Technical Details

### Bug Manifestation Points

1. **GameManager.cs:213-215** - Presenter initialization without coordination
2. **ActorPresenter.cs:111-114** - Actor creation without health bar notification  
3. **HealthPresenter.cs:75** - Checking for TestPlayerId that doesn't exist yet
4. **HealthBarNode.cs:524** - Creating nodes in constructor before scene tree
5. **ActorPresenter.cs:161** - Movement without health bar coordination

### The Fix

1. **Added Presenter Coordination**:
```csharp
// ActorPresenter.cs
private HealthPresenter? _healthPresenter;
public void SetHealthPresenter(HealthPresenter healthPresenter) { ... }

// Notify on actor creation
await _healthPresenter.HandleActorCreatedAsync(actor.Id, startPosition, actor.Health);

// Notify on actor movement  
await _healthPresenter.HandleActorMovedAsync(actorId, fromPosition, toPosition);
```

2. **Fixed Godot Lifecycle**:
```csharp
// HealthBarNode.cs
public override void _Ready() {
    base._Ready();
    if (!_initialized) {
        CreateHealthBarElements(); // Create children AFTER in scene tree
        _initialized = true;
    }
}
```

3. **Connected Presenters in GameManager**:
```csharp
_actorPresenter.SetHealthPresenter(_healthPresenter);
```

## Lessons Learned

### Architectural Lessons

1. **Presenter Coordination is Essential**  
   - Presenters managing related UI must communicate
   - Can't rely solely on application layer events
   - Need explicit coordination protocol

2. **Event Flow Must Be Explicit**  
   - Document which presenter notifies which
   - Avoid implicit temporal coupling
   - Consider observer pattern for multi-presenter updates

3. **Framework Lifecycle Knowledge Critical**  
   - Godot node initialization has specific requirements
   - Constructor vs _Ready() has major implications
   - Scene tree presence required for child operations

### Code Quality Lessons

1. **Initialization Order Matters**  
   - Don't assume presenter initialization sequence
   - Use lazy initialization or explicit dependencies
   - Avoid checking for other presenters' state during init

2. **Visual Feedback Needs Coordination**  
   - Actor visual + health bar must move together
   - Single source of truth (actor position) but multiple views
   - Coordination through presenter, not through view

## Prevention Measures

### Immediate Actions Taken

1. ✅ Implemented SetHealthPresenter() coordination pattern
2. ✅ Fixed all Godot node initialization to use _Ready()
3. ✅ Added health bar movement coordination
4. ✅ Documented critical coordination points with comments

### Future Prevention Patterns

1. **Presenter Coordination Protocol**:
   - All presenters that need coordination must have SetXPresenter() methods
   - GameManager responsible for wiring presenter connections
   - Document presenter dependencies in each presenter class

2. **Godot Best Practices**:
   - ALWAYS use _Ready() for node creation
   - NEVER create child nodes in constructors
   - Use CallDeferred for thread-safe UI updates

3. **Event Flow Documentation**:
   - Create presenter interaction diagram
   - Document in HANDBOOK.md
   - Add to architecture decision records

## Recommendations

### For HANDBOOK.md

Add section on "Presenter Coordination Patterns":
- When to use SetXPresenter() pattern
- How to handle cross-presenter events
- Example: Actor ↔ Health coordination

Add section on "Godot Integration Patterns":
- Node lifecycle (_Ready, _EnterTree, _ExitTree)
- Scene tree requirements
- Thread safety with CallDeferred

### For Future Features

When implementing VS_002 (Combat):
- CombatPresenter will need SetHealthPresenter()
- CombatPresenter will need SetActorPresenter()
- Consider PresenterCoordinator base class

### For Testing

Create integration tests for:
- Presenter coordination scenarios
- Health bar follows actor movement
- Multiple actors with health bars

## Metrics

- **Investigation Time**: ~2 hours
- **Fix Implementation**: ~1 hour  
- **Files Modified**: 4 (ActorPresenter, HealthPresenter, HealthView, GameManager)
- **Lines Changed**: ~80
- **Bug Recurrence Risk**: Low (pattern now established)

## Sign-Off

This post-mortem captures critical architectural learnings about presenter coordination and Godot framework integration. The patterns discovered here will prevent similar issues in all future UI features.

**Status**: Ready for consolidation into HANDBOOK.md patterns

---

*Generated by Debugger Expert for learning extraction and prevention pattern establishment*