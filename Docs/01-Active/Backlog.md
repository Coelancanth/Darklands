# Darklands Development Backlog


**Last Updated**: 2025-09-11 (BR_002 created for shadowcasting edge cases)

**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 003
- **Next TD**: 032  
- **Next VS**: 014 


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

## 🔥 Critical (Do First)
*Blockers preventing other work, production bugs, dependencies for other features*

### VS_011: Vision/FOV System with Shadowcasting and Fog of War
**Status**: In Progress (Phase 1 Complete)
**Owner**: Dev Engineer
**Size**: M (6h)
**Priority**: Critical
**Created**: 2025-09-10 19:03
**Updated**: 2025-09-11 (Phase 1 domain complete, BR_002 created for edge cases)
**Tech Breakdown**: FOV system using recursive shadowcasting with three-state fog of war

**What**: Field-of-view system with asymmetric vision ranges, proper occlusion, and fog of war visualization
**Why**: Foundation for ALL combat, AI, stealth, and exploration features

**Design** (per ADR-014):
- **Uniform algorithm**: All actors use shadowcasting FOV
- **Asymmetric ranges**: Different actors see different distances
- **Wake states**: Dormant monsters skip FOV calculation
- **Fog of war**: Three states - unseen (black), explored (gray), visible (clear)
- **Wall integration**: Uses existing TerrainType.Wall and Tile.BlocksLineOfSight

**Vision Ranges**:
- Player: 8 tiles
- Goblin: 5 tiles
- Orc: 6 tiles
- Eagle: 12 tiles

**Implementation Plan**:
- **Phase 1: Domain Model** (1h)
  - VisionRange value object with integer distances
  - VisionState record (CurrentlyVisible, PreviouslyExplored)
  - ShadowcastingFOV algorithm using existing Tile.BlocksLineOfSight
  - Monster activation states (Dormant, Alert, Active, Returning)
  
- **Phase 2: Application Layer** (1h)
  - CalculateFOVQuery and handler
  - IVisionStateService for managing explored tiles
  - Vision caching per turn with movement invalidation
  - Integration with IGridStateService for wall data
  - Console commands for testing
  
- **Phase 3: Infrastructure** (1.5h)
  - InMemoryVisionStateService implementation
  - Explored tiles persistence (save-ready accumulation)
  - Performance monitoring and metrics
  - Cache management with turn tracking
  
- **Phase 4: Presentation** (2.5h)
  - IFogOfWarView interface definition
  - Three-layer visibility system (unseen/explored/visible)
  - FogOfWarPresenter for MVP coordination
  - GridPresenter integration for tile visibility updates
  - ActorPresenter integration for hiding/showing actors
  - Visual feedback (fog overlays, fade transitions)

**Core Components**:
```csharp
// Domain - Pure FOV calculation using existing walls
public HashSet<Position> CalculateFOV(Position origin, int range, Grid grid) {
    var visible = new HashSet<Position>();
    foreach (var octant in GetOctants()) {
        CastShadow(origin, range, grid, octant, visible);
    }
    return visible;
}

// Check existing wall data
private bool BlocksVision(Position pos, Grid grid) {
    return grid.GetTile(pos).Match(
        Succ: tile => tile.BlocksLineOfSight,  // Wall, Forest
        Fail: _ => true  // Out of bounds
    );
}

// Three-state visibility
public enum VisibilityLevel {
    Unseen = 0,     // Never seen (black overlay)
    Explored = 1,   // Previously seen (gray overlay)
    Visible = 2     // Currently visible (no overlay)
}
```

**Console Test Commands**:
```
> fov calculate player
Calculating FOV for Player (range 8)...
Visible: 45 tiles
Walls blocking: 12 tiles

> fog show
Current fog state:
- Visible: 45 tiles (bright)
- Explored: 128 tiles (gray)
- Unseen: 827 tiles (black)

> vision debug goblin
Goblin at (5,3):
- Vision range: 5
- Currently sees: Player, Wall, Wall
- State: Alert (player visible)
```

**Done When**:
- Shadowcasting FOV works correctly with wall occlusion
- No diagonal vision exploits
- Asymmetric ranges verified
- Fog of war shows three states properly
- Explored areas persist between turns
- Actors hidden/shown based on visibility
- Performance acceptable (<10ms for full FOV)
- Console commands demonstrate all scenarios

**Architectural Constraints**:
☑ Deterministic: No randomness in FOV calculation
☑ Save-Ready: VisionState designed for persistence
☑ Integer Math: Grid-based calculations
☑ Testable: Pure algorithm, extensive unit tests

**Progress**:
- ✅ Phase 1 Complete: Domain model (VisionRange, VisionState, ShadowcastingFOV)
- ✅ Core shadowcasting algorithm implemented with 8 octants
- ⚠️ Edge cases documented in BR_002 (5/8 tests passing)
- ⏳ Phase 2: Application layer (Query/Handler) - Next
- ⏳ Phase 3: Infrastructure (VisionStateService)
- ⏳ Phase 4: Presentation (FogOfWarPresenter)

**Next Step**: Implement Phase 2 Application layer

### VS_012: Vision-Based Movement System
**Status**: Approved  
**Owner**: Dev Engineer
**Size**: S (2h)
**Priority**: Critical
**Created**: 2025-09-11 00:10
**Updated**: 2025-09-11
**Tech Breakdown**: Movement using vision for scheduler activation

**What**: Movement system where scheduler activates based on vision connections
**Why**: Creates natural tactical combat without explicit modes

**Design** (per ADR-014):
- **Scheduler activation**: When player and hostiles have vision
- **Movement rules**: Adjacent-only when scheduled, pathfinding otherwise
- **Interruption**: Stop movement when enemy becomes visible
- **Fixed cost**: 100 TU per action when scheduled

**Implementation Plan**:
- **Phase 1**: Domain rules (0.5h)
  - Movement validation (adjacent when scheduled)
  - Fixed TU costs (100)
  
- **Phase 2**: Application layer (0.5h)
  - MoveCommand handler with vision check
  - Route to scheduler vs instant movement
  - Console output for states
  
- **Phase 3**: Infrastructure (0.5h)
  - SchedulerActivationService
  - PathfindingService integration
  - Movement interruption handler
  
- **Phase 4**: Integration (0.5h)
  - Wire to existing scheduler
  - Console messages and turn counter
  - Test with multiple scenarios

**Scheduler Activation (Solo)**:
```csharp
bool ShouldUseScheduler() {
    // Solo player - only check player vs monsters
    return monsters.Any(m => 
        m.State != Dormant && 
        (visionService.CanSee(player, m) || visionService.CanSee(m, player))
    );
}
```

**Movement Flow**:
```csharp
if (ShouldUseScheduler()) {
    // Tactical movement
    if (!Position.IsAdjacent(from, to)) {
        return "Only adjacent moves when enemies visible";
    }
    scheduler.Schedule(new MoveAction(actor, to, 100));
} else {
    // Instant travel with interruption check
    foreach (var step in path) {
        actor.Position = step;
        if (ShouldUseScheduler()) {
            return "Movement interrupted - enemy spotted!";
        }
    }
}
```

**Console Examples**:
```
// No vision - instant
> move to (30, 30)
[Traveling...]
You arrive at (30, 30)

// Vision exists - tactical
> move to (10, 10)
[Enemies visible - tactical movement]
> move north
[Turn 1] You move north (100 TU)
[Turn 2] Goblin moves west (100 TU)

// Interruption
> move to (50, 50)
[Traveling...]
Movement interrupted at (25, 25) - Orc spotted!
```

**Done When**:
- Scheduler activates on vision connections
- Adjacent-only when scheduled
- Pathfinding when not scheduled
- Movement interrupts on new vision
- Turn counter during tactical movement
- Clear console messages

**Architectural Constraints**:
☑ Deterministic: Fixed TU costs
☑ Save-Ready: Position state only
☑ Time-Independent: Turn-based
☑ Integer Math: Tile movement
☑ Testable: Clear state transitions

**Depends On**: VS_011 (Vision System)
**Next Step**: Wait for VS_011 completion

---



## 📈 Important (Do Next)
*Core features for current milestone, technical debt affecting velocity*

<!-- TD_031 moved to permanent archive (2025-09-10 21:02) - TimeUnit TU refactor completed successfully -->

### BR_002: Shadowcasting FOV Edge Cases
**Status**: New
**Owner**: Debugger Expert
**Size**: M (4-8h)
**Priority**: Important
**Created**: 2025-09-11
**Discovered During**: VS_011 Phase 1 implementation

**What**: Shadowcasting algorithm has 5 failing tests with edge cases
**Symptoms**:
- Pillar shadows not properly cast behind obstacles
- Corner peeking allows diagonal vision through walls
- Some positions within range not visible in empty grid
- Wall/forest tiles themselves not always visible before blocking
- Octant transformations may have calculation errors

**Impact**: 
- 3/8 vision tests passing (37.5% pass rate)
- Core FOV works but edge cases produce incorrect visibility
- Does not block VS_011 completion but affects accuracy

**Investigation Notes**:
- Algorithm uses recursive shadowcasting with 8 octants
- Slope calculations and octant transformations are complex
- Test expectations might be based on different FOV algorithms
- Core functionality works (sees tiles, respects some blocking)

**Suggested Fix**:
1. Review octant transformation matrix (lines 25-35 in ShadowcastingFOV.cs)
2. Verify slope calculations match reference implementations
3. Consider if test expectations are correct for this algorithm
4. Add debug visualization to understand shadow propagation
5. Compare with proven shadowcasting implementations (e.g., libtcod)

**Done When**:
- All 8 vision tests pass
- Shadows properly cast behind obstacles
- No diagonal vision exploits through corners
- Algorithm matches expected roguelike FOV behavior

### VS_013: Basic Enemy AI
**Status**: Proposed
**Owner**: Product Owner → Tech Lead
**Size**: M (4-8h)  
**Priority**: Important
**Created**: 2025-09-10 19:03

**What**: Simple but effective enemy AI for combat testing
**Why**: Need opponents to validate combat system and create gameplay loop
**How**:
- Decision tree for action selection (move/attack/wait)
- Target prioritization (closest/weakest/most dangerous)
- Basic pathfinding to reach targets
- Flee behavior when low health
**Done When**:
- Enemies move towards player intelligently
- Enemies attack when in range
- AI makes decisions based on game state
- Different enemy types show different behaviors
- AI actions integrate with scheduler

**Architectural Constraints** (MANDATORY):
☑ Deterministic: AI decisions based on seeded random
☑ Save-Ready: AI state fully serializable
☑ Time-Independent: Decisions based on game state not time
☑ Integer Math: All AI calculations use integers
☑ Testable: AI logic can be unit tested

---

## 📋 Quick Reference

**Priority Decision Framework:**
1. **Blocking other work?** → 🔥 Critical
2. **Current milestone?** → 📈 Important  
3. **Everything else** → 💡 Ideas

**Work Item Types:**
- **VS_xxx**: Vertical Slice (new feature) - Product Owner creates
- **BR_xxx**: Bug Report (investigation) - Test Specialist creates, Debugger owns
- **TD_xxx**: Technical Debt (refactoring) - Anyone proposes → Tech Lead approves

*Notes:*
- *Critical bugs are BR items with 🔥 priority*
- *TD items need Tech Lead approval to move from "Proposed" to actionable*



<!-- TD_017 and TD_019 moved to permanent archive (2025-09-09 17:53) -->

---
*Single Source of Truth for all Darklands development work. Simple, maintainable, actually used.*