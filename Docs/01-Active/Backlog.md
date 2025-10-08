# Darklands Development Backlog


**Last Updated**: 2025-10-08 14:52 (Dev Engineer: VS_025 implementation plan - Multi-stage temperature rendering)

**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 008
- **Next TD**: 019
- **Next VS**: 026


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

### VS_025: WorldGen Pipeline Stage 2 - Temperature Simulation
**Status**: In Progress
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

**Phase 2: Pipeline Integration** (~0.5h)
4. Update `WorldGenerationResult` to store **4 temperature maps** + per-world params (AxialTilt, DistanceToSun)
5. Update `GenerateWorldPipeline` Stage 2 to call `TemperatureCalculator.Calculate()`
6. Verify 433 tests still GREEN (no regressions)

**Phase 3: Multi-Stage Visualization** (~1.5-2h)
7. Add 4 `MapViewMode` enum values:
   - `TemperatureLatitudeOnly` (debug: horizontal bands, tilt visible)
   - `TemperatureWithNoise` (debug: subtle climate fuzz)
   - `TemperatureWithDistance` (debug: hot/cold planet variation)
   - `TemperatureFinal` (production: complete with mountain cooling)
8. Implement `RenderTemperatureMap()` with 5-stop gradient (Blue → Cyan → Green → Yellow → Red)
9. Update `WorldMapLegendNode` with **stage-specific legends** (show which component is active)
10. Update `WorldMapProbeNode` to display **all 4 values** + per-world parameters
11. Add 4 UI buttons grouped as "Temperature Debug" section

**Phase 4: Visual Validation** (~0.5h)
12. **Validate each stage visually**:
    - Latitude Only: Horizontal bands, equator shifts with tilt
    - With Noise: Subtle "fuzz" on bands (not dramatic)
    - With Distance: Same pattern, hotter/colder overall (seed variation)
    - Final: Mountains blue at **all latitudes** (cooling works!)
13. Performance check (<1.5s total for 512×512, no regression)
14. All 433 tests GREEN

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
1. **Phase 1: Elevation Post-Processing** ✅ COMPLETE (VS_024, M, ~8h actual)
   - ✅ Ported 4 WorldEngine algorithms (~150 lines): add_noise, fill_ocean, harmonize_ocean, sea_depth
   - ✅ Dual-heightmap architecture: Original raw + Post-processed raw (both [0.1-20] range)
   - ✅ Quantile-based thresholds: SeaLevel, HillLevel, MountainLevel, PeakLevel (adaptive per-world)
   - ✅ Real-world meters mapping: ElevationMapper for UI display (Presentation layer utility)
   - ✅ BFS flood-fill ocean detection (OceanMask, not simple threshold)
   - ✅ FastNoiseLite integration: 8-octave OpenSimplex2 noise for terrain variation
   - ✅ Three colored elevation views: Original, Post-Processed, Normalized (visual validation)
   - ✅ Format v2 serialization: Saves post-processed data with backward compatibility (TD_018)
   - **Outcome**: Foundation complete for Stages 2-6, all 433 tests GREEN

2. **Phase 2: Climate - Temperature** (VS_025, S, ~3-4h)
   - Temperature calculation (latitude + noise + elevation cooling)
   - Temperature map visualization (5-stop gradient: -40°C to +40°C)
   - Tests: Temperature gradient validation
   - **Status**: Approved, ready for Dev Engineer after VS_024 ✅

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