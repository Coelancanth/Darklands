# Darklands Development Archive - October 2025

**⚠️ CRITICAL: This is an APPEND-ONLY archive. Never delete or overwrite existing entries.**

**Purpose**: Completed and rejected work items for historical reference and lessons learned.

**Created**: 2025-10-08
**Archive Period**: October 2025 (Part 4)
**Previous Archive**: Completed_Backlog_2025-10_Part2.md

## Archive Protocol

### Extraction Status
Items are moved here COMPLETE with all context, then marked for extraction:
- **NOT EXTRACTED** ⚠️ - Full context preserved, patterns not yet extracted
- **PARTIALLY EXTRACTED** 🔄 - Some learnings captured in ADRs/HANDBOOK
- **FULLY EXTRACTED** ✅ - All valuable patterns documented elsewhere

### Format for Completed Items
```markdown
### [Type]_[Number]: Title
**Extraction Status**: NOT EXTRACTED ⚠️
**Completed**: Date
**Archive Note**: [One-line summary of achievement]
---
[PASTE ENTIRE ORIGINAL ITEM HERE - PRESERVE EVERYTHING]
---
**Extraction Targets**:
- [ ] ADR needed for: [architectural decision]
- [ ] HANDBOOK update: [pattern to document]
- [ ] Test pattern: [testing approach to capture]
```

## Format for Rejected Items
```markdown
### [Type]_[Number]: Title ❌ REJECTED
**Rejected**: Date
**Reason**: Why rejected
**Alternative**: What we did instead
[RESURRECT-IF: Specific conditions that would make this relevant]
```

---

## Completed Items (October 2025 - Part 4)

### TD_012: WorldMap Visualization - Dynamic Legends
**Extraction Status**: NOT EXTRACTED ⚠️
**Completed**: 2025-10-08
**Archive Note**: Fixed WorldMapLegendNode to properly display color keys for each view mode, moved to upper-left with PanelContainer, reordered view modes (ColoredElevation as default), dynamic legend content per view mode (RawElevation 3-band, ColoredElevation 7-band terrain gradient, Plates 10-color). All 433 tests GREEN.

---

**Status**: Done (2025-10-08)
**Owner**: Dev Engineer
**Size**: S (~2h actual)
**Priority**: Ideas
**Markers**: [WORLDGEN] [UI] [VISUALIZATION] [TECHNICAL-DEBT]

**What**: Fix WorldMapLegendNode to properly display color keys for each view mode, move to upper-left, reorder view modes with ColoredElevation as default

**Why**: Current legend renders but not optimally positioned. Essential for understanding terrain colors. User requested ColoredElevation as primary view.

**Implementation Summary**:
- ✅ Moved legend to upper-left corner (anchor system: 10px from top-left)
- ✅ Added PanelContainer background for visibility
- ✅ Implemented dynamic legend content per view mode:
  - RawElevation: 3-band grayscale (black/gray/white)
  - ColoredElevation: **7-band terrain gradient** (deep ocean → peaks)
  - Plates: "Each color = unique plate" (10 plates)
- ✅ Removed Plates from UI dropdown (kept ColoredElevation + RawElevation only)
- ✅ Reordered dropdown: ColoredElevation first, RawElevation second
- ✅ Changed default view mode to ColoredElevation in all nodes
- ✅ Legend updates dynamically when switching views
- ✅ All 433 tests GREEN

**Color Legend Details** (ColoredElevation):
1. Deep Blue → Deep ocean
2. Blue → Ocean
3. Cyan → Shallow water
4. Green → Grass/Lowlands
5. Yellow-Green → Hills
6. Yellow → Mountains
7. Brown → Peaks

**Completed**: 2025-10-08 05:53 by Dev Engineer

---

**Extraction Targets**:
- [ ] HANDBOOK update: Dynamic UI content pattern (legend updates based on view mode)
- [ ] HANDBOOK update: Godot anchor system usage (position UI via anchors, not hardcoded coords)
- [ ] Reference implementation: WorldMapLegendNode as template for mode-dependent UI
- [ ] UX pattern: Color legend design for scientific visualization (quantile-based terrain mapping)

---

### TD_013: WorldMap Visualization - Fix Colored Elevation Rendering
**Extraction Status**: NOT EXTRACTED ⚠️
**Completed**: 2025-10-08
**Archive Note**: Fixed colored elevation view bug - quantile-based color mapping used raw heightmap values instead of normalized [0,1] range, causing only 2 visible bands (ocean/land). Added normalization step before quantile calculation. All 7 color bands now visible (deep ocean → peaks). Matches map_drawing.cpp reference implementation exactly. All 433 tests GREEN.

---

**Status**: Done (2025-10-08)
**Owner**: Dev Engineer
**Size**: S (~2h actual)
**Priority**: Ideas
**Markers**: [WORLDGEN] [VISUALIZATION] [TECHNICAL-DEBT] [BUG]

**What**: Fix colored elevation view - currently shows only blue/brown, missing intermediate colors (cyan, green, yellow)

**Why**: Quantile-based color mapping implementation has a bug. Only shows ocean (blue) and land (brown), no intermediate terrain colors (shallows, grass, hills, mountains).

**Root Cause Found**:
- ColoredElevation used **raw heightmap values** directly (e.g., [-0.3, 1.6] range)
- Reference implementation expects **normalized [0, 1]** range
- Quantiles collapsed to only 2 visible bands (ocean/land) instead of 7
- First gradient band used hardcoded `0.0f` min, but heightmap could be negative!

**Implementation Summary**:
- ✅ Added normalization step (find min/max, normalize to [0,1]) before quantile calculation
- ✅ Created `normalizedHeightmap` array for quantile processing
- ✅ Quantiles now calculated on normalized data (matches reference behavior)
- ✅ All 7 color bands now visible: deep ocean → ocean → shallows → grass → hills → mountains → peaks
- ✅ Matches `map_drawing.cpp` reference implementation exactly
- ✅ All 433 tests GREEN

**Key Insight**:
The bug was architectural - raw vs normalized data mismatch. `RawElevation` renderer already normalized correctly, but `ColoredElevation` skipped this step. Fix: Apply same normalization pattern.

**Completed**: 2025-10-08 05:48 by Dev Engineer

---

**Extraction Targets**:
- [ ] ADR needed for: Data normalization requirement for quantile-based visualization
- [ ] HANDBOOK update: Quantile calculation pattern (always normalize input data first)
- [ ] HANDBOOK update: Root cause analysis - architectural mismatches (raw vs normalized data)
- [ ] Test pattern: Visual rendering validation (reference implementation comparison)
- [ ] Bug pattern: Hardcoded assumptions in gradient mapping (0.0 min breaks with negative values)

---

### TD_015: WorldMap Persistence - Disk Serialization
**Extraction Status**: NOT EXTRACTED ⚠️
**Completed**: 2025-10-08
**Archive Note**: Binary serialization/deserialization of world data with manual save/load UI. Binary format (~2.1 MB per 512×512 world) with magic number "DWLD" and version header. Created WorldMapSerializationService, added Save/Load buttons to WorldMapUINode. Save directory: user://worldgen_saves/, filename convention: world_{seed}.dwld. Format validation working, status feedback in UI. All 433 tests GREEN.

---

**Status**: Done (2025-10-08)
**Owner**: Dev Engineer
**Size**: M (~4h actual)
**Priority**: Ideas
**Markers**: [WORLDGEN] [PERFORMANCE] [SERIALIZATION] [TECHNICAL-DEBT]

**What**: Binary serialization/deserialization of world data with manual save/load UI

**Why**: Users can save generated worlds to disk and reload them later (testing, iteration, sharing seeds)

**Implementation Approach** (NO auto-cache):
- **Manual save/load** buttons in UI (user-triggered, not automatic)
- **Binary format** for compact storage (~2.1 MB per 512×512 world)
- **Versioned format** with magic number for validation
- **Simple workflow**: Generate → Save → Load later

**Binary Format**:
```
Header (16 bytes):
- Magic: "DWLD" (4 bytes) - File type identifier
- Version: uint32 (4 bytes) - Format versioning
- Seed: int32 (4 bytes) - Original generation seed
- Reserved: 4 bytes - Future expansion

Data Section:
- Width/Height: uint32 each (8 bytes)
- Heightmap: float[h, w] row-major (4 bytes × cells)
- PlatesMap: uint[h, w] row-major (4 bytes × cells)
```

**Implementation Summary**:
- ✅ Created WorldMapSerializationService (binary I/O)
- ✅ Added Save/Load buttons to WorldMapUINode (horizontal layout)
- ✅ Wired signals in WorldMapOrchestratorNode
- ✅ Save directory: `user://worldgen_saves/` (auto-created)
- ✅ Filename convention: `world_{seed}.dwld`
- ✅ Format validation (magic number, version check)
- ✅ Status feedback in UI ("Saved: world_42.dwld")
- ✅ All 433 tests GREEN

**User Workflow**:
1. Generate world (seed=42, 3-5s wait)
2. Click "Save World" → `user://worldgen_saves/world_42.dwld` created (~2.1 MB)
3. Close/reopen scene
4. Click "Load World" → Instant load from disk (~100ms)

**Why No Auto-Cache**:
- Simpler: User explicitly saves what they want to keep
- Clearer: No hidden cache management/eviction logic
- Flexible: User controls disk usage (delete .dwld files to clean up)

**Completed**: 2025-10-08 06:05 by Dev Engineer

---

**Extraction Targets**:
- [ ] ADR needed for: Manual save/load vs auto-cache architecture (explicit user control)
- [ ] ADR needed for: Binary format design (versioned header, magic number validation)
- [ ] HANDBOOK update: Godot file I/O pattern (user:// directory for persistent storage)
- [ ] HANDBOOK update: Binary serialization pattern (row-major array layout, format versioning)
- [ ] Reference implementation: WorldMapSerializationService as template for game state persistence
- [ ] UX pattern: Manual save/load workflow (user-controlled persistence, no hidden caching)
- [ ] Performance insight: Binary vs text formats (~2.1 MB binary vs ~10+ MB CSV for 512×512 world)

---

### VS_023: WorldGen Pipeline - GenerateWorldPipeline Architecture
**Extraction Status**: NOT EXTRACTED ⚠️
**Completed**: 2025-10-08
**Archive Note**: Created three-layer architecture (Handler → Pipeline → Simulator) with WorldGenerationResult DTO supporting optional post-processing fields. Pipeline currently pass-through (Stage 0), ready for incremental VS_022 phases. Introduced IWorldGenerationPipeline abstraction, registered in GameStrapper. All 433 tests GREEN.

---

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

**Extraction Targets**:
- [ ] ADR needed for: Three-layer worldgen architecture (Handler → Pipeline → Simulator separation of concerns)
- [ ] ADR needed for: Optional field pattern for incremental pipeline features (null until phase implemented)
- [ ] HANDBOOK update: Pipeline pattern for multi-stage processing (orchestration vs execution)
- [ ] HANDBOOK update: DTO evolution pattern (WorldGenerationResult extends PlateSimulationResult semantics)
- [ ] Reference implementation: GenerateWorldPipeline as template for incremental feature delivery
- [ ] Architecture pattern: DI registration for transient vs singleton services (Pipeline = Transient, correct choice)
- [ ] Testing insight: Zero new tests needed when refactoring to cleaner architecture (433 existing tests validated behavior preservation)

---

### TD_018: Upgrade World Serialization to Save Post-Processed Data (Format v2)
**Extraction Status**: NOT EXTRACTED ⚠️
**Completed**: 2025-10-08 08:58
**Archive Note**: Upgraded world save file format from v1 (raw native only) to v2 (includes post-processed heightmap, thresholds, ocean mask, sea depth) with backward compatibility. Simplified orchestrator code (-45 lines). Bit-packed OceanMask for space efficiency (8× savings). All 433 tests GREEN.

---

**Status**: Done (2025-10-08 08:58)
**Owner**: Dev Engineer
**Size**: S (~3h actual)
**Priority**: Ideas
**Markers**: [WORLDGEN] [SERIALIZATION] [TECHNICAL-DEBT] [PERFORMANCE]

**What**: Upgrade world save file format from v1 (raw native only) to v2 (includes post-processed heightmap, thresholds, ocean mask, sea depth) with backward compatibility

**Why**: VS_024 post-processing data was lost on save/load, causing regeneration overhead and degraded features for cached worlds

**How**:

**Current Format v1** (2MB for 512x512):
```
Header (16 bytes): Magic "DWLD" + Version(1) + Seed + Reserved
Dimensions (8 bytes): Width + Height
Heightmap (W×H×4 bytes): Raw float[,]
PlatesMap (W×H×4 bytes): Raw uint[,]
```

**Proposed Format v2** (4MB for 512x512):
```
Header (16 bytes): Magic "DWLD" + Version(2) + Seed + Reserved
Dimensions (8 bytes): Width + Height
Heightmap (W×H×4 bytes): Original raw float[,]
PlatesMap (W×H×4 bytes): Raw uint[,]

--- NEW SECTIONS (VS_024 data) ---
PostProcessed Flag (1 byte): HasPostProcessed (0/1)
[If HasPostProcessed = 1:]
  PostProcessedHeightmap (W×H×4 bytes): float[,]
  Thresholds (16 bytes): SeaLevel + HillLevel + MountainLevel + PeakLevel
  OceanMask (W×H÷8 bytes): Bit-packed bool[,] for space efficiency
  SeaDepth Flag (1 byte): HasSeaDepth (0/1)
  [If HasSeaDepth = 1:]
    SeaDepth (W×H×4 bytes): float[,]
```

**Implementation Details**:

1. **WorldMapSerializationService.cs Changes**:
   ```csharp
   // Change signature from PlateSimulationResult → WorldGenerationResult
   public bool SaveWorld(WorldGenerationResult world, int seed, string filename)

   // Version detection on load
   public (bool Success, WorldGenerationResult? World, int Seed) LoadWorld(string filename)
   {
       uint version = file.Get32();
       return version switch {
           1 => LoadV1(file, seed),  // Backward compat: thresholds=null
           2 => LoadV2(file, seed),  // Full data
           _ => (false, null, 0)      // Unsupported
       };
   }
   ```

2. **Dimension Validation** (corruption detection):
   ```csharp
   // After reading Width × Height, validate all arrays match
   if (postProcessedHeightmap != null &&
       (postProcessedHeightmap.GetLength(0) != height ||
        postProcessedHeightmap.GetLength(1) != width))
   {
       _logger.LogError("Dimension mismatch: Post-processed heightmap doesn't match declared size");
       return (false, null, 0);
   }
   // Same validation for OceanMask, SeaDepth
   ```

3. **Backward Compatibility**:
   - v1 files load as before → `WorldGenerationResult` with `thresholds: null`
   - Orchestrator already handles null gracefully (shows fallback message)
   - No data loss (can re-save as v2 after regeneration)

4. **Orchestrator Updates** (3 call sites):
   ```csharp
   // Auto-save after generation
   _serializationService.SaveWorld(result.Value, seed, cacheFilename);

   // User manual save
   _serializationService.SaveWorld(_currentWorld, _currentSeed, filename);
   ```

**File Size Impact**:
- 512x512 world: 2MB (v1) → 4MB (v2)
- Acceptable trade-off: Instant full-featured load vs regeneration cost

**Implementation Summary** (2025-10-08):

**Core Changes**:
1. **WorldMapSerializationService.cs** (~200 lines added):
   - FORMAT_VERSION upgraded: 1 → 2
   - Method signatures changed: `PlateSimulationResult` → `WorldGenerationResult`
   - Version detection: `LoadWorld()` routes to `LoadV1()` or `LoadV2()`
   - Helper methods: `SaveOptionalFloatArray()`, `SaveOptionalBoolArray()` (bit-packing)
   - Dimension validation: `ValidateDimensions<T>()` catches corruption

2. **Format v2 Structure**:
   - Header (16 bytes): Magic + Version(2) + Seed + Reserved
   - Core data: Heightmap + PlatesMap (v1 compatibility)
   - VS_024 data: PostProcessedHeightmap + Thresholds (16 bytes) + OceanMask (bit-packed) + SeaDepth
   - Optional flags: Each field has HasData flag (forward-compatible for VS_025+)

3. **Orchestrator Simplification** (-45 lines):
   - Line 150: `SaveWorld(_currentWorld, ...)` (was: `SaveWorld(_currentWorld.RawNativeOutput, ...)`)
   - Line 176: `_currentWorld = world` (was: 15 lines of manual wrapping)
   - Line 222: Same simplification for auto-load cache
   - Line 265: Same simplification for auto-save

**Technical Wins**:
- ✅ **Bit-packing**: OceanMask 256KB → 32KB (8× savings)
- ✅ **Backward compat**: v1 files load with graceful degradation
- ✅ **Corruption detection**: Dimension validation before object creation
- ✅ **Forward compat**: Optional fields pattern supports VS_025 TemperatureMap without version bump

**File Size Impact**:
- 512×512 world: 2MB (v1) → ~4MB (v2 with full post-processing)
- Trade-off: 2MB extra for instant full-featured load (no regeneration delay)

**Quality Gates**:
- ✅ All 433 tests GREEN
- ✅ Clean compilation (zero warnings)
- ✅ Orchestrator code simplified significantly

**Depends On**: VS_024 (completed) ✅

---

**Extraction Targets**:
- [ ] ADR needed for: Versioned binary format design with backward compatibility strategy
- [ ] ADR needed for: Bit-packing optimization for boolean arrays (8× space savings)
- [ ] HANDBOOK update: Binary serialization evolution pattern (optional fields + version detection)
- [ ] HANDBOOK update: Corruption detection pattern (dimension validation before object creation)
- [ ] Reference implementation: WorldMapSerializationService as template for versioned data formats
- [ ] Performance insight: Bit-packing optimization techniques (OceanMask 256KB → 32KB)
- [ ] Code simplification: How format v2 eliminated 45 lines of orchestrator wrapping code

---

### VS_024: WorldGen Pipeline Stage 1 - Elevation Post-Processing & Real-World Mapping
**Extraction Status**: NOT EXTRACTED ⚠️
**Completed**: 2025-10-08 08:35
**Archive Note**: Implemented Stage 1 of world generation pipeline: WorldEngine elevation post-processing (4 algorithms: add_noise, fill_ocean, harmonize_ocean, sea_depth) + quantile-based thresholds + real-world meters mapping. Dual-heightmap architecture (original raw + post-processed raw) + ElevationThresholds + ElevationMapper. Three colored elevation views for validation. All 433 tests GREEN.

---

**Status**: Done (2025-10-08 08:35)
**Owner**: Dev Engineer
**Size**: M (~8h actual)
**Priority**: Ideas
**Markers**: [WORLDGEN] [PIPELINE] [STAGE-1] [FOUNDATION] [WORLDENGINE-COMPLETE]

**What**: Implement Stage 1 of world generation pipeline: WorldEngine elevation post-processing (4 algorithms: add_noise, fill_ocean, harmonize_ocean, sea_depth) + quantile-based thresholds + real-world meters mapping

**Why**: Foundation for ALL downstream pipeline stages (temperature, precipitation, biomes). WorldEngine post-processing produces high-quality terrain: varied elevation (8-octave Simplex noise), realistic oceans (BFS flood-fill + smoothing), depth maps. Quantile thresholds enable adaptive algorithms (flat vs mountainous worlds). Real-world mapping provides human-readable UI.

**How** (implemented 2025-10-08):

**FINAL ARCHITECTURE**: Dual-Heightmap + Quantile Thresholds + Real-World Mapping
```
Original Heightmap (raw [0.1-20])         → ColoredOriginalElevation (native baseline)
Post-Processed Heightmap (raw [0.1-20])   → ColoredPostProcessedElevation (after 4 algorithms)
ElevationThresholds (quantiles)           → Adaptive per-world (SeaLevel, HillLevel, MountainLevel, PeakLevel)
ElevationMapper (Presentation only)       → Real-world meters for UI display
```

**Key Decisions**:
- **Removed** normalized [0,1] heightmap (confusing, no clear purpose)
- **Added** quantile-based thresholds (WorldEngine's adaptive approach)
- **Added** real-world meters mapping (Presentation layer utility)
- **Rationale**: Algorithms use raw values + thresholds (simple), UI shows meters (meaningful)

**Stage 1 Algorithm** (4 WorldEngine steps, ~150 lines ported):

**Step 1: Post-Processing** (4 algorithms ported from WorldEngine):

1. **add_noise_to_elevation()** (FastNoiseLite integration):
   - Clone original heightmap (SACRED - never modify original!)
   - Use FastNoiseLite (MIT licensed, single-file library)
   - OpenSimplex2 noise, 8 octaves, frequency = 1/128.0
   - Matches WorldEngine parameters exactly (generation.py:74-80)
   - Result: Smooth terrain-scale variation (not pixel noise!)

2. **fill_ocean()** (BFS flood fill):
   - Start from border cells below sea level
   - Flood-fill connected regions
   - Mark all reachable cells as ocean
   - Result: `oceanMask` (true=ocean, false=land)

3. **harmonize_ocean()**:
   - Smooth ocean floor (reduce jaggedness)
   - Calculate elevation class thresholds (deep/shallow/shore)
   - Result: Realistic ocean bathymetry

4. **sea_depth()**:
   - Calculate normalized depth below sea level
   - Modulate by distance to nearest land
   - Anti-alias depth transitions
   - Result: `seaDepth` map (for future ocean rendering)

**Step 2: Quantile Threshold Calculation** (FINAL APPROACH - adaptive per-world):

**ElevationThresholds** (4 quantile-based values):
- **SeaLevel**: 50th percentile overall (median, typically ~1.0)
- **HillLevel**: 70th percentile of land cells
- **MountainLevel**: 85th percentile of land cells (used by temperature algorithm for cooling!)
- **PeakLevel**: 95th percentile of land cells
- Result: Thresholds adapt to each world's terrain distribution (flat vs mountainous)

**Step 3: Real-World Mapping** (Presentation layer utility):

**ElevationMapper** (meters mapping for human-readable display):
- Ocean: [0.1, seaLevel] → [-11,000m, 0m] (Mariana Trench to sea level)
- Land: [seaLevel, 20.0] → [0m, 8,849m] (sea level to Mt. Everest)
- Used ONLY in UI (probe tooltips, displays)
- Algorithms use raw values + thresholds (NOT meters!)

**Step 4: Output Assembly**:
- `Heightmap` (raw [0.1-20]) - SACRED native baseline
- `PostProcessedHeightmap` (raw [0.1-20]) - after 4 algorithms
- `Thresholds` - quantile-based (SeaLevel, HillLevel, MountainLevel, PeakLevel)
- `OceanMask` - flood-filled (BFS, not simple threshold!)
- `SeaDepth` - normalized depth map [0,1]

**WorldGenerationResult DTO Update** (FINAL):
```csharp
public record WorldGenerationResult
{
    // DUAL HEIGHTMAP (raw values for algorithms)
    public float[,] Heightmap { get; init; }              // Original [0.1-20] - SACRED!
    public float[,]? PostProcessedHeightmap { get; init; } // Post-processed [0.1-20] ← NEW!

    // QUANTILE THRESHOLDS (adaptive per-world)
    public ElevationThresholds? Thresholds { get; init; }  // SeaLevel, HillLevel, MountainLevel, PeakLevel ← NEW!

    // DERIVED DATA (ocean/depth)
    public bool[,]? OceanMask { get; init; }              // Flood-filled ocean ← NEW!
    public float[,]? SeaDepth { get; init; }              // Normalized depth [0,1] ← NEW!

    public uint[,] PlatesMap { get; init; }
    public float[,]? TemperatureMap { get; init; }       // Stage 2 (VS_025)
    public PlateSimulationResult RawNativeOutput { get; init; }
}
```

**Visualization Integration** (3 colored elevation views):
1. **View Mode Enum Updates**:
   ```csharp
   public enum MapViewMode
   {
       ColoredOriginalElevation,          // Quantile colors on ORIGINAL (raw [0-20]) ← NEW!
       ColoredPostProcessedElevation,     // Quantile colors on POST-PROCESSED (raw [0-20]) ← NEW!
       ColoredNormalizedElevation,        // Quantile colors on NORMALIZED ([0, 1]) ← NEW! (validation)
       Plates,
       Temperature,                       // VS_025 will add
       SeaDepth                           // Optional: ocean depth visualization (future)
   }
   ```

2. **Renderer Changes** (WorldMapRendererNode.cs):
   - Change signature: `SetWorldData(WorldGenerationResult data)` (not PlateSimulationResult!)
   - `RenderColoredElevation()` accepts `float[,] heightmap` parameter (normalizes internally if needed)
   - `ColoredOriginalElevation` calls `RenderColoredElevation(data.Heightmap)` ← ORIGINAL RAW!
   - `ColoredPostProcessedElevation` calls `RenderColoredElevation(data.PostProcessedHeightmap!)` ← POST-PROCESSED RAW!
   - `ColoredNormalizedElevation` calls `RenderColoredElevation(data.NormalizedHeightmap!)` ← NORMALIZED!

3. **Legend Updates** (WorldMapLegendNode.cs):
   - `ColoredOriginalElevation` legend: "Original Elevation (native output, raw)"
   - `ColoredPostProcessedElevation` legend: "Post-Processed Elevation (noise + smooth oceans, raw)"
   - `ColoredNormalizedElevation` legend: "Normalized Elevation (post-processed, [0,1])"
   - ColoredPostProcessed vs ColoredNormalized should look identical (validation)

4. **Probe Updates** (WorldMapProbeNode.cs):
   - Show original (raw): `"Original: {rawHeight:F2}"`
   - Show post-processed (raw): `"PostProc: {postProcHeight:F2}"`
   - Show normalized: `"Normalized: {normalized:F3}"`
   - Show ocean status: `"Ocean: {isOcean}"` (flood-filled)
   - Show sea depth: `"Depth: {depth:F2}"` (if ocean, optional)

5. **UI Updates** (WorldMapUINode.cs):
   - Add "Colored Original" button (original raw, native baseline)
   - Add "Colored Post-Processed" button (post-processed raw, see noise/smoothing effect)
   - Add "Colored Normalized" button (normalized [0,1], validation - should match post-processed)

**Orchestrator Changes** (WorldMapOrchestratorNode.cs):
- Pass full `WorldGenerationResult` to renderer (not `RawNativeOutput`!)
- Update serialization to save/load full result

**Pipeline Changes** (GenerateWorldPipeline.cs):
```csharp
// Stage 1: WorldEngine elevation post-processing (4 steps, NO center_land/place_oceans)
var original = nativeResult.Value.Heightmap;

// Post-process (4 steps: add_noise, fill_ocean, harmonize_ocean, sea_depth)
var postProcessed = ElevationPostProcessor.Process(
    heightmap: original,  // Clone internally to preserve original!
    seaLevel: parameters.SeaLevel,
    seed: parameters.Seed);

// postProcessed contains:
//   - ProcessedHeightmap (after 4 steps, still raw [0-20] range)
//   - OceanMask (flood-filled)
//   - SeaDepth (optional)

// Normalize ONLY post-processed (original stays raw for rendering)
var normalized = NormalizeHeightmap(postProcessed.ProcessedHeightmap);

return new WorldGenerationResult(
    heightmap: original,                              // ← Keep raw native (render original)!
    platesMap: nativeResult.Value.PlatesMap,
    rawNativeOutput: nativeResult.Value,
    postProcessedHeightmap: postProcessed.ProcessedHeightmap,  // ← NEW! Raw post-processed (render)
    normalizedHeightmap: normalized,                  // ← NEW! Normalized (validation + Stages 2+)
    oceanMask: postProcessed.OceanMask,              // ← NEW! Flood-filled
    seaDepth: postProcessed.SeaDepth,                // ← NEW! Optional
    temperatureMap: null                             // ← Stage 2 (VS_025)
);
```

**Done When**:
1. ✅ **WorldEngine post-processing complete** (4 algorithms ported, ~150 lines):
   - `add_noise_to_elevation()` - Perlin variation added
   - `fill_ocean()` - BFS flood fill ocean detection
   - `harmonize_ocean()` - ocean floor smoothed
   - `sea_depth()` - depth map calculated
   - SKIPPED: center_land, place_oceans_at_map_borders (not needed)

2. ✅ **Triple heightmap working**:
   - `Heightmap` field unchanged (raw native [0-20], render original)
   - `PostProcessedHeightmap` field populated (raw [0-20], render post-processed)
   - `NormalizedHeightmap` field populated ([0, 1], validation + Stages 2+)
   - `OceanMask` field populated (flood-filled, not threshold!)
   - `SeaDepth` field populated (optional, for future ocean views)

3. ✅ **3 colored elevation views working**:
   - ColoredOriginalElevation: Quantile colors on original raw (native baseline)
   - ColoredPostProcessedElevation: Quantile colors on post-processed raw (see noise/smoothing)
   - ColoredNormalizedElevation: Quantile colors on normalized (validation - matches post-processed!)

4. ✅ **Visual validation**:
   - ColoredOriginalElevation vs ColoredPostProcessedElevation: Should differ (post-processing adds noise/smoothing)
   - ColoredPostProcessedElevation vs ColoredNormalizedElevation: Should be **identical** (proves normalization correct)
   - Visual QA: Post-processed has varied terrain (noise), smoother oceans, flood-filled ocean mask

5. ✅ **Architecture clean**:
   - Renderer accepts `WorldGenerationResult` (not `PlateSimulationResult`)
   - Orchestrator passes full pipeline output
   - `ElevationPostProcessor.cs` exists with 4 WorldEngine algorithms
   - `GenerateWorldPipeline.cs` has clear Stage 1 section with TODO Stage 2
   - All 433 tests remain GREEN

6. ✅ **Memory acceptable**:
   - +3 MB for `PostProcessedHeightmap` + `NormalizedHeightmap` + `SeaDepth` (512×512 world)
   - Total cache size ~9-12 MB (acceptable)

**Depends On**: VS_023 (GenerateWorldPipeline architecture) ✅ Complete

**Tech Lead Decision** (2025-10-08 07:20 - Final: 4 Steps + Raw Rendering):
- **Triple heightmap**: Preserve raw native (render original) + raw post-processed (render post-processed) + normalized post-processed (validation + Stages 2+). Memory cost acceptable (+3 MB).
- **Three render targets**: Original (raw [0-20]), PostProcessed (raw [0-20]), Normalized ([0, 1]).
- **View modes**: 3 colored elevation variants (OriginalColor, PostProcessedColor, NormalizedColor) - all use quantile renderer.
- **WorldEngine selective**: 4 post-processing steps included (add_noise, fill_ocean, harmonize, sea_depth). SKIP center_land + place_oceans_at_map_borders (not needed).
- **Size estimate**: M (~5-6h) due to 4 algorithm ports (~150 lines) + single normalization + 3 view modes.
- **Quality + Comparison**: Render BOTH original and post-processed (raw) for visual comparison. Normalized validates post-processing correctness.
- **Architecture shift**: Renderer signature changes to `WorldGenerationResult` - affects Orchestrator, tests (~3 files).
- **Validation strategy**:
  - Toggle OriginalColor ↔ PostProcessedColor: See post-processing impact (noise, smoother oceans)
  - Toggle PostProcessedColor ↔ NormalizedColor: Should be **identical** (proves normalization correct)
- **References**: WorldEngine `basic.py` (elevation post-processing module) - port 4 algorithms to C# `ElevationPostProcessor.cs`.
- **Next steps**: Dev Engineer implements 4 post-processing algorithms → normalization → 3 view modes → visual comparison + validation, then unblocks VS_025 (Temperature uses NormalizedHeightmap).

---

**Extraction Targets**:
- [ ] ADR needed for: Dual-heightmap architecture (original + post-processed preservation strategy)
- [ ] ADR needed for: Quantile-based threshold system for adaptive terrain algorithms
- [ ] ADR needed for: Real-world mapping layer (meters for UI vs raw values for algorithms)
- [ ] HANDBOOK update: WorldEngine algorithm porting pattern (4-step post-processing pipeline)
- [ ] HANDBOOK update: BFS flood-fill ocean detection (vs simple threshold approach)
- [ ] HANDBOOK update: FastNoiseLite integration pattern (8-octave Simplex noise for terrain variation)
- [ ] Reference implementation: ElevationPostProcessor as template for WorldEngine C# ports
- [ ] Reference implementation: ElevationThresholds as adaptive per-world parameter system
- [ ] Reference implementation: ElevationMapper as Presentation layer utility (algorithm-agnostic)
- [ ] Testing pattern: Visual validation strategy (3 view modes for comparing processing stages)
- [ ] Performance insight: Memory trade-offs (3MB extra for dual-heightmap preservation)
- [ ] Architecture insight: Optional field evolution (WorldGenerationResult supports incremental pipeline stages)

---

### VS_025: WorldGen Pipeline Stage 2 - Temperature Simulation
**Extraction Status**: NOT EXTRACTED ⚠️
**Completed**: 2025-10-08 16:29
**Archive Note**: Implemented Stage 2 temperature map with 4-component WorldEngine algorithm (latitude+tilt 92%, noise 8%, distance-to-sun, mountain-cooling) and 4-stage debug visualization (LatitudeOnly → WithNoise → WithDistance → Final). Per-world climate variation via AxialTilt and DistanceToSun. Fixed noise configuration bug (missing FBm fractal + frequency). Visual validation: smooth latitude bands, subtle fuzzy climate variation, hot/cold planets, mountains blue at all latitudes. All 447 tests GREEN.

---

**Status**: Done ✅ (2025-10-08 15:42)
**Owner**: Dev Engineer
**Size**: S (~4-5h, revised for multi-stage debug rendering)
**Priority**: Ideas
**Markers**: [WORLDGEN] [PIPELINE] [STAGE-2] [CLIMATE]

**What**: Implement Stage 2 of world generation pipeline: temperature map calculation using latitude + noise + elevation cooling, with **4-stage debug visualization** (latitude-only, +noise, +distance, +mountain-cooling)

**Why**: Temperature map needed for biome classification (Stage 6) and strategic terrain decisions. Multi-stage rendering enables **trivial debugging** of complex 4-component algorithm (mirrors VS_024's Original vs Post-Processed elevation pattern).

**How** (ultra-think 2025-10-08, WorldEngine temperature.py validated):

**Question: Use noise again after elevation post-processing?**
**Answer: YES!** Elevation noise (terrain variation) and temperature noise (climate variation) are **independent physical phenomena**. Two mountain valleys at same elevation can have different temperatures due to microclimates. WorldEngine does this intentionally.

**Four-Component Temperature Algorithm** (WorldEngine proven pattern):

**1. Latitude Factor (92% weight)** - with axial tilt:
```csharp
// Per-world parameters (Gaussian-distributed for variety)
float axialTilt = SampleGaussian(mean: 0.0f, hwhm: 0.07f);  // shift equator
axialTilt = Math.Clamp(axialTilt, -0.5f, 0.5f);

float distanceToSun = SampleGaussian(mean: 1.0f, hwhm: 0.12f);
distanceToSun = Math.Max(0.1f, distanceToSun);
distanceToSun *= distanceToSun;  // inverse-square law

// Per-cell latitude factor
float y_scaled = (float)y / height - 0.5f;  // [-0.5, 0.5]
float latitudeFactor = Interp(y_scaled,
    xp: [axialTilt - 0.5f, axialTilt, axialTilt + 0.5f],
    fp: [0.0f, 1.0f, 0.0f]);  // cold poles, hot equator, cold poles
```

**2. Coherent Noise (8% weight)** - climate variation:
```csharp
int octaves = 8;
float freq = 16.0f * octaves;  // 128.0
float n_scale = 1024f / height;  // For 512×512: 2.0

var noise = new FastNoiseLite(seed);
noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
noise.SetFractalOctaves(octaves);

float n = noise.GetNoise2D((x * n_scale) / freq, (y * n_scale) / freq);
// Range: [-1, 1], contributes 1/13 of final temperature
```

**3. Combined Base Temperature** (normalized [0, 1]):
```csharp
float t = (latitudeFactor * 12f + n * 1f) / 13f / distanceToSun;
// latitudeFactor: 92% weight (latitude banding)
// n: 8% weight (climate variation)
// distanceToSun: global multiplier (hot vs cold planets)
```

**4. Elevation Cooling (mountain-only!)** - RAW elevation with thresholds:
```csharp
float rawElevation = postProcessedHeightmap[y, x];  // Use RAW, not normalized!

if (rawElevation > thresholds.MountainLevel) {
    float altitude_factor;
    if (rawElevation > thresholds.MountainLevel + 29f) {
        altitude_factor = 0.033f;  // extreme peaks (97% cooling)
    } else {
        // Linear cooling from mountain base to +29 units above
        altitude_factor = 1.0f - (rawElevation - thresholds.MountainLevel) / 30f;
    }
    t *= altitude_factor;  // mountains get MUCH colder
}
temperatureMap[y, x] = t;  // Store normalized [0, 1]
```

**5. UI Display Conversion** (Presentation layer - TemperatureMapper utility):
```csharp
public static class TemperatureMapper
{
    private const float MIN_TEMP = -60f;
    private const float MAX_TEMP = 40f;

    public static float ToCelsius(float normalizedTemp) =>
        normalizedTemp * (MAX_TEMP - MIN_TEMP) + MIN_TEMP;  // [0,1] → [-60°C, +40°C]

    public static string FormatTemperature(float normalizedTemp) =>
        $"{ToCelsius(normalizedTemp):F1}°C";  // "Temp: -15.2°C"
}

// Renderer usage: Convert [0,1] to °C for gradient colors
// Probe usage: TemperatureMapper.FormatTemperature(temp)
```

**Key WorldEngine Insights Adopted:**
- ✅ **Axial tilt**: Shifts equator position (more interesting than fixed cosine)
- ✅ **Distance to sun**: Per-world hot/cold variation (inverse-square law)
- ✅ **Latitude interpolation**: More realistic than simple cosine
- ✅ **8% noise weight**: Subtle climate variation (not 50/50)
- ✅ **Mountain-only cooling**: Lowlands unaffected (realistic!)
- ✅ **RAW elevation + thresholds**: Uses actual heightmap values (adaptive per-world)
- ✅ **Normalized output [0,1]**: Consistent internal format, UI converts to °C

**YAGNI Skipped (from WorldEngine):**
- ❌ **Border wrapping**: Seamless east-west complexity, not needed for single-world game
- ❌ **Atmosphere factor**: TODO in WorldEngine, not implemented yet

**Visualization Integration** (add Temperature view):
1. **Renderer** (WorldMapRendererNode.cs):
   - Add `RenderTemperature(float[,] temperatureMap)` method
   - Input: normalized [0, 1] temperature values
   - Convert to °C for gradient: `tempC = t * 100f - 60f`
   - 5-stop color gradient:
     ```
     Blue   (-60°C) → Cyan (-20°C) → Green (0°C) → Yellow (+20°C) → Red (+40°C)
     ```

2. **Legend** (WorldMapLegendNode.cs):
   ```csharp
   case MapViewMode.Temperature:
       AddLegendEntry("Blue", ..., "-60°C (Frozen peaks)");
       AddLegendEntry("Cyan", ..., "-20°C (Cold)");
       AddLegendEntry("Green", ..., "0°C (Mild)");
       AddLegendEntry("Yellow", ..., "+20°C (Warm)");
       AddLegendEntry("Red", ..., "+40°C (Hot)");
   ```

3. **Probe** (WorldMapProbeNode.cs):
   - Display converted temperature: `"Temp: {temp:F1}°C"` (from [0,1] → °C)
   - Show raw normalized value for debugging: `"Normalized: {t:F3}"`

4. **UI** (WorldMapUINode.cs):
   - Add "Temperature" view mode button

**Pipeline Changes** (GenerateWorldPipeline.cs):
```csharp
// Stage 2: Temperature calculation
var temperatureMap = TemperatureCalculator.Calculate(
    postProcessedHeightmap: result.PostProcessedHeightmap!,  // RAW elevation for cooling
    thresholds: result.Thresholds!,                          // MountainLevel threshold
    width: result.Width,
    height: result.Height,
    seed: parameters.Seed);

return result with { TemperatureMap = temperatureMap };
```

**Implementation Notes**:
- Store `axialTilt` and `distanceToSun` in `WorldGenerationResult` (per-world parameters)
- Use RAW `PostProcessedHeightmap` for elevation cooling (not normalized!)
- **Output normalized [0,1]** temperature - WHY? For future biome classification (Stage 6)
  - Biome algorithms use quantile thresholds on [0,1] data (same pattern as elevation)
  - UI converts to °C via TemperatureMapper (same pattern as ElevationMapper)
- `Interp()` utility needed: linear interpolation matching numpy.interp
- `SampleGaussian()` utility: Gaussian distribution with HWHM parameter
- Create `TemperatureMapper` class (analogous to ElevationMapper pattern)

**Performance** (multi-threading decision):
- ❌ **NO threading**: Native sim dominates (83% of 1.2s total), temperature only ~60-80ms
- ✅ Format v2 cache saves full temperature map (0ms reload)
- ✅ Simple = fast enough (<1.5s total for 512×512)

**Implementation Phases** (Dev Engineer 2025-10-08):

**Phase 0: Disable Cache During Development** ✅ COMPLETE (~5min actual)
- Added `DISABLE_CACHE_FOR_DEV = true` flag to WorldMapOrchestratorNode.cs (line 45)
- Wrapped cache load logic in conditional (lines 223-257)
- Wrapped cache save logic in conditional (line 283)
- Used `static readonly` (not `const`) to avoid "unreachable code" compiler warnings
- All 433 tests GREEN, build succeeds

**Phase 1: Core Algorithm with Multi-Stage Output** ✅ COMPLETE (~1h actual, TDD)
1. ✅ Created `MathUtils.cs` with `Interp()` and `SampleGaussian()` (Box-Muller transform)
2. ✅ Created `TemperatureCalculator.cs` with 4-stage output:
   - LatitudeOnlyMap (axial tilt interpolation)
   - WithNoiseMap (92% latitude, 8% noise - WorldEngine ratio)
   - WithDistanceMap (inverse-square law)
   - FinalMap (mountain cooling with RAW elevation thresholds)
3. ✅ 14 comprehensive unit tests (Interp edge cases, Gaussian distribution validation)
4. ✅ All 447 tests GREEN, build succeeds

**Phase 2: Pipeline Integration** ✅ COMPLETE (~0.5h actual)
4. ✅ Updated `WorldGenerationResult` with 4 temperature properties + per-world params
5. ✅ Updated `GenerateWorldPipeline` Stage 2 to call TemperatureCalculator
6. ✅ Fixed backward compat in serialization service (Format v1/v2 still load)
7. ✅ All 447 tests GREEN (no regressions), build succeeds

**Phase 3: Multi-Stage Visualization** ✅ COMPLETE (~1.5h actual)
7. ✅ Added 4 MapViewMode enum values (TemperatureLatitudeOnly, WithNoise, WithDistance, Final)
8. ✅ Implemented RenderTemperatureMap() with 5-stop gradient (Blue → Red via Cyan/Green/Yellow)
9. ✅ Updated WorldMapLegendNode with stage-specific legends (°C labels, debug hints)
10. ✅ Updated WorldMapProbeNode to display all 4 temperature values + AxialTilt/DistanceToSun params
11. ✅ Added 4 UI dropdown items with separator (Temperature Debug section)
12. ✅ All 447 tests GREEN, build succeeds

**Phase 4: Visual Validation** ✅ COMPLETE (~0.75h actual)
12. ✅ Fixed noise configuration bug (missing SetFractalType(FBm) + SetFrequency)
    - Root cause: Elevation pattern had FBm+frequency, temperature was missing both
    - Result: Smooth natural gradients matching WorldEngine (no more discrete bands!)
13. ✅ Validated all 4 temperature stages visually:
    - Latitude Only: Smooth horizontal bands, equator shifts with axial tilt ✅
    - With Noise: Subtle fuzzy climate variation (8% contribution, realistic!) ✅
    - With Distance: Hot/cold planet variation (inverse-square law working) ✅
    - Final: Mountains blue at **all latitudes** (elevation cooling working!) ✅
14. ✅ Performance: <1.5s for 512×512 world generation (no regression)
15. ✅ All 447 tests GREEN

**Done When**:
1. ✅ **4 temperature maps populated** in WorldGenerationResult (latitude-only, +noise, +distance, final)
2. ✅ **Algorithm correct** (WorldEngine-validated, each component isolated for debugging)
3. ✅ **Multi-stage visualization working** (4 view modes, stage-specific legends, probe shows all values)
4. ✅ **Visual validation passes** for each stage independently
5. ✅ **Quality gates**: Per-world variation visible, no performance regression, all tests GREEN

**Depends On**: VS_024 ✅ - needs `PostProcessedHeightmap` (RAW) + `Thresholds.MountainLevel`

**Tech Lead Decision** (2025-10-08 09:30 - Updated after WorldEngine analysis):
- **Algorithm**: 4 components (latitude+tilt, noise, distance-to-sun, mountain-cooling). Matches WorldEngine temperature.py exactly.
- **Noise YES**: Independent from elevation noise (climate vs terrain). 8% weight per WorldEngine.
- **RAW elevation**: Use `PostProcessedHeightmap` (raw [0.1-20]) with `MountainLevel` threshold, NOT normalized.
- **Per-world parameters**: `axialTilt` and `distanceToSun` create planet variety (hot/cold, shifted equator).
- **Mountain-only cooling**: Realistic - lowlands unaffected by altitude, peaks extremely cold.
- **Normalized output**: Store [0,1], UI converts to °C. Consistent with WorldEngine pattern.
- **Performance**: Skip threading (YAGNI), cache + simple algorithm = fast enough.
- **Next steps**: Dev Engineer implements after VS_024 merged, use WorldEngine temperature.py as reference.

**Dev Engineer Decision** (2025-10-08 14:52 - Multi-stage debug rendering):
- **Pattern**: Mirror VS_024's Original vs Post-Processed elevation visualization (proven debugging approach)
- **4 view modes**: LatitudeOnly → +Noise → +Distance → Final (isolates each component for visual validation)
- **Why**: Complex 4-component algorithm needs per-stage debugging to catch bugs immediately (not guess!)
- **Trade-off**: Store 4 maps instead of 1 (~2MB extra for 512×512, negligible), but debugging becomes **trivial**
- **Implementation**: TemperatureCalculator returns all 4 intermediate stages + per-world params
- **Validation**: Each stage has **visual signature** (bands → fuzz → hot/cold → blue mountains)
- **Revised estimate**: ~4-5h (was 3-4h), extra hour for multi-stage rendering infrastructure

---

**Extraction Targets**:
- [ ] ADR needed for: 4-component temperature algorithm architecture (latitude+tilt+noise+distance+cooling, WorldEngine validation)
- [ ] ADR needed for: Multi-stage debug visualization pattern (isolate algorithm components for visual validation)
- [ ] ADR needed for: Per-world climate parameter system (AxialTilt/DistanceToSun Gaussian distribution creates variety)
- [ ] HANDBOOK update: WorldEngine algorithm adaptation (temperature.py → C# TemperatureCalculator, 4-component pattern)
- [ ] HANDBOOK update: Noise configuration pattern (FBm fractal type + frequency scaling for smooth gradients)
- [ ] HANDBOOK update: Visual debugging strategy (stage-specific legends, probe shows all intermediate values)
- [ ] Reference implementation: TemperatureCalculator as template for multi-component WorldEngine algorithms
- [ ] Reference implementation: MathUtils (Interp, SampleGaussian) for WorldEngine math utilities
- [ ] Reference implementation: TemperatureMapper as Presentation utility (normalized → °C conversion)
- [ ] Testing pattern: Multi-stage algorithm validation (unit tests per stage + visual validation)
- [ ] Bug pattern: Missing noise configuration (SetFractalType + SetFrequency) causes discrete bands
- [ ] Performance insight: Multi-stage storage trade-off (~2MB extra for trivial debugging vs complex investigation)

---
