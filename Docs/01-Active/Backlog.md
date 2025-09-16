# Darklands Development Backlog


**Last Updated**: 2025-09-16 22:06 (Backlog Assistant - Archived 3 completed TD items)

**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 008
- **Next TD**: 056
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

### Chain 1: Architecture Foundation (MUST be first)
```
TD_046 → (All other work depends on this)
├─ Enables: Clean separation of concerns
├─ Enables: Compile-time MVP enforcement
└─ Blocks: All VS and complex TD items until complete
```

### Chain 2: Movement & Vision System
```
VS_014 (A* Pathfinding) → VS_012 (Vision-Based Movement) → VS_013 (Enemy AI)
├─ VS_014: Foundation for all movement
├─ VS_012: Tactical movement using pathfinding
└─ VS_013: AI needs movement system to function
```

### Chain 3: Technical Debt Cleanup
```
TD_035 (Error Handling) → TD_046 completion
├─ Can be done in parallel with TD_046
└─ Should be completed before new feature development
```

### Chain 4: Future Features (After foundations)
```
All IDEA_* items depend on:
├─ Chain 1 (Architecture) - COMPLETE
├─ Chain 2 (Movement/Vision) - COMPLETE
└─ Chain 3 (Technical Debt) - COMPLETE
```

## 🚀 Ready for Immediate Execution

*Items with no blocking dependencies, approved and ready to start*





### TD_054: Dependency Chain Maintenance Protocol
**Status**: Approved
**Owner**: Tech Lead
**Size**: S (2h)
**Priority**: Important - Planning accuracy
**Created**: 2025-09-16 19:32 (Tech Lead)
**Complexity**: 3/10
**Markers**: [PROCESS] [PLANNING]

**What**: Create protocol for maintaining accurate dependency chains in backlog
**Why**: Chains drift out of sync, causing confusion about what's actually blocked

**Protocol Elements**:
1. Weekly dependency review (during planning)
2. Automated chain validation script
3. Clear "blocks/blocked-by" notation
4. Status transitions when dependencies resolve

**Implementation**:
```powershell
./scripts/backlog/validate-dependencies.ps1
# Checks all items for:
# - Broken dependency references
# - Completed blockers not removed
# - Circular dependencies
# - Items marked blocked without blockers
```

**Done When**:
- [ ] Protocol documented in PROTOCOLS.md
- [ ] Validation script created
- [ ] Backlog template updated
- [ ] All current chains validated
- [ ] CI check for PR updates

### TD_050: NetArchTest for Architecture Enforcement
**Status**: Approved - CRITICAL
**Owner**: Test Specialist
**Size**: S (4h)
**Priority**: Critical - Prevents architecture violations
**Created**: 2025-09-16 19:29 (Tech Lead)
**Complexity**: 4/10
**Markers**: [ARCHITECTURE] [TESTING] [SAFETY-CRITICAL]

**What**: Add NetArchTest to enforce Clean Architecture boundaries after TD_046
**Why**: Without automated tests, developers can accidentally violate architecture (Domain → Infrastructure)

**Scope**:
1. Domain purity tests (no external dependencies)
2. Layer dependency tests (Domain ← Application ← Infrastructure)
3. Feature isolation tests (Features don't reference each other)
4. Godot containment tests (Godot types only in main project)

**Done When**:
- [ ] Domain project has zero dependency tests
- [ ] Layer violations fail CI build
- [ ] Feature cross-references are detected
- [ ] Godot type leakage is prevented
- [ ] Tests run in <1s in CI pipeline

**Reference**: TD_046 created the boundaries, this enforces them

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

### TD_048: Update Protocols to Reference Established Patterns
**Status**: Approved
**Owner**: Tech Lead
**Size**: S (2h)
**Priority**: Important - Developer friction
**Created**: 2025-09-16 19:29 (Tech Lead)
**Complexity**: 2/10
**Markers**: [DOCUMENTATION] [DEVELOPER-EXPERIENCE]

**What**: Update all protocol docs to link to actual pattern implementations
**Why**: Developers waste time searching for patterns that already exist

**Scope**:
1. HANDBOOK.md - Link to Move Block pattern
2. Persona docs - Reference actual code examples
3. ADRs - Add "Implementation" sections with file paths
4. CLAUDE.md - Include pattern directory

**Done When**:
- [ ] Every mentioned pattern has a code reference
- [ ] File paths use format: `src/Features/Block/Move/Commands.cs:45`
- [ ] New developer can find any pattern in <30 seconds
- [ ] VSA organization clearly documented with examples




### TD_035: Standardize Error Handling in Infrastructure Services
**Status**: Approved - Can run parallel with TD_046
**Owner**: Dev Engineer
**Size**: S (3h)
**Priority**: Important - Technical debt cleanup
**Created**: 2025-09-11 18:07
**Updated**: 2025-09-16 01:00 (Tech Lead - Moved to immediate execution)
**Complexity**: 3/10
**Markers**: [TECHNICAL-DEBT] [ERROR-HANDLING] [CHAIN-3-CLEANUP]

**What**: Replace remaining try-catch blocks with Fin<T> in infrastructure services
**Why**: Inconsistent error handling breaks functional composition and makes debugging harder

**DEPENDENCY CHAIN**: Chain 3 - Can run parallel with TD_046
- Compatible with: TD_046 (different code areas)
- Should complete: Before new VS development

**Scope** (LIMITED TO):
1. **PersistentVisionStateService** (7 try-catch blocks)
2. **GridPresenter** (3 try-catch in event handlers)
3. **ExecuteAttackCommandHandler** (mixed side effects)

**Done When**:
- [ ] Zero try-catch blocks in listed services
- [ ] All errors flow through Fin<T> consistently
- [ ] Side effects isolated into dedicated methods
- [ ] Performance unchanged (measure before/after)
- [ ] All existing tests still pass

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

## 🗂️ Archived - Completed or Rejected

*Items moved out of active development*







## 🔄 Execution Summary
**Status**: Approved - Ready for implementation
**Owner**: Dev Engineer
**Size**: L (8h total) - Project extraction (3h) + EventAwareNode refactor (1h) + Feature namespaces (4h)
**Priority**: Important - Architectural clarity and purity enforcement
**Created**: 2025-09-15 23:15 (Tech Lead)
**Updated**: 2025-09-16 00:30 (Tech Lead - Created detailed migration plan)
**Complexity**: 4/10 - Increased due to EventAwareNode refactoring
**Markers**: [ARCHITECTURE] [CLEAN-ARCHITECTURE] [FEATURE-ORGANIZATION] [BREAKING-CHANGE]
**ADRs**: ADR-021 (minimal separation with MVP), ADR-018 (DI alignment updated)
**Migration Plan**: Docs/01-Active/TD_046_Migration_Plan.md

**What**: Extract Domain and Presentation to separate projects + reorganize into feature namespaces
**Why**: Enforce architectural purity at compile-time while eliminating namespace collisions

**Final Project Structure**:
```
Darklands.csproj            → Godot Views & Entry (has Godot references)
Darklands.Domain.csproj     → Pure domain logic (NO external dependencies)
Darklands.Core.csproj       → Application & Infrastructure (NO Godot references)
Darklands.Presentation.csproj → Presenters & View Interfaces (NO Godot references)
```

**Combined Implementation Plan**:

### Part 1: Project Extraction (3h)
1. **Create Domain Project** (30min):
   - Create `src/Darklands.Domain/Darklands.Domain.csproj`
   - Add to solution
   - Reference from Core project

2. **Move Domain Types** (1h):
   - Move entire `src/Domain/` to `src/Darklands.Domain/`
   - Update namespace from `Darklands.Core.Domain` to `Darklands.Domain`

3. **Fix References** (30min):
   - Update all using statements
   - Verify build succeeds

### Part 2: Feature Organization (4h)
Apply to ALL layers (Domain, Application, Infrastructure, Presentation):

```
Domain.World/      → Grid, Tile, Position
Domain.Characters/ → Actor, Health, ActorState
Domain.Combat/     → Damage, TimeUnit, AttackAction
Domain.Vision/     → VisionRange, VisionState, ShadowcastingFOV

Application.World.Commands/
Application.Characters.Handlers/
Application.Combat.Commands/
Application.Vision.Queries/

(Similar for Infrastructure and Presentation)
```

**Done When**:
- [ ] Domain project created and referenced
- [ ] Domain types extracted with no external dependencies
- [ ] Feature namespaces applied consistently across all layers
- [ ] No namespace-class collisions exist
- [ ] All 662 tests pass
- [ ] Architecture test validates domain purity
- [ ] IntelliSense shows clear, intuitive suggestions

**Benefits**:
- Compile-time domain purity enforcement
- No namespace collisions
- Intuitive feature organization
- Minimal complexity (just 3 projects total)
- Aligns with post-TD_042 simplification







---

## 💡 Future Ideas - Chain 4 Dependencies

*Features and systems to consider when foundational work is complete*

**DEPENDENCY CHAIN**: All future ideas are Chain 4 - blocked until prerequisites complete:
- ✅ Chain 1 (Architecture Foundation): TD_046 → MUST COMPLETE FIRST
- ⏳ Chain 2 (Movement/Vision): VS_014 → VS_012 → VS_013
- ⏳ Chain 3 (Technical Debt): TD_035
- 🚫 Chain 4 (Future Features): Cannot start until Chains 1-3 complete

### IDEA_001: Life-Review/Obituary System
**Status**: Future Consideration
**Owner**: Unassigned
**Size**: L (2-3 days)
**Priority**: Ideas
**Created**: 2025-09-12

**What**: Battle Brothers-style obituary and company history system
**Why**: Creates narrative and emotional attachment to characters
**How**: 
- Track all character events (battles, injuries, level-ups, deaths)
- Generate procedural obituaries for fallen characters
- Company timeline showing major events
- Statistics and achievements per character
**Technical Approach**: 
- Separate IGameHistorian system (not debug logging)
- SQLite or JSON for structured event storage
- Query system for generating reports
**Reference**: ADR-007 Future Considerations section

### IDEA_002: Economy Analytics System  
**Status**: Future Consideration
**Owner**: Unassigned
**Size**: M (1-2 days)
**Priority**: Ideas
**Created**: 2025-09-12

**What**: Track economic metrics for balance analysis
**Why**: Balance item prices, loot tables, and gold flow
**How**:
- Record all transactions (buy/sell/loot/reward)
- Aggregate metrics (avg gold per battle, popular items)
- Export reports for balance decisions
**Technical Approach**:
- Separate IEconomyTracker system (not debug logging)
- Aggregated analytics database
- Periodic report generation
**Reference**: ADR-007 Future Considerations section

### IDEA_003: Player Analytics Dashboard
**Status**: Future Consideration  
**Owner**: Unassigned
**Size**: L (3-4 days)
**Priority**: Ideas
**Created**: 2025-09-12

**What**: Comprehensive player behavior analytics
**Why**: Understand difficulty spikes, player preferences, death patterns
**How**:
- Heat maps of death locations
- Progression funnel analysis
- Play session patterns
- Difficulty curve validation
**Technical Approach**:
- Separate IPlayerAnalytics system (not debug logging)
- Event stream processing
- Visual dashboard for analysis
**Reference**: ADR-007 Future Considerations section

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