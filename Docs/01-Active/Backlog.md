# Darklands Development Backlog


**Last Updated**: 2025-09-10 21:02 (TD_031 TimeUnit TU refactor completed and archived)

**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 002
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

### VS_011: Vision/FOV System with Shadowcasting
**Status**: Approved  
**Owner**: Dev Engineer
**Size**: M (4h)
**Priority**: Critical
**Created**: 2025-09-10 19:03
**Updated**: 2025-09-11
**Tech Breakdown**: FOV system using recursive shadowcasting

**What**: Field-of-view system with asymmetric vision ranges and proper occlusion
**Why**: Foundation for ALL combat, AI, and stealth features

**Design** (per ADR-014):
- **Uniform algorithm**: All actors use shadowcasting FOV
- **Asymmetric ranges**: Different actors see different distances
- **Wake states**: Dormant monsters skip FOV calculation
- **Caching**: FOV cached per turn for performance

**Vision Ranges**:
- Player: 8 tiles
- Goblin: 5 tiles
- Orc: 6 tiles
- Eagle: 12 tiles

**Implementation Plan**:
- **Phase 1**: Domain model (0.5h)
  - VisionRange value object
  - Monster activation states
  - FOV result structures
  
- **Phase 2**: Shadowcasting algorithm (2h)
  - Recursive shadowcasting implementation
  - Octant calculation
  - Shadow queue management
  - Wall occlusion logic
  
- **Phase 3**: Application layer (0.5h)
  - VisionQuery handler
  - GetVisibleActors query
  - FOV caching system
  - Console commands for testing
  
- **Phase 4**: Testing & validation (1h)
  - Unit tests for shadowcasting
  - Wake state transition tests
  - Performance benchmarking
  - Console test scenarios

**Core Algorithm** (Uniform Shadowcasting):
```csharp
public class VisionSystem {
    private Dictionary<Actor, HashSet<Position>> fovCache = new();
    
    public HashSet<Position> CalculateFOV(Actor actor) {
        // Skip dormant monsters
        if (actor is Monster m && m.State == Dormant) {
            return new HashSet<Position>();
        }
        
        // Check cache
        if (fovCache.ContainsKey(actor) && !actor.HasMoved) {
            return fovCache[actor];
        }
        
        // All actors use same shadowcasting
        var fov = RecursiveShadowcast(actor.Position, actor.VisionRange);
        fovCache[actor] = fov;
        return fov;
    }
}
```

**Console Test Commands**:
```
> fov show player
Player FOV (range 8):
# # # . . . # # #
# . . . . . . . #
# . . @ . . . . #
# . . . . . . . #
# # # . # # # # #

> vision check player goblin
Player (8 range) CAN see Goblin at distance 6
Goblin (5 range) CANNOT see Player at distance 6

> vision list player
Player can see: Goblin (5,3), Orc (7,2)
Goblin can see: Orc (7,2)
Player not visible to: Goblin
```

**Done When**:
- Shadowcasting FOV works correctly
- No diagonal vision exploits
- Asymmetric ranges verified
- Can hide behind corners
- Performance acceptable (<10ms for full FOV)
- Console commands demonstrate all scenarios

**Architectural Constraints**:
☑ Deterministic: No randomness in FOV calculation
☑ Save-Ready: FOV is pure calculation, no state
☑ Integer Math: Grid-based calculations
☑ Testable: Pure algorithm, extensive unit tests

**Next Step**: Research shadowcasting algorithm details

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