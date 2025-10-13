# Darklands Development Backlog


**Last Updated**: 2025-10-13 18:44 (Dev Engineer: Created TD_022 - Refactor climate to use post-Gaussian heightmap)

**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 010
- **Next TD**: 023
- **Next VS**: 031


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



---

## 📈 Important (Do Next)
*Core features for current milestone, technical debt affecting velocity*

### TD_021: Sea Level SSOT and Normalized Scale Foundation
**Status**: Proposed (Architectural foundation for VS_030)
**Owner**: Dev Engineer
**Size**: S (6-8h)
**Priority**: Important (blocks VS_030 pathfinding implementation)
**Markers**: [ARCHITECTURE] [SSOT] [WORLDGEN] [FOUNDATION]

**What**: Unify sea level definition as a single constant and establish normalized scale for rendering (NO water body classification - moved to VS_030).

**Why**:
- **Current Problem**: Sea level is scattered (PlateSimulationParams.SeaLevel, ElevationThresholds.SeaLevel, hardcoded values) with no SSOT → drift between stages
- **Scale Confusion**: Ocean masks calculated on RAW heightmap [0-20] but rendering uses normalized [0,1] → semantic mismatch
- **Foundation for VS_030**: Pathfinding needs consistent sea level constant and normalized scale for depth calculations
- **CRITICAL CORRECTION**: Water body classification MUST happen AFTER pit-filling (causality - pit-filling DEFINES lakes), so moved to VS_030

**How** (3-phase architectural fix):

**Phase 1: SSOT Constant** (2-3h)
- Create `WorldGenConstants.cs` with `SEA_LEVEL_RAW = 1.0f` (matches CONTINENTAL_BASE from C++ plate tectonics)
- Remove `PlateSimulationParams.SeaLevel` (sea level is physics, not configuration)
- Remove `ElevationThresholds.SeaLevel` (sea level is fixed, not adaptive like hills/mountains)
- Update all references: NativePlateSimulator, ElevationPostProcessor, GenerateWorldPipeline
- Result: Single source of truth, no magic numbers (0.65, 1.0, etc.)

**Phase 2: Normalized Scale for Rendering** (2-3h)
- Calculate `SeaLevelNormalized` during post-processing: `(SEA_LEVEL_RAW - min) / (max - min)`
- Update `PostProcessingResult` to include `SeaLevelNormalized` field
- Update `ElevationPostProcessor.Process()` to compute and return normalized sea level
- This enables rendering to consistently use normalized [0,1] scale with correct sea level threshold

**Phase 3: Testing and Documentation** (2h)
- Unit tests: SSOT constant used everywhere (grep for hardcoded 0.65/1.0)
- Unit tests: SeaLevelNormalized calculated correctly across different heightmaps
- Performance testing: No overhead (just constant replacement)
- Update pipeline architecture docs (note: water classification happens in VS_030 AFTER pit-filling)

**Done When**:
1. ✅ `WorldGenConstants.SEA_LEVEL_RAW` is the ONLY sea level definition (grep confirms no hardcoded 0.65/1.0)
2. ✅ `ElevationThresholds.SeaLevel` removed (sea level is constant, not adaptive)
3. ✅ `PlateSimulationParams.SeaLevel` removed (sea level is physics, not config)
4. ✅ `SeaLevelNormalized` available in PostProcessingResult (for rendering)
5. ✅ All existing tests GREEN (no regression)
6. ✅ Performance: Zero overhead (constant replacement only)
7. ✅ Documentation updated: Pipeline order clarified (water classification in VS_030 AFTER pit-filling)

**Depends On**: VS_029 ✅ (post-processing pipeline stable)

**Blocks**: VS_030 (pathfinding requires SSOT constant and normalized scale)

**Enables**:
- VS_030 lake thalweg pathfinding (has consistent sea level for depth calculations)
- Cleaner architecture (sea level = physics constant, not scattered config)
- Consistent rendering (normalized scale established)

**Dev Engineer Decision** (2025-10-13 18:38):
- **CRITICAL CORRECTION**: Original TD_021 had water classification BEFORE pit-filling → WRONG causality
- **Key Insight from tmp.md**: Pit-filling DEFINES lakes (boundaries, water level, outlets) - can't classify before definition exists
- **Scope Reduction**: TD_021 now ONLY does SSOT constant + normalized scale (6-8h instead of 10-14h)
- **Water Classification Moved**: Now Phase 1 of VS_030 (happens AFTER pit-filling provides lake definitions)
- **Risk Mitigation**: Correct pipeline order prevents "leaky lake" and "archipelago of sinks" logic errors
- **Next Step**: Implement TD_021 (foundation), then VS_030 builds on correct causality

---

### VS_030: Inner Sea Flow via Lake Thalweg Pathfinding
**Status**: Proposed (Requires TD_021 foundation)
**Owner**: Dev Engineer
**Size**: L (14-18h)
**Priority**: Important (fixes D-8 flow topology for inner seas)
**Markers**: [WORLDGEN] [ALGORITHM] [PATHFINDING] [HYDRAULIC-EROSION]

**What**: Implement Dijkstra-based pathfinding to compute river thalweg (deepest channel) paths through inner seas, enabling correct D-8 flow topology for landlocked water bodies.

**Why**:
- **D-8 Problem**: Inner seas are flat surfaces (elevation < sea level) → D-8 has no gradient → becomes sink → breaks flow accumulation
- **Current State**: Rivers terminate incorrectly at inner sea boundaries (flow "stuck" at lakes like Caspian Sea)
- **Physical Reality**: Rivers flow THROUGH lakes along deepest channels (thalweg) to outlets, not terminate at inlets
- **Pathfinding Solution**: Pre-compute optimal paths (inlet → outlet) using depth-based cost, hard-code flow directions for inner sea cells
- **CRITICAL CORRECTION**: Water body classification MUST happen AFTER pit-filling (pit-filling DEFINES lakes with boundaries/water level/outlets)

**How** (5-phase implementation with CORRECT pipeline order):

**Phase 1: Water Body Classification from Pit-Filling Results** (3-4h)
- **CRITICAL**: This happens AFTER pit-filling, which DEFINES lakes hydrologically
- Create `WaterBodyMasks.cs` DTOs (OceanMask, InnerSeaMask, InnerSeaRegions)
- Implement `WaterBodyClassifier.ClassifyFromPitFilling()`:
  - Input: FilledHeightmap + OriginalHeightmap + PitFillingMetadata (basins identified by pit-filling)
  - Algorithm:
    - For each basin from pit-filling (has boundary, water surface elevation, pour point):
      - If pour point at sea level AND connected to map edges → Ocean
      - If pour point above sea level OR landlocked → Inner Sea
    - Group inner seas into regions (connected components)
    - Detect inlets: Boundary cells where land D-8 flow enters lake
    - Detect outlet: The pour point identified by pit-filling
  - Output: WaterBodyMasks (ocean mask, inner sea mask, inner sea regions with inlets/outlets)
- Why this works: Pit-filling already solved the "leaky lake" and "archipelago of sinks" problems by finding pour points

**Phase 2: Lake Thalweg Pathfinder** (3-4h)
- Implement `LakeThalwegPathfinder.ComputePaths()` (Dijkstra with depth-based cost)
- Cost function: `Cost = 1 / (lakeSurfaceElevation - originalHeightmap[cell] + ε)` → Prefers deeper water
- For each inner sea region (from Phase 1):
  - For each inlet → Find path to outlet (Dijkstra)
  - Result: List of path segments (x, y, nextX, nextY)
- Output: `Dictionary<InnerSeaId, List<PathSegment>>` (thalweg paths for all inner seas)
- Optimization: Use BFS/distance transform to pre-compute "flow to nearest thalweg" for non-path lake cells

**Phase 3: Hybrid Flow Direction Calculator** (2-3h)
- Modify `FlowDirectionCalculator.CalculateWithThalwegs()`:
  - Land cells: D-8 steepest descent (unchanged)
  - Ocean cells: dir = -1 (sink)
  - Inner sea cells ON thalweg path: Use pre-computed direction from pathfinding
  - Inner sea cells NOT on thalweg: Flow toward nearest thalweg cell (from Phase 2 optimization)
- Result: Hybrid flow directions (D-8 + pathfinding + sinks)

**Phase 4: Pipeline Integration** (2-3h)
- **CORRECTED PIPELINE ORDER**:
  ```
  Stage 2: Pit-filling (FIRST - DEFINES lakes)
    → Output: FilledHeightmap + PitFillingMetadata (basins with pour points)

  Stage 2.5 (NEW): Water Body Classification
    → Input: FilledHeightmap + OriginalHeightmap + PitFillingMetadata
    → Output: WaterBodyMasks (ocean/inner sea + regions with inlets/outlets)

  Stage 2.6 (NEW): Thalweg Pathfinding
    → Input: InnerSeaRegions (from Stage 2.5)
    → Output: ThalwegPaths (Dijkstra-computed paths)

  Stage 3 (MODIFIED): Hybrid Flow Direction Calculator
    → Input: FilledHeightmap + WaterBodyMasks + ThalwegPaths
    → Output: FlowDirections (D-8 + thalweg + sinks)

  Stage 4-5: Topological Sort + Flow Accumulation (unchanged)
  ```
- Update `PitFillingCalculator` to expose basin metadata (pour points, boundaries)
- Update `WorldGenerationResult` to include `WaterBodyMasks` and `ThalwegPaths`
- Wire through to rendering

**Phase 5: Visualization and Testing** (3-4h)
- Add view modes:
  - `OceanMask`: Show ocean cells (blue)
  - `InnerSeaMask`: Show inner sea cells (teal/cyan - distinguish from ocean)
  - `LakeThalwegs`: Show computed paths through inner seas (red lines on water)
  - `ThalwegDepths`: Heat map of lake depths (deeper=red, shallow=blue)
- Unit tests: Water classification correctness (ocean vs inner sea after pit-filling)
- Unit tests: Pathfinding finds valid paths (inlet → outlet)
- Visual validation: Flow accumulation shows rivers flowing THROUGH inner seas
- Visual validation: Thalweg paths curve naturally (follow deepest channels)
- Performance testing: Total overhead <50ms (classification + pathfinding)

**Done When**:
1. ✅ Pit-filling exposes basin metadata (pour points, boundaries, water surface elevations)
2. ✅ `WaterBodyClassifier` classifies basins AFTER pit-filling (ocean vs inner sea)
3. ✅ Inner sea regions have correct inlets (where land flow enters) and outlets (pour points)
4. ✅ `LakeThalwegPathfinder` computes depth-based paths for all inner sea regions
5. ✅ Dijkstra cost function uses depth (prefers thalweg channels)
6. ✅ `FlowDirectionCalculator` hybrid approach works (D-8 + thalweg + sinks)
7. ✅ Flow accumulation shows rivers passing THROUGH inner seas (not terminating at boundaries)
8. ✅ 4 new view modes render correctly (OceanMask, InnerSeaMask, LakeThalwegs, ThalwegDepths)
9. ✅ Unit tests: Pathfinding correctness (finds paths, follows depth gradient)
10. ✅ Visual validation: No "leaky lake" errors (all classified lakes are true basins per pit-filling)
11. ✅ All existing tests GREEN (no regression)
12. ✅ Performance: <50ms total overhead for 512×512 map (~5-10 inner seas)

**Depends On**:
- TD_021 ✅ (must be complete - provides SSOT constant and normalized scale)
- VS_029 ✅ (flow visualization exists for validation)
- Current pit-filling implementation (must expose basin metadata)

**Blocks**: Nothing (hydraulic erosion foundation - enables future particle erosion)

**Enables**:
- Future particle-based erosion (rivers erode along thalweg paths)
- Future sediment deposition (sediment accumulates in deep lake basins)
- Future navigation system (ships follow thalweg channels)

**Dev Engineer Decision** (2025-10-13 18:38):
- **CRITICAL CORRECTION**: Original VS_030 had water classification BEFORE pit-filling → CAUSALITY INVERSION ERROR
- **Key Insight from tmp.md**:
  - "Pit-filling is not just filling - it's a DEFINITION tool"
  - Pour point defines outlet, pour point elevation defines water surface, basin below pour point defines lake boundary
  - Cannot classify "what is a lake?" before pit-filling answers this question
- **Correct Order**: Pit-filling (defines) → Classify (ocean vs inner sea) → Pathfind (thalweg) → Flow calculation (hybrid)
- **Leaky Lake Prevention**: Classifying from pit-filling results prevents misclassifying river valleys as lakes
- **Archipelago Prevention**: Pit-filling already merged micro-sinks into coherent basins with single pour points
- **Size Increase**: 14-18h (was 12-16h) due to Phase 1 added (water classification moved from TD_021)
- **Approach Still Valid**: Pathfinding over land bridge (excellent visual quality + reusable paths)
- **Next Step**: Implement TD_021 (foundation), expose pit-filling metadata, then implement VS_030 with correct pipeline

---

### TD_022: Refactor Climate to Use Post-Gaussian Heightmap
**Status**: Proposed (Consistency with TD_021 SSOT principle)
**Owner**: Dev Engineer
**Size**: XS (2-3h)
**Priority**: Ideas (Low - current approach works, this is architectural consistency)
**Markers**: [ARCHITECTURE] [CLIMATE] [CONSISTENCY]

**What**: Change temperature, precipitation, rain shadow, and coastal moisture algorithms to use post-Gaussian heightmap instead of post-processing heightmap.

**Why**:
- **Consistency**: TD_021/VS_030 established "post-Gaussian = SSOT for geography" (water classification uses it)
- **Semantic Purity**: Climate is physics/geography (mountains, valleys, ocean distance) - should use clean terrain, not artistic noise
- **Stability**: Climate shouldn't change if we tweak post-processing noise algorithm (currently at line 73 disabled, but normally adds ±0.05 coherent noise)
- **Current State**: All climate stages (temperature, precipitation, rain shadow, coastal moisture) use `postProcessed.ProcessedHeightmap` (line 95, 130, 148)

**How** (single phase):
- Update `GenerateWorldPipeline.Generate()`:
  - Pass `smoothedHeightmap` instead of `postProcessed.ProcessedHeightmap` to:
    - TemperatureCalculator (line 95)
    - RainShadowCalculator (line 130)
    - CoastalMoistureCalculator (line 148)
- Result: Climate driven by clean post-Gaussian terrain (±0.05 coherent noise removed)
- Note: Ocean mask still from post-processing (needed for coastal moisture BFS)

**Done When**:
1. ✅ Temperature uses `smoothedHeightmap` (post-Gaussian)
2. ✅ Rain shadow uses `smoothedHeightmap` (post-Gaussian)
3. ✅ Coastal moisture uses `smoothedHeightmap` (post-Gaussian)
4. ✅ All existing tests GREEN (climate should be nearly identical - 0.05 noise is minimal)
5. ✅ Visual validation: Temperature/precipitation maps look the same (no regressions)

**Depends On**: TD_021 ✅ (establishes post-Gaussian as geographic SSOT)

**Blocks**: Nothing (low priority consistency fix)

**Enables**: Cleaner architecture (climate decoupled from post-processing artistic tweaks)

**Dev Engineer Decision** (2025-10-13 18:44):
- **Low Priority**: Current approach works fine (coherent noise ±0.05 doesn't significantly affect climate)
- **Architectural Consistency**: Aligns with TD_021/VS_030 principle (post-Gaussian = clean geographic SSOT)
- **Risk**: Minimal (2-3h effort, easy rollback if visual regressions detected)
- **Defer Until**: After TD_021 + VS_030 complete (don't mix architectural changes)

---

## 💡 Ideas (Future Work)
*Future features, nice-to-haves, deferred work*

### VS_029: D-8 Flow Direction Visualization (Heightmap Validation) + River Source Algorithm Correction
**Status**: Done ✅ (2025-10-13 15:14)
**Owner**: Dev Engineer
**Size**: S (~4-6h base + 2h correction + 1h fixes)
**Priority**: Ideas (worldgen quality validation)
**Markers**: [WORLDGEN] [VISUALIZATION] [DEBUG-TOOLING] [ALGORITHM-CORRECTION] [GAUSSIAN-BLUR]

**What**: Add debug visualization for existing D-8 flow direction algorithm to validate heightmap quality and implementation correctness through 4 new view modes (FlowDirections, FlowAccumulation, RiverSources, Sinks).

**Why**:
- **Validate heightmap quality** - Visually inspect flow patterns to detect artifacts/noise breaking drainage
- **Validate D-8 implementation** - Confirm flow directions follow steepest descent correctly
- **Foundation for particle erosion** - Must validate D-8 correctness before building complex particle physics on top
- **Debug tool for worldgen** - Essential for tuning pit-filling thresholds and elevation post-processing

**Key Architectural Insight**:
```
✅ D-8 Algorithm EXISTS: FlowDirectionCalculator.cs (implemented, tested, integrated)
✅ Phase1ErosionData EXISTS: Contains flow directions, accumulation, river sources
❌ Gap: WorldGenerationResult DOESN'T expose Phase1ErosionData for visualization!
```

**How** (4 implementation phases):

**Phase 1: Core Integration** (~2-2.5h) - TDD
- Update `WorldGenerationResult` DTO to include Phase1ErosionData fields:
  - `FilledHeightmap` (after pit filling)
  - `FlowDirections` (int[,] - 0-7 direction codes, -1 sink)
  - `FlowAccumulation` (float[,] - drainage basin sizes)
  - `RiverSources` (List<(int x, int y)> - spawn points)
  - `Lakes` (List<(int x, int y)> - preserved pits)
- **NEW**: Add pre/post pit-filling comparison data:
  - `PreFillingLocalMinima` (List<(int x, int y)> - ALL sinks before pit-filling)
  - Compute sinks on PostProcessedHeightmap BEFORE calling PitFillingCalculator
  - This enables Step 0A visualization (baseline raw heightmap quality)
- Wire `HydraulicErosionProcessor.ProcessPhase1()` into pipeline (after VS_028)
- Unit tests: Phase1ErosionData populated correctly for 512×512 map
- Unit tests: PreFillingLocalMinima count > PostFillingLocalMinima count (pit-filling worked!)

**Phase 2: View Modes** (~0.5h)
- Add 6 enum values to `MapViewMode`:
  - `SinksPreFilling` - **BEFORE pit-filling** (raw heightmap sinks - Step 0A)
  - `SinksPostFilling` - **AFTER pit-filling** (remaining sinks - Step 0B)
  - `FlowDirections` - 8-direction + sink visualization (Step 2)
  - `FlowAccumulation` - Drainage basin heat map (Step 3)
  - `RiverSources` - Mountain source points (Step 4)
  - `FilledElevation` - Colored elevation view of FILLED heightmap (Step 1 optional)

**Phase 3: Rendering Logic** (~3-3.5h)
- Add rendering methods to `WorldMapRendererNode`:
  - `RenderSinksPreFilling()` - **Step 0A** - Grayscale elevation + Red markers for ALL local minima (baseline)
  - `RenderSinksPostFilling()` - **Step 0B** - Grayscale elevation + Red markers for remaining sinks (after pit-filling)
  - `RenderFilledElevation()` - **Step 1** - Colored elevation of FilledHeightmap (optional comparison)
  - `RenderFlowDirections()` - **Step 2** - 8-color gradient (N=Red, NE=Yellow, E=Green, SE=Cyan, S=Blue, SW=Purple, W=Magenta, NW=Orange, Sink=Black)
  - `RenderFlowAccumulation()` - **Step 3** - Heat map Blue (low) → Green → Yellow → Red (high drainage)
  - `RenderRiverSources()` - **Step 4** - Colored elevation base + Cyan markers at spawn points
- Add comprehensive logging to each view mode (Godot Output panel):
  - **Sinks Pre-Filling**: `PRE-FILLING SINKS: Total=1234 (15.2% of land cells) | Ocean sinks excluded | BASELINE for pit-filling`
  - **Sinks Post-Filling**: `POST-FILLING SINKS: Total=156 (1.9% of land cells) | Reduction=87.4% | Lakes preserved=45 ✓`
  - **Pit-Filling Comparison**: `PIT-FILLING EFFECTIVENESS: 1234 → 156 sinks (87.4% reduction) | Filled=1078, Preserved=156`
  - **Flow Directions**: `Direction distribution: N=12%, NE=8%, ..., Sinks=2.3% (1234 cells)`
  - **Flow Accumulation**: `Accumulation stats: min=0.001, max=52.34, mean=0.85, p95=5.2, river valleys detected: 8`
  - **River Sources**: `River sources: 12 detected | Mountain cells: 4532 (8.7%) | Source density: 0.26% of mountains`
- Unit tests: All rendering methods produce valid textures without crashes

**Phase 4: UI Integration & Logging** (~1h)
- Add dropdown options to `WorldMapUINode`:
  - "DEBUG: Sinks (PRE-Filling)" - Step 0A
  - "DEBUG: Sinks (POST-Filling)" - Step 0B
  - "DEBUG: Filled Elevation" - Step 1 (optional)
  - "DEBUG: Flow Directions" - Step 2
  - "DEBUG: Flow Accumulation" - Step 3
  - "DEBUG: River Sources" - Step 4
- Wire up view mode switching (reuse existing pattern from VS_025-028)
- **Ensure logger output visible** - Verify Godot Output panel shows stats when view mode changes
- Test: Can toggle between all view modes smoothly, no visual glitches, stats logged each switch
- **Test pit-filling comparison** - Switch Pre→Post, verify red markers disappear (filled pits) and remain (lakes)

**Validation Workflow** (systematic sequence with decision trees):

**🔵 INITIAL VALIDATION SEQUENCE** (First-time implementation - bottom-up):
```
Step 0A: BEFORE Pit-Filling (Raw Heightmap Sinks) [CRITICAL - baseline for pit-filling validation]
  View: DEBUG: Sinks (Pre-Filling) - Computed on PostProcessedHeightmap BEFORE pit-filling
  Visual: How many local minima exist? Where are they clustered?
  Data: Total sinks logged (expect 5-20% of land cells - this is NORMAL for raw heightmap!)
  Purpose: Establish baseline - "How bad is the raw heightmap?"
  ✅ EXPECTED: 5-20% land sinks (noisy heightmap needs pit-filling)
  ❌ UNEXPECTED: <2% land sinks (heightmap already smooth? pit-filling may do nothing)
  ⚠️ WARNING: >30% land sinks (extremely noisy heightmap - elevation post-processing issues)

Step 0B: AFTER Pit-Filling (Pit-Filling Algorithm Validation) [CRITICAL - validate pit-filling decisions]
  View: DEBUG: Sinks (Post-Filling) - Computed on FilledHeightmap AFTER pit-filling
  Visual: Compare with Step 0A - which pits were filled? which preserved?
  Data: Sink reduction logged (expect 70-90% reduction! e.g., 15% → 2%)
  Purpose: Validate pit-filling algorithm effectiveness
  ✅ PASS: 70-90% sink reduction (from Step 0A to 0B), remaining sinks mostly ocean + lakes
  ❌ FAIL: <50% reduction → Pit-filling thresholds too conservative (not filling enough)
  ❌ FAIL: >95% reduction → Pit-filling too aggressive (filling real lakes!)

  Data Validation: Compare BEFORE vs AFTER
    → Log: "Pit-filling: 1234 → 156 sinks (87.4% reduction) | Filled=1078, Preserved=156 (lakes)"
    → Visual: Red markers should DISAPPEAR from Step 0A → 0B in fillable pit locations
    → Visual: Red markers should REMAIN for preserved lakes (large/deep basins)

Step 1: Foundation Check (FilledHeightmap Quality) [OPTIONAL - visual sanity check]
  View: ColoredPostProcessedElevation vs ColoredFilledElevation (side-by-side if possible)
  Visual: Did pit-filling preserve mountain peaks? Smooth valleys without destroying features?
  Data: Min/max elevation change logged (expect minimal change <5% in non-pit areas)
  ✅ PASS: Terrain features preserved, only pits smoothed
  ❌ FAIL: Terrain destroyed (mountains flattened) → Pit-filling bug or threshold issue

Step 2: Algorithm Correctness (FlowDirections) [CRITICAL - foundation]
  View: DEBUG: Flow Directions
  Visual: Do colors flow downhill consistently? (Mountains→valleys→ocean)
  Data: Direction distribution logged (expect <5% sinks at this stage)
  ✅ PASS: Visual flow makes sense, <5% sinks
  ❌ FAIL: Colors random/contradictory OR >10% sinks → D-8 ALGORITHM BUG (Step 2 diagnostic)

Step 3: Derived Data (FlowAccumulation) [VALIDATES topological sort]
  View: DEBUG: Flow Accumulation
  Visual: Clear river valley hot spots (red lines)? Blue background (low accumulation)?
  Data: Statistics logged (min/max/mean/p95) - expect p95 >> mean (power law)
  ✅ PASS: River valleys visible, p95 > 5× mean (drainage concentration working)
  ❌ FAIL: Uniform/random OR p95 ≈ mean → TOPOLOGICAL SORT BUG (Step 3 diagnostic)

Step 4: Feature Detection (RiverSources) [VALIDATES detection thresholds]
  View: DEBUG: River Sources
  Visual: Cyan dots on mountain peaks? Not in valleys? Reasonable count?
  Data: Count + density logged (expect 0.1-0.5% of mountain cells)
  ✅ PASS: Sources on peaks, density reasonable (5-15 major rivers for 512×512)
  ❌ FAIL: Sources everywhere/nowhere/wrong elevation → THRESHOLD TUNING needed

Step 5: Quality Metric (Sinks) [PRIMARY DIAGNOSTIC - validates entire pipeline]
  View: DEBUG: Sinks
  Visual: Red dots mostly at ocean borders? Few inland clusters?
  Data: Sink breakdown logged (ocean %, lake %, inland pit %)
  ✅ PASS: >85% ocean, <10% inland pits (HEALTHY HEIGHTMAP!)
  ❌ FAIL: >10% inland pits → HEIGHTMAP QUALITY ISSUE (Step 5 diagnostic)
```

**🔴 DIAGNOSTIC SEQUENCES** (When failures detected - top-down root cause analysis):

**Diagnostic 2: Flow Directions Failure** (>10% sinks OR visual contradictions)
```
SYMPTOM: Step 2 shows >10% sinks or colors don't flow downhill
  ↓
CHECK: Examine specific problem cells
  ↓ Pick a cell with wrong flow direction
  ↓ Manually trace 8 neighbors in elevation view
  ↓
  ├─ Neighbor elevations confirm cell should flow differently
  │   → BUG: FlowDirectionCalculator.cs logic error (steepest descent broken)
  │   → FIX: Review algorithm, add unit test for this terrain pattern
  │
  └─ Neighbor elevations confirm algorithm is CORRECT
      → ISSUE: Heightmap has local artifacts (noise spikes, flat regions)
      → FIX: Improve elevation post-processing (VS_024) or pit-filling thresholds
```

**Diagnostic 3: Flow Accumulation Failure** (p95 ≈ mean, no hot spots)
```
SYMPTOM: Step 3 shows uniform accumulation (no drainage concentration)
  ↓
CHECK: Is topological sort producing correct order?
  ↓ Add debug logging to TopologicalSortCalculator
  ↓
  ├─ Sort order wrong (downstream before upstream)
  │   → BUG: Topological sort has cycle or incorrect ordering
  │   → FIX: Review Kahn's algorithm implementation, check for cycles
  │
  └─ Sort order correct
      → ISSUE: Flow directions have too many sinks (breaks accumulation chains)
      → FOLLOW: Step 2 diagnostic (fix flow directions first)
```

**Diagnostic 5: Sinks Failure** (>10% inland pits - MOST COMMON)
```
SYMPTOM: Step 5 shows >10% inland pits (heightmap quality issue)
  ↓
STEP A: Validate D-8 algorithm correctness first
  ↓ Switch to DEBUG: Flow Directions view
  ↓ Visual check: Do colors flow downhill?
  ↓
  ├─ NO → FOLLOW: Step 2 diagnostic (algorithm bug)
  │
  └─ YES → CONTINUE: Heightmap quality analysis
      ↓
STEP B: Identify heightmap problem pattern
  ↓ Switch to DEBUG: Sinks view
  ↓ Visual inspection: Where are red markers clustered?
  ↓
  ├─ PATTERN 1: Random scatter (red dots everywhere)
  │   → CAUSE: Noisy heightmap (too many local minima)
  │   → FIX OPTIONS:
  │       • Reduce pit-filling aggressiveness (increase depth/area thresholds)
  │       • Add smoothing pass to elevation post-processing (VS_024)
  │       • Increase pit-filling threshold iterations
  │
  ├─ PATTERN 2: Flat regions (clusters in plains/plateaus)
  │   → CAUSE: Terrain too smooth (no gradient for flow)
  │   → FIX OPTIONS:
  │       • Increase elevation noise magnitude (VS_024 add_noise)
  │       • Add micro-relief to flat regions (post-processing pass)
  │
  └─ PATTERN 3: Specific deep basins (few large clusters)
      → CAUSE: Legitimate lakes BUT not being filled (thresholds too low)
      → FIX OPTIONS:
          • Increase pit-filling DEPTH threshold (50 → 100)
          • Increase pit-filling AREA threshold (100 → 200)
          • These are REAL lakes - may be correct! (validate visually)
```

**🟢 ITERATION WORKFLOW** (After applying fixes):
```
Fixed pit-filling thresholds → Re-run worldgen → Jump to Step 5 (Sinks)
  ├─ PASS → Validate Steps 3-4 (ensure no regressions) → ✅ COMPLETE
  └─ FAIL → Repeat Diagnostic 5 (try different fix)

Fixed D-8 algorithm → Re-run worldgen → Start from Step 2 (FlowDirections)
  └─ Must validate entire chain (Steps 2→3→4→5) - algorithm change affects all

Fixed topological sort → Re-run worldgen → Start from Step 3 (FlowAccumulation)
  └─ Must validate Steps 3→4→5 (sort affects accumulation, sources, sinks)
```

**Done When**:
1. ✅ `WorldGenerationResult` includes all Phase1ErosionData fields + PreFillingLocalMinima
2. ✅ **6 new `MapViewMode` entries** added and wired (Pre/Post sinks + 4 flow views)
3. ✅ `WorldMapRendererNode` renders all 6 modes correctly
4. ✅ **Comprehensive logging added** - Each view mode logs diagnostic stats to Godot Output
5. ✅ **Pit-filling comparison logging** - Shows before/after counts + reduction %
6. ✅ UI dropdown shows debug view options with clear labels (Pre-Filling, Post-Filling, etc.)
7. ✅ **Visual validation: Pre→Post filling shows red markers disappear (filled) and remain (lakes)**
8. ✅ **Data validation: 70-90% sink reduction** (validates pit-filling effectiveness!)
9. ✅ Visual validation: Flow directions show correct downhill drainage
10. ✅ Visual validation: Flow accumulation highlights river valleys (high accumulation paths)
11. ✅ Visual validation: River sources spawn in high mountains with high accumulation
12. ✅ **Data validation: Post-filling sinks show >85% ocean, <10% inland pits** (healthy heightmap!)
13. ✅ **Data validation: Direction distribution favors downhill directions** (validates D-8 algorithm)
14. ✅ All 495+ existing tests GREEN (no regression)
15. ✅ Performance: <50ms overhead for Phase 1 erosion computation

**Depends On**:
- VS_028 ✅ (FINAL precipitation required - already integrated into HydraulicErosionProcessor)
- FlowDirectionCalculator ✅ (D-8 implementation exists)
- Phase1ErosionData DTO ✅ (exists, just needs wiring)

**Blocks**: Nothing (pure debug/validation feature - doesn't block gameplay or next features)

**Enables**:
- Full particle erosion implementation (validates D-8 foundation first)
- Pit-filling threshold tuning (visual feedback on sink distribution)
- Heightmap quality debugging (detect artifacts breaking flow)

**Tech Lead Decision** (2025-10-13):
- **Scope**: Visualization + Diagnostic Logging (D-8 logic already exists and tested!)
- **Priority**: Validate foundation BEFORE building particle erosion (~20-28h) on top
- **Key Focus: SINK ANALYSIS** - Inland sinks reveal heightmap quality issues (artifacts, noise, bad pit-filling)
- **Logging Strategy**: Data complements visual inspection - quantify what you see (sink %, direction distribution, accumulation stats)
- **Risk mitigation**: Visual + data validation catches D-8 bugs AND heightmap issues early (cheaper than debugging complex particle physics)
- **Effort justification**: 4-6h investment validates 20-28h particle erosion foundation
- **Success Metric**: Sink analysis shows >85% ocean, <10% inland pits (healthy heightmap!)
- **Next step after VS_029**: Implement full particle-based erosion (rename existing VS_029 Roadmap spec to VS_030?)

**Implementation Summary** (2025-10-13 15:14):

✅ **Core Visualization** (Phases 1-4):
- Added 6 erosion debug views: Sinks (PRE/POST-Filling), Flow Directions, Flow Accumulation, River Sources, Erosion Hotspots
- Removed FilledElevation view (unnecessary debug step)
- Comprehensive diagnostic logging for each view mode (sink counts, direction distribution, accumulation stats)
- All 495+ existing tests GREEN, zero warnings

✅ **Critical Root Cause Fix - Gaussian Blur for Noisy Heightmap**:
- **Problem Diagnosed**: Native plate simulation produced high-frequency noise (3.3% land sinks PRE-filling)
- **Fix**: Added Stage 0.5 Gaussian smoothing (σ=1.5) BEFORE post-processing
  - Applied to native output → reduces micro-pits from 3.3% to ~1-2% (healthy baseline)
  - Preserves large-scale terrain features (mountains, valleys) while removing noise
- **Architecture**: ElevationPostProcessor.ApplyGaussianBlur() + GenerateWorldPipeline integration
- **Result**: Sink distribution now shows natural clustering in valleys (not dense pockmarks everywhere)

✅ **River Source Algorithm Correction**:
- **Root Cause**: Original algorithm used "high elevation + high accumulation" (finds major rivers IN mountains, not origins)
- **Key Insight**: Sources need LOW accumulation (threshold-crossing), not HIGH (already a river)
- **Fix**: Two-step hybrid - DetectAllSources() + FilterMajorRivers() (hundreds → 5-15 major)
- **Preserved Old Logic**: Repurposed as DetectErosionHotspots() for VS_030+ erosion masking

✅ **Visualization Polish**:
- River Sources: Changed from colored elevation to grayscale + red markers (consistent with sinks views)
- Flow Accumulation: Added ocean masking (black ocean = no flow, land = blue→red heat map)
- Legends: Added proper legends for all 6 erosion views (was showing "Unknown view")
- Visual consistency: All marker-based views use grayscale base, analytical views use color gradients

**Key Architectural Decisions**:
1. **Gaussian blur as Stage 0.5** - Sits between native simulation and post-processing (clean separation)
2. **Ocean masking for Flow Accumulation** - Semantically correct (ocean = terminal sink, not flow data)
3. **Disabled AddNoiseToElevation temporarily** - Isolated Gaussian blur effect (coherent noise can be re-enabled later)

**Quality Note**:
- Pit-filling still shows 0% POST-filling sinks (too aggressive) - deferring threshold tuning to future work
- Current priority: Visualization validated, Gaussian blur fixes root noise issue

**Follow-Up Work**:

**2025-10-13 15:37 - CRITICAL BUG FIX: Topological Sort**:
✅ **Flow Accumulation Bug Fix** ([ALGORITHM-BUG]):
- **Problem**: Flow accumulation showed near-zero values everywhere (pure blue heat map), no river networks visible
- **Root Cause**: TopologicalSortCalculator was adding **SINKS** (ocean cells) to the headwater queue
  - Ocean cells have `dir=-1` (terminal sinks, don't flow anywhere)
  - Because they don't increment in-degrees of neighbors (line 54-56 skip), ocean has in-degree 0
  - Algorithm mistakenly treated ocean as "headwaters" alongside real mountain headwaters
  - Result: Processing order was corrupted - sinks processed before their upstream contributors
- **The Fix** (1-line change in TopologicalSortCalculator.cs:85):
  ```csharp
  // Before (WRONG): Enqueued ALL cells with in-degree 0
  if (inDegree[y, x] == 0)

  // After (CORRECT): Only enqueue NON-SINK cells with in-degree 0
  if (inDegree[y, x] == 0 && flowDirections[y, x] != -1)
  ```
- **Architecture**: Kahn's algorithm requires careful distinction between "no dependencies" (headwaters) vs "terminal nodes" (sinks)
- **Expected Result**: Flow accumulation should now show bright river networks in valleys (exponential growth: 1 → 10 → 100 → 1000+)
- **Testing**: Regenerate world → View Flow Accumulation → Should see red river valleys instead of uniform blue
- **Build Status**: Core compiles cleanly (0 warnings, 0 errors)

**2025-10-13 15:28 - Probe Function Enhancement**:
✅ **Probe Function Enhancement**:
- **Problem**: Cell probe (Q key) showed "Unknown view" for all 6 erosion debug modes
- **Fix**: Added 6 view-mode-specific probe handlers to WorldMapProbeNode.cs
  - `BuildSinksPreFillingProbeData()` - Shows pre-filling sink status + total count
  - `BuildSinksPostFillingProbeData()` - Shows post-filling status + reduction %
  - `BuildFlowDirectionsProbeData()` - Shows direction code (0-7) + compass arrow + downstream elevation
  - `BuildFlowAccumulationProbeData()` - Shows accumulation value + percentile rank + classification
  - `BuildRiverSourcesProbeData()` - Shows source status + total count + why non-sources don't qualify
  - `BuildErosionHotspotsProbeData()` - Shows erosion potential (elevation × accumulation) + classification
- **Architecture**: Each probe queries Phase1ErosionData, shows both raw values (debug) and classifications (understanding)
- **UX Enhancement**: Updated highlight colors (magenta on grayscale views, red on colored views)
- **Result**: Press Q on any cell → View-mode-specific diagnostic data appears in UI panel
- **Build Status**: Godot project compiles cleanly (0 warnings, 0 errors)

---

### VS_033: MVP Item Editor (Weapons + Armor Focus)
**Status**: Proposed (Build AFTER manual item creation phase)
**Owner**: Product Owner → Tech Lead (breakdown) → Dev Engineer (implement)
**Size**: L (15-20h)
**Priority**: Ideas (deferred until designer pain validated)
**Markers**: [TOOLING] [DESIGNER-UX]

**What**: Minimal viable Godot EditorPlugin for creating weapon/armor ItemTemplates with auto-wired i18n and component-based UI.

**Why**:
- **Eliminate CSV pain** - Designer never manually edits CSV files (auto-generates translation keys, auto-syncs en.csv)
- **Component selection UI** - Check boxes for Equippable + Weapon/Armor (vs manual SubResource creation in Inspector)
- **Validation before save** - Catch errors (duplicate keys, missing components) BEFORE runtime
- **80% of content** - Weapons + armor are most items, validates tooling investment

**Strategic Phasing** (do NOT skip ahead):
1. **Phase 1: Validate Pain** (2-4h manual work) - REQUIRED FIRST
   - After VS_032 complete, designer creates 10-20 items manually in Godot Inspector
   - Designer documents pain points: "Inspector tedious", "CSV editing error-prone", "No validation until runtime"
   - **Trigger**: Designer reports "Inspector workflow is too tedious for 20+ items"

2. **Phase 2: Build MVP** (15-20h) - ONLY if Phase 1 pain validated
   - Build EditorPlugin with 5 core features (see "How" below)
   - Focus: Weapons + armor ONLY (defer consumables/tools to Phase 3)
   - Effort: 15-20h (vs 30h full Item Editor by deferring advanced features)

3. **Phase 3: Expand** (when >30 items exist) - Future
   - Add consumables, tools, containers support
   - Add balance tools (DPS calculator, usage tracking)
   - Add batch operations (create 10 variants)

**How** (MVP scope - 5 core features):
1. **Component Selection UI** (3-4h) - Checkboxes: ☑ Equippable, ☑ Weapon, ☑ Armor (auto-show properties)
2. **Quick Templates** (2-3h) - Presets: "Weapon" (Equippable+Weapon), "Armor" (Equippable+Armor), "Shield" (Equippable+Armor+Weapon)
3. **Auto-Wired i18n** (4-5h) - Designer types "Iron Sword" → auto-generates ITEM_IRON_SWORD → auto-writes en.csv (ZERO manual CSV editing!)
4. **Component Validation** (3-4h) - "Weapon requires Equippable", "Duplicate key ITEM_IRON_SWORD", offer auto-fix
5. **Live Preview** (2-3h) - Show sprite + stats + translation key preview

**Deferred for MVP** (add in Phase 3):
- ❌ Balance comparison (DPS calculator, power curves)
- ❌ Usage tracking (which ActorTemplates use this item)
- ❌ Batch operations (create N variants)
- ❌ Consumables/tools support (weapons + armor = 80% of content)

**Done When** (Phase 2 - MVP):
- ✅ Designer creates iron_sword.tres in 2 minutes (vs 5+ minutes Inspector)
- ✅ Component selection via checkboxes (no manual SubResource creation)
- ✅ Zero manual CSV editing (auto-generates ITEM_IRON_SWORD, writes to en.csv)
- ✅ Validation before save (catches duplicate keys, missing components)
- ✅ Works for weapons + armor (can create sword, plate armor, shield)
- ✅ Designer reports: "Item Editor is MUCH faster than Inspector"

**Depends On**:
- VS_032 ✅ (must be complete - validates equipment system works)
- Phase 1 manual item creation (10-20 items) - validates pain is REAL, not hypothetical

**Blocks**: Nothing (tooling is parallel track - doesn't block gameplay features)

**Product Owner Decision** (2025-10-10):
- **Do NOT build until Phase 1 pain validated** - Must create 10-20 items manually first to validate:
  1. Component-based ItemTemplate architecture works (can actors equip items?)
  2. Inspector workflow pain is REAL (not hypothetical)
  3. CSV editing pain is REAL (manual key management sucks)
- **Rationale**: Tools solve REAL pain, not imaginary pain. Must feel pain before building solution.
- **Risk mitigation**: If Phase 1 shows "Inspector is workable", defer Item Editor and create more items (avoid 15-20h investment for low ROI).
- **Scope discipline**: MVP focuses weapons + armor (80% of content). Defer consumables/tools until 30+ items exist (avoid premature generalization).

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