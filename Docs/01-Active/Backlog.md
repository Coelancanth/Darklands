# Darklands Development Backlog


**Last Updated**: 2025-09-12 16:20 (Tech Lead simplified TD_032 using modular-monolith pluralization strategy)

**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 008
- **Next TD**: 046
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

## 🔥 Critical (Do First)
*Blockers preventing other work, production bugs, dependencies for other features*

### TD_041: DDD Phase 1 - Foundation Patterns (ADR-017)
**Status**: Approved
**Owner**: Dev Engineer
**Size**: S (4h)
**Priority**: Critical
**Created**: 2025-09-12 16:13
**Markers**: [ARCHITECTURE] [DDD] [PHASE-1]

**What**: Implement foundation patterns for DDD bounded contexts
**Why**: Enable true module isolation without breaking existing code

**Implementation Steps**:
1. Create Contracts assemblies for each context
   - `Darklands.Tactical.Contracts.csproj`
   - `Darklands.Diagnostics.Contracts.csproj`
   - `Darklands.Platform.Contracts.csproj`
2. Add interfaces to SharedKernel
   - `IDomainEvent` for internal events
   - `IContractEvent` for public API events
   - `IBusinessRule` for validation
3. Implement `TypedIdValueBase` for strongly-typed IDs
4. Add `Entity` base class with domain event collection
5. Create architecture tests with smart exclusions

**Done When**:
- [ ] Contracts assemblies created (empty initially)
- [ ] SharedKernel interfaces added
- [ ] Architecture tests pass with exclusions
- [ ] Single MediatR configured for both event types
- [ ] No existing code broken

**Tech Lead Decision** (2025-09-12):
- Start with empty Contracts assemblies
- Add events incrementally as we refactor
- Existing code continues working unchanged

### TD_042: DDD Phase 2 - Migrate First Vertical Slice
**Status**: Proposed
**Owner**: Tech Lead → Dev Engineer
**Size**: M (6h)
**Priority**: Critical
**Created**: 2025-09-12 16:13
**Depends On**: TD_041
**Markers**: [ARCHITECTURE] [DDD] [PHASE-2]

**What**: Migrate Attack feature to new DDD structure as proof of concept
**Why**: Validate the pattern with a real feature before full migration

**Implementation Steps**:
1. Create `Features/Attack/` folder structure
2. Move attack-related code to vertical slice
3. Create `ActorDamagedContractEvent` in Contracts
4. Implement `TacticalContractAdapter` for event mapping
5. Wire up Diagnostics to consume contract event

**Done When**:
- [ ] Attack feature follows VSA structure
- [ ] Domain events stay internal
- [ ] Contract events cross boundaries
- [ ] Diagnostics receives events via Contracts
- [ ] All attack tests still pass

### TD_043: DDD Phase 3 - Complete Tactical Context Migration
**Status**: Proposed
**Owner**: Tech Lead → Dev Engineer
**Size**: L (2 days)
**Priority**: Important
**Created**: 2025-09-12 16:13
**Depends On**: TD_042
**Markers**: [ARCHITECTURE] [DDD] [PHASE-3]

**What**: Migrate all Tactical features to VSA + Contracts structure
**Why**: Complete the bounded context transformation

**Features to Migrate**:
- Movement → `Features/Movement/`
- Vision → `Features/Vision/`
- Combat Scheduler → `Features/Scheduler/`
- Shared aggregates → `Domain/Aggregates/`

**Done When**:
- [ ] All features in VSA structure
- [ ] All cross-context events in Contracts
- [ ] Module isolation tests pass
- [ ] No direct references between contexts

### TD_040: Extract Diagnostics Bounded Context
**Status**: Updated → Depends on TD_041
**Owner**: Dev Engineer  
**Size**: M (6h) - Reduced with new approach
**Priority**: Important (no longer critical)
**Depends On**: TD_041
**Created**: 2025-09-12 14:52
**Updated**: 2025-09-12 15:45
**Markers**: [ARCHITECTURE] [DDD]

**What**: Create separate Diagnostics bounded context with assembly boundaries
**Why**: Enables non-deterministic types without violating ADR-004, enforces true isolation

**Problem**: 
- Performance monitoring needs DateTime/double (non-deterministic)
- Namespace-only separation allows accidental coupling
- Using ActorId in Diagnostics violates context isolation
- Need compile-time enforcement of boundaries

**Solution - Assembly-Based Bounded Contexts**:

1. **Phase 1: Create Assembly Structure** (2h)
   ```xml
   <!-- Create separate projects -->
   src/Diagnostics/Darklands.Diagnostics.Domain.csproj
   src/Diagnostics/Darklands.Diagnostics.Application.csproj  
   src/Diagnostics/Darklands.Diagnostics.Infrastructure.csproj
   ```

2. **Phase 2: Use Shared Identity Types** (2h)
   ```csharp
   // SharedKernel - EntityId (NOT ActorId!)
   public readonly record struct EntityId(Guid Value);
   
   // Diagnostics uses EntityId, never ActorId
   public record VisionPerformanceReport(
       DateTime Timestamp,
       Dictionary<EntityId, double> Metrics  // ✅ EntityId not ActorId
   );
   ```

3. **Phase 3: Integration Event Bus** (2h)
   - Separate bus for cross-context events
   - Integration events use primitives only
   - Versioning and correlation IDs

4. **Phase 4: Main Thread Dispatcher** (2h)
   - Implement IMainThreadDispatcher
   - Ensure Godot calls on main thread
   - Update presenters to use dispatcher

**Assembly References**:
```
Darklands.csproj (Main)
├─> Tactical.Application
├─> Diagnostics.Application
├─> Platform.Infrastructure.Godot
└─> SharedKernel

NO cross-context references!
```

**Done When**:
- [ ] Separate assemblies created for Diagnostics
- [ ] Using EntityId instead of ActorId
- [ ] Integration event bus implemented
- [ ] Main thread dispatcher working
- [ ] Architecture tests enforce assembly boundaries
- [ ] No direct references between contexts

**Tech Lead Decision**:
- Assembly boundaries provide compile-time safety
- Dual event bus strategy (MediatR + Integration)
- NO scoped services (Singleton or Transient only)
- See ADR-017 (revised) for complete strategy



## 📈 Important (Do Next)
*Core features for current milestone, technical debt affecting velocity*

### TD_044: DDD Phase 4 - Platform & Diagnostics Contexts
**Status**: Proposed
**Owner**: Tech Lead → Dev Engineer
**Size**: M (8h)
**Priority**: Important
**Created**: 2025-09-12 16:13
**Depends On**: TD_042
**Markers**: [ARCHITECTURE] [DDD] [PHASE-4]

**What**: Complete Platform and Diagnostics bounded contexts
**Why**: Finish the context separation for all non-tactical concerns

**Implementation Steps**:
1. Move performance monitoring to Diagnostics context
2. Use EntityId (not ActorId) in Diagnostics
3. Move audio/input abstractions to Platform context
4. Create Platform.Contracts for audio/input events
5. Update all references to use Contracts only

**Done When**:
- [ ] Diagnostics uses only EntityId and contract events
- [ ] Platform handles all Godot abstractions
- [ ] No cross-context direct references
- [ ] Architecture tests pass

### TD_045: DDD Phase 5 - Documentation & Training
**Status**: Proposed
**Owner**: Tech Lead
**Size**: S (3h)
**Priority**: Important
**Created**: 2025-09-12 16:13
**Depends On**: TD_043
**Markers**: [ARCHITECTURE] [DDD] [PHASE-5]

**What**: Update all documentation and create training materials
**Why**: Ensure team understands new architecture

**Deliverables**:
1. Update all persona docs with DDD guidance
2. Create example features showing patterns
3. Update CLAUDE.md with new structure
4. Hold team review session
5. Create troubleshooting guide

**Done When**:
- [ ] All personas reference DDD protocol
- [ ] Example code demonstrates patterns
- [ ] Team understands where features go
- [ ] Common mistakes documented

<!-- TD_031 moved to permanent archive (2025-09-10 21:02) - TimeUnit TU refactor completed successfully -->


### TD_032: Fix Namespace-Class Collisions (Pluralization Strategy)
**Status**: Revised - Simple Solution
**Owner**: Dev Engineer
**Size**: S (2h) - Reduced complexity
**Priority**: Important
**Created**: 2025-09-11
**Updated**: 2025-09-12 16:18 (Tech Lead simplified using modular-monolith pattern)
**Complexity**: 1/10 - Much simpler now
**References**: modular-monolith-with-ddd namespace strategy

**What**: Use pluralized folder names to eliminate namespace-class collisions
**Why**: Current `Domain.Grid.Grid` is verbose; plural folders solve this elegantly

**Simple Implementation** (inspired by modular-monolith-with-ddd):
1. **Rename Aggregate Folders** (1h):
   - `Domain/Actor/` → `Domain/Actors/` (plural)
   - `Domain/Grid/` → `Domain/Grids/` (plural)
   - Keep class names singular: `Actor`, `Grid`
   
2. **Update Namespace Declarations** (1h):
   - Change `namespace Domain.Actor` → `namespace Domain.Actors`
   - Change `namespace Domain.Grid` → `namespace Domain.Grids`
   - Update all using statements

**Result**:
- Before: `Domain.Grid.Grid` (collision!)
- After: `Domain.Grids.Grid` (no collision!)
- Clean references: `Actors.Actor`, `Grids.Grid`
   
3. **Tests** (1h):
   - Update test imports
   - Verify all tests pass

**Done When**:
- No namespace-class collisions remain
- All tests pass without warnings
- Architecture fitness tests validate structure
- IntelliSense shows clear suggestions

**Technical Notes**:
- Single atomic PR for entire refactoring
- No behavior changes, pure reorganization
- Follow bounded context pattern from ADR-015





### TD_039: Fix Application→Infrastructure Boundary Violations
**Status**: ✅ COMPLETED  
**Owner**: Dev Engineer → Completed
**Size**: S (2h) → Actual: 2.5h
**Priority**: Important
**Created**: 2025-09-12 13:59
**Updated**: 2025-09-12 14:52
**Complexity**: 2/10
**Markers**: [ARCHITECTURE]

**What**: Fix inverted dependencies where Application layer references Infrastructure
**Why**: Violates Clean Architecture - dependencies should flow inward, not outward

**✅ Implementation Complete** (Dev Engineer 2025-09-12):

**Fixed Violations** (4/5):
1. ✅ `ActorFactory.cs` → Removed `Infrastructure.Debug`, uses `Domain.Debug.ICategoryLogger`
2. ✅ `InMemoryGridStateService.cs` → Removed `Infrastructure.Identity`, refactored to use DI for `IStableIdGenerator`
3. ✅ `GameLoopCoordinator.cs` → Removed `Infrastructure.Debug`, uses `Domain.Debug.ICategoryLogger`
4. ✅ `UIEventForwarder.cs` → Removed `Infrastructure.Debug`, uses `Domain.Debug.ICategoryLogger`

**Key Fixes Implemented**:
- **Service Locator Fix**: Refactored `InMemoryGridStateService` constructor to receive `IStableIdGenerator` via dependency injection instead of using `GuidIdGenerator.Instance`
- **Architecture Test Enhancement**: Updated `Application_Should_Not_Reference_Infrastructure` test to actively fail on violations (was previously just logging)
- **Clean Import Removal**: Eliminated all redundant Infrastructure imports where Domain interfaces were available

**⚡ Tech Lead Decision** (2025-09-12 14:52):
**REJECTED Option B (Domain Interface Pattern)** - Violates ADR-004 Deterministic Simulation!

The real issue isn't the boundary violation - it's that `VisionPerformanceReport` contains:
- `DateTime` (wall clock time) 
- `double` for timing measurements
- Non-deterministic performance metrics

Per ADR-004, these CANNOT exist in Domain layer as they break determinism.

**Solution**: Move the shared types to Domain BUT refactor them first:
1. Replace `DateTime` with turn/action counts
2. Replace `double` timings with integer microseconds  
3. Make metrics deterministic (tile counts, cache hits as integers)

This maintains Clean Architecture AND determinism. The types belong in Domain as they're shared contracts, but must be deterministic.

**Follow-up**: Create TD_040 to refactor VisionPerformanceReport for determinism, then move to Domain.




## 💡 Future Ideas (Not Current Priority)
*Features and systems to consider when foundational work is complete*


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