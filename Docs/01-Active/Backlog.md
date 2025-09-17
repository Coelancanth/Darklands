# Darklands Development Backlog


**Last Updated**: 2025-09-17 19:15 (Tech Lead - TD_060 revised for ADR-006 compliance, simpler and more elegant)

**Last Aging Check**: 2025-08-29
> 📚 See [Workflow.md - Backlog Aging Protocol](Workflow.md#-backlog-aging-protocol---the-3-10-rule) for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 008
- **Next TD**: 061
- **Next VS**: 015 


**Protocol**: Check your type's counter → Use that number → Increment the counter → Update timestamp

## 📖 How to Use This Backlog

### 🧠 Owner-Based Protocol

**Each item has a single Owner persona responsible for decisions and progress.**

#### When You Embody a Persona:
1. **Filter** for items where `Owner: [Your Persona]`
3. **Quick Scan** for other statuses you own (<2 min updates)
4. **Update** the backlog before ending your session
5. **Reassign** owner when handing off to next persona


### Default Ownership Rules
| Item Type | Status | Default Owner | Next Owner |
|-----------|--------|---------------|------------|
| **VS** | Proposed | Product Owner | → Tech Lead (breakdown) |
| **VS** | Approved | Tech Lead | → Dev Engineer (implement) |
| **BR** | New | Test Specialist | → Debugger Expert (complex) |
| **TD** | Proposed | Tech Lead | → Dev Engineer (approved) |

### Pragmatic Documentation Approach
- **Quick items (<1 day)**: 5-10 lines inline below
- **Medium items (1-3 days)**: 15-30 lines inline (like VS_001-003 below)
- **Complex items (>3 days)**: Create separate doc and link here

**Rule**: Start inline. Only extract to separate doc if it grows beyond 30 lines or needs diagrams.

### Adding New Items
```markdown
### [Type]_[Number]: Short Name
**Status**: Proposed | Approved | In Progress | Done
**Owner**: [Persona Name]  ← Single responsible persona
**Size**: S (<4h) | M (4-8h) | L (1-3 days) | XL (>3 days)
**Priority**: Critical | Important | Ideas
**Markers**: [ARCHITECTURE] [SAFETY-CRITICAL] etc. (if applicable)

**What**: One-line description
**Why**: Value in one sentence  
**How**: 3-5 technical approach bullets (if known)
**Done When**: 3-5 acceptance criteria
**Depends On**: Item numbers or None

**[Owner] Decision** (date):  ← Added after ultra-think
- Decision rationale
- Risks considered
- Next steps
```

#### 🚨 CRITICAL: VS Items Must Include Architectural Compliance Check
```markdown
**Architectural Constraints** (MANDATORY for VS items):
□ Deterministic: Uses IDeterministicRandom for any randomness (ADR-004)
□ Save-Ready: Entities use records and ID references (ADR-005)  
□ Time-Independent: No wall-clock time, uses turns/actions (ADR-004)
□ Integer Math: Percentages use integers not floats (ADR-004)
□ Testable: Can be tested without Godot runtime (ADR-006)
```

## 🔗 Dependency Chain Analysis

**EXECUTION ORDER**: Following this sequence ensures no item is blocked by missing dependencies.


## 🚀 Ready for Immediate Execution

*Items with no blocking dependencies, approved and ready to start*

### TD_061: Camera Follow During Movement Animation
**Status**: Not Started
**Owner**: Unassigned
**Size**: S (1-2h estimate)
**Priority**: High - UX improvement
**Created**: 2025-09-17 20:35 (Dev Engineer)
**Markers**: [CAMERA] [MOVEMENT] [UX]

**What**: Make camera follow player smoothly during movement animation
**Why**: Currently camera only updates on click destination, not during movement

**Problem Statement**:
- Player moves cell-by-cell with smooth animation (TD_060 complete)
- Camera jumps to destination immediately on click
- Player can move off-screen during animation
- Poor UX when moving long distances

**Proposed Solution**:
1. GridView subscribes to actor position updates during animation
2. Camera smoothly interpolates to follow actor
3. Optional: Camera leads slightly ahead on path for better visibility
4. Ensure camera doesn't jitter with tween updates

**Technical Approach**:
- Hook into ActorView's tween updates
- Update camera position per frame or per cell
- Use smooth camera interpolation (lerp)
- Consider viewport boundaries

---

### TD_060: Movement Animation Foundation ✅
**Status**: COMPLETE
**Owner**: Dev Engineer
**Size**: M (4h actual - scope expanded)
**Priority**: Critical - Prerequisite for VS_012
**Created**: 2025-09-17 18:21
**Updated**: 2025-09-17 20:30 (FULLY COMPLETE with A* integration)
**Markers**: [MOVEMENT] [ANIMATION] [FOUNDATION] [COMPLETE]

**What**: Add movement animation capability to ActorView using Godot directly
**Why**: Foundation needed before VS_012 - enables smooth visual movement

**Final Implementation** (2025-09-17 20:30):
✅ Tests: 688/692 passing (4 skipped, 0 failed)
✅ Build: Zero warnings
✅ Files Modified (Final):
  - `Views\ActorView.cs` - Non-blocking AnimateMovementAsync with CallDeferred
  - `src\Darklands.Presentation\Views\IActorView.cs` - Added interface method
  - `src\Darklands.Presentation\Presenters\ActorPresenter.cs` - HandleActorMovedWithPathAsync
  - `src\Darklands.Presentation\Presenters\GridPresenter.cs` - Pre-calculates A* path
  - `src\Darklands.Presentation\Presenters\IActorPresenter.cs` - Updated interface

**Implementation Journey**:
1. **Phase 1**: Basic tween - caused game freeze with await ToSignal
2. **Phase 2**: Non-blocking with CallDeferred queue - fixed freeze
3. **Phase 3**: A* path integration - animation matches preview perfectly

**Key Achievements**:
- ✅ Cell-by-cell animation along exact A* pathfinding route
- ✅ Non-blocking using queue + CallDeferred pattern (no freeze)
- ✅ Perfect match between hover preview dots and movement animation
- ✅ Path calculated BEFORE move to avoid self-blocking in pathfinding
- ✅ Fallback to straight-line if A* fails

**Critical Lessons**:
- Godot Tween + async/await can deadlock main thread
- Must calculate path BEFORE domain state changes
- CallDeferred essential for thread-safe Godot operations
- Animation and preview must use same path source

**Implementation Details**:
```csharp
// In ActorView.cs (Presentation layer) - Direct Godot usage
public partial class ActorView : Node2D
{
    public async Task AnimateMovement(List<Vector2> path, float speed = 3.0f)
    {
        var tween = CreateTween();
        foreach (var position in path)
        {
            tween.TweenProperty(this, "position", position, 1.0f / speed);
        }
        await ToSignal(tween, Tween.SignalName.Finished);
    }
}

// In MovementPresenter.cs - Coordinates domain & view
public class MovementPresenter : EventAwarePresenter<IActorView>
{
    protected override void SubscribeToEvents()
    {
        _eventBus.Subscribe<ActorMovedEvent>(this, OnActorMoved);
    }

    private async void OnActorMoved(ActorMovedEvent e)
    {
        await _view.AnimateMovement(e.Path, e.Speed);
        _eventBus.PublishAsync(new MovementAnimationCompletedEvent(e.ActorId));
    }
}
```

**Done When**:
- [ ] ActorView.AnimateMovement method implemented
- [ ] MovementPresenter coordinates animations
- [ ] Smooth tile-by-tile movement visible
- [ ] UIEventBus notifications working
- [ ] Tested with VS_014 pathfinding

**Tech Lead Decision** (2025-09-17 19:15):
- **ARCHITECTURALLY ALIGNED** - Respects ADR-006 (no animation abstraction)
- **SIMPLER** - Reduced from 2-3h to 1-2h by removing unnecessary service
- **ELEGANT** - Uses Godot directly as intended, presenter coordinates
- **PATTERN** - Establishes correct View-Presenter animation pattern



## 📋 Blocked - Waiting for Dependencies

*Items that cannot start until blocking dependencies are resolved*

### VS_012: Vision-Based Movement System
**Status**: Approved - BLOCKED by TD_060
**Owner**: Tech Lead → Dev Engineer
**Size**: S (2h)
**Priority**: Critical
**Created**: 2025-09-11 00:10
**Updated**: 2025-09-17 18:30 (Tech Lead - Added technical breakdown, blocked by TD_060)
**Markers**: [MOVEMENT] [VISION] [CHAIN-2-MOVEMENT]

**What**: Movement system where scheduler activates based on vision connections
**Why**: Creates natural tactical combat without explicit modes

**DEPENDENCY CHAIN**: Chain 2 - Step 2 (Vision-Based Movement)
- Dependencies: VS_014 ✅ COMPLETE, TD_060 (Animation Foundation) ⏳
- Enables: VS_013 (Enemy AI needs movement system)

**Architectural Constraints**:
☑ Deterministic: Fixed TU costs (integer-based)
☑ Save-Ready: Position state only, no animation state
☑ Time-Independent: Turn-based execution
☑ Integer Math: All movement costs in TU (Time Units)
☑ Testable: Clear state transitions without UI

### 📐 Technical Breakdown (Tech Lead - 2025-09-17 18:30)

**Complexity Score**: 4/10 - Straightforward with clear patterns
**Pattern Reference**: `src/Application/Combat/Commands/ExecuteAttackCommand.cs`

#### Phase 1: Domain Model [0.5h]
**Location**: `src/Darklands.Domain/Movement/`
- Create `MovementCost.cs` - Value object (TU costs per tile)
- Create `MovementPath.cs` - Validated path with total cost
- Create `IMovementValidator.cs` - Interface for validation rules
- Create `MovementValidator.cs` - Vision & TU validation logic
**Pattern**: Follow AttackDamage/AttackResult pattern

#### Phase 2: Application Layer [0.5h]
**Location**: `src/Application/Movement/Commands/`
- Create `MoveActorCommand.cs` - Command with actor ID and target
- Create `MoveActorCommandHandler.cs` - Orchestrate movement
- Integration: Call CalculatePathQuery for pathfinding
- Integration: Update GridStateService with new position
- Integration: Publish ActorMovedNotification
**Pattern**: Copy ExecuteAttackCommandHandler structure

#### Phase 3: Infrastructure [0.5h]
**Location**: `src/Core/Infrastructure/Services/`
- Extend `ISchedulerService` with vision-based activation
- Add `CheckVisionTrigger(Position from, Position to)` method
- Implement enemy activation when player enters vision
**Key Logic**: If enemy sees movement → activate enemy turn

#### Phase 4: Presentation [0.5h]
**Location**: `godot_project/features/movement/`
- Create `MovementPresenter.cs` - MVP presenter
- Integration: Use ActorAnimator from TD_060
- Handle: Path preview → click → animation → completion
- Update: PathOverlay to trigger actual movement
**Critical**: Animation completes BEFORE next turn starts



## 📋 Blocked - Waiting for Dependencies

*Items that cannot start until blocking dependencies are resolved*

### VS_013: Basic Enemy AI
**Status**: Proposed - BLOCKED by VS_012
**Owner**: Product Owner → Tech Lead
**Size**: M (4-8h)
**Priority**: Important
**Created**: 2025-09-10 19:03
**Updated**: 2025-09-16 01:00 (Tech Lead - Moved to blocked section)
**Markers**: [AI] [COMBAT] [CHAIN-2-MOVEMENT] [BLOCKED]
**Blocking Dependency**: VS_012 (Vision-Based Movement System)

**What**: Simple but effective enemy AI for combat testing
**Why**: Need opponents to validate combat system and create gameplay loop

**DEPENDENCY CHAIN**: Chain 2 - Step 3 (Enemy Intelligence)
- Blocked by: VS_012 (AI needs movement system to function)
- Enables: Complete tactical combat gameplay loop

**Architectural Constraints**:
☑ Deterministic: AI decisions based on seeded random
☑ Save-Ready: AI state fully serializable
☑ Time-Independent: Decisions based on game state not time
☑ Integer Math: All AI calculations use integers
☑ Testable: AI logic can be unit tested

---



## 🔄 Execution Summary

**Current State**: All items properly organized by dependency chains after ADR consistency review
**Critical Path**: TD_046 → VS_014 → VS_012 → VS_013 → Future Features

**Next Actions**:
1. **Immediate**: Execute TD_046 (8h) - Architectural foundation that blocks all other work
2. **Parallel**: Execute TD_035 (3h) - Technical debt cleanup, compatible with TD_046
3. **After Chain 1**: Begin VS_014 → VS_012 → VS_013 sequence (7h total)
4. **Future**: Evaluate IDEA_* items once foundations are complete

**Estimated Timeline**:
- ✅ **Week 1**: TD_046 + TD_035 (Architecture + Cleanup)
- ⏳ **Week 2**: VS_014 + VS_012 (Movement Foundation)
- ⏳ **Week 3**: VS_013 (Enemy AI) + Polish
- 🔮 **Future**: Feature expansion with solid architectural foundation

## 📋 Quick Reference

**Dependency Chain Rules:**
- 🚫 **Never** start items with blocking dependencies
- ✅ **Always** complete architectural foundations first
- ⚡ **Parallel** work only when items are in different code areas
- 🔄 **Re-evaluate** priorities after each chain completion

**Work Item Types:**
- **TD_xxx**: Technical Debt (refactoring) - Anyone proposes → Tech Lead approves
- **VS_xxx**: Vertical Slice (new feature) - Product Owner creates, Tech Lead breaks down
- **BR_xxx**: Bug Report (investigation) - Test Specialist creates, Debugger owns
- **IDEA_xxx**: Future Features - No owner until prerequisite chains complete

---
*Single Source of Truth for all Darklands development work. Organized by dependency chains for optimal execution order.*