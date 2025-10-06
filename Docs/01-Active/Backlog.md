# Darklands Development Backlog


**Last Updated**: 2025-10-07 01:23 (Dev Engineer: Revised TD_009 Phase 1 - Two-layer TileMapLayer system with biome mapping)

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

### TD_009: WorldGen Quality Polish & TileSet Visualization
**Status**: Proposed
**Owner**: Tech Lead → Dev Engineer
**Size**: M (6-8 hours - revised scope)
**Priority**: Ideas
**Markers**: [WORLDGEN] [PERFORMANCE] [QUALITY] [TILESET]
**Parent**: VS_019 (Phase 4)

**What**: Two-layer TileMapLayer rendering system + WorldEngine erosion/rivers

**Why**: Replace flat Image/Texture2D with tile-based pixel art aesthetic (strategy game style). Add terrain realism via erosion and rivers.

**Current State** (as of 2025-10-07 01:23):
✅ **Implemented**:
- 41 WorldEngine biomes - complete Holdridge life zones
- Percentile-based moisture classification (automatic biome balance)
- WorldEngine color scheme (Biomes.html palette)
- Fixed elevation cooling (0.25× lapse rate)
- Multi-octave noise (coarse + fine-grain variation)
- Rain shadow effect (orographic lift, leeward reduction)
- Image/Texture2D rendering (262,000× faster than DrawRect)
- Camera zoom (20× for detail inspection)

⚠️ **Quality Issues Remaining**:
1. **No tile-based rendering** - Using colored pixels instead of pixel art tiles
2. **No erosion** - Sharp terrain, unrealistic coastlines
3. **No rivers** - Missing hydrology simulation

**Revised Work** (Two-Layer TileMapLayer + WorldEngine algorithms):

1. **Two-Layer TileMapLayer System** (3-4h) ← REVISED:
   - **Architecture**: Base TileMapLayer ("plain" tile background) + Overlay TileMapLayer (terrain tiles)
   - **Tile size**: 32×32 pixels (2×2 atlas cells @ 16px base) per world cell
   - **Coverage**: 60% overlay (moderate density), 40% plain base shows through gaps
   - **Biome mapping** (Core logic): 41 WorldEngine biomes → 7 TileSet terrain types
     - Ocean/ShallowWater → "ocean" tile (25:9)
     - Polar/Subpolar (6 biomes) → "tundra" tile (10:12)
     - Boreal (5 biomes) → "taiga" tile (28:18)
     - Temperate/Tropical forests (17 biomes) → "forest" tile (28:21)
     - Desert biomes (10 biomes) → "desert" tile (10:6)
     - Grassland/Steppe (3 biomes) → Skip overlay (show "plain" base, 10:15)
   - **Mountain placement** (elevation-based, density-scaled):
     - High elevation (>0.8): Dense placement (every 2 cells) for tall peaks
     - Medium (0.6-0.8): Moderate (every 4 cells) for hills
     - Low (0.4-0.6): Sparse (every 8 cells) for highlands
   - **Implementation**: `GetTerrainTileMapQuery` in Core (SSOT principle)
   - **Impact**: Pixel art aesthetic, validates multi-layer strategic map architecture

2. **Hydraulic Erosion** (3-4h) ← DEFERRED TO PHASE 2:
   - Port `erosion.py` → `HydraulicErosionProcessor.cs` (WorldEngine's proven implementation)
   - Iterative erosion passes (sediment transport, deposition)
   - Creates realistic valleys, river beds, smooth coastlines
   - **Reference**: `References/worldengine/worldengine/erosion.py`
   - **Impact**: Natural terrain features, realistic geography

3. **River Simulation** (2-3h) ← DEFERRED TO PHASE 2:
   - Port `rivers.py` → `RiverSimulator.cs` (WorldEngine's proven implementation)
   - Flow accumulation from precipitation + elevation gradients
   - River path generation (follows downhill flow)
   - **Reference**: `References/worldengine/worldengine/rivers.py`
   - **Impact**: Realistic hydrology, rivers connect to oceans

4. **Performance Optimization** (1-2h) ← DEFERRED TO PHASE 2:
   - Multi-thread erosion/rivers (independent per-pass)
   - Profile generation pipeline (identify actual bottlenecks)
   - **Target**: <5 seconds for 512×512 world

**Done When** (Phase 1 - TileSet System):
- ✅ Two TileMapLayers render correctly (base "plain" + overlay terrain)
- ✅ 41 biomes mapped to 7 terrain tiles (Core query handles logic)
- ✅ Mountains placed with elevation-based density scaling
- ✅ 60% overlay coverage achieved (moderate density, plain shows through)
- ✅ Visual quality matches reference image (pixel art tiles, natural variation)
- ✅ All existing tests remain GREEN

**Tech Notes**:
- TileSet: `assets/overworld/overworld.tres` with 7 terrain types (all 2×2 atlas, 32×32px)
- Rendering: Replace Sprite2D/Image/Texture2D → 2× TileMapLayer (base + overlay)
- World size: 512×512 cells → 16,384×16,384 pixel map (32px per cell)
- Core query: `GetTerrainTileMapQuery` returns `TerrainTileType?[,]` (null = show base)

**Dependencies**: TD_008 complete ✅

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