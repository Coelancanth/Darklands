# Darklands Development Backlog


**Last Updated**: 2025-09-17 09:36 (Dev Engineer - Added TD_056 logging architecture strategy)

**Last Aging Check**: 2025-08-29
> 📚 See [Workflow.md - Backlog Aging Protocol](Workflow.md#-backlog-aging-protocol---the-3-10-rule) for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 008
- **Next TD**: 057
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




### TD_047: Unify Error Handling with LanguageExt
**Status**: Approved
**Owner**: Dev Engineer
**Size**: M (6-8h)
**Priority**: Important - Debugging complexity
**Created**: 2025-09-16 19:29 (Tech Lead)
**Complexity**: 5/10
**Markers**: [ERROR-HANDLING] [TECHNICAL-DEBT]

**What**: Replace all try-catch blocks with LanguageExt Fin<T> for consistent error handling
**Why**: Mixed error handling (try-catch vs Fin<T>) breaks functional composition and makes debugging harder

**Scope** (System-wide):
1. Infrastructure services (remaining try-catch blocks)
2. Presenters (error propagation to UI)
3. Command handlers (side effect isolation)
4. Godot integration points (thread marshalling errors)

**Done When**:
- [ ] Zero try-catch blocks outside Godot integration boundary
- [ ] All errors flow through Fin<T> pipeline
- [ ] Error aggregation uses LanguageExt combinators
- [ ] Performance unchanged (measure before/after)
- [ ] All 664 tests still pass

**Reference Pattern**: `ExecuteAttackCommandHandler` for Fin<T> usage

### TD_056: Unified Logging Architecture Strategy
**Status**: Proposed
**Owner**: Tech Lead
**Size**: M (4-6h analysis + implementation)
**Priority**: Important - Developer Experience
**Created**: 2025-09-17 09:34 (Dev Engineer)
**Complexity**: 7/10
**Markers**: [LOGGING] [ARCHITECTURE] [DEVELOPER-EXPERIENCE]

**Problem**: Multiple logging systems capture different messages, creating gaps in saved logs
**Root Cause**: 4 separate logging systems running in parallel with different outputs

**Current Architecture Analysis**:
1. **Serilog System**: Infrastructure layer → `darklands-current20250917.log`
2. **UnifiedCategoryLogger**: Business logic → `darklands-session-*.log`
3. **GodotCategoryLogger**: Godot integration → Console only (no file)
4. **Direct Godot**: `GD.Print()` calls → Console only (no file)

**Issue**: Systems 3 & 4 deliberately don't write to files due to Godot sandbox constraints

**Strategic Questions for Tech Lead**:
- Should we accept console-only messages as architectural constraint?
- Is a unified file output worth the complexity/risk of file conflicts?
- Can we create safe bridge from Godot console to file system?
- Should we standardize on fewer logging systems?

**Previous Attempt**: Dev Engineer tried unification but caused file access conflicts that broke game

**Options to Evaluate**:
A) **Accept Current** (Low risk): Document architecture, improve dev workflow
B) **Safe Bridge** (Medium risk): Forward Godot messages via IPC/queue to file
C) **Full Unification** (High risk): Redesign all logging through single system

**Success Criteria** (TBD by Tech Lead):
- [ ] Complete logging architecture documented
- [ ] Strategy decision made and communicated
- [ ] Implementation plan created
- [ ] Developer experience improved
- [ ] No game stability regression

**Files**: `GameStrapper.cs`, `UnifiedCategoryLogger.cs`, `GodotCategoryLogger.cs`








## 📋 Blocked - Waiting for Dependencies

*Items that cannot start until blocking dependencies are resolved*

### VS_014: A* Pathfinding Foundation
**Status**: Approved - BLOCKED by TD_046
**Owner**: Dev Engineer
**Size**: S (3h)
**Priority**: Critical - Foundation for movement system
**Created**: 2025-09-11 18:12
**Updated**: 2025-09-16 01:00 (Tech Lead - Moved to blocked section)
**Markers**: [MOVEMENT] [PATHFINDING] [CHAIN-2-MOVEMENT] [BLOCKED]
**Blocking Dependency**: TD_046 (project structure must be established first)

**What**: Implement A* pathfinding algorithm with visual path display
**Why**: Foundation for VS_012 movement system and all future tactical movement

**DEPENDENCY CHAIN**: Chain 2 - Step 1 (Movement Foundation)
- Blocked by: TD_046 (architectural foundation)
- Enables: VS_012 (Vision-Based Movement)
- Blocks: VS_013 (Enemy AI needs movement)

**Done When**:
- [ ] A* finds optimal paths deterministically
- [ ] Diagonal movement works correctly (1.41x cost)
- [ ] Path visualizes on grid before movement
- [ ] Performance <10ms for typical paths (50 tiles)
- [ ] Handles no-path-exists gracefully (returns None)
- [ ] All tests pass including edge cases

**Architectural Constraints**:
☑ Deterministic: Consistent tie-breaking rules
☑ Save-Ready: Paths are transient, not saved
☑ Time-Independent: Pure algorithm
☑ Integer Math: Use 100/141 for movement costs
☑ Testable: Pure domain function

### VS_012: Vision-Based Movement System
**Status**: Approved - BLOCKED by VS_014
**Owner**: Dev Engineer
**Size**: S (2h)
**Priority**: Critical
**Created**: 2025-09-11 00:10
**Updated**: 2025-09-16 01:00 (Tech Lead - Moved to blocked section)
**Markers**: [MOVEMENT] [VISION] [CHAIN-2-MOVEMENT] [BLOCKED]
**Blocking Dependency**: VS_014 (A* Pathfinding Foundation)

**What**: Movement system where scheduler activates based on vision connections
**Why**: Creates natural tactical combat without explicit modes

**DEPENDENCY CHAIN**: Chain 2 - Step 2 (Vision-Based Movement)
- Blocked by: VS_014 (needs pathfinding for non-adjacent movement)
- Enables: VS_013 (Enemy AI needs movement system)

**Architectural Constraints**:
☑ Deterministic: Fixed TU costs
☑ Save-Ready: Position state only
☑ Time-Independent: Turn-based
☑ Integer Math: Tile movement
☑ Testable: Clear state transitions

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