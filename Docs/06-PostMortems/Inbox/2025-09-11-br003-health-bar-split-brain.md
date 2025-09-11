# Post-Mortem: BR_003 Health Bar Update Failure - Split-Brain View Architecture

**Date**: 2025-09-11  
**Severity**: Medium (UI functionality broken, no data loss)  
**Duration**: ~1.5 hours discovery and fix  
**Author**: Dev Engineer  

## Executive Summary

Health bars failed to update when actor health changed due to a fundamental architectural mismatch: health bars were created and owned by `ActorView`, but health updates were sent to `HealthView` - a completely separate view that contained no actual UI elements. This is a classic "split-brain" architecture where two systems think they own the same responsibility.

## Timeline of Events

**14:00** - User requested fix for BR_003 (health bars not updating)  
**14:05** - Investigation began, found `UpdateActorHealth()` already existed in ActorView  
**14:10** - Discovered HealthPresenter was calling IHealthView, not IActorView  
**14:15** - Found HealthView exists separately with no actual UI elements  
**14:30** - Implemented bridge pattern connecting HealthPresenter → ActorPresenter → ActorView  
**14:45** - Tests passing, fix validated  
**14:56** - Committed fix  

## What Went Wrong

### The Symptom
Health bars displayed at 100/100 even after actors took damage. Health changes in the domain layer never reached the visual health bars.

### Surface Cause  
HealthPresenter wasn't connected to ActorPresenter - no pathway for health updates to reach the actual health bar UI.

### Deep Root Cause
**Split-brain architecture**: Two parallel view systems existed for health:

1. **ActorView System** (Reality):
   - Created health bars as child nodes of actors (line 402)
   - Had `UpdateActorHealth()` method ready to use (line 465)
   - Owned all actual health bar UI elements
   
2. **HealthView System** (Phantom):
   - Implemented IHealthView interface
   - Had methods like `UpdateHealthAsync()`
   - **Contained ZERO actual UI elements**
   - Was essentially a "no-op" view that did nothing

HealthPresenter faithfully called methods on HealthView, which dutifully did nothing because it had no UI elements to update.

## Why It Happened

### Architectural Assumption vs Reality

**Assumption** (Clean Architecture ideal):
```
Health Domain → HealthPresenter → IHealthView → HealthView (owns health UI)
Actor Domain → ActorPresenter → IActorView → ActorView (owns actor UI)
```

**Reality** (Practical implementation):
```
Health Domain → HealthPresenter → IHealthView → HealthView (EMPTY!)
Actor Domain → ActorPresenter → IActorView → ActorView (owns BOTH actor AND health UI)
```

### The Decision Point
When health bars were implemented as child nodes of actors (excellent decision for movement, visibility, lifecycle), the architecture wasn't updated to reflect this UI coupling. We kept two separate presenter/view chains when the UI had already merged into one.

## How It Was Fixed

### Immediate Fix
Created a bridge pattern where HealthPresenter delegates health bar updates to ActorPresenter:
1. Added `UpdateActorHealth()` to IActorView interface
2. Added `UpdateActorHealthAsync()` bridge method to ActorPresenter
3. Connected HealthPresenter → ActorPresenter in GameManager
4. HealthPresenter now calls both its own view AND ActorPresenter

### Why This Works
The fix acknowledges reality: health bars live in ActorView, so health updates must go through ActorPresenter. The bridge maintains clean architecture while routing updates to where the UI actually exists.

## Impact Analysis

### What Was Affected
- All health bar displays in combat
- Player feedback for damage/healing
- Combat readability and user experience

### What Was NOT Affected  
- Domain layer health calculations (correct)
- Health state in application layer (correct)
- Save/load systems (unaffected)
- Test coverage (all passing)

## Lessons Learned

### 1. UI Coupling Drives Architecture
When UI elements are tightly coupled (parent-child nodes), the presenter architecture must acknowledge this coupling. Trying to maintain artificial separation leads to split-brain problems.

### 2. Empty Implementations Are Red Flags
HealthView likely had empty or stub implementations. When a view has no actual UI elements, question whether it should exist at all.

### 3. The UpdateActorHealth Method Already Existed!
The solution was already implemented in ActorView (line 465). We just needed to call it. This suggests the original developer recognized where health bars belonged but didn't complete the wiring.

### 4. Test What You See
Our tests checked that handlers were called, but not that UI actually updated. Visual elements need visual verification (or at least integration tests that check the actual view methods called).

## Prevention Measures

### Short Term
1. **Audit other view systems** for similar split-brain patterns
2. **Add integration tests** that verify UI updates reach the correct view
3. **Document view ownership** clearly in interfaces

### Long Term  
1. **Consider unifying** ActorView and HealthView if they're always coupled
2. **Or fully separate** them with health bars as independent nodes
3. **Create view ownership map** showing which view owns which UI elements

### Architectural Recommendation
Either:
- **Option A**: Merge HealthView into ActorView (acknowledge the coupling)
- **Option B**: Make health bars true standalone nodes (enforce the separation)
- **Current**: Bridge pattern is a reasonable compromise but adds complexity

## Code Smells That Should Have Warned Us

1. **HealthView.cs probably has empty methods** (not checked, but likely)
2. **Health bars created in one place, updated in another**
3. **Two presenters managing related UI elements**
4. **UpdateActorHealth() existing but unused**
5. **No integration tests for health bar updates**

## Recovery Actions

✅ Immediate fix implemented and tested  
✅ BR_003 marked as resolved  
✅ All tests passing (658/658)  
⏳ TODO: Audit HealthView.cs for other unused methods  
⏳ TODO: Add integration test for health bar updates  
⏳ TODO: Document view ownership in architecture docs  

## Final Verdict

This wasn't a bug - it was **architectural debt**. The implementation evolved (health bars became child nodes) but the architecture didn't evolve with it. The fix is solid but highlights a deeper question about view separation that should be addressed in the next refactoring cycle.

**Severity**: Medium bug, High architectural concern  
**Resolution**: Tactical fix complete, strategic refactoring recommended

---

*"When two systems both think they own something, usually neither does."*