# Post-Mortem: TD_005 Visual Movement Bug
**Date**: 2025-09-07  
**Author**: Dev Engineer  
**Severity**: High (Core Functionality Blocked)  
**Duration**: ~2 hours debugging  
**Final Status**: RESOLVED ✅

## Executive Summary
Actor visual movement was completely broken despite logical movement working perfectly. The bug had THREE distinct root causes that compounded each other, requiring systematic debugging to identify and fix each layer.

## Timeline of Events

### Initial State (12:22 PM)
- User reported: "Actor doesn't move visually despite logical success"
- Console showed: "Success at (1, 1): Moved"
- Visual observation: Blue actor square remained at position (0, 0)

### Investigation Phase 1 (12:26 PM)
- **Hypothesis**: Tween property name might be wrong
- **Finding**: Property "position" should be "Position" (PascalCase in C#)
- **Action**: Fixed tween property names
- **Result**: ❌ Still not working

### Investigation Phase 2 (12:28 PM)
- **Hypothesis**: ActorPresenter not being notified of moves
- **Finding**: GridPresenter wasn't calling ActorPresenter.HandleActorMovedAsync
- **Action**: Added presenter communication pipeline
- **Result**: ❌ Still not working (but logs showed "Handling actor move")

### Investigation Phase 3 (12:32 PM)
- **Hypothesis**: Position tracking failing
- **Finding**: FromPosition search was returning None
- **Action**: Used GridStateService directly for position tracking
- **Result**: ✅ Position tracking fixed, but ❌ visual still not moving

### Investigation Phase 4 (12:33 PM)
- **Discovery**: Actor always at pixel (0,0) when movement starts
- **Finding**: Previous moves weren't actually updating the node position
- **Root Cause**: Tween wasn't executing/completing in deferred context

### Final Fix (12:36 PM)
- **Action**: Bypassed tween, used direct position assignment
- **Result**: ✅ VISUAL MOVEMENT WORKING!

## Root Cause Analysis

### Layer 1: Godot C# API Mismatch
**Problem**: Tween property names require exact casing
```csharp
// ❌ WRONG - GDScript style
tween.TweenProperty(node, "position", target, duration);

// ✅ CORRECT - C# style  
tween.TweenProperty(node, "Position", target, duration);
```
**Why it happened**: Confusion between GDScript (snake_case) and C# (PascalCase) conventions

### Layer 2: Missing Presenter Communication
**Problem**: MVP architecture wasn't complete
```csharp
// GridPresenter was sending commands but not notifying ActorPresenter
// Missing: _actorPresenter.HandleActorMovedAsync() call
```
**Why it happened**: Incomplete implementation of presenter-to-presenter communication

### Layer 3: Tween Execution in Deferred Context
**Problem**: Most critical issue - tweens not executing properly
```csharp
// Called from async context
CallDeferred("MoveActorNodeDeferred");

// Inside deferred method
var tween = CreateTween();  // Creates tween
tween.TweenProperty(...);    // Sets up animation
// But tween never actually executes/completes!
```

**Deep Analysis of Tween Failure**:
1. Tween created in deferred call context
2. No error messages, silent failure
3. Tween.Finished callback never triggered
4. Node.Position never updated by tween
5. Each subsequent move started from (0,0) because position never changed

**Why it happened**: 
- Godot's threading model conflict with C# async/await
- CallDeferred changes execution context
- Tween lifecycle may require scene tree pump that doesn't happen in deferred context
- Possible race condition between tween creation and scene tree processing

## What Went Wrong

1. **Cascading Failures**: Three separate bugs masked each other
2. **Silent Failures**: Tween didn't error, just didn't execute
3. **Incorrect Assumptions**: Assumed tween would work in any context
4. **Missing Diagnostics**: No tween completion logging initially

## What Went Right

1. **Systematic Debugging**: Identified and fixed each layer methodically
2. **Incremental Fixes**: Each fix improved the situation
3. **Diagnostic Logging**: Added comprehensive debug output
4. **Fallback Strategy**: Direct position assignment as workaround

## Lessons Learned

### Technical Lessons
1. **Godot C# Gotcha**: Always use PascalCase for property names in tweens
2. **Tween Context Matters**: Tweens may not work properly in deferred calls
3. **MVP Wiring**: Presenters must be explicitly connected for coordination
4. **Trust But Verify**: Always log tween completion to verify execution

### Process Lessons
1. **Layer Debugging**: Complex bugs often have multiple root causes
2. **Simplify First**: Remove complexity (tween) to verify basics work
3. **Log Everything**: Especially async/deferred operations
4. **Test Incrementally**: Verify each fix before adding complexity back

## Prevention Measures

### Immediate Actions
1. ✅ Document Godot C# property name requirements
2. ✅ Add fallback for tween failures (direct position set)
3. ✅ Comprehensive logging in all deferred methods

### Future Improvements
1. **Investigate Tween Fix**: Research proper tween usage in deferred context
2. **Animation System**: Consider custom animation system if tweens unreliable
3. **Unit Tests**: Add tests for presenter communication
4. **Code Review**: Check all other tween usage for similar issues

## Technical Debt Created

### TD_006 (Proposed): Re-enable Smooth Movement Animation
**Problem**: Currently using instant position changes instead of smooth animation
**Solution Options**:
1. Fix tween execution in deferred context
2. Implement frame-based animation in _Process
3. Use Godot's AnimationPlayer instead of tweens
4. Create custom coroutine-based movement system

**Complexity**: 5/10 (Requires Godot engine knowledge)
**Priority**: Low (Visual polish, not functional)

## Code Changes Summary

### Files Modified
1. `Views/ActorView.cs` (3 changes)
   - Fixed tween property names
   - Added node reference fix
   - Implemented direct position fallback

2. `src/Presentation/Presenters/GridPresenter.cs` (2 changes)
   - Added ActorPresenter reference
   - Implemented movement notification

3. `GameManager.cs` (1 change)
   - Connected presenters together

### Critical Code Section
```csharp
// Current working solution (direct assignment)
actualActorNode.Position = _pendingEndPosition;

// TODO: Fix and re-enable animation
// var tween = CreateTween();
// tween.TweenProperty(actualActorNode, "Position", _pendingEndPosition, MoveDuration);
```

## Conclusion

This bug was particularly challenging because it had three independent root causes that all needed to be fixed for visual movement to work. The most insidious was the tween execution issue, which failed silently without any error messages.

The systematic debugging approach - fixing each layer and adding diagnostics - was crucial to solving this. The final fallback to direct position assignment proves the architecture is sound, and smooth animation can be added as a future enhancement.

**Key Takeaway**: When dealing with game engine integration, especially with async operations and deferred calls, always have a simple fallback that bypasses complex systems like animation engines. Test the basic functionality first, then add polish.

## References
- Godot C# API Documentation: Property naming conventions
- Issue TD_005 in Backlog.md
- VS_008 Grid Scene implementation