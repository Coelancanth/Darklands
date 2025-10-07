# Darklands Development Backlog


**Last Updated**: 2025-10-08 06:21 (Dev Engineer: Completed VS_023 - GenerateWorldPipeline architecture)

**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 008
- **Next TD**: 018
- **Next VS**: 024


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

**No critical items!** ✅ VS_021 completed and archived, VS_020 unblocked.

---

*Recently completed and archived (2025-10-06):*
- **VS_021**: i18n + Data-Driven Entity Infrastructure (ADR-005 + ADR-006) - 5 phases complete! Translation system (18 keys in en.csv), ActorTemplate system with GodotTemplateService, player.tres template, pre-push validation script, architecture fix (templates → Presentation layer). Bonus: Actor type logging enhancement (IPlayerContext integration). All 415 tests GREEN. ✅ (2025-10-06 16:23) *See: [Completed_Backlog_2025-10_Part2.md](../07-Archive/Completed_Backlog_2025-10_Part2.md) for full archive*

---

*Recently completed and archived (2025-10-05):*
- **VS_019**: TileSet-Based Visual Scene + TileSet as Terrain Catalog (SSOT) - All 4 phases complete! TileMapLayer pixel art rendering (terrain), Sprite2D actors with smooth tweening, fog overlay system, 300+ line cleanup. ✅ (2025-10-05)
- **VS_019_FOLLOWUP**: Fix Wall Autotiling (Manual Edge Assignment) - Manual tile assignment for symmetric bitmasks, walls render seamlessly. ✅ (2025-10-05)
- *See: [Completed_Backlog_2025-10_Part2.md](../07-Archive/Completed_Backlog_2025-10_Part2.md) for full archive*

---
## 📈 Important (Do Next)
*Core features for current milestone, technical debt affecting velocity*

**No important items!** ✅ VS_020 completed and archived.

---

*Recently completed and archived (2025-10-06):*
- **VS_020**: Basic Combat System (Attacks & Damage) - All 4 phases complete! Click-to-attack combat UI, component pattern (Actor + HealthComponent + WeaponComponent), ExecuteAttackCommand with range validation (melee adjacent, ranged line-of-sight), damage application, death handling bug fix. All 428 tests GREEN. Ready for VS_011 (Enemy AI). ✅ (2025-10-06 19:03) *See: [Completed_Backlog_2025-10_Part2.md](../07-Archive/Completed_Backlog_2025-10_Part2.md) for full archive*

---

*Recently completed and archived (2025-10-04 19:35):*
- **VS_007**: Time-Unit Turn Queue System - Complete 4-phase implementation with natural mode detection, 49 new tests GREEN, 6 follow-ups complete. ✅ (2025-10-04 17:38)

---

## 💡 Ideas (Future Work)
*Future features, nice-to-haves, deferred work*

### VS_022: World Generation Pipeline (Incremental Post-Processing)
**Status**: Proposed
**Owner**: Product Owner → Tech Lead (breakdown)
**Size**: XL (multi-phase, build incrementally)
**Priority**: Ideas
**Markers**: [WORLDGEN] [ARCHITECTURE] [INCREMENTAL]

**What**: Build post-processing pipeline on top of native plate tectonics output - elevation normalization, climate, erosion, biomes - **one algorithm at a time** with proper testing

**Why**: Current system outputs raw heightmap only. Need processed terrain data (ocean masks, climate zones, biomes) for gameplay. Clean foundation (native-only) established in refactor commit f84515d.

**Current State** (2025-10-08):
- ✅ Native library wrapper working (heightmap + plates)
- ✅ Modular visualization (5 focused nodes, ~700 lines)
- ✅ 433 tests GREEN
- ✅ Clean architecture (no premature complexity)
- ❌ No post-processing (intentional - start simple!)

**Proposed Incremental Approach:**
1. **Phase 1: Elevation Normalization** (S, ~4h)
   - Normalize raw heightmap to [0, 1] range
   - Calculate dynamic sea level threshold (Otsu's method)
   - Add ocean mask generation
   - Tests: Verify normalization, sea level calculation

2. **Phase 2: Climate - Temperature** (M, ~6h)
   - Temperature calculation (latitude + elevation cooling)
   - Temperature map visualization
   - Tests: Temperature gradient validation

3. **Phase 3: Climate - Precipitation** (M, ~6h)
   - Precipitation calculation (with rain shadow)
   - Precipitation map visualization
   - Tests: Precipitation patterns

4. **Phase 4: Hydraulic Erosion** (L, ~12h)
   - River generation (flow accumulation)
   - Valley carving around rivers
   - Lake formation
   - Tests: River connectivity, erosion effects

5. **Phase 5: Hydrology** (M, ~8h)
   - Watermap (droplet simulation)
   - Irrigation (moisture spreading)
   - Humidity (combined moisture metric)
   - Tests: Flow patterns, moisture distribution

6. **Phase 6: Biome Classification** (M, ~6h)
   - Holdridge life zones model
   - Biome map generation
   - Biome visualization + legends
   - Tests: Biome distribution validation

**Technical Principles:**
- ✅ **One algorithm at a time** - No big-bang integration
- ✅ **Test coverage for each phase** - Regression protection
- ✅ **Visual validation** - Probe + view modes for each stage
- ✅ **Algorithm independence** - Each phase self-contained
- ✅ **ADR documentation** - Capture design decisions

**References:**
- [TD_009: Pipeline Gap Analysis](../08-Learnings/WorldEngine/TD_009-Pipeline-Gap-Analysis.md) - WorldEngine algorithms inventory
- [TD_011 completion notes](../08-Learnings/WorldEngine/TD_011-completion-notes.md) - Sea level bug + cleanup lessons
- Refactor commit: `f84515d` (removed 5808 lines, modular nodes)

**Done When:**
- All 6 phases complete with tests
- Each algorithm has visual validation mode
- Biome map renders correctly
- Performance acceptable (<10s for 512×512 world)
- Documentation updated with architecture decisions

**Depends On**: None (foundation ready)

**Next Steps:**
1. Product Owner: Review and approve scope
2. Tech Lead: Break down Phase 1 into detailed tasks
3. Dev Engineer: Implement Phase 1 (elevation normalization)

**Prerequisite Issues** (now TD_012-014):
Before starting pipeline phases, fix visualization foundation technical debt discovered during testing.

---


### ~~VS_023: WorldGen Pipeline - GenerateWorldPipeline Architecture~~ ✅ COMPLETE
**Status**: Done (2025-10-08)
**Owner**: Tech Lead → Dev Engineer
**Size**: M (~4h actual)
**Priority**: Ideas
**Markers**: [WORLDGEN] [ARCHITECTURE] [FOUNDATION]

**What**: Create GenerateWorldPipeline class that orchestrates post-processing stages, calling NativePlateSimulator directly

**Why**: Need clear architecture for incremental pipeline phases (VS_022). Pipeline should call native sim, then apply stages one by one.

**Implementation Summary**:

**Three-Layer Architecture** (Handler → Pipeline → Simulator):
```
GenerateWorldCommandHandler
    ↓ (depends on IWorldGenerationPipeline)
GenerateWorldPipeline
    ↓ (depends on IPlateSimulator)
NativePlateSimulator
    ↓ (wraps native C++ library)
```

**Key Design Decisions**:
1. **WorldGenerationResult** - Pipeline output DTO with optional post-processing fields
   - `Heightmap`, `PlatesMap` (always present)
   - `OceanMask?`, `TemperatureMap?`, `PrecipitationMap?` (null until phases implement)
   - `RawNativeOutput` (preserved for debugging/visualization)

2. **IWorldGenerationPipeline** - High-level abstraction for complete generation
   - Orchestrates: native sim + post-processing stages
   - Currently pass-through (Stage 0 only)
   - Ready for VS_022 Phase 1 (normalization + ocean mask)

3. **Separation of Concerns**:
   - `PlateSimulationResult` - Raw native output only (unchanged)
   - `WorldGenerationResult` - Pipeline output with optional processed data
   - Presentation layer extracts `RawNativeOutput` for rendering

**Files Created**:
- ✅ `WorldGenerationResult.cs` - Pipeline output DTO
- ✅ `IWorldGenerationPipeline.cs` - Pipeline interface
- ✅ `GenerateWorldPipeline.cs` - Pipeline implementation (Stage 0)

**Files Modified**:
- ✅ `GenerateWorldCommand.cs` - Return type changed to WorldGenerationResult
- ✅ `GenerateWorldCommandHandler.cs` - Now depends on IWorldGenerationPipeline
- ✅ `WorldMapOrchestratorNode.cs` - Extracts RawNativeOutput for presentation
- ✅ `GameStrapper.cs` - Registered IWorldGenerationPipeline (Transient)

**Current Behavior** (Stage 0 - Pass-Through):
- Pipeline calls native simulator
- Returns raw heightmap + plates
- Optional fields all null
- Ready for incremental VS_022 phases

**Next Steps** (VS_022 Phase 1):
1. Add `NormalizeElevation()` stage to pipeline
2. Add `CalculateOceanMask()` stage
3. Populate `OceanMask` field in result
4. Tests verify normalization + ocean detection

**Tests**: All 433 tests GREEN ✅

**Completed**: 2025-10-08 06:21 by Dev Engineer

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