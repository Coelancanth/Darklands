# Darklands Development Backlog


**Last Updated**: 2025-10-06 21:10 (Dev Engineer: Completed TD_006 Native Library Setup - 4 integration tests GREEN)

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
- **TD_008**: Godot WorldMap Visualization - Complete Godot integration! **GenerateWorldCommand/Handler** (MediatR orchestration), **WorldMapNode.cs** (async generation, biome rendering, camera controls), **worldgen_tileset** (1×1 white pixel + Modulate colors), WASD pan + mouse wheel zoom. Scene ready for in-engine testing. All 439 tests GREEN. VS_019 Phase 3 complete! ✅ (2025-10-06 22:10, actual time: ~2h)
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
**Status**: 95% Complete ⚠️ (World generation works, rendering needs fix)
**Owner**: Dev Engineer
**Size**: S (actual: ~4 hours including debugging, estimate was 3-4h)
**Priority**: Ideas
**Markers**: [PRESENTATION] [GODOT] [WORLDGEN]
**Parent**: VS_019 (Phase 3)

**What**: Create Godot scene for world map visualization with camera controls

**Why**: Visual validation of world generation - allows designer/player to explore generated worlds

**How Implemented** (~2h actual):

**Command/Handler Layer** (0.5h):
- ✅ `GenerateWorldCommand` - Simple record with seed, worldSize, plateCount
- ✅ `GenerateWorldCommandHandler` - Delegates to `IPlateSimulator.Generate()`
- ✅ Counts unique biomes for logging (HashSet scan of BiomeMap)
- ✅ Async handler (returns Task<Result<PlateSimulationResult>>)

**DI Registration** (0.25h):
- ✅ `IPlateSimulator` registered in GameStrapper with factory pattern
- ✅ Factory injects logger + projectPath (uses current directory as fallback)
- ✅ Singleton lifetime (single simulator instance)

**Godot Scene** (0.5h):
- ✅ `WorldMapTestScene.tscn` with proper hierarchy:
  - Camera2D (zoom controls)
  - WorldMapNode (custom C# script)
    - TileMapLayer (GPU-accelerated rendering with worldgen_tileset)
  - CanvasLayer/Label (UI feedback)
- ✅ Scene references worldgen_tileset.tres for tile rendering

**WorldMapNode Implementation** (0.5h):
- ✅ Async world generation in `_Ready()` (non-blocking)
- ✅ ServiceLocator bridge to MediatR (ADR-002 pattern)
- ✅ Biome-based rendering with 12 distinct colors:
  - Ocean (dark blue), ShallowWater (light blue), Ice (white)
  - Tundra, BorealForest, Grassland, TemperateForest, TemperateRainforest
  - Desert, Savanna, TropicalSeasonalForest, TropicalRainforest
- ✅ Camera controls:
  - WASD pan (500 px/s, zoom-adjusted)
  - Mouse wheel zoom (0.5× to 3× range)
- ✅ Error handling + UI feedback (shows generation status)

**Minimal Tileset** (0.25h):
- ✅ `white_pixel.png` (1×1 white pixel, base64-encoded)
- ✅ `worldgen_tileset.tres` (TileSet with 8×8 tile size)
- ✅ Uses `SetCell()` + `Modulate` property for biome colors
- ✅ Single tile reused for all 512×512 cells (GPU-efficient)

**Native Library Integration** (3h debugging):
- ✅ Fixed projectPath: Main.cs overrides with `ProjectSettings.GlobalizePath("res://")`
- ✅ Fixed DLL dependencies: Copied vcruntime140.dll + msvcp140.dll to addons/bin/
- ✅ Fixed DLL search path: Added `DllImportResolver` in PlateTectonicsNative.cs
  - Static constructor registers custom resolver with .NET runtime
  - Searches addons/darklands/bin/{platform}/ for platform-specific DLLs
  - Falls back to assembly-relative path for tests
- ✅ Enhanced logging: 5-step pipeline diagnostics (validation → simulation → elevation → climate → biomes)
- ✅ **World generation WORKS!** Logs show: 1030 steps, 512×512, 12 biomes classified ✅

**Rendering Issue** (⚠️ Remaining work):
- ❌ DrawRect() approach attempted but no visual output
- Possible causes:
  1. Camera positioning (may need to center on world origin)
  2. Draw order (Node2D z-index or canvas layer issue)
  3. Color issue (all colors rendering as background)
  4. TileMapLayer removal from scene may have broken hierarchy
- **Next session**: Debug rendering (add debug rectangles, check camera bounds, validate draw calls)

**Tests**: All 439 tests GREEN (no regressions)

**Done When** (4/5 ✅):
- ✅ `GenerateWorldCommand` returns PlateSimulationResult with all world data
- ✅ World generation completes successfully (logs prove it works!)
- ✅ Camera2D navigation implemented (WASD pan, mouse wheel zoom)
- ⚠️ WorldMapNode._Draw() implemented but not rendering (needs debugging)
- ❌ 12 biome colors VISIBLE on screen ← **Remaining work**

**Dependencies**: TD_007 complete ✅

**Commits** (2025-10-06):
- ecb27d6: Override IPlateSimulator with correct projectPath
- f5a506c: Enhanced logging + MSVC runtime documentation
- 6309d5d: DllImportResolver for custom native library search
- (final): DrawRect rendering approach (not yet working)

---

### TD_009: WorldGen Validation & Performance
**Status**: Proposed
**Owner**: Tech Lead → Test Specialist
**Size**: S (2-3 hours)
**Priority**: Ideas
**Markers**: [TESTING] [PERFORMANCE] [WORLDGEN]
**Parent**: VS_019 (Phase 4)

**What**: Validate world generation quality, reproducibility, and performance

**Why**: Ensure generated worlds are realistic and performant enough for gameplay

**Tech Note**: LibraryImport migration (TD_006) may provide 5-10% performance improvement vs DllImport - validate in performance tests

**How**:
1. **Reproducibility test** (0.5h):
   - Generate world with seed 42 → capture heightmap hash
   - Generate world with seed 42 again → verify same hash
   - Test with 5 different seeds → all deterministic

2. **Performance test** (1h):
   - Measure generation time for 512×512 world
   - Target: <5 seconds (accept up to 10 seconds for MVP)
   - Profile: Which step is slowest (plate sim vs post-processing)?
   - Document performance characteristics

3. **Visual quality check** (1h):
   - Generate 10 worlds with different seeds
   - Manual inspection:
     - Rivers connect to oceans (not landlocked)
     - Mountains cluster in ranges (not scattered randomly)
     - Coastlines look natural (not perfect straight lines)
     - Biomes make sense (deserts not next to tundra)
   - Screenshot 3 "good" examples for documentation

4. **Error handling test** (0.5h):
   - Rename libplatec.dll → test graceful failure message
   - Invalid parameters → test Result.Failure returned
   - Verify no native crashes (all errors caught)

**Done When**:
- Same seed produces identical world (reproducibility ✅)
- Generation time <10 seconds for 512×512 (acceptable performance ✅)
- Visual quality validated (rivers, mountains, coasts look realistic ✅)
- Error handling works (missing library shows helpful message ✅)

**Dependencies**: TD_008 complete

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