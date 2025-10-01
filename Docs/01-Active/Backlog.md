# Darklands Development Backlog


**Last Updated**: 2025-10-01 11:40 (VS_005 approved by Tech Lead with architecture breakdown)

**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 004
- **Next TD**: 002
- **Next VS**: 006 


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

## 🔥 Critical (Do First)
*Blockers preventing other work, production bugs, dependencies for other features*

### VS_005: Grid, FOV & Terrain System
**Status**: Approved → Ready for Implementation
**Owner**: Tech Lead → Dev Engineer (for phased implementation)
**Size**: M (1-2 days: 4h Domain + 4h Application + 6h Infrastructure + 6h Godot = 20h total)
**Priority**: Critical (tactical foundation for all combat)
**Markers**: [ARCHITECTURE] [PHASE-1-START]
**Created**: 2025-10-01 11:15
**Approved**: 2025-10-01 11:40 (Tech Lead)

**What**: Grid-based movement with libtcod-style FOV (via GoRogue library), terrain variety (wall/floor/smoke), and manually controlled dummy enemy for testing

**Why**:
- Positioning creates tactical depth (cover, line-of-sight, ambush mechanics)
- FOV is table-stakes for roguelike feel (exploration, fog of war, vision-based tactics)
- Terrain variety enables strategic choices (hide in smoke to break vision)
- Dummy enemy validates mechanics before AI complexity
- Foundation for all future combat features (VS_006-010 depend on this)

**How**:
- **GoRogue NuGet Integration** - Use RecursiveShadowcastingFOV (libtcod shadowcasting algorithm)
- **Study GoRogue source code** - Understand algorithm for future extensions (sound propagation, semi-transparency)
- **Godot TileMap** - 30x30 grid with 3 terrain types (wall/floor/smoke)
- **FOV Service** - Wraps GoRogue, adapts our GridMap to GoRogue's IMapView
- **Dummy Controls** - Arrow keys = player, WASD = dummy, Tab = switch FOV display

**Done When**:
- ✅ GoRogue package added, RecursiveShadowcastingFOV source studied and documented
- ✅ Unit tests: Wall blocks movement + vision, Smoke blocks vision only, Vision radius = 8 tiles
- ✅ Integration tests: MoveActorCommand → FOV recalculates, GetVisibleActorsQuery uses FOV
- ✅ Godot scene: 30x30 test map loads, player + dummy controllable, FOV visualization works
- ✅ Manual validation: Hide player behind smoke → dummy's FOV doesn't include player tile
- ✅ Fog of war persists correctly (explored tiles stay darker when not currently visible)

**Depends On**: VS_001 (Health System - already complete ✅)

**Product Owner Notes** (2025-10-01 11:15):
- Start here per Game Designer feedback (positioning before timing)
- Grid + FOV creates roguelike "feel" before adding time-unit complexity
- Dummy enemy testing validates each layer independently
- See detailed spec in [Roadmap.md](../02-Design/Game/Roadmap.md#vs_005-grid-fov--terrain-system)

---

**Tech Lead Decision** (2025-10-01 11:40):

**Architecture Approval**: ✅ **Proceed with implementation**

**Key Technical Decisions**:
1. **Adapter Pattern for GoRogue** - Core defines `IFOVService` abstraction, Infrastructure provides `GoRogueFOVService` implementation. This keeps GoRogue swappable and maintains Clean Architecture boundaries.

2. **Three-Layer Separation**:
   - **Domain**: `GridMap` entity with `TerrainType` enum (Wall/Floor/Smoke), enforces business rules
   - **Application**: Commands (`MoveActorCommand`) and Queries (`CalculateFOVQuery`, `GetVisibleActorsQuery`) using MediatR
   - **Infrastructure**: `TransparencyMapAdapter` bridges `GridMap` → GoRogue's `IMapView<bool>`, isolating external dependency

3. **Smoke Terrain Design** - Opaque (blocks vision) + Passable (can walk through) creates tactical depth. This differentiates smoke from walls and enables hide/ambush mechanics.

4. **Phase Enforcement** - Strict progression: Domain → Application → Infrastructure → Presentation. Each phase must pass tests before proceeding:
   - Phase 1 (Domain): Pure C#, `[Category("Phase1")]` tests, <10ms each
   - Phase 2 (Application): MOCKED `IFOVService`, `[Category("Phase2")]` tests
   - Phase 3 (Infrastructure): REAL GoRogue integration, `[Category("Phase3")]` tests with performance validation (<10ms)
   - Phase 4 (Presentation): Godot UI with manual testing checklist

**Risks Identified & Mitigated**:
- ⚠️ **GoRogue dependency lock-in** → Mitigated via `IFOVService` abstraction (swappable)
- ⚠️ **Performance (FOV recalc every move)** → Phase 3 performance test enforces <10ms (expected: 2-5ms for 30x30)
- ⚠️ **Fog of war complexity** → Separate concern in Phase 4, can ship without it if needed

**Implementation Notes**:
- Study GoRogue source during Phase 3, document findings in `Docs/03-Reference/GoRogue_FOV_Analysis.md`
- `GridMap` is singleton for Phase 1 (YAGNI), refactor to repository if Phase 3 needs multiple maps
- Fog of war is 3-state: Unexplored (black) → Explored (dark gray) → Visible (full brightness)

**Next Step**: Hand off to Dev Engineer for Phase 1 implementation (Domain layer)

**Command to Start**:
```bash
git checkout -b feat/VS_005-grid-fov-terrain
./scripts/core/build.ps1 test --filter "Category=Phase1"  # Should have 0 tests initially
```

---

**Dev Engineer Progress** (2025-10-01):

**✅ Phase 1 Complete** - Domain entities ([39a6755](https://github.com/user/repo/commit/39a6755))
- Implemented: `Position`, `TerrainType` (Floor/Wall/Smoke), `GridMap` (30x30 with Result<T>)
- Tests: 41 new Phase 1 tests (102 total suite, 0.38s execution)
- Railway-oriented: `IsPassable`/`IsOpaque` use functional composition via `.Map()`

**✅ Phase 2 Complete** - Application layer ([a587a3f](https://github.com/user/repo/commit/a587a3f) + next commit)
- Service abstractions: `IFOVService`, `IActorPositionService` (Clean Architecture boundaries)
- Commands: `MoveActorCommand` with passability validation + position updates
- Queries: `CalculateFOVQuery` (FOV delegation), `GetVisibleActorsQuery` (query composition via IMediator)
- Tests: 28 new Phase 2 tests using NSubstitute (130 total suite, 0.57s execution)
- **Next**: Phase 3 - Infrastructure (real IFOVService with GoRogue, ActorPositionService impl)

---

*Recently completed and archived (2025-10-01):*
- **VS_001**: Health System Walking Skeleton - Architectural foundation validated ✅
- **BR_001**: Race Condition - Fixed with WithComponentLock pattern ✅
- **BR_002**: Fire-and-Forget Events - Fixed with async/await ✅
- **BR_003**: Heal Button CQRS Bypass - Removed per YAGNI ✅
- **TD_001**: Architecture Enforcement Tests - 10 tests enforcing all 4 ADRs ✅
- *See: [Completed_Backlog_2025-10.md](../07-Archive/Completed_Backlog_2025-10.md)*

---
## 📈 Important (Do Next)
*Core features for current milestone, technical debt affecting velocity*

**No important items!** ✅ (Ready for new work)

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



---



---
*Single Source of Truth for all Darklands development work. Simple, maintainable, actually used.*