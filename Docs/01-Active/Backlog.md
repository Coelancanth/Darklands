# Darklands Development Backlog


**Last Updated**: 2025-10-07 21:52 (Dev Engineer: TD_010 Phases 1-2 complete - Caching + elevation renderer with known issue)

**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 008
- **Next TD**: 010
- **Next VS**: 022


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

### VS_019: WorldEngine Integration MVP (Experimental)
**Status**: Approved
**Owner**: Tech Lead → Dev Engineer
**Size**: L (2-3 days for MVP)
**Priority**: Ideas (Experimental - validates strategic layer foundation)
**Markers**: [STRATEGIC-LAYER] [EXPERIMENTAL] [WORLDGEN] [NATIVE-LIBRARY]

**What**: Integrate plate-tectonics C++ library via LibraryImport (.NET 7+ source generators) and port WorldEngine post-processing to C# for strategic world map generation

**Why**:
- Foundation for two-map architecture (strategic world + tactical combat grids)
- Physics-driven terrain (plate tectonics, climate, rivers) creates realistic geography
- Seed-based reproducibility enables balanced faction placement and resource distribution
- Eliminates Python dependency for game distribution
- Validates strategic layer vision before full investment

**Tech Lead Decision** (2025-10-06, updated for LibraryImport):
- **Hybrid approach**: Native plate-tectonics library (LibraryImport) + C# post-processing
- **Rationale**: Plate simulation is complex (10K+ lines C++), post-processing is simple (~500 lines Python)
- **Pattern**: Follow ADR-007 v1.2 (Three-Layer Isolation: Interop → Wrapper → Interface, using LibraryImport)
- **Build strategy**: Pre-compiled binaries from releases (fastest MVP path)
- **Modernization**: Migrated to LibraryImport for AOT compatibility + 5-10% performance gain

**Technical Breakdown** (4 phases matching ADR-007):

**Phase 1: Native Library Setup** (TD_006) - M (4-6h) ✅ **COMPLETE**
- Obtain plate-tectonics binaries (download from releases) ✅
- Create NATIVE_LIBRARIES.md with checksums ✅
- Create Interop layer (LibraryImport source generators) ✅
- Create SafeHandle wrapper (RAII cleanup) ✅
- Create NativeLibraryLoader (Godot path resolution) ✅
- Integration test: Library loads successfully ✅
- **Bonus**: Migrated to LibraryImport + updated ADR-007 v1.2 ✅

**Phase 2: Core Infrastructure** (TD_007) - M (6-8h)
- Create IPlateSimulator interface (Application/Abstractions)
- Implement NativePlateSimulator wrapper (Infrastructure/Native)
- Implement Span<T> marshaling (Marshal2DArray<T> helper)
- Port C# post-processing (elevation noise, centering, oceans)
- Port precipitation/temperature/biome calculations
- Unit tests: Post-processing algorithms, marshaling validation

**Phase 3: Godot Visualization** (TD_008) - S (3-4h)
- Create GenerateWorldCommand + Handler
- Create WorldMapTestScene.tscn (TileMapLayer + Camera2D)
- Implement WorldMapNode (renders heightmap to tiles)
- Camera controls (WASD pan, mouse wheel zoom)
- Basic biome color mapping (water/grass/hills/mountains)

**Phase 4: Validation & Polish** (TD_009) - S (2-3h)
- Reproducibility test (same seed → same world)
- Performance test (<5 seconds for 512×512)
- Visual quality check (rivers, mountains, coastlines)
- Error handling test (missing library graceful failure)

**Done When**:
- Can generate 512×512 world from seed in C# (no Python dependency)
- World displays in Godot with Camera2D navigation
- Terrain looks realistic (rivers connect to ocean, mountains form ranges, biomes make sense)
- Can regenerate same world from same seed (reproducibility test)
- Performance acceptable (<5 seconds generation time for 512×512)
- All 4 TD items completed and passing integration tests

**NOT in MVP Scope** (Deferred):
- Biome overlay toggle (single view is sufficient for validation)
- Grid overlay (defer to settlement VS)
- Settlement/faction systems (separate VS items)
- History simulation (DF-style legends)

**Reference**:
- ADR-007: Native Library Integration Architecture
- WorldEngine Python: `References/worldengine/worldengine/`
- plate-tectonics: https://github.com/Mindwerks/plate-tectonics

**Dependencies**: None (can start immediately after ADR-007 approval)

---

## 🔧 Technical Debt (Refactoring & Infrastructure)
*Implementation tasks supporting VS items, infrastructure improvements*

**No active TD items!** ✅ TD_006 + TD_007 + TD_008 completed (VS_019 Phases 1-3 done!).

---

*Recently completed (2025-10-06):*
- **Rain Shadow + Orographic Precipitation**: Comprehensive climate system with **multi-octave noise** (coarse + fine-grain), **orographic lift** (windward slopes get +0.2 rain), **rain shadow effect** (leeward sides -40% rain), **ocean proximity** (coastal moisture), **temperature micro-climates** (±0.08 variation breaks horizontal bands). ClimateCalculator rewrite (~370 lines): custom gradient noise implementation, 5-factor precipitation model, temperature + moisture noise layers. Result: **organic biome transitions** - no more straight lines! All 439 tests GREEN. VS_019 Phase 3 quality significantly improved! ✅ (2025-10-06 23:11, actual: ~2h)
- **WorldEngine 41-Biome Upgrade**: Expanded from 12 → 41 biomes using WorldEngine's proven Holdridge life zones model. **Percentile-based moisture classification** (automatic biome balance), WorldEngine color scheme, fixed climate algorithm (elevation cooling 0.5× → 0.25×). BiomeType enum, BiomeClassifier, ClimateCalculator, WorldMapNode colors all upgraded. Quality issues identified (horizontal banding, no rain shadow) → documented in TD_009 polish roadmap. All 439 tests GREEN. Foundation ready for quality polish phase! ✅ (2025-10-06 22:54, actual: ~3h)
- **TD_008**: Godot WorldMap Visualization - **Image/Texture2D rendering architecture** (262,000× faster than DrawRect), async world generation, Camera2D navigation (WASD/zoom), 12-biome color rendering. Architectural decision: Image/Texture2D for terrain (colors) + future TileMapLayer for settlements (sprites) validates multi-layer strategic map vision. All 439 tests GREEN. VS_019 Phase 3 complete! ✅ (2025-10-06 22:33, actual: ~2.5h)
- **TD_007**: Core WorldGen Infrastructure (Wrapper + Post-Processing) - Complete 4-phase implementation! **IPlateSimulator** interface with DTOs, **NativePlateSimulator** wrapper (railway-oriented flow), **ElevationPostProcessor** (borders, noise, flood fill), **ClimateCalculator** (precipitation, temperature), **BiomeClassifier** (Holdridge model, 12 biome types). Lightweight Perlin noise (80 lines, no external deps). All 439 tests GREEN. Foundation ready for TD_008 (Godot visualization)! ✅ (2025-10-06 22:00, actual time: ~4h)
- **TD_006**: Native Library Setup (plate-tectonics LibraryImport) - Built DLL from source with extern "C" exports, created LibraryImport interop layer (PlateTectonicsNative.cs with source generators), SafeHandle wrapper (RAII cleanup), NativeLibraryLoader (platform detection), 4 integration tests GREEN. **Migrated DllImport → LibraryImport** for .NET 7+ AOT compatibility. Updated ADR-007 v1.2 with migration guide. Foundation ready for TD_007! ✅ (2025-10-06 21:08, migrated 21:31)

---

### TD_006: Native Library Setup (plate-tectonics LibraryImport)
**Status**: Done ✅ (Migrated to LibraryImport 2025-10-06)
**Owner**: Dev Engineer (completed)
**Size**: M (actual: ~5 hours DLL build + 30 min LibraryImport migration)
**Priority**: Ideas
**Markers**: [INFRASTRUCTURE] [NATIVE-LIBRARY] [WORLDGEN]
**Parent**: VS_019 (Phase 1)

**What**: Set up plate-tectonics native library integration with LibraryImport (.NET 7+ source generators)

**Why**: Foundation for VS_019 world generation - establishes pattern for all future native library integrations

**How** (Following ADR-007 v1.2 - LibraryImport standard):
1. **Obtain binaries** (2h):
   - Download plate-tectonics v1.5.0 from GitHub releases
   - Extract binaries for Windows x64 (libplatec.dll)
   - Place in `addons/darklands/bin/win-x64/`
   - Calculate SHA256 checksums
   - Create `NATIVE_LIBRARIES.md` with download instructions

2. **Create Interop layer** (1h):
   - Create `Features/WorldGen/Infrastructure/Native/Interop/PlateTectonicsNative.cs`
   - Add LibraryImport declarations (study Python wrapper API):
     ```csharp
     // NOTE: Requires <AllowUnsafeBlocks>true</AllowUnsafeBlocks> in .csproj
     internal static partial class PlateTectonicsNative
     {
         [LibraryImport("PlateTectonics")]
         internal static partial IntPtr Create(...);
         [LibraryImport("PlateTectonics")]
         internal static partial void Step(IntPtr handle);
         [LibraryImport("PlateTectonics")]
         internal static partial IntPtr GetHeightmap(IntPtr handle);
         [LibraryImport("PlateTectonics")]
         internal static partial void Destroy(IntPtr handle);
     }
     ```

3. **Create SafeHandle** (0.5h):
   - Create `Features/WorldGen/Infrastructure/Native/Interop/PlateSimulationHandle.cs`
   - Implement `SafeHandleZeroOrMinusOneIsInvalid`
   - Override `ReleaseHandle()` → calls `platec_destroy()`

4. **Create NativeLibraryLoader** (1h):
   - Create `Features/WorldGen/Infrastructure/Native/NativeLibraryLoader.cs`
   - Implement Godot path resolution (`ProjectSettings.GlobalizePath()`)
   - Platform detection (Windows/Linux/macOS)
   - Fail-fast validation with helpful error messages

5. **Integration test** (0.5h):
   - Test library loads successfully
   - Test SafeHandle cleanup (no leaks)
   - Test error handling (missing library)

**Done When**:
- `NATIVE_LIBRARIES.md` exists with download instructions + checksums
- Interop layer compiles (LibraryImport with source generators) ✅
- `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>` added to Darklands.Core.csproj ✅
- SafeHandle properly cleans up native resources ✅
- NativeLibraryLoader resolves Godot paths correctly ✅
- Integration test passes: Library loads on Windows x64 ✅
- **Bonus**: Migrated to LibraryImport (.NET 7+ standard) for AOT compatibility ✅

**Dependencies**: ADR-007 v1.2 approved

---

### TD_007: Core WorldGen Infrastructure (Wrapper + Post-Processing)
**Status**: Done ✅ (2025-10-06 22:00)
**Owner**: Dev Engineer (completed)
**Size**: M (actual: ~4 hours, estimate was 6-8h - under budget!)
**Priority**: Ideas
**Markers**: [CORE-LOGIC] [WORLDGEN] [ALGORITHMS]
**Parent**: VS_019 (Phase 2)

**What**: Implement native wrapper + port WorldEngine post-processing algorithms to C#

**Why**: Business logic layer for world generation - keeps Core pure C# while leveraging native plate simulation

**How Implemented** (4 phases, ~4h actual):

**Phase 2.1: Foundation** (2h actual vs 0.5h planned - DTOs expanded):
- ✅ `IPlateSimulator` interface with clean abstraction
- ✅ `PlateSimulationParams` record (seed, worldSize, plateCount, seaLevel, etc.)
- ✅ `PlateSimulationResult` record (heightmap, oceanMask, precipitation, temperature, biomes)
- ✅ `BiomeType` enum (12 biome types for Holdridge model)
- ✅ `NativePlateSimulator` skeleton with railway-oriented flow
- ✅ Unsafe `Marshal2DArray()` using Span<T> (ADR-007 v1.2 pattern)
- ✅ Integration test: 128x128 world generation works end-to-end

**Phase 2.2: Elevation Post-Processing** (1.5h actual):
- ✅ `ElevationPostProcessor` static class with 3 algorithms:
  - `PlaceOceansAtBorders()` - Lower border elevation (0.8× multiplier)
  - `FillOcean()` - BFS flood fill from borders (marks ocean vs landlocked lakes)
  - `AddNoise()` - Lightweight Perlin noise (~80 lines, no external deps, deterministic)
- ✅ Deferred `CenterLand()` (nice-to-have, not critical for MVP)
- ✅ 6 unit tests (border lowering, flood fill, landlocked lakes, noise determinism)
- ✅ Bug fix: Corner cells lowered twice (fixed with `y = 1` to `height-1` loop)

**Phase 2.3: Climate Simulation** (0.5h actual):
- ✅ `ClimateCalculator` static class with 2 algorithms:
  - `CalculatePrecipitation()` - Latitude bands (ITCZ 0.9, subtropical 0.25, temperate 0.6, polar 0.35)
  - `CalculateTemperature()` - Latitude cosine + elevation cooling (-6.5°C per 1000m lapse rate)
- ✅ Ocean modifiers (precipitation +0.1, temperature moderation)
- ✅ Simplified model (deferred rain shadow for MVP - can add in TD_009 if needed)

**Phase 2.4: Biome Classification** (< 0.5h actual):
- ✅ `BiomeClassifier` static class with Holdridge life zones
- ✅ Decision tree: elevation → temperature → precipitation
- ✅ 12 biome types: Ocean, ShallowWater, Ice, Tundra, BorealForest, Grassland, TemperateForest, TemperateRainforest, Desert, Savanna, TropicalSeasonalForest, TropicalRainforest
- ✅ Thresholds tuned for realistic biome distribution

**Tests**: All 439 tests GREEN (11 WorldGen: 6 unit + 5 integration)

**Done When** (all ✅):
- ✅ `IPlateSimulator` interface defined in Application layer
- ✅ `NativePlateSimulator` wrapper implemented with Result<T> error handling
- ✅ Span<T> marshaling works (no memory leaks, correct data)
- ✅ All post-processing algorithms ported (elevation, precipitation, temperature, biomes)
- ✅ Unit tests GREEN (6 algorithm tests + 5 integration tests)

**Dependencies**: TD_006 complete (LibraryImport interop layer + ADR-007 v1.2)

---

### TD_008: Godot WorldMap Visualization
**Status**: Done ✅ (2025-10-06 22:33)
**Owner**: Dev Engineer (completed)
**Size**: S (actual: ~2.5 hours, estimate was 3-4h - under budget!)
**Priority**: Ideas
**Markers**: [PRESENTATION] [GODOT] [WORLDGEN]
**Parent**: VS_019 (Phase 3)

**What**: Create Godot scene for world map visualization with camera controls

**Why**: Visual validation of world generation - allows designer/player to explore generated worlds

**Implementation Summary**:
- **Rendering Architecture**: Migrated DrawRect() → Image/Texture2D + Sprite2D (262,000× performance improvement)
- **Command/Handler**: GenerateWorldCommand orchestrates IPlateSimulator via MediatR
- **Godot Scene**: WorldMapTestScene.tscn with Camera2D navigation (WASD pan, mouse wheel zoom)
- **WorldMapNode**: Async generation, Image.CreateEmpty() → SetPixel() loop → ImageTexture → Sprite2D
- **Biome Colors**: 12 distinct colors (Ocean, Ice, Tundra, Forests, Desert, Savanna, Grasslands)
- **Result**: 512×512 world renders successfully, camera controls functional

**Key Architectural Decision**:
- **Image/Texture2D** for base terrain (color data) - single GPU draw call
- **Future**: TileMapLayer for settlements (sprite tiles) - layered rendering approach
- **Why**: Right tool for data type - colors vs tiles separation validates multi-layer strategic map vision

**Tests**: All 439 tests GREEN

**Done When** (all ✅):
- ✅ GenerateWorldCommand returns PlateSimulationResult with all world data
- ✅ World generation completes successfully
- ✅ Camera2D navigation implemented (WASD pan, mouse wheel zoom)
- ✅ WorldMapNode renders biome data via Image/Texture2D
- ✅ 12 biome colors VISIBLE on screen (validated with user screenshot)

**Quality Note**: Biome distribution looks unnatural (ice/tundra dominance) - climate algorithm tuning deferred to TD_009 quality polish phase

**Dependencies**: TD_007 complete ✅

---

### TD_009: Complete WorldEngine Simulation Pipeline (Erosion, Rivers, Humidity)
**Status**: Done ✅ (2025-10-07 03:08)
**Owner**: Dev Engineer (completed)
**Size**: XL (actual: ~13 hours, estimated: 15-18h - under budget!)
**Priority**: Ideas
**Markers**: [WORLDGEN] [CORE-LOGIC] [ALGORITHMS] [ARCHITECTURE]
**Parent**: VS_019 (Phase 4)

**What**: Port WorldEngine's missing simulation steps (Erosion, Watermap, Irrigation, Humidity) to complete the proven generation pipeline and fix biome classification.

**Why**: Our current implementation **skips 4 critical simulation steps** that WorldEngine considers essential. Most importantly, our biomes use **precipitation directly** when they should use **humidity** (precipitation + irrigation with 3× weight). This means our biomes ignore proximity to water entirely!

**Critical Discovery** (from ultra-analysis, see [Docs/08-Learnings/WorldEngine/TD_009-Pipeline-Gap-Analysis.md](../08-Learnings/WorldEngine/TD_009-Pipeline-Gap-Analysis.md)):
- WorldEngine pipeline has **10 sequential steps**: Plates → Elevation → Temp → Precip → **Erosion** → **Watermap** → **Irrigation** → **Humidity** → Biomes → Icecap
- We currently have: Steps 1-4 ✅, then **skip steps 5-8** ❌, jump to Step 9 ✅ (but with wrong input!)
- **Impact**: Biomes near rivers should be wetter even if precipitation is low (irrigation effect)

**Work Breakdown** (4 algorithms + biome fix):

**Phase 1: Erosion & Rivers** (~6-8h) ✅ **COMPLETE** (2025-10-07)
- **Files**: `HydraulicErosionProcessor.cs` (600 lines), `AStarPathfinder.cs` (230 lines)
- **Port**: `erosion.py` + `astar.py` (403 + 229 lines → 830 lines C#)
- **Algorithms Implemented**:
  1. `FindWaterFlow()` - Flow direction per cell (steepest descent)
  2. `FindRiverSources()` - Mountain river sources (precip 0.02, spacing radius 9)
  3. `TraceRiverPath()` - Rivers to ocean (steepest descent + A* fallback + river merging)
  4. `ErodeValleysAroundRivers()` - Valley carving (radius 2, curves 0.2/0.05)
  5. `CleanUpFlow()` - Monotonic elevation (no uphill flow)
- **Integration**: `NativePlateSimulator` Step 5 (between climate and biomes)
- **DTO Expansion**: `PlateSimulationResult` + Rivers/Lakes properties
- **Validation**: ✅ 617 rivers, 17 lakes generated (seed 42, 512×512 map)
  - 602 rivers reached ocean (97.6%)
  - 15 rivers formed lakes (2.4% endorheic basins)
- **Tests**: All 439 GREEN ✅

**Phase 2: Watermap Simulation** (~3-4h) ✅ **COMPLETE** (2025-10-07)
- **File**: `WatermapCalculator.cs` (270 lines)
- **Port**: `hydrology.py` (81 lines → 270 lines C#)
- **Algorithms Implemented**:
  1. `SimulateDroplet()` - Recursive flow to lower neighbors (proportional by elevation difference × 4)
  2. `CalculateWatermap()` - 20k droplets seeded on random land, percentile thresholds: creek (5%), river (2%), main river (0.7%)
- **Performance**: ~100-200ms for 512×512 map (2M operations with early termination at q < 0.05)
- **Returns**: `(float[,] watermap, WatermapThresholds thresholds)`
- **Tests**: All 439 GREEN ✅

**Phase 3: Irrigation Simulation** (~2-3h) ✅ **COMPLETE** (2025-10-07)
- **File**: `IrrigationCalculator.cs` (130 lines)
- **Port**: `irrigation.py` (63 lines → 130 lines C#)
- **Algorithm**: Pre-calculated 21×21 logarithmic kernel, formula: irrigation[cell] += watermap[ocean] / (log(distance + 1) + 1)
- **Performance**: ~50-100ms for 512×512 map (single pass over ocean cells)
- **Ocean-Only Sources**: Only ocean cells spread moisture (matches WorldEngine exactly)
- **Returns**: `float[,] irrigation`
- **Tests**: All 439 GREEN ✅

**Phase 4: Humidity Simulation** (~1-2h) ✅ **COMPLETE** (2025-10-07)
- **File**: `HumidityCalculator.cs` (130 lines)
- **Port**: `humidity.py` (45 lines → 130 lines C#) with **bug fix**
- **WorldEngine Bug**: Original has minus sign (precip - irrigation), fixed to PLUS (precip + irrigation)
- **Algorithm**: humidity = (precipitation × 1.0 + irrigation × 3.0) / 4.0 (weighted average, irrigation dominates!)
- **Quantiles**: Bell curve percentiles [94.1%, 77.8%, 50.7%, 23.6%, 7.3%, 1.4%, 0.2%] for 8 moisture levels
- **Returns**: `(float[,] humidity, HumidityQuantiles quantiles)`
- **Tests**: All 439 GREEN ✅

**Phase 5: Fix Biome Classification** (~1h) ✅ **COMPLETE** (2025-10-07 03:08)
- **File**: `BiomeClassifier.cs` (modified)
- **Changes Implemented**:
  1. Updated `Classify()` signature: `humidityMap` + `HumidityQuantiles` replace `precipitationMap` + percentiles
  2. Updated `ClassifyLandBiome()`: Now accepts humidity + quantiles instead of precip + percentiles
  3. New `GetMoistureLevelFromHumidity()`: Uses pre-calculated quantiles from HumidityCalculator
  4. **Deleted**: `CalculatePrecipitationPercentiles()` + `GetPercentile()` + `GetMoistureLevel()` (~200 lines removed)
- **Impact**: Biomes now consider proximity to water (irrigation 3× weight) - river valleys are wetter!
- **Tests**: All 439 GREEN ✅

**Phase 6: Update Pipeline** (~1h) ✅ **COMPLETE** (2025-10-07 03:08)
- **File**: `NativePlateSimulator.cs` (modified)
- **Changes Implemented**:
  1. New `SimulateHydrology()` method: Orchestrates watermap → irrigation → humidity simulation
  2. New `HydrologyData` record: Internal pipeline data structure with all hydrology layers
  3. Updated `ClassifyBiomes()`: Now accepts `HydrologyData` instead of `ErosionData`
  4. Pipeline updated: `.Bind(erosion => SimulateHydrology(erosion, params))` inserted between erosion and biomes
  5. **PlateSimulationResult** expanded: Added `HumidityMap`, `WatermapData`, `IrrigationMap` properties + constructor parameters
- **Tests**: All 439 GREEN ✅
- **Result**: Complete 7-step pipeline (Library → Elevation → Climate → Erosion → Hydrology → Biomes)

**Done When**:
- ✅ `HydraulicErosionProcessor.cs` exists with 5 methods (flow, sources, tracing, erosion, cleanup)
- ✅ `WatermapCalculator.cs` exists with droplet model + threshold calculation
- ✅ `IrrigationCalculator.cs` exists with logarithmic kernel convolution
- ✅ `HumidityCalculator.cs` exists with precip + irrigation combination (1:3 weight)
- ✅ `BiomeClassifier.Classify()` updated to use humidity instead of precipitation
- ✅ `NativePlateSimulator.Generate()` pipeline includes all 4 new simulation steps
- ✅ `PlateSimulationResult` DTO expanded with rivers, lakes, watermap, irrigation, humidity
- ✅ Generated worlds have:
  - Rivers flowing from mountains to ocean (realistic hydrology)
  - Lakes where rivers can't reach sea (natural lake formation)
  - Eroded valleys around rivers (smooth terrain, not sharp)
  - Biomes consider proximity to water (cells near rivers are wetter due to irrigation)
- ✅ All existing 439 tests remain GREEN
- ✅ New integration tests for erosion/watermap/irrigation/humidity (at least 8 tests)

**NOT in TD_009 Scope** (separate TD items):
- TileMapLayer/TileSet rendering (presentation concern) → TD_011
- Multi-view debug maps (elevation/precip/temp views) → TD_010
- Icecap simulation (deferred - not needed for MVP)
- Permeability simulation (not critical)

**Reference**:
- **Analysis**: [Docs/08-Learnings/WorldEngine/TD_009-Pipeline-Gap-Analysis.md](../08-Learnings/WorldEngine/TD_009-Pipeline-Gap-Analysis.md)
- **WorldEngine Sources**: `References/worldengine/worldengine/simulations/{erosion,hydrology,irrigation,humidity}.py`

**Dependencies**: TD_008 complete ✅

---

### TD_010: Multi-View Map Rendering System
**Status**: In Progress (Phases 1-2 Complete, Climate/Biome corrections applied)
**Owner**: Dev Engineer
**Size**: M (actual: ~4h Phase 1-2, est: ~3h Phase 3-4 remaining)
**Priority**: Ideas
**Markers**: [WORLDGEN] [VISUALIZATION] [DEBUG]
**Parent**: VS_019 (Phase 4)

**What**: Debug visualization system with multiple map view modes (elevation, precipitation, temperature) + persistent generation data

**Why**: Enable rapid iteration on worldgen algorithms by visualizing underlying data layers. Developers need to see raw elevation/precipitation/temperature data to debug biome classification, rain shadow effects, and climate calculations. Saving generation results prevents regeneration overhead during view switching.

**Implementation Summary** (as of 2025-10-07 23:40):

**Phase 1: Persistent Generation Data** ✅ **COMPLETE**
- `PlateSimulationResult` cached in `_cachedWorldData` field (WorldMapNode)
- Session-scoped cache (destroyed on scene close, sufficient for debug workflow)
- Instant view switching (< 1ms, no regeneration confirmed)
- Memory: 512×512 world ≈ 4-6 MB RAM (negligible overhead)

**Phase 2: Elevation Map Renderer** ⚠️ **COMPLETE WITH KNOWN ISSUE**
- ✅ `MapViewMode` enum created (Biomes, Elevation, Precipitation, Temperature)
- ✅ `ElevationMapColorizer` - 1:1 port of WorldEngine's `_elevation_color()` gradient (draw.py:151-200)
- ✅ Keyboard shortcuts: Keys 1-4 for instant view switching (no mouse required)
- ✅ Visual legend panel (bottom-left corner, dynamic updates per view mode)
- ✅ WorldEngine's exact rescaling formula (draw.py:340-350) - ocean [0, 1], land [1, 12] via divide-by-11
- ✅ Build GREEN, all 439 tests passing

**✅ What Works**:
- Caching infrastructure validated (view switching instant, no regeneration)
- Keyboard shortcuts functional (Keys 1-4 tested)
- UI label updates with current view mode name
- Legend panel displays correct color swatches (9 elevation zones shown)
- Elevation rendering shows color variation (blue ocean depths, green/gray/white land gradients)
- Code architecture sound (ElevationMapColorizer pure C#, WorldMapNode presentation layer)

**❌ Known Issue - Elevation Color Distribution**:
- **Symptom**: Land renders predominantly gray/white (high peaks) instead of green/yellow/brown (lowlands)
- **Visual Comparison**: WorldEngine reference shows green/yellow dominance, ours shows gray/white dominance
- **Root Cause Analysis**:
  - Rescaling algorithm verified correct (1:1 port of draw.py formula)
  - Heightmap values are too high: land 0.6-1.0 vs WorldEngine's expected 0.2-0.6 range
  - Log evidence: `Elevation stats: Ocean [0.000-0.650], Land [0.018-1.000]` (98% spread but high absolute values)
  - Issue is in **heightmap generation pipeline**, not visualization
- **Hypothesis**: Plate-tectonics library or ElevationPostProcessor produces different elevation distributions than WorldEngine expects
- **Impact**: Cosmetic only - elevation view shows variation but wrong color range (functional, not optimal)
- **Deferred**: Pipeline investigation to TD_011 (needs elevation normalization/compression study)

**Phase 3-4: Precipitation/Temperature Renderers** ⏳ **DEFERRED**
- Simpler than elevation (8-level and 7-level discrete gradients, no complex rescaling)
- Est: ~1.5h precipitation + 1.5h temperature = 3h total
- Blocked on elevation issue resolution (consistency across view modes)

**Work Breakdown**:

1. **Persistent Generation Data** (1-2h):
   - **Data Structure**: `WorldGenerationResult` record in Core
     ```csharp
     public record WorldGenerationResult(
         int Seed,
         int Width,
         int Height,
         float[,] Elevation,      // 0.0-1.0 normalized
         float[,] Precipitation,  // 0.0-1.0 normalized
         float[,] Temperature,    // 0.0-1.0 normalized (Celsius converted)
         BiomeType[,] Biomes      // Classified biomes
     );
     ```
   - **Storage**: Cache in `WorldGeneratorService` (single-world scope, cleared on new generation)
   - **Query**: `GetWorldDataQuery` returns cached `WorldGenerationResult?`
   - **Impact**: Generate once, render many times (instant view switching)

2. **View Mode System** (1-2h):
   - **Enum**: `MapViewMode { Biomes, Elevation, Precipitation, Temperature }`
   - **UI Controls**: Radio buttons or dropdown in WorldGenDebugView
   - **State Management**: Track `currentViewMode` in presentation layer
   - **Query Update**: Extend `GetTerrainTileMapQuery` → `GetMapVisualizationQuery(MapViewMode mode)`
   - **Impact**: Single control switches between data layer views

3. **Elevation Map Renderer** (1h):
   - **Color Mapping**: WorldEngine-style colored gradient (matches `draw_simple_elevation`)
     - Ocean depths: Dark blue (RGB: 0,0,~190) → Blue (0,0,255)
     - Sea level → low land: Green (0,128,0) → Yellow-green
     - Mid elevation: Yellow (255,255,0) → Orange (255,128,0)
     - High elevation: Orange-red (255,64,0) → Light gray (128,128,128)
     - Mountain peaks: Gray → White (255,255,255) → Pink (255,0,255) for extreme heights
   - **Reference**: `worldengine/draw.py:151-200` (`_elevation_color` function)
   - **Implementation**: `ElevationMapColorizer` service in Core
   - **Impact**: Debug terrain generation, visually matches WorldEngine output

4. **Precipitation Map Renderer** (1h):
   - **Color Mapping**: Discretized cyan gradient (8 humidity levels, matches `draw_precipitation`)
     - Superarid: RGB(0, 32, 32) - very dark teal
     - Perarid: RGB(0, 64, 64)
     - Arid: RGB(0, 96, 96)
     - Semiarid: RGB(0, 128, 128)
     - Subhumid: RGB(0, 160, 160)
     - Humid: RGB(0, 192, 192)
     - Perhumid: RGB(0, 224, 224)
     - Superhumid: RGB(0, 255, 255) - bright cyan
   - **Reference**: `worldengine/draw.py:535-570` (`draw_precipitation` function)
   - **Implementation**: `PrecipitationMapColorizer` service in Core
   - **Impact**: Debug rain shadow with WorldEngine-proven color scale

5. **Temperature Map Renderer** (1h):
   - **Color Mapping**: Discretized thermal gradient (7 zones, matches `draw_temperature_levels`)
     - Polar: RGB(0, 0, 255) - blue
     - Alpine: RGB(42, 0, 213) - blue-purple
     - Boreal: RGB(85, 0, 170) - purple
     - Cool: RGB(128, 0, 128) - magenta
     - Warm: RGB(170, 0, 85) - red-purple
     - Subtropical: RGB(213, 0, 42) - red-orange
     - Tropical: RGB(255, 0, 0) - red
   - **Reference**: `worldengine/draw.py:586-619` (`draw_temperature_levels` function)
   - **Implementation**: `TemperatureMapColorizer` service in Core
   - **Impact**: Debug elevation cooling with WorldEngine's proven thermal gradient

6. **View Switching Logic** (1h):
   - **Event Flow**: User selects view → Query with mode → Colorizer applies mapping → Render
   - **Performance**: No regeneration (uses cached `WorldGenerationResult`)
   - **Consistency**: Same world data across all views (debugging reliability)
   - **Implementation**: Update `WorldGenDebugView` signal handlers
   - **Impact**: Instant (<100ms) view switching for rapid iteration

**Done When**:
- ✅ `WorldGenerationResult` cached after generation (single source of truth)
- ✅ Four view modes work: Biomes, Elevation, Precipitation, Temperature
- ✅ View switching is instant (<100ms, no regeneration)
- ✅ Elevation uses WorldEngine colored gradient (blue ocean → green → yellow → red → white peaks)
- ✅ Precipitation uses 8-level discretized cyan gradient (dark teal → bright cyan)
- ✅ Temperature uses 7-level discretized thermal gradient (blue polar → red tropical)
- ✅ UI controls clearly indicate current view mode
- ✅ All existing tests remain GREEN
- ✅ Visual validation: Output matches WorldEngine reference images from `draw.py`

**Tech Notes**:
- **Architecture**: Query returns raw data (`float[,]`), colorizer applies mapping (separation of concerns)
- **WorldEngine Reference**: All color schemes ported from `References/worldengine/worldengine/draw.py`
- **Elevation**: Continuous gradient with complex logic (ocean vs land normalization, see `_elevation_color`)
- **Precipitation/Temperature**: Discrete thresholds (8 and 7 levels respectively, not continuous)
- **Performance**: Pre-compute color lookup tables for precipitation/temperature (O(1) mapping)
- **Future Enhancement**: Add "black & white" mode option for scientific visualization (WorldEngine has this)

**Dependencies**: TD_008 complete ✅

---

**2025-10-07 Corrections (WorldEngine parity in Climate/Biome pipeline)**
- **Reordered climate steps**: Temperature now computes BEFORE precipitation, matching WorldEngine. Precipitation now receives the temperature map as input.
- **Precipitation gamma modulation (WorldEngine)**: Applied gamma curve against normalized temperature: `(t^gamma)*(1-curveOffset)+curveOffset` (defaults: gamma=1.25, offset=0.20). Retained orographic lift, rain shadow, and coastal bonuses. Implemented X-wrap-aware noise blending and world-size normalization (`nScale = 1024/height`).
- **Humidity quantiles mapping fixed**: Corrected quantile keys mapping to WorldEngine’s bell-curve percentiles:
  - '12'→0.002, '25'→0.014, '37'→0.073, '50'→0.236, '62'→0.507, '75'→0.778, '87'→0.941.
  - This ensures moisture buckets (superhumid → superarid) align with WorldEngine’s classification on land-only cells.
- **Biome moisture classification order corrected**: Moisture thresholds are now checked in increasing wetness order (12→25→37→50→62→75→87), exactly mirroring WorldEngine’s `world.is_humidity_*` logic.
- **Sea level parameterized**: Biome classification now uses `PlateSimulationParams.SeaLevel` instead of a hard-coded value.
- **Hydrology minor correctness tweak**: Droplet recursion guard in watermap uses `<= MinFlowQuantity` for consistent termination.

**Impact**
- Biome distribution improves notably near coasts and rivers (irrigation weight 3× reflected via humidity). Tropical/wet zones form where expected; tundra/ice no longer over-expand due to precipitation mis-scaling.
- Precipitation maps avoid excessive horizontal banding (noise + gamma), and rain shadow/orographic effects remain visible.
- Results align better with WorldEngine reference outputs, making TD_010 multi-view debugging more reliable.

**Next**
- Implement precipitation and temperature view renderers (use WorldEngine color scales) now that climate layers are corrected.

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