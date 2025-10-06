# TD_009: WorldEngine Pipeline Gap Analysis

**Date**: 2025-10-07
**Author**: Dev Engineer
**Purpose**: Ultra-careful analysis of WorldEngine simulation pattern vs our current implementation to identify gaps and design proper TD_009 scope.

---

## 🔍 WorldEngine Simulation Pipeline (Complete)

WorldEngine follows a **sequential simulation pattern** with 10 distinct steps:

```
1. Plate Tectonics (C extension: platec)
   ↓ produces: heightmap (raw elevation)

2. Elevation Post-Processing (basic.py)
   - center_land() - shifts continents away from borders
   - place_oceans_at_map_borders() - lowers border elevation
   - add_noise_to_elevation() - Perlin noise variation
   - fill_ocean() - BFS flood fill from borders
   ↓ produces: heightmap (processed), ocean_mask

3. TemperatureSimulation (temperature.py)
   - Latitude banding with axial tilt
   - Coherent noise field
   - Altitude lapse-rate effect (mountain cooling)
   ↓ produces: temperature_map, temperature_thresholds (6 bands)

4. PrecipitationSimulation (precipitation.py)
   - Coherent noise field (wrap-aware)
   - Gamma-shaped by temperature (curve_offset limits cold wetness)
   - Normalized to [-1, 1]
   ↓ produces: precipitation_map, precipitation_thresholds (low/med/high)

5. ErosionSimulation (erosion.py) ⚠️ **CRITICAL STEP**
   - Computes flow direction per cell (find_water_flow)
   - Finds river sources (river_sources: mountains + precip threshold)
   - Traces river paths to sea (river_flow with A* fallback)
   - Erodes valleys around rivers (river_erosion: radius 2, curve 0.2/0.05)
   - Cleans up elevation monotonicity (cleanUpFlow)
   ↓ produces: river_map, lake_map
   ↓ MUTATES: heightmap (valley carving)

6. WatermapSimulation (hydrology.py)
   - Droplet model: 20,000 droplets seeded on random land
   - Weighted by precipitation (wetter areas seed more droplets)
   - Accumulates flow downhill (recursive droplet function)
   - Calculates thresholds: creek (5%), river (2%), main river (0.7%)
   ↓ produces: watermap, watermap_thresholds

7. IrrigationSimulation (irrigation.py)
   - Spreads watermap influence via logarithmic kernel
   - 21×21 neighborhood convolution
   - Models moisture availability from nearby water
   ↓ produces: irrigation_map

8. HumiditySimulation (humidity.py) ⚠️ **KEY FOR BIOMES**
   - Combines precipitation + irrigation (weights 1:3)
   - Calculates quantiles (12/25/37/50/62/75/87 percentiles)
   - Produces 8-level moisture classification
   ↓ produces: humidity_map, humidity_quantiles

9. BiomeSimulation (biome.py)
   - Holdridge-style classification
   - Uses temperature_thresholds × humidity_quantiles
   - 39 land biomes + ocean/ice categories
   ↓ produces: biome_map

10. IcecapSimulation (icecap.py) [OPTIONAL]
    - Freezing over ocean tiles based on temperature
    - Neighbor influence spreading
    ↓ produces: icecap_map (thickness)
```

---

## 📊 Our Current Implementation

**File**: `NativePlateSimulator.cs`

```csharp
public Result<PlateSimulationResult> Generate(PlateSimulationParams parameters)
{
    return EnsureLibraryLoaded()
        .Bind(() => RunNativeSimulation(parameters))              // Step 1: Plate Tectonics ✅
        .Bind(raw => PostProcessElevation(raw, parameters))       // Step 2: Elevation Post-Processing ✅
        .Bind(elevation => CalculateClimate(elevation, params))   // Steps 3-4: Temp + Precip ✅
        .Map(climate => ClassifyBiomes(climate));                 // Step 9: Biomes ✅ (BUT WRONG INPUT!)
}
```

**What We Have**:
1. ✅ **Plate Tectonics**: Native library integration (PlateTectonicsNative.cs)
2. ✅ **Elevation Post-Processing**: PlaceOceansAtBorders, AddNoise, FillOcean (ElevationPostProcessor.cs)
3. ✅ **Temperature Simulation**: Latitude cosine + elevation cooling + noise (ClimateCalculator.cs:397-497)
4. ✅ **Precipitation Simulation**: Multi-octave noise + orographic lift + rain shadow (ClimateCalculator.cs:36-170)
5. ❌ **MISSING**: ErosionSimulation (rivers, lakes, valley carving)
6. ❌ **MISSING**: WatermapSimulation (creek/river/main river flow)
7. ❌ **MISSING**: IrrigationSimulation (moisture kernel spreading)
8. ❌ **MISSING**: HumiditySimulation (precip + irrigation combined)
9. ⚠️ **INCORRECT**: BiomeClassifier uses **precipitation** directly (BiomeClassifier.cs:47-85)
   - **Should use**: humidity (precipitation + irrigation with 1:3 weight)
10. ⚠️ **NOT PLANNED**: IcecapSimulation (deferred for MVP)

---

## 🚨 Critical Gap Identified

### **Our Biome Classification is Wrong!**

**Current Code** (BiomeClassifier.cs:80):
```csharp
biomes[y, x] = ClassifyLandBiome(temp, precip, precipPercentiles);
//                                      ^^^^^
//                                      WRONG! Should be humidity!
```

**WorldEngine Pattern** (biome.py:30-40):
```python
def execute(self, world, seed):
    humidity_level = world.humidity_level_at((x, y))  # Uses HUMIDITY, not precipitation!
    temperature_level = world.temperature_level_at((x, y))
    return holdridge_classification(humidity_level, temperature_level)
```

**Why This Matters**:
- **Precipitation** = raw rainfall at location
- **Humidity** = precipitation + irrigation (moisture from nearby rivers/lakes)
- **Irrigation weight is 3×** stronger than direct precipitation!

**Impact**: Cells near rivers should be **wetter** (humid) even if precipitation is low. Our current biomes ignore proximity to water entirely!

---

## 🎯 TD_009 Revised Scope

### **What TD_009 Should Be**

Port WorldEngine's **missing simulation steps** (5-8) to complete the proven pipeline and fix biome classification.

### **Work Breakdown** (4 algorithms + biome fix)

#### **Phase 1: Erosion & Rivers** (~6-8h)
**File**: `HydraulicErosionProcessor.cs` (new)

Port `erosion.py` (403 lines → ~500 lines C#):

1. **Flow Direction Calculation** (erosion.py:73-88)
   ```csharp
   public static int[,] FindWaterFlow(float[,] heightmap)
   {
       // For each cell, find steepest downhill neighbor
       // Store direction as index: 0=center, 1=N, 2=E, 3=S, 4=W
   }
   ```

2. **River Source Detection** (erosion.py:122-173)
   ```csharp
   public static List<(int x, int y)> FindRiverSources(
       float[,] heightmap,
       bool[,] oceanMask,
       float[,] precipitation,
       int[,] waterPath)
   {
       // Find cells where:
       // - Is mountain (elevation > threshold)
       // - Water flow accumulated > RIVER_TH (0.02)
       // - Not within radius 9 of another source
   }
   ```

3. **River Path Tracing** (erosion.py:175-284)
   ```csharp
   public static List<(int x, int y)> TraceRiverPath(
       (int x, int y) source,
       float[,] heightmap,
       bool[,] oceanMask,
       List<List<(int, int)>> existingRivers)
   {
       // Follow steepest descent to ocean
       // Merge into existing rivers if encountered
       // Use A* pathfinding for complex terrain (findLowerElevation)
       // Dead-ends become lakes
   }
   ```

4. **Valley Erosion** (erosion.py:346-388)
   ```csharp
   public static void ErodeValleysAroundRivers(
       float[,] heightmap,
       List<List<(int x, int y)>> rivers)
   {
       // For each river cell:
       //   For radius 2 around cell:
       //     If neighbor higher than river:
       //       Erode neighbor towards river level
       //       Curve factor: 0.2 (adjacent), 0.05 (diagonal)
   }
   ```

5. **Elevation Monotonicity** (erosion.py:286-297)
   ```csharp
   public static void CleanUpFlow(
       List<(int x, int y)> river,
       float[,] heightmap)
   {
       // Ensure river flows downhill monotonically
       // If cell higher than previous: lower it to previous elevation
   }
   ```

**Returns**: `(float[,] erodedHeightmap, List<River> rivers, List<(int,int)> lakes)`

#### **Phase 2: Watermap Simulation** (~3-4h)
**File**: `WatermapCalculator.cs` (new)

Port `hydrology.py` (81 lines → ~120 lines C#):

1. **Droplet Flow Model** (hydrology.py:18-57)
   ```csharp
   private static void SimulateDroplet(
       (int x, int y) position,
       float quantity,
       float[,] heightmap,
       float[,] watermap,
       bool[,] oceanMask)
   {
       // Recursive function:
       // 1. Find lower neighbors
       // 2. Distribute quantity proportionally by elevation difference
       // 3. Recurse into each lower neighbor
       // 4. Stop if quantity < 0.05 or reached ocean
   }
   ```

2. **Watermap Generation** (hydrology.py:59-80)
   ```csharp
   public static (float[,] watermap, WatermapThresholds thresholds) CalculateWatermap(
       float[,] heightmap,
       float[,] precipitation,
       bool[,] oceanMask,
       int seed,
       int dropletCount = 20000)
   {
       // 1. Sample 20,000 random land positions (weighted by precipitation)
       // 2. For each position: SimulateDroplet(pos, precipitation[pos], ...)
       // 3. Calculate thresholds:
       //    - creek: 5th percentile (land cells only)
       //    - river: 2nd percentile
       //    - main_river: 0.7th percentile
   }
   ```

**Returns**: `(float[,] watermap, WatermapThresholds thresholds)`

#### **Phase 3: Irrigation Simulation** (~2-3h)
**File**: `IrrigationCalculator.cs` (new)

Port `irrigation.py` (63 lines → ~80 lines C#):

1. **Logarithmic Kernel** (irrigation.py:28-53)
   ```csharp
   public static float[,] CalculateIrrigation(float[,] watermap)
   {
       // For each cell:
       //   For 21×21 neighborhood:
       //     distance = sqrt((dx)^2 + (dy)^2)
       //     if distance <= 10:
       //       influence = watermap[neighbor] / log(distance + 1)
       //       irrigation[cell] += influence
   }
   ```

**Returns**: `float[,] irrigation`

#### **Phase 4: Humidity Simulation** (~1-2h)
**File**: `HumidityCalculator.cs` (new)

Port `humidity.py` (45 lines → ~60 lines C#):

1. **Combine Precipitation + Irrigation** (humidity.py:18-27)
   ```csharp
   public static (float[,] humidity, float[] quantiles) CalculateHumidity(
       float[,] precipitation,
       float[,] irrigation)
   {
       // For each cell:
       //   humidity[cell] = precipitation[cell] * 1.0 + irrigation[cell] * 3.0
       //
       // Calculate quantiles: [12, 25, 37, 50, 62, 75, 87] percentiles
       // (for 8-level moisture classification: superarid → superhumid)
   }
   ```

**Returns**: `(float[,] humidity, float[] quantiles)`

#### **Phase 5: Fix Biome Classification** (~1h)
**File**: `BiomeClassifier.cs` (modify existing)

1. **Update Method Signature**:
   ```csharp
   // OLD:
   public static BiomeType[,] Classify(..., float[,] precipitationMap, ...)

   // NEW:
   public static BiomeType[,] Classify(..., float[,] humidityMap, float[] humidityQuantiles, ...)
   ```

2. **Update Classification Logic**:
   ```csharp
   // OLD:
   var moistureLevel = GetMoistureLevel(precip, precipPercentiles);

   // NEW:
   var moistureLevel = GetMoistureLevel(humidity, humidityQuantiles);
   ```

3. **Update NativePlateSimulator Pipeline**:
   ```csharp
   // Add after CalculateClimate():
   .Bind(climate => SimulateHydrology(climate, parameters))

   private Result<HydrologyData> SimulateHydrology(ClimateData climate, PlateSimulationParams p)
   {
       // 1. Erosion (rivers, lakes, eroded heightmap)
       var (erodedHeightmap, rivers, lakes) = HydraulicErosionProcessor.Execute(...);

       // 2. Watermap (flow accumulation)
       var (watermap, watermapThresholds) = WatermapCalculator.CalculateWatermap(...);

       // 3. Irrigation (moisture spreading)
       var irrigation = IrrigationCalculator.CalculateIrrigation(watermap);

       // 4. Humidity (precip + irrigation)
       var (humidity, humidityQuantiles) = HumidityCalculator.CalculateHumidity(
           climate.PrecipitationMap, irrigation);

       return Result.Success(new HydrologyData(
           erodedHeightmap, rivers, lakes, watermap, irrigation, humidity, humidityQuantiles));
   }
   ```

---

## 🏗️ Architecture Pattern

### **WorldEngine: Mutation Pattern**
```python
# Each simulation mutates the World object
world = World(...)
TemperatureSimulation().execute(world, seed)
PrecipitationSimulation().execute(world, seed)
ErosionSimulation().execute(world, seed)  # Modifies world.heightmap in-place!
HumiditySimulation().execute(world, seed)
BiomeSimulation().execute(world, seed)
```

### **Our Pattern: Functional Pipeline**
```csharp
// Pure functions returning new arrays (immutable)
return RunNativeSimulation(params)
    .Bind(raw => PostProcessElevation(raw, params))
    .Bind(elevation => CalculateClimate(elevation, params))
    .Bind(climate => SimulateHydrology(climate, params))  // NEW!
    .Map(hydrology => ClassifyBiomes(hydrology));

// Each step returns Result<T> with new data structures
```

**Benefits of Our Approach**:
- ✅ Immutable data (no side effects)
- ✅ Railway-oriented error handling
- ✅ Easier to test (pure functions)
- ✅ Clearer data flow

**Trade-off**:
- ⚠️ More memory allocations (but negligible for 512×512 maps)

---

## 📋 Updated PlateSimulationResult DTO

**Current** (PlateSimulationResult.cs):
```csharp
public record PlateSimulationResult(
    float[,] Heightmap,
    bool[,] OceanMask,
    float[,] PrecipitationMap,
    float[,] TemperatureMap,
    BiomeType[,] BiomeMap);
```

**After TD_009**:
```csharp
public record PlateSimulationResult(
    float[,] Heightmap,              // Eroded elevation (valleys carved)
    bool[,] OceanMask,
    float[,] PrecipitationMap,
    float[,] TemperatureMap,
    float[,] HumidityMap,            // NEW: precip + irrigation (3× weight)
    float[,] WatermapData,           // NEW: flow accumulation
    float[,] IrrigationMap,          // NEW: moisture from nearby water
    List<River> Rivers,              // NEW: river paths
    List<(int x, int y)> Lakes,      // NEW: lake positions
    BiomeType[,] BiomeMap);          // Now uses humidity (correct!)

public record River(
    List<(int x, int y)> Path,
    bool ReachedOcean);

public record WatermapThresholds(
    float Creek,      // 5th percentile
    float River,      // 2nd percentile
    float MainRiver); // 0.7th percentile
```

---

## ✅ Done When (TD_009 Complete)

1. ✅ **HydraulicErosionProcessor.cs** exists with 5 methods (flow direction, sources, tracing, erosion, cleanup)
2. ✅ **WatermapCalculator.cs** exists with droplet model + threshold calculation
3. ✅ **IrrigationCalculator.cs** exists with logarithmic kernel convolution
4. ✅ **HumidityCalculator.cs** exists with precip + irrigation combination
5. ✅ **BiomeClassifier** updated to use humidity instead of precipitation
6. ✅ **NativePlateSimulator** pipeline includes all 4 new steps
7. ✅ **PlateSimulationResult** DTO expanded with rivers, lakes, watermap, irrigation, humidity
8. ✅ Generated worlds have:
   - Rivers flowing from mountains to ocean
   - Lakes where rivers can't reach sea
   - Eroded valleys around rivers (smooth terrain)
   - Biomes consider proximity to water (irrigation effect)
9. ✅ All existing 439 tests remain GREEN
10. ✅ New integration tests for erosion/watermap/irrigation/humidity (at least 8 tests)

---

## 🚫 NOT in TD_009 Scope

**Visualization/Rendering** (separate TD items):
- TileMapLayer rendering (pixel art tiles) → **TD_011** (TileSet Visualization)
- Multi-view debug maps (elevation/precip/temp) → **TD_010** (Debug Visualization)
- Camera controls (already done in TD_008)

**Deferred Simulations** (future work):
- IcecapSimulation (ocean freezing) → Not needed for strategy layer MVP
- Permeability simulation → Not critical for MVP
- Ancient map rendering → Aesthetic feature, not core simulation

---

## 📚 Reference Files

**WorldEngine Sources** (References/worldengine/):
- `worldengine/simulations/erosion.py` (403 lines) - Rivers, lakes, valley carving
- `worldengine/simulations/hydrology.py` (81 lines) - Watermap droplet model
- `worldengine/simulations/irrigation.py` (63 lines) - Logarithmic kernel
- `worldengine/simulations/humidity.py` (45 lines) - Precip + irrigation combination
- `worldengine/simulations/biome.py` (180 lines) - Holdridge classification (reference)

**Our Current Implementation**:
- `src/Darklands.Core/Features/WorldGen/Infrastructure/Native/NativePlateSimulator.cs` (275 lines)
- `src/Darklands.Core/Features/WorldGen/Infrastructure/Algorithms/ElevationPostProcessor.cs` (222 lines)
- `src/Darklands.Core/Features/WorldGen/Infrastructure/Algorithms/ClimateCalculator.cs` (501 lines)
- `src/Darklands.Core/Features/WorldGen/Infrastructure/Algorithms/BiomeClassifier.cs` (273 lines)

---

## 💡 Key Insights

1. **Simulation Order Matters**: Each step builds on previous results (erosion needs precip, humidity needs irrigation)
2. **Humidity ≠ Precipitation**: Irrigation spreads moisture influence with 3× weight (major impact on biomes!)
3. **River Tracing is Complex**: Needs A* fallback for challenging terrain (not just steepest descent)
4. **Valley Erosion is Subtle**: Small radius (2 cells), gentle curves (0.2/0.05 factors) prevent over-erosion
5. **Watermap Uses Sampling**: 20,000 random droplets (not all cells) for performance
6. **Thresholds are Percentile-Based**: Creek/river/main river use 5%/2%/0.7% thresholds (automatic balancing)

---

**Conclusion**: TD_009 is about **completing the simulation pipeline** to match WorldEngine's proven architecture. Visualization/rendering are separate concerns handled by other TD items.
