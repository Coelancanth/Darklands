# Darklands Development Backlog


**Last Updated**: 2025-10-06 20:14 (Product Owner: Added VS_019 WorldEngine Integration MVP to Ideas)

**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 008
- **Next TD**: 006
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

**What**: Integrate plate-tectonics C++ library via PInvoke and port WorldEngine post-processing to C# for strategic world map generation

**Why**:
- Foundation for two-map architecture (strategic world + tactical combat grids)
- Physics-driven terrain (plate tectonics, climate, rivers) creates realistic geography
- Seed-based reproducibility enables balanced faction placement and resource distribution
- Eliminates Python dependency for game distribution
- Validates strategic layer vision before full investment

**Tech Lead Decision** (2025-10-06):
- **Hybrid approach**: Native plate-tectonics library (PInvoke) + C# post-processing
- **Rationale**: Plate simulation is complex (10K+ lines C++), post-processing is simple (~500 lines Python)
- **Pattern**: Follow ADR-007 (Three-Layer Isolation: Interop → Wrapper → Interface)
- **Build strategy**: Pre-compiled binaries from releases (fastest MVP path)

**Technical Breakdown** (4 phases matching ADR-007):

**Phase 1: Native Library Setup** (TD_006) - M (4-6h)
- Obtain plate-tectonics binaries (download from releases)
- Create NATIVE_LIBRARIES.md with checksums
- Create Interop layer (DllImport declarations)
- Create SafeHandle wrapper (RAII cleanup)
- Create NativeLibraryLoader (Godot path resolution)
- Integration test: Library loads successfully

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

### TD_006: Native Library Setup (plate-tectonics PInvoke)
**Status**: Proposed
**Owner**: Tech Lead → Dev Engineer
**Size**: M (4-6 hours)
**Priority**: Ideas (Blocked by ADR-007 approval)
**Markers**: [INFRASTRUCTURE] [NATIVE-LIBRARY] [WORLDGEN]
**Parent**: VS_019 (Phase 1)

**What**: Set up plate-tectonics native library integration with PInvoke scaffolding

**Why**: Foundation for VS_019 world generation - establishes pattern for all future native library integrations

**How** (Following ADR-007):
1. **Obtain binaries** (2h):
   - Download plate-tectonics v1.5.0 from GitHub releases
   - Extract binaries for Windows x64 (libplatec.dll)
   - Place in `addons/darklands/bin/win-x64/`
   - Calculate SHA256 checksums
   - Create `NATIVE_LIBRARIES.md` with download instructions

2. **Create Interop layer** (1h):
   - Create `Features/WorldGen/Infrastructure/Native/Interop/PlateTectonicsNative.cs`
   - Add DllImport declarations (study Python wrapper API):
     ```csharp
     [DllImport("libplatec")]
     internal static extern PlateSimulationHandle platec_create(...);
     [DllImport("libplatec")]
     internal static extern void platec_step(PlateSimulationHandle handle);
     [DllImport("libplatec")]
     internal static extern IntPtr platec_get_heightmap(PlateSimulationHandle handle);
     [DllImport("libplatec")]
     internal static extern void platec_destroy(PlateSimulationHandle handle);
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
- Interop layer compiles (DllImport declarations)
- SafeHandle properly cleans up native resources
- NativeLibraryLoader resolves Godot paths correctly
- Integration test passes: Library loads on Windows x64

**Dependencies**: ADR-007 approved

---

### TD_007: Core WorldGen Infrastructure (Wrapper + Post-Processing)
**Status**: Proposed
**Owner**: Tech Lead → Dev Engineer
**Size**: M (6-8 hours)
**Priority**: Ideas
**Markers**: [CORE-LOGIC] [WORLDGEN] [ALGORITHMS]
**Parent**: VS_019 (Phase 2)

**What**: Implement native wrapper + port WorldEngine post-processing algorithms to C#

**Why**: Business logic layer for world generation - keeps Core pure C# while leveraging native plate simulation

**How**:
1. **Create interface** (0.5h):
   - `Features/WorldGen/Application/Abstractions/IPlateSimulator.cs`
   - Define `Generate(PlateSimulationParams) → Result<PlateSimulationResult>`
   - Define DTOs (PlateSimulationParams, PlateSimulationResult)

2. **Implement wrapper** (2h):
   - `Features/WorldGen/Infrastructure/Native/NativePlateSimulator.cs : IPlateSimulator`
   - Implement `Generate()` with railway-oriented flow:
     - `EnsureLibraryLoaded() → CreateSimulation() → RunSimulation() → ExtractResults()`
   - Implement `Marshal2DArray<T>()` using Span<T> (ADR-007 pattern)
   - Convert all exceptions → `Result.Failure(ERROR_NATIVE_*)`

3. **Port C# post-processing** (3h):
   - Study Python source: `worldengine/generation.py`
   - Port `center_land()` → Array rotation algorithm
   - Port `add_noise_to_elevation()` → Simplex noise (find C# noise library)
   - Port `place_oceans_at_map_borders()` → Border elevation lowering
   - Port `fill_ocean()` → Flood fill algorithm

4. **Port precipitation/temperature/biomes** (1.5h):
   - Study Python source: `worldengine/simulations/`
   - Port precipitation calculation (latitude + rain shadow)
   - Port temperature calculation (elevation + latitude)
   - Port biome classification (Holdridge life zones model)

5. **Unit tests** (1h):
   - Test marshaling (native pointer → C# array)
   - Test post-processing algorithms (deterministic output)
   - Test biome classification (known inputs → expected biomes)

**Done When**:
- `IPlateSimulator` interface defined in Application layer
- `NativePlateSimulator` wrapper implemented with Result<T> error handling
- Span<T> marshaling works (no memory leaks, correct data)
- All post-processing algorithms ported (elevation, precipitation, temperature, biomes)
- Unit tests GREEN (>80% coverage for algorithms)

**Dependencies**: TD_006 complete

---

### TD_008: Godot WorldMap Visualization
**Status**: Proposed
**Owner**: Tech Lead → Dev Engineer
**Size**: S (3-4 hours)
**Priority**: Ideas
**Markers**: [PRESENTATION] [GODOT] [WORLDGEN]
**Parent**: VS_019 (Phase 3)

**What**: Create Godot scene for world map visualization with camera controls

**Why**: Visual validation of world generation - allows designer/player to explore generated worlds

**How**:
1. **Create command/handler** (1h):
   - `Features/WorldGen/Application/Commands/GenerateWorldCommand.cs`
   - `GenerateWorldCommandHandler` orchestrates:
     - Call `IPlateSimulator.Generate()`
     - Run post-processing
     - Return `WorldData` DTO (heightmap, biomes, ocean mask)

2. **Create test scene** (1h):
   - `godot_project/features/worldgen/WorldMapTestScene.tscn`
   - Add `TileMapLayer` (terrain rendering)
   - Add `Camera2D` (navigation)
   - Add `CanvasLayer` (UI controls)

3. **Implement WorldMapNode** (1.5h):
   - `godot_project/features/worldgen/WorldMapNode.cs : Node2D`
   - Override `_Ready()`: Send `GenerateWorldCommand`
   - `RenderWorld(WorldData)`: Convert heightmap → tile IDs
     - Water (elevation < sea level) → Tile 0
     - Grassland (low elevation) → Tile 1
     - Hills (medium elevation) → Tile 2
     - Mountains (high elevation) → Tile 3
   - Use `TileMapLayer.SetCell()` to render tiles

4. **Add camera controls** (0.5h):
   - WASD pan (update Camera2D.Position)
   - Mouse wheel zoom (update Camera2D.Zoom)
   - Clamp zoom (0.5× to 3×)

**Done When**:
- `GenerateWorldCommand` returns `WorldData` with heightmap + biomes
- WorldMapTestScene renders 512×512 world to TileMapLayer
- Camera2D navigation works (WASD pan, mouse wheel zoom)
- Basic biome colors visible (water blue, grass green, mountains gray)

**Dependencies**: TD_007 complete

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