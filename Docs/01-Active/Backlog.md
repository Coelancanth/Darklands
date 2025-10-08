# Darklands Development Backlog


**Last Updated**: 2025-10-08 16:55 (Tech Lead: Created VS_026-028 precipitation pipeline - base + rain shadow + coastal moisture)

**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 008
- **Next TD**: 019
- **Next VS**: 029


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

### VS_026: WorldGen Stage 3 - Base Precipitation (Noise + Temperature Curve)
**Status**: Proposed
**Owner**: Tech Lead → Dev Engineer (approved)
**Size**: S (~3-4h)
**Priority**: Ideas
**Markers**: [WORLDGEN] [PIPELINE] [STAGE-3] [CLIMATE]

**What**: Generate global precipitation map using coherent noise shaped by temperature gamma curve (WorldEngine algorithm), with **3-stage debug visualization** (noise-only, temperature-shaped, final-normalized)

**Why**: Complete basic climate foundation (elevation + temperature + precipitation). Validate temperature ranges (cold = less evaporation). Foundation for rain shadow (VS_027) and coastal moisture (VS_028) enhancements.

**How** (WorldEngine `precipitation.py` validated pattern):

**Three-Stage Precipitation Algorithm**:

**1. Base Noise Field** (Stage 1 output):
```csharp
// Simplex noise (6 octaves, frequency 64×6 = 384)
var noise = new FastNoiseLite(seed);
noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
noise.SetFractalType(FastNoiseLite.FractalType.FBm);
noise.SetFractalOctaves(6);

float freq = 64.0f * 6;  // 384.0
float n_scale = 1024f / height;  // For 512×512: 2.0

float n = noise.GetNoise2D((x * n_scale) / freq, (y * n_scale) / freq);
baseNoiseMap[y, x] = (n + 1) * 0.5f;  // [-1,1] → [0,1]
```

**2. Temperature Gamma Curve** (Stage 2 output - THE KEY!):
```csharp
// WorldEngine gamma curve: cold = less precipitation (evaporation physics)
float t = temperatureMap[y, x];  // [0,1] from VS_025
float p = baseNoiseMap[y, x];    // [0,1] from Stage 1

// Gamma curve with minimum bonus (prevents zero precip in Arctic)
float gamma = 2.0f;         // Quadratic curve (WorldEngine default)
float curveBonus = 0.2f;    // Minimum 20% precip at coldest regions
float curve = MathF.Pow(t, gamma) * (1 - curveBonus) + curveBonus;

temperatureShapedMap[y, x] = p * curve;
// Arctic (t=0.0) → curve=0.2 → precip×0.2 (20% of base)
// Tropical (t=1.0) → curve=1.0 → precip×1.0 (100% of base)
```

**3. Renormalization** (Stage 3 output - final):
```csharp
// Stretch to fill [0,1] range after temperature shaping
float min = temperatureShapedMap.Min();
float max = temperatureShapedMap.Max();
float delta = max - min;

precipitationMap[y, x] = (temperatureShapedMap[y, x] - min) / delta;  // [0,1]
```

**Key WorldEngine Insights Adopted:**
- ✅ **6 octaves**: Creates natural-looking rainfall patterns (not too smooth, not too noisy)
- ✅ **Gamma curve (2.0)**: Physically realistic (cold air holds less moisture)
- ✅ **Curve bonus (0.2)**: Prevents zero precipitation in polar regions (realistic - even Arctic has snow!)
- ✅ **Renormalization**: Ensures full dynamic range after temperature shaping
- ✅ **Temperature dependency**: Validates VS_025 temperature ranges (fast feedback loop)

**YAGNI Skipped** (WorldEngine complexity not needed for single-world):
- ❌ **Border wrapping**: Seamless east-west for planet simulation (we don't wrap maps)
- ❌ **[-1, 1] range**: Use [0, 1] for consistency with elevation/temperature pipeline

**Visualization Integration** (3-stage debug rendering - mirrors VS_025 pattern):
1. **Renderer** (WorldMapRendererNode.cs):
   - Add `RenderPrecipitationMap(MapViewMode mode, float[,] precipMap)` with 3 stages
   - 3-stop color gradient:
     ```
     Brown (0.0) → Yellow (0.5) → Blue (1.0)
     Dry        →  Moderate   →  Wet
     ```

2. **Legend** (WorldMapLegendNode.cs):
   ```csharp
   case MapViewMode.PrecipitationNoiseOnly:
       AddLegendEntry("Brown", ..., "Dry (base noise)");
       AddLegendEntry("Yellow", ..., "Moderate");
       AddLegendEntry("Blue", ..., "Wet (base noise)");

   case MapViewMode.PrecipitationTemperatureShaped:
       AddLegendEntry("Brown", ..., "Dry (cold = less evaporation)");
       AddLegendEntry("Yellow", ..., "Moderate");
       AddLegendEntry("Blue", ..., "Wet (hot = high evaporation)");

   case MapViewMode.PrecipitationFinal:
       AddLegendEntry("Brown", ..., "Low (<400mm/year)");
       AddLegendEntry("Yellow", ..., "Medium (400-800mm/year)");
       AddLegendEntry("Blue", ..., "High (>800mm/year)");
   ```

3. **Probe** (WorldMapProbeNode.cs):
   - Display all 3 precipitation values (noise-only, temp-shaped, final)
   - Show gamma curve value: `"Temp Curve: {curve:F2}"`
   - Show quantile threshold: `"Classification: Low/Medium/High"`

4. **UI** (WorldMapUINode.cs):
   - Add 3 dropdown items with separator (Precipitation Debug section)

**Pipeline Changes** (GenerateWorldPipeline.cs):
```csharp
// Stage 3: Precipitation calculation
var precipitationMaps = PrecipitationCalculator.Calculate(
    temperatureMap: result.FinalTemperatureMap!,  // Stage 2 output
    width: result.Width,
    height: result.Height,
    seed: parameters.Seed);

return result with {
    BaseNoisePrecipitationMap = precipitationMaps.NoiseOnly,
    TemperatureShapedPrecipitationMap = precipitationMaps.TemperatureShaped,
    FinalPrecipitationMap = precipitationMaps.Final,
    PrecipitationThresholds = precipitationMaps.Thresholds  // Quantile-based
};
```

**Implementation Phases**:

**Phase 0: Update WorldGenerationResult DTOs** (~15min)
- Add 3 precipitation map properties (NoiseOnly, TemperatureShaped, Final)
- Add PrecipitationThresholds record (Low, Medium, High)
- All 447 tests GREEN (no breaking changes)

**Phase 1: Core Algorithm** (~1-1.5h, TDD)
1. ✅ Create `PrecipitationCalculator.cs` with 3-stage output
2. ✅ Implement noise generation (FastNoiseLite, 6 octaves, OpenSimplex2)
3. ✅ Implement gamma curve (WorldEngine formula exactly)
4. ✅ Implement renormalization (stretch to [0,1])
5. ✅ Calculate quantile thresholds (low=75th percentile, med=30th, extends VS_024 pattern)
6. ✅ 10-12 unit tests (gamma curve edge cases, threshold validation, temperature correlation)
7. ✅ All tests GREEN

**Phase 2: Pipeline Integration** (~0.5h)
8. ✅ Update `GenerateWorldPipeline` Stage 3 to call PrecipitationCalculator
9. ✅ Update serialization service (Format v2 backward compat, extends TD_018 pattern)
10. ✅ All 447 tests GREEN (no regressions)

**Phase 3: Multi-Stage Visualization** (~1-1.5h)
11. ✅ Add 3 MapViewMode enum values (PrecipitationNoiseOnly, TemperatureShaped, Final)
12. ✅ Implement RenderPrecipitationMap() with 3-stop gradient (Brown → Yellow → Blue)
13. ✅ Update WorldMapLegendNode with stage-specific legends (mm/year labels, debug hints)
14. ✅ Update WorldMapProbeNode to display all 3 precipitation values + gamma curve + thresholds
15. ✅ Add 3 UI dropdown items with separator (Precipitation Debug section)
16. ✅ All 447 tests GREEN

**Done When**:
1. ✅ **3 precipitation maps populated** in WorldGenerationResult (noise-only, temp-shaped, final)
2. ✅ **Algorithm correct** (WorldEngine-validated, gamma=2.0, curveBonus=0.2)
3. ✅ **Multi-stage visualization working** (3 view modes, stage-specific legends, probe shows all values)
4. ✅ **Visual validation passes** for each stage independently:
   - Noise Only: Random wet/dry patterns (no temperature correlation)
   - Temperature Shaped: Tropical regions wetter, polar regions drier (strong correlation)
   - Final: Full dynamic range restored (renormalization working)
5. ✅ **Temperature validation**: Hot equator = blue (wet), cold poles = brown (dry)
6. ✅ **Quality gates**: No performance regression (<1.5s total), all tests GREEN

**Depends On**: VS_025 ✅ (temperature map required for gamma curve)

**Tech Lead Decision** (2025-10-08 16:55):
- **Algorithm**: WorldEngine `precipitation.py` exact port (gamma=2.0, curveBonus=0.2 - proven physics)
- **No border wrapping**: Single-world generation doesn't need seamless east-west (YAGNI)
- **[0,1] output**: Consistent with elevation/temperature pipeline (not WorldEngine's [-1,1])
- **3-stage debug**: Mirrors VS_025 multi-stage pattern (isolates noise vs temperature shaping)
- **Performance**: Precipitation ~20-30ms (simple noise + per-pixel curve), no threading needed
- **Blocks**: VS_027 (rain shadow needs base precip), VS_028 (coastal moisture needs base precip)
- **Next steps**: Dev Engineer implements after review, use WorldEngine precipitation.py as reference

---

### VS_027: WorldGen Stage 4 - Rain Shadow Effect (Directional Orographic Blocking)
**Status**: Proposed
**Owner**: Tech Lead → Dev Engineer (after VS_026)
**Size**: S (~2-3h)
**Priority**: Ideas
**Markers**: [WORLDGEN] [PIPELINE] [STAGE-4] [RAIN-SHADOW]

**What**: Add directional rain shadow effect to precipitation using simplified orographic blocking (mountains block moisture from prevailing winds), with **2-stage debug visualization** (base-precipitation, with-rain-shadow)

**Why**: Realistic deserts on leeward side of mountains (e.g., Gobi Desert east of Himalayas, rain shadow from Tibetan Plateau). Strategic gameplay: Mountain ranges create dry/wet climate zones, affects settlement placement and resource distribution.

**How** (simplified directional blocking, NO full wind simulation):

**Two-Stage Algorithm**:

**1. Base Precipitation** (from VS_026):
```csharp
// Input: VS_026 final precipitation map (noise + temperature curve + renorm)
float[,] basePrecipitation = result.FinalPrecipitationMap;
```

**2. Directional Rain Shadow** (simplified orographic effect):
```csharp
// Assume prevailing wind: west → east (realistic for mid-latitudes, 30°-60°)
Vector2 windDirection = new Vector2(1, 0);  // Eastward
int maxUpwindDistance = 20;  // Check 20 cells upwind

for (int y = 0; y < height; y++) {
    for (int x = 0; x < width; x++) {
        float mountainBlocking = 0;
        float currentElevation = elevation[y, x];

        // Trace upwind, accumulate blocking from mountains
        for (int step = 1; step <= maxUpwindDistance; step++) {
            int upwindX = x - (int)(windDirection.X * step);
            int upwindY = y - (int)(windDirection.Y * step);

            if (outOfBounds(upwindX, upwindY)) break;

            float upwindElevation = elevation[upwindY, upwindX];

            // Mountain blocks moisture if significantly higher upwind
            if (upwindElevation > currentElevation + 200) {  // 200m threshold
                mountainBlocking += 0.05f;  // 5% blocking per mountain cell
            }
        }

        // Apply rain shadow (max 80% reduction)
        float rainShadow = Math.Max(0.2f, 1 - mountainBlocking);
        precipitationWithRainShadow[y, x] = basePrecipitation[y, x] * rainShadow;
    }
}
```

**Key Insights**:
- ✅ **Prevailing wind assumption**: West → East (NO Coriolis, NO pressure systems, YAGNI)
- ✅ **Directional blocking**: Only UPWIND mountains matter (leeward side dry, windward side unaffected)
- ✅ **Accumulative blocking**: Multiple mountain ranges stack (realistic for Himalayas → Gobi)
- ✅ **20-cell trace distance**: ~1000km at 512×512 world (realistic atmospheric moisture range)
- ✅ **200m elevation threshold**: Prevents hills from blocking (only significant mountains)
- ✅ **Max 80% reduction**: Prevents zero precipitation (even deserts get occasional rain)

**YAGNI Skipped**:
- ❌ **Latitude-dependent wind**: Coriolis effect (trade winds, westerlies, polar easterlies) - over-engineering
- ❌ **Seasonal variation**: Monsoons, wind shifts - adds complexity without gameplay value
- ❌ **Windward moisture increase**: Orographic lift (mountains CREATE rain on windward side) - defer to VS_028

**Visualization Integration** (2-stage rendering):
1. **Renderer**:
   - MapViewMode.PrecipitationBase (VS_026 output)
   - MapViewMode.PrecipitationWithRainShadow (VS_027 output)

2. **Legend**:
   ```csharp
   case MapViewMode.PrecipitationWithRainShadow:
       AddLegendEntry("Brown", ..., "Dry (rain shadow deserts)");
       AddLegendEntry("Yellow", ..., "Moderate");
       AddLegendEntry("Blue", ..., "Wet (windward coasts)");
   ```

3. **Probe**:
   - Show base vs rain-shadow precipitation: `"Base: 0.62 → Shadow: 0.31 (-50%)"`
   - Show blocking factor: `"Mountain Blocking: 0.50 (50% reduction)"`

**Implementation Phases**:

**Phase 1: Algorithm** (~1-1.5h)
- Implement directional upwind trace (20 cells, elevation threshold)
- Calculate mountain blocking accumulation
- Apply rain shadow multiplier (max 80% reduction)
- 8-10 unit tests (single mountain, multiple mountains, edge cases)

**Phase 2: Visualization** (~0.5-1h)
- Add 2 MapViewMode values (Base, WithRainShadow)
- Update renderer/legend/probe
- Add UI dropdown items

**Done When**:
1. ✅ **Deserts appear on leeward side** of mountains (visual validation)
2. ✅ **Windward side unaffected** (no moisture increase yet - correct for VS_027)
3. ✅ **Multiple mountain ranges stack** (Himalayas create extreme Gobi dryness)
4. ✅ **Performance acceptable** (<50ms for 512×512 upwind trace)
5. ✅ **All tests GREEN**

**Depends On**: VS_026 ✅ (base precipitation map required)

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

2. **Phase 2: Climate - Temperature** ✅ COMPLETE (VS_025, S, ~5h actual)
   - ✅ 4-component temperature algorithm: Latitude (92%, with axial tilt) + Noise (8%, FBm fractal) + Distance-to-sun (inverse-square) + Mountain-cooling (RAW elevation thresholds)
   - ✅ Per-world climate variation: AxialTilt and DistanceToSun (Gaussian-distributed) create hot/cold planets with shifted equators
   - ✅ 4-stage debug visualization: LatitudeOnly → WithNoise → WithDistance → Final (isolates each component for visual validation)
   - ✅ Normalized [0,1] output: Internal format for biome classification (Stage 6), UI converts to °C via TemperatureMapper
   - ✅ MathUtils library: Interp() for latitude interpolation, SampleGaussian() for per-world parameters
   - ✅ Multi-stage testing: 14 unit tests (Interp edge cases, Gaussian distribution validation, temperature ranges)
   - ✅ Visual validation passed: Smooth latitude bands, subtle noise variation, hot/cold planets, mountains blue at all latitudes
   - ✅ Performance: ~60-80ms for temperature calculation (no threading needed, native sim dominates at 83%)
   - **Outcome**: Temperature maps ready for biome classification (Stage 6), all 447 tests GREEN
   - **Deferred**: TD_020 Thermal Diffusion (requires water mask from Phase 3-4, will be implemented as part of Phase 5)

3. **Phase 3: Water Table & Rivers** (M, ~6-8h)
   - Sea level calculation (quantile-based threshold, extends Phase 1 pattern)
   - Lake detection (elevation basins below sea level)
   - River generation (flow from high → low elevation, Dijkstra paths to ocean/lakes)
   - Water mask output (bool[,]) - BLOCKS TD_020 thermal diffusion
   - Tests: Water table accuracy, river connectivity

4. **Phase 4: Hydraulic Erosion** (L, ~10-12h)
   - River erosion (carve valleys along river paths)
   - Coastal erosion (smooth coastlines)
   - Mountain weathering (reduce extreme peaks slightly)
   - Eroded elevation map output (more realistic terrain)
   - Tests: Erosion effects, valley formation

5. **Phase 5: Thermal Diffusion & Climate Polish** (M, ~6-8h)
   - TD_020: Physics-based heat diffusion (water/land thermal mass from Phase 3)
   - Coastal moderation + smooth mountain temperature gradients
   - Precipitation calculation (rain shadow, distance from water)
   - Precipitation map visualization
   - Tests: Coastal moderation validation, precipitation patterns

6. **Phase 6: Biome Classification** (M, ~6h)
   - 48 biome types (WorldEngine catalog)
   - Classification based on: elevation, temperature, precipitation
   - Biome transitions (smooth gradients, not hard borders)
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