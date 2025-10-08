# Darklands Development Backlog


**Last Updated**: 2025-10-09 00:53 (Tech Lead: Restructured VS_022 pipeline with correct logical order, created VS_028 coastal moisture + VS_029 erosion placeholder)

**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 008
- **Next TD**: 019
- **Next VS**: 030


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

### VS_029: WorldGen Stage 6 - Erosion & Rivers (Hydraulic Erosion)
**Status**: Proposed
**Owner**: Product Owner → Tech Lead (for breakdown)
**Size**: M (8-10h estimate)
**Priority**: Ideas
**Markers**: [WORLDGEN] [PIPELINE] [STAGE-6] [HYDROLOGY]

**What**: Generate rivers and carve realistic valleys using **hydraulic erosion simulation** (river source detection, path tracing, valley carving) - WorldEngine erosion.py port

**Why**: Realistic terrain requires water erosion (valleys, river networks). Rivers spawn in wet mountains, flow to ocean/lakes, carve valleys over geological time. Critical for gameplay (river resources, navigation, terrain tactics).

**How** (WorldEngine-validated algorithm):

**Three-Phase Erosion Process**:

1. **River Source Detection** (mountains + high precipitation)
   - Uses **FINAL PRECIPITATION** from VS_028 (all geographic effects applied)
   - Finds high-elevation cells with accumulated rainfall > threshold
   - Filters sources (min 9-cell spacing to prevent overlap)

2. **River Path Tracing** (downhill flow to ocean/lakes)
   - Traces steepest descent from source to ocean
   - A* pathfinding fallback for challenging terrain (local minima)
   - Merges into existing rivers when encountered (tributary system)
   - Dead-ends form lakes (endorheic basins)

3. **Valley Carving** (gentle erosion around river paths)
   - Radius 2 erosion around each river cell (subtle valleys)
   - Curve factors: 0.2 (adjacent), 0.05 (diagonal) - gentle shaping
   - Elevation monotonicity cleanup (rivers flow downhill smoothly)

**Key Outputs**:
- Eroded heightmap (valleys carved, realistic terrain)
- River network (List<River>, path coordinates + ocean-reached flag)
- Lakes (List<(int x, int y)>, endorheic basin locations)

**Implementation**: Port WorldEngine `erosion.py` (403 lines → ~500 lines C#) - See TD_009 Phase 1 for detailed breakdown

**Done When**:
1. Rivers spawn in realistically wet locations (FINAL precipitation input)
2. Rivers flow downhill to ocean or form lakes
3. Valleys carved around river paths (subtle, radius 2)
4. Eroded heightmap smoother than input (realistic weathering)
5. All tests GREEN + 10-12 new erosion/river tests

**Depends On**: VS_028 ✅ (FINAL precipitation required for realistic river sources)

**Blocks**: VS_022 Phase 4-6 (watermap, irrigation, humidity, biomes all need eroded terrain + rivers)

**Tech Lead Decision** (2025-10-09):
- **Algorithm**: Direct port of WorldEngine erosion.py (proven, well-tested)
- **Precipitation input**: FINAL (VS_028 output) - ensures leeward deserts don't spawn rivers, coastal mountains do
- **Complexity**: Medium (M, 8-10h) - river tracing is most complex part (A* fallback needed)
- **Architecture**: First stage of hydrological processes (Stage 3), modifies heightmap (geological timescale)
- **Next steps**: Create detailed breakdown after VS_028 complete

---

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

**STAGE 1: TECTONIC FOUNDATION**
1. **Phase 1: Elevation Post-Processing** ✅ COMPLETE (VS_024, M, ~8h actual)
   - ✅ Ported 4 WorldEngine algorithms (~150 lines): add_noise, fill_ocean, harmonize_ocean, sea_depth
   - ✅ Dual-heightmap architecture: Original raw + Post-processed raw (both [0.1-20] range)
   - ✅ Quantile-based thresholds: SeaLevel, HillLevel, MountainLevel, PeakLevel (adaptive per-world)
   - ✅ Real-world meters mapping: ElevationMapper for UI display (Presentation layer utility)
   - ✅ BFS flood-fill ocean detection (OceanMask, not simple threshold)
   - ✅ FastNoiseLite integration: 8-octave OpenSimplex2 noise for terrain variation
   - ✅ Three colored elevation views: Original, Post-Processed, Normalized (visual validation)
   - ✅ Format v2 serialization: Saves post-processed data with backward compatibility (TD_018)
   - **Outcome**: Foundation complete for climate stages, all 433 tests GREEN

**STAGE 2: ATMOSPHERIC CLIMATE (Instantaneous processes, no terrain modification)**
2. **Phase 2: Climate - Complete Precipitation Pipeline** (PARTIALLY COMPLETE)

   **2a. Temperature** ✅ COMPLETE (VS_025, S, ~5h actual)
   - ✅ 4-component temperature algorithm: Latitude (92%, with axial tilt) + Noise (8%, FBm fractal) + Distance-to-sun (inverse-square) + Mountain-cooling (RAW elevation thresholds)
   - ✅ Per-world climate variation: AxialTilt and DistanceToSun (Gaussian-distributed) create hot/cold planets with shifted equators
   - ✅ 4-stage debug visualization: LatitudeOnly → WithNoise → WithDistance → Final (isolates each component for visual validation)
   - ✅ Normalized [0,1] output: Internal format for biome classification (Stage 6), UI converts to °C via TemperatureMapper
   - ✅ MathUtils library: Interp() for latitude interpolation, SampleGaussian() for per-world parameters
   - ✅ Multi-stage testing: 14 unit tests (Interp edge cases, Gaussian distribution validation, temperature ranges)
   - ✅ Visual validation passed: Smooth latitude bands, subtle noise variation, hot/cold planets, mountains blue at all latitudes
   - ✅ Performance: ~60-80ms for temperature calculation (no threading needed, native sim dominates at 83%)
   - **Outcome**: Temperature maps ready, all 447 tests GREEN

   **2b. Base Precipitation** ✅ COMPLETE (VS_026, S, ~3.5h actual)
   - ✅ 3-stage algorithm: Noise (6 octaves) → Temperature gamma curve → Renormalization
   - ✅ Multi-stage debug visualization: NoiseOnly → TemperatureShaped → Final
   - ✅ Quantile-based thresholds (30th/70th/95th percentiles for classification)
   - ✅ WorldEngine algorithm exact match (gamma=2.0, curveBonus=0.2)
   - **Outcome**: Base precipitation ready for geographic modifiers, all 457 tests GREEN

   **2c. Rain Shadow Effect** ✅ COMPLETE (VS_027, S, ~3h actual)
   - ✅ Latitude-based prevailing winds (Polar Easterlies / Westerlies / Trade Winds)
   - ✅ Orographic blocking: Upwind mountain trace (max 20 cells ≈ 1000km)
   - ✅ Accumulative reduction (5% per mountain, max 80% total blocking)
   - ✅ Real-world desert patterns (Sahara, Gobi, Atacama validation)
   - **Outcome**: Rain shadow precipitation ready, 481/482 tests GREEN (99.8%)

   **2d. Coastal Moisture Enhancement** (VS_028, S, 3h) ✅ **COMPLETE** (2025-10-09)
   - ✅ Distance-to-ocean BFS (O(n) flood fill, copied from VS_024 ocean fill pattern)
   - ✅ Exponential decay: `bonus = 0.8 × e^(-dist/30)` - matches real atmospheric moisture transport
   - ✅ Coastal bonus: 80% at coast (dist=0), 29% at 1500km (dist=30), <10% deep interior (dist=60+)
   - ✅ Elevation resistance: `factor = 1 - min(1, elev × 0.02)` - mountain plateaus resist coastal penetration
   - ✅ Additive enhancement: Preserves rain shadow deserts while adding maritime climate effect
   - ✅ Real-world validation: Maritime (Seattle, UK) wetter than continental (Spokane, central Asia)
   - **Outcome**: FINAL PRECIPITATION MAP ready (Stage 5 complete), 495/495 tests GREEN (100%)

**STAGE 3: HYDROLOGICAL PROCESSES (Slow geological processes, terrain modification)**
3. **Phase 3: Erosion & Rivers** (VS_029, M, ~8-10h) ← AFTER VS_028
   - River source detection (uses FINAL PRECIPITATION from VS_028)
   - River path tracing (downhill flow to ocean/lakes)
   - Valley carving (erosion around river paths, radius 2, gentle curves)
   - **Output**: Eroded heightmap, rivers[], lakes[]
   - **Critical**: Uses final precipitation (rivers spawn in realistically wet locations)

4. **Phase 4: Watermap Simulation** (M, ~3-4h)
   - Droplet flow model (20,000 droplets weighted by final precipitation)
   - Flow accumulation (recursive downhill distribution)
   - Quantile thresholds (creek 5%, river 2%, main river 0.7%)
   - **Output**: Watermap (flow intensity per cell)

5. **Phase 5: Irrigation & Humidity** (M, ~3-4h)
   - Irrigation: Logarithmic kernel (21×21 neighborhood, moisture spreading from watermap)
   - Humidity: Combine precipitation × 1 + irrigation × 3 (hydrologic moisture boost)
   - Quantile-based classification (8-level moisture: superarid → superhumid)
   - **Output**: Humidity map (final moisture for biome classification)

6. **Phase 6: Biome Classification** (M, ~6h)
   - 48 biome types (WorldEngine catalog)
   - Classification: temperature + humidity + elevation
   - Biome transitions (smooth gradients, not hard borders)
   - Biome visualization + legends
   - **Output**: Biome map

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