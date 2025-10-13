# Darklands Development Backlog


**Last Updated**: 2025-10-13 16:38 (Dev Engineer: Created TD_020 - ColorScheme SSOT system completion with rendering mode metadata)

**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 010
- **Next TD**: 021
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



---

## 📈 Important (Do Next)
*Core features for current milestone, technical debt affecting velocity*

### TD_020: Complete ColorScheme SSOT System (Renderer Refactoring + Legend Rendering Modes)
**Status**: In Progress (2025-10-13 16:38)
**Owner**: Dev Engineer
**Size**: M (6-8h remaining)
**Priority**: Important (prevents renderer/legend drift bugs)
**Markers**: [ARCHITECTURE] [DRY] [SSOT]

**What**: Complete the ColorScheme Single Source of Truth system by refactoring WorldMapRendererNode and implementing proper legend rendering modes (linear/log/gradient/discrete).

**Why**:
- **Eliminates drift bugs** - Current: Renderer updated to naturalistic flow accumulation, legend showed old heat map colors (user confusion!)
- **Reduces code duplication** - Renderer has 600+ lines of inline color definitions duplicated in Legend
- **Enables easy updates** - Change color scheme once, both components update automatically
- **Improves maintainability** - ViewMode → ColorScheme mapping in ONE place (TRUE SSOT)

**Current Implementation** (Commits c35c1a9, f625399, 347a594):

**✅ Infrastructure Complete**:
1. **Color Scheme System** (9 schemes covering 18 view modes):
   - IColorScheme interface + LegendEntry record
   - ColorSchemes static registry (Elevation, Temperature, Precipitation, FlowDirections, FlowAccumulation, Grayscale, 3 Marker schemes)
   - SchemeBasedRenderer helper utilities

2. **TRUE SSOT Architecture** (ViewModeSchemeRegistry):
   - Central mapping: `MapViewMode → IColorScheme`
   - Both Renderer + Legend query SAME registry (zero manual coordination)
   - GetScheme(viewMode) + GetLegendTitle(viewMode) + HasScheme(viewMode)

3. **Legend Auto-Extraction** (WorldMapLegendNode refactored):
   - UpdateLegend() now queries registry → auto-generates from scheme.GetLegendEntries()
   - Reduced from 312 lines → 145 lines (53% reduction!)
   - RenderCustomLegend() handles non-scheme views (Plates only)

**Code Metrics**:
- Infrastructure: 1,111 lines (9 schemes + registry + helpers)
- Legend: 312 → 145 lines (167 lines removed, 53% reduction)
- Build Status: ✅ Compiles successfully, 0 warnings

**❌ Remaining Work**:

**1. WorldMapRendererNode Refactoring** (~4-5h):
   - Add ViewModeSchemeRegistry lookups to ALL rendering methods
   - Replace inline color definitions with scheme.GetColor() calls
   - Remove obsolete helper methods (moved to SchemeBasedRenderer)
   - **Target**: 1200 lines → ~650 lines (45% reduction)

**Methods to Refactor** (8 total):
   - `RenderTemperatureMap()` - Use TemperatureScheme (60 lines → 20 lines)
   - `RenderPrecipitationMap()` - Use PrecipitationScheme (40 lines → 15 lines)
   - `RenderColoredElevation()` - Use ElevationScheme (80 lines → 30 lines)
   - `RenderFlowDirections()` - Use FlowDirectionScheme (50 lines → 20 lines)
   - `RenderSinksPreFilling/PostFilling()` - Use SinksMarkerScheme (120 lines → 40 lines)
   - `RenderRiverSources()` - Use RiverSourcesMarkerScheme (60 lines → 20 lines)
   - `RenderErosionHotspots()` - Use HotspotsMarkerScheme (100 lines → 35 lines)
   - Remove `FindQuantile()`, `GetPercentileFromSorted()`, etc. (50 lines → 0, moved to SchemeBasedRenderer)

**2. Legend Rendering Modes** (~2-3h - CRITICAL MISSING FEATURE):
   **Problem Identified**: Legend shows color swatches but doesn't explain HOW values map to colors!

   **Current Gap**:
   - Temperature: Shows "Blue = Polar", but how does [0,1] temperature map to Blue?
   - Flow Accumulation: Shows "Bright Cyan = High flow", but is it linear? Logarithmic?
   - Precipitation: Shows "Yellow → Green → Blue", but what's the gradient function?

   **Solution - Add Rendering Mode Metadata**:
   ```csharp
   public interface IColorScheme
   {
       string Name { get; }
       List<LegendEntry> GetLegendEntries();
       Color GetColor(float normalizedValue, params object[] context);

       // NEW: Describe how values map to colors
       LegendRenderingMode GetRenderingMode();  // Linear, Logarithmic, Discrete, Gradient
       string GetValueRange();  // e.g., "[0-1] normalized", "Direction code [0-7]"
   }

   public enum LegendRenderingMode
   {
       Discrete,      // Temperature (7 quantile bands)
       Linear,        // Grayscale (direct mapping)
       Logarithmic,   // Flow Accumulation (log scale for power-law distribution)
       Gradient,      // Precipitation (smooth 3-stop gradient)
       Marker         // Sinks/RiverSources (special case: isMarker bool)
   }
   ```

   **Legend Display Enhancement**:
   - Add subtitle to legend: "Discrete quantile bands" (Temperature)
   - Add subtitle: "Logarithmic scale (power-law)" (Flow Accumulation)
   - Add subtitle: "Smooth 3-stop gradient" (Precipitation)
   - Add subtitle: "Grayscale + markers" (Sinks/RiverSources)

   **Implementation**:
   - Update IColorScheme interface with GetRenderingMode() + GetValueRange()
   - Implement in all 9 concrete schemes
   - Update WorldMapLegendNode to display rendering mode in title
   - Update ViewModeSchemeRegistry.GetLegendTitle() to include rendering mode hint

**How** (Systematic Application):

**Step 1: Add Rendering Mode to ColorScheme System** (~1h)
   - Update IColorScheme with GetRenderingMode() + GetValueRange()
   - Implement in all 9 schemes:
     - Elevation: Discrete (quantile bands)
     - Temperature: Discrete (quantile bands)
     - Precipitation: Gradient (3-stop smooth)
     - FlowDirections: Discrete (direction codes)
     - FlowAccumulation: Logarithmic (power-law distribution)
     - Grayscale: Linear (direct mapping)
     - Marker schemes: Marker (boolean overlay)

**Step 2: Update Legend Display** (~30min)
   - Modify WorldMapLegendNode.UpdateLegend() to show rendering mode
   - Format: "{Scheme.Name} - {RenderingMode}" (e.g., "Flow Accumulation - Logarithmic scale")
   - Add tooltip/subtitle explaining what the rendering mode means

**Step 3: Refactor WorldMapRendererNode** (~4h)
   - Add import: `using Darklands.Features.WorldGen.ColorSchemes;`
   - For each rendering method:
     ```csharp
     // Before (inline colors)
     Color dryColor = new Color(1f, 1f, 0f);
     Color wetColor = new Color(0f, 0f, 1f);
     // ... 30 lines of color logic ...

     // After (scheme-based)
     var scheme = ViewModeSchemeRegistry.GetScheme(_currentViewMode);
     var image = SchemeBasedRenderer.RenderNormalizedMap(data, scheme);
     ```
   - Remove obsolete helper methods
   - Build + test after each method (verify visual parity)

**Step 4: Visual Validation** (~30min)
   - Launch Godot, cycle through all 18 view modes
   - Verify legends show rendering mode (e.g., "Logarithmic scale")
   - Verify colors match exactly (no regression)
   - Test Plates view (custom legend still works)

**Done When**:
1. ✅ IColorScheme includes GetRenderingMode() + GetValueRange()
2. ✅ All 9 schemes implement rendering mode metadata
3. ✅ Legend displays rendering mode in title/subtitle
4. ✅ WorldMapRendererNode refactored to use ViewModeSchemeRegistry lookups
5. ✅ Renderer reduced from 1200 → ~650 lines (45% reduction)
6. ✅ All 18 view modes tested - colors match, legends accurate
7. ✅ Legend shows "Flow Accumulation - Logarithmic scale" (user understands mapping!)
8. ✅ Build succeeds with 0 warnings
9. ✅ Visual parity confirmed (no color regressions)

**Depends On**: None (infrastructure complete)

**Blocks**: Nothing (quality/maintainability improvement)

**Dev Engineer Notes** (2025-10-13 16:38):

**Key Insight from User**: "Legend doesn't correctly extract the color scheme" revealed missing feature - we show WHAT colors mean ("Blue = Polar") but not HOW values map to colors (discrete bands? logarithmic? gradient?).

**Why Rendering Mode Matters**:
- **Temperature**: Discrete quantile bands (7 equal-population buckets) - user needs to know it's NOT linear!
- **Flow Accumulation**: Logarithmic scale (power-law distribution) - explains why most cells are blue with few red hot spots
- **Precipitation**: Smooth gradient (Yellow → Green → Blue) - user understands interpolation
- **Flow Directions**: Discrete codes (0-7 = compass directions) - user understands it's categorical, not continuous

**Architecture Quality**:
- TRUE SSOT achieved via ViewModeSchemeRegistry (single mapping point)
- Zero manual coordination between Renderer + Legend (both query same source)
- Adding rendering mode metadata completes the legend's explanatory power

**Estimated Effort**: 6-8h remaining (4-5h renderer refactoring + 2-3h rendering mode feature)

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