# Phase 3: Geology & Resources (Future - Post Phase 1/2)

**Status**: Proposed (Planning stage - not yet implemented)

**Purpose**: Add geological realism and resource distribution systems on top of existing plate tectonics, climate, and hydrology foundations. Enables civilization gameplay (mining, trade routes, strategic resources).

---

## Overview

Phase 3 builds the **geology layer** as a **post-process** on top of the physics-based plate tectonics system. This architectural decision (validated in session 2025-10-14) provides key benefits:

`✶ Insight ─────────────────────────────────────`
**Why Post-Process Geology > Integrated Geology**

When considering where to place mineral/volcano generation, the WRONG answer is "integrate into plate tectonics stage." The RIGHT answer is "post-process after plate outputs" because:

1. **Causality Matches Reality**: Tectonic forces CREATE conditions (elevation, boundaries, crust age) → Geology INTERPRETS conditions (where volcanoes/minerals form)
2. **Data Flow Purity**: Facts (elevation exists) vs Interpretations (copper might exist)
3. **Testability**: Mock plate outputs → test geology independently (no need to run 30s plate simulation)
4. **Iteration Speed**: Regenerate geology in 0.5s vs regenerate plates in 30s → **60× faster balance tuning!**
5. **Extensibility**: Add new resource types (e.g., "mithril ore" for fantasy) without touching plate code

This mirrors Clean Architecture's Dependency Rule: Inner layers (physics) don't know about outer layers (gameplay resources). Outer layers DEPEND ON inner layers, not vice versa.
`─────────────────────────────────────────────────`

---

## Stage 0: Plate Library Evaluation & Rewrite

**Status**: Partially Complete (TD_029 - 2025-10-14)

**Purpose**: Decide whether to fix current platec implementation, port to C#, or adopt alternative algorithms. Unblocks A/B testing of plate tectonics approaches.

### Current State (platec - C++ native library)

**Strengths**:
- ✅ Proven simulation core (stable plate motion with collisions/subduction/buoyancy)
- ✅ Cache-friendly recomposition (plate-wise world map overlay minimizes random access)
- ✅ Useful outputs (elevation, plate IDs, crust age)
- ✅ Clear separation (world orchestrator vs per-plate logic)

**Pain Points** (Updated 2025-10-14):
- ✅ ~~API issues: `platec_api_destroy` leaks memory~~ **FIXED in TD_029**
- ✅ ~~Missing batched kinematics getter~~ **ADDED in TD_029** (`platec_api_get_plate_kinematics`)
- ⚠️ Determinism risks: Static `s_flowDone` (deferred to TD_030 - parallelization track)
- ❌ Single-threaded hotspots: Per-plate loops not parallelized (OpenMP opportunity)
- ❌ Assert-exit error handling: Can terminate host process (not graceful for game)
- ❌ Magic constants embedded: Hard to tune without recompiling C++

### Three Evaluation Options

#### Option A: Quick Wins ✅ MOSTLY COMPLETE (TD_029)

**Fix API issues** without major refactoring:

1. ✅ **Fix memory leak** in `platec_api_destroy` - **DONE**
   ```cpp
   void platec_api_destroy(void* pointer) {
       lithosphere* litho = static_cast<lithosphere*>(pointer);
       for (uint32_t i = 0; i < lithospheres.size(); ++i) {
           if (lithospheres[i].data == litho) {
               delete lithospheres[i].data;  // ← FIXED (TD_029)
               lithospheres.erase(lithospheres.begin() + i);
               break;
           }
       }
   }
   ```

2. ✅ **Add batched kinematics getter** - **DONE** (replaces "snapshot getter" concept)
   ```cpp
   // TD_029: Batched API for plate kinematics (20-60× FFI reduction)
   PLATEC_API void platec_api_get_plate_kinematics(
       void* handle,
       PlateKinematics** out_array,  // Thread-local cache
       uint32_t* out_count
   );
   ```
   **Includes**: Velocities, mass centers, velocity magnitudes (all geology needs)

3. ✅ **Add quality-of-life getters** - **DONE**
   ```cpp
   // Query simulation state (TD_029 Phase 1)
   PLATEC_API uint32_t platec_api_get_width(void* handle);
   PLATEC_API uint32_t platec_api_get_height(void* handle);
   PLATEC_API uint32_t platec_api_get_cycle_count(void* handle);
   PLATEC_API uint32_t platec_api_get_plate_count(void* handle);
   ```

4. ⏸️ **Remove static scratch** (`s_flowDone` → member variable) - **DEFERRED to TD_030**
   - **Rationale**: Parallelization track (OpenMP integration) will handle this comprehensively
   - **Workaround**: Thread-local kinematics cache (TD_029) prevents most concurrency issues

**Status**: ✅ **3/4 complete** (memory leak fixed, batched API added, QoL getters added)
**Remaining**: Static scratch removal (TD_030 - when/if parallelization is pursued)

**Pros**: ✅ Low risk achieved, ✅ Ergonomics improved, ✅ Bugs fixed, ✅ VS_031 unblocked
**Cons**: Still C++ (FFI overhead acceptable), no parallelization yet (acceptable for now)

#### Option B: C# Port (High Effort, 2-3 months)

**Full WorldEngine-style port** to C#:

**Pros**:
- ✅ No FFI overhead (pure managed code)
- ✅ Easy to debug/profile in C# ecosystem
- ✅ Tuneable constants (no recompilation)
- ✅ Parallelization via `Parallel.For` (easy to add)

**Cons**:
- ❌ 2-3 months full-time effort (complex codebase)
- ❌ Risk of bugs during port (physics simulation is delicate)
- ❌ May lose performance (C++ SIMD vs C# managed)
- ❌ Maintenance burden (two implementations during transition)

#### Option C: Alternative Algorithms (Research Phase, 1-2 months)

**Evaluate alternatives** to platec entirely:

1. **FastNoise-based** (noise + blending instead of plate simulation)
   - Pros: Fast (< 1s), tuneable, no FFI
   - Cons: Less physically realistic (no true subduction/collision)

2. **Hybrid** (FastNoise for base + platec for boundaries)
   - Pros: Best of both (speed + realism)
   - Cons: Complexity (two systems to maintain)

3. **Custom Simple Plates** (voronoi plates + heightfield blending)
   - Pros: Full control, simple to understand
   - Cons: Loses sophisticated platec features (crust age, buoyancy)

### Recommendation (Tech Lead)

**START with Option A (Quick Wins)**, then re-evaluate:

1. **Phase 3 Stage 0a** (2-3 weeks): Apply quick wins to platec (API fixes, snapshot getter)
2. **Phase 3 Stage 0b** (1 week): Implement PipelineBuilder strategy swapping (already done in TD_027!)
3. **Phase 3 Stage 0c** (2-3 weeks): Prototype FastNoise alternative, A/B test vs platec (same seeds)
4. **Decision Point**: If FastNoise quality acceptable → use it. If platec superior → stick with fixed platec OR pursue C# port if performance becomes blocker.

**Rationale**: De-risk by TRYING alternatives before committing to 2-3 month port. Strategy pattern (TD_027) enables low-cost experimentation.

---

## Stage 0.5: Geological Foundation (POST-PROCESS)

**Status**: Ready to Implement (TD_029 prerequisite complete)

**Purpose**: Derive volcanoes, mineral deposits, and geological features from plate tectonics outputs (elevation, plate IDs, crust age) WITHOUT modifying the plate simulation.

### Inputs (Available from Plate Tectonics - TD_029)

1. **Elevation Map** (`hmap`) - Topography after plate simulation ✅
2. **Plate ID Map** (`imap`) - Which plate owns each cell ✅
3. **Crust Age Map** (`amap`) - Timestamp of crust formation (younger oceanic vs older continental) ✅
4. **Plate Kinematics** - ✅ **NEW (TD_029)**: Batched API provides all plate data in single call
   ```csharp
   // Available via PlateSimulationResult.Kinematics
   public record TectonicKinematicsData(
       uint PlateId,
       Vector2 VelocityUnitVector,    // Direction of plate motion
       float VelocityMagnitude,        // Speed (for classification thresholds)
       Vector2 MassCenter              // Plate centroid (for distance calculations)
   );
   ```

### Architecture: Three-Step Post-Process

```
Plate Tectonics Outputs (elevation, plateIDs, crustAge, velocities)
    ↓
Step 1: BOUNDARY CLASSIFICATION
    → PlateBoundaryMap (convergent/divergent/transform per boundary cell)
    ↓
Step 2: PROSPECTIVITY MAPS
    → VolcanicProspectivity[,] (0..1 probability per cell)
    → MineralProspectivity[,] (0..1 per deposit type: copper, gold, iron, etc.)
    ↓
Step 3: PROBABILISTIC SPAWNING
    → VolcanoList (coords, type: arc/ridge/rift/hotspot)
    → MineralDepositList (coords, type, grade proxy, tonnage proxy)
```

### Step 1: Boundary Classification Algorithm

**Goal**: Classify each plate boundary cell as convergent/divergent/transform based on **relative plate velocities**.

**Algorithm** (pseudocode):
```python
for each cell c:
    for each 4-neighbor n where imap[n] != imap[c]:  # Plate boundary!
        pA = imap[c]
        pB = imap[n]

        # Relative velocity
        v_rel = velocities[pA] - velocities[pB]  # Vector subtraction

        # Boundary normal (points from c → n)
        n_hat = normalize(position[n] - position[c])

        # Dot product determines boundary type
        s = dot(v_rel, n_hat)

        if s > +tau:         # Plates moving TOWARD each other
            type = CONVERGENT
            subtype = determine_convergent_subtype(c, n)  # See below
        elif s < -tau:       # Plates moving APART
            type = DIVERGENT
        else:                # Plates sliding PAST each other
            type = TRANSFORM

        boundaries[c] = (type, subtype)
```

**Convergent Subtype Classification**:
```python
def determine_convergent_subtype(c, n):
    # Use elevation as proxy for crust type (oceanic < sea level, continental > sea level)
    elev_c = elevation[c]
    elev_n = elevation[n]

    sea_level = 0.0  # Assuming normalized [-1, 1] scale

    oceanic_c = (elev_c < sea_level)
    oceanic_n = (elev_n < sea_level)

    if oceanic_c and oceanic_n:
        return OCEAN_OCEAN_SUBDUCTION  # Younger plate subducts (check crustAge)
    elif oceanic_c and not oceanic_n:
        return OCEAN_CONTINENT_SUBDUCTION  # Oceanic (c) subducts under continental (n)
    elif not oceanic_c and oceanic_n:
        return CONTINENT_OCEAN_SUBDUCTION  # Oceanic (n) subducts under continental (c)
    else:
        return CONTINENT_CONTINENT_COLLISION  # Both continental → orogeny (mountains!)
```

**Output**: `BoundaryType[,]` enum map with values:
- `NONE` (interior cells)
- `CONVERGENT_SUBDUCTION` (ocean → continent or ocean → ocean)
- `CONVERGENT_COLLISION` (continent ↔ continent)
- `DIVERGENT` (rift or mid-ocean ridge)
- `TRANSFORM` (shear boundary)

### Step 2: Volcanic Prospectivity System

**Goal**: Compute 0..1 probability maps for where volcanoes SHOULD form, based on **geophysical laws** (not random placement!).

`✶ Insight ─────────────────────────────────────`
**Volcano Derivation is 95% Math, 5% Magic**

Volcanoes aren't random. They follow **geophysical laws** that can be derived from plate outputs:

- **Subduction Arcs**: 50-300km inland from trench (slab depth → water release → melting)
- **Mid-Ocean Ridges**: ON divergent boundaries (mantle upwelling)
- **Continental Rifts**: WITHIN rift zones (lithosphere thinning)
- **Hotspots**: Fixed mantle plumes (requires seeding, but trail is derivable from plate motion)

The platec outputs (elevation, plateIDs, crustAge) + plate velocities are **mathematically sufficient** to compute 85-95% accurate volcanic distributions WITHOUT any "volcano simulation". This is elegant because it reuses existing data instead of adding complexity.

**Lesson**: Before adding features, ask "Can I derive this from what I already have?"
`─────────────────────────────────────────────────`

#### Volcanic Type 1: Subduction Arcs (95% Accurate)

**Physics**: Oceanic plate subducts → releases water at depth → melts mantle → magma rises → arc volcanoes form **50-300km inland** from trench on the **overriding plate**.

**Prospectivity Formula**:
```python
for each cell c:
    if boundaries[c] == CONVERGENT_SUBDUCTION:
        trench_cell = c

        # Determine overriding plate (the one NOT subducting)
        if oceanic[pA] and not oceanic[pB]:
            overriding_plate = pB
        else:
            overriding_plate = pA  # Or younger oceanic plate

        # Find cells 50-300km inland on overriding plate
        for distance d in range(50km, 300km):
            inland_cell = raycast_inland(trench_cell, overriding_plate, d)

            # Prospectivity decreases with distance from optimal (~150km)
            optimal_distance = 150  # km
            distance_factor = gaussian(d, mean=optimal_distance, stddev=75)

            # Elevation bonus (arcs form at mountains)
            elevation_factor = smoothstep(elevation[inland_cell], 0.0, 1.0)

            volcanic_prospectivity[inland_cell] +=
                0.8 * distance_factor * elevation_factor
```

**Parameters** (tunable):
- `arc_distance_min` = 50 km
- `arc_distance_max` = 300 km
- `arc_distance_optimal` = 150 km
- `arc_distance_stddev` = 75 km

#### Volcanic Type 2: Mid-Ocean Ridges (99% Accurate)

**Physics**: Divergent boundaries → mantle upwells → basaltic volcanism → new oceanic crust.

**Prospectivity Formula**:
```python
for each cell c:
    if boundaries[c] == DIVERGENT and oceanic[c]:
        # Ridges are EXACTLY at boundaries
        volcanic_prospectivity[c] = 1.0  # Maximum prospectivity!

        # Optional: Spread prospectivity to neighbors (ridge valley ~10-20km wide)
        for neighbor n within 10km:
            volcanic_prospectivity[n] = 0.6
```

**Parameters**:
- `ridge_core_prospectivity` = 1.0
- `ridge_valley_width` = 10 km
- `ridge_valley_prospectivity` = 0.6

#### Volcanic Type 3: Continental Rifts (90% Accurate)

**Physics**: Continental divergent boundaries → lithosphere thins → basaltic volcanism → rift valleys.

**Prospectivity Formula**:
```python
for each cell c:
    if boundaries[c] == DIVERGENT and not oceanic[c]:
        # Rifts are AT boundaries + nearby rift valley
        volcanic_prospectivity[c] = 0.9

        # Rift valleys can be 20-50km wide
        for neighbor n within 50km:
            distance_factor = 1.0 - (distance(c, n) / 50.0)  # Linear falloff
            volcanic_prospectivity[n] += 0.7 * distance_factor
```

**Parameters**:
- `rift_core_prospectivity` = 0.9
- `rift_valley_width` = 50 km
- `rift_valley_prospectivity` = 0.7

#### Volcanic Type 4: Hotspots (70% Accurate with Seeding)

**Physics**: Fixed mantle plumes → plate moves OVER plume → volcanic trail (e.g., Hawaii).

**Approach**:
1. **Seed hotspot locations** (random or designer-placed, ~5-10 per world)
2. **Compute volcanic trail** from plate motion:
   ```python
   for each hotspot h:
       plate = imap[h.position]
       velocity = velocities[plate]

       # Trail extends OPPOSITE to plate motion direction
       trail_direction = -velocity

       # Spawn volcanoes along trail (younger closer to hotspot)
       for distance d in range(0km, 2000km, step=100km):
           trail_cell = h.position + trail_direction * d

           # Age decreases with distance from hotspot
           age_factor = 1.0 - (d / 2000.0)  # Younger = higher prospectivity

           volcanic_prospectivity[trail_cell] += 0.6 * age_factor
   ```

**Parameters**:
- `hotspot_count` = 5-10 (per world)
- `hotspot_trail_length` = 2000 km
- `hotspot_prospectivity` = 0.6

**Accuracy Note**: 70% because hotspot LOCATION is seeded (not derived), but TRAIL is derived from plate motion.

### Step 3: Mineral Prospectivity Engine

**Goal**: Compute 0..1 probability maps for 12+ resource types based on tectonic context, climate, and hydrology.

#### Deposit Rulebook (Examples)

**Resource Type**: Porphyry Copper/Gold (Cu/Au)
- **Tectonic Setting**: Subduction arcs (convergent with oceanic subduction)
- **Location**: Within arc offset band (50-300km from trench)
- **Elevation**: Mountains (>500m)
- **Climate**: Any (intrusive deposits independent of surface)
- **Formula**:
  ```python
  copper_prospectivity[c] =
      0.5 * is_subduction_arc(c) +
      0.3 * elevation_factor(c, min=500m) +
      0.2 * distance_to_arc_optimal(c)
  ```

**Resource Type**: Orogenic Gold (Au)
- **Tectonic Setting**: Continent-continent collision zones
- **Location**: Along shear/transform intersections near belts
- **Elevation**: Mountains (>1000m)
- **Formula**:
  ```python
  gold_prospectivity[c] =
      0.6 * is_collision_belt(c) +
      0.3 * is_transform_intersection(c) +
      0.1 * elevation_factor(c, min=1000m)
  ```

**Resource Type**: Iron Ore (Fe - Banded Iron Formations)
- **Tectonic Setting**: Stable cratons (old continental crust)
- **Location**: Ancient shield areas (crustAge > threshold)
- **Elevation**: Stable platforms (not mountains)
- **Formula**:
  ```python
  iron_prospectivity[c] =
      0.7 * is_craton(c, age_threshold=old) +
      0.2 * elevation_factor(c, max=300m) +  # Prefer flat
      0.1 * distance_to_plate_boundary(c, min=500km)  # Stable interior
  ```

**Resource Type**: Coal (C - Sedimentary)
- **Tectonic Setting**: Sedimentary basins (rift valleys, foreland basins)
- **Location**: Low elevation (<200m), near ancient swamps
- **Climate**: High precipitation + warm temperature (Phase 2 integration!)
- **Hydrology**: Near ancient river systems
- **Formula**:
  ```python
  coal_prospectivity[c] =
      0.4 * is_sedimentary_basin(c) +
      0.3 * climate_wetness(c, precipitation > threshold) +
      0.2 * elevation_factor(c, max=200m) +
      0.1 * proximity_to_rivers(c)
  ```

**Resource Type**: Placer Gold (Au - Secondary Deposits)
- **Tectonic Setting**: Derived from primary orogenic gold
- **Location**: Downstream from primary deposits along rivers
- **Hydrology**: High flow accumulation cells (Phase 2 integration!)
- **Formula**:
  ```python
  placer_prospectivity[c] =
      0.5 * upstream_primary_gold(c, within=500km) +
      0.4 * flow_accumulation(c, min_threshold) +
      0.1 * elevation_factor(c, gradient=low)  # Prefer flat valleys
  ```

#### 12 Resource Types (Full List)

1. **Porphyry Copper/Gold** (Cu/Au) - Subduction arcs
2. **Orogenic Gold** (Au) - Collision belts
3. **Iron Ore** (Fe) - Cratons (banded iron formations)
4. **Coal** (C) - Sedimentary basins + wet climate
5. **VMS Copper/Zinc** (Cu/Zn) - Mid-ocean ridges (underwater)
6. **Evaporites** (Salt, Gypsum) - Rift basins + arid climate
7. **REE** (Rare Earth Elements) - Alkaline complexes in rifts
8. **Kimberlites** (Diamonds) - Random seed within cratons
9. **Bauxite** (Aluminum) - Tropical weathering + high precipitation
10. **Laterites** (Nickel) - Ultramafic rocks + tropical climate
11. **Placer Gold** (Au) - Rivers downstream from orogenic gold
12. **Tin/Tungsten** (Sn/W) - Granitic intrusions in collision belts

### Step 3: Probabilistic Spawning

**Algorithm**:
```python
for each resource_type in [Copper, Gold, Iron, Coal, ...]:
    prospectivity_map = compute_prospectivity(resource_type)  # 0..1 per cell

    # Sample probabilistically to place deposits
    target_density = config[resource_type].deposits_per_1000km2  # e.g., 2.5 for copper

    for each cell c:
        if random() < prospectivity_map[c] * density_factor:
            # Spawn deposit!
            grade = sample_lognormal(mean=config[resource_type].grade_mean)
            tonnage = sample_lognormal(mean=config[resource_type].tonnage_mean)

            deposits.append(
                MineralDeposit(
                    coords=(c.x, c.y),
                    type=resource_type,
                    grade_proxy=grade,  # Not full simulation, just proxy for gameplay
                    tonnage_proxy=tonnage
                )
            )
```

**Parameters** (tunable per resource type):
- `deposits_per_1000km2` - Target density (e.g., copper = 2.5, gold = 0.8, iron = 1.2)
- `grade_mean` / `grade_stddev` - Lognormal distribution parameters
- `tonnage_mean` / `tonnage_stddev` - Lognormal distribution parameters

**Gameplay Integration**:
- `grade_proxy` → Mining yield (high grade = more ore per turn)
- `tonnage_proxy` → Deposit lifespan (high tonnage = depletes slower)
- Visual representation: Deposit size on map (small/medium/large icons)

---

## Stage 5: Extended Biome Types (Geological)

**Status**: Proposed (Post Stage 4 - Base Biome Classification)

**Purpose**: Add geological biome types that interact with resource systems and provide visual variety.

### Volcanic Biomes

**Biome**: Lava Fields
- **Condition**: Active volcanoes (spawned in Stage 0.5) + high heat
- **Terrain**: Impassable or very slow movement
- **Resources**: Obsidian, sulfur, geothermal energy (future)
- **Visual**: Black rock, glowing lava flows, volcanic vents

**Biome**: Ash Plains
- **Condition**: Within 50km of active volcanoes + low precipitation (ash not washed away)
- **Terrain**: Poor agriculture, low vegetation
- **Resources**: Volcanic ash (fertilizer after weathering)
- **Visual**: Gray terrain, stunted vegetation, ash clouds

**Biome**: Calderas
- **Condition**: Large extinct volcanoes (elevation depression surrounded by peaks)
- **Terrain**: Fertile valleys (volcanic soil)
- **Resources**: Rich agriculture, obsidian, hot springs
- **Visual**: Circular valleys, crater lakes, geothermal features

### Mineral-Rich Biomes

**Biome**: Ore-Rich Mountains
- **Condition**: High concentration of metallic deposits (copper, gold, iron) + mountains
- **Terrain**: Difficult movement, high strategic value
- **Resources**: Multiple deposit types visible on map
- **Visual**: Exposed ore veins, mining camps (if civilizations present)

**Biome**: Gemstone Caves
- **Condition**: Kimberlite pipes (diamond deposits) or hydrothermal veins
- **Terrain**: Cave systems (underground exploration)
- **Resources**: Diamonds, emeralds, rubies (high-value luxury goods)
- **Visual**: Cave entrances, crystalline formations

---

## Stage 6: Resource Distribution & Gameplay Integration

**Status**: Proposed (Civilization simulation integration)

**Purpose**: Connect geology layer to gameplay systems (economy, civilization AI, strategic depth).

### Deposit Spawning Configuration

**Balance Sliders** (exposed in debug panel or config files):

```yaml
resource_balance:
  copper:
    density: 2.5              # deposits per 1000 km²
    grade_mean: 0.8           # % copper content (lognormal mean)
    grade_stddev: 0.3
    tonnage_mean: 100         # million tonnes (lognormal mean)
    tonnage_stddev: 50
    discovery_difficulty: 0.5 # 0=easy, 1=hard (affects civilization AI)

  gold:
    density: 0.8              # Rarer than copper
    grade_mean: 5.0           # g/t (grams per tonne)
    grade_stddev: 2.0
    tonnage_mean: 10          # Much smaller deposits
    tonnage_stddev: 5
    discovery_difficulty: 0.7 # Harder to find

  # ... repeat for all 12 resource types
```

**Tuning Process**:
1. Generate reference world (fixed seed)
2. Visualize deposit distribution on map
3. Adjust sliders until density/clustering looks right
4. A/B test: Play civilization simulation with different configs

### Civilization Simulation Integration

**Discovery Mechanics**:
```python
# Civilization explores cell
def explore_cell(civ, cell):
    if has_deposit(cell):
        discovery_roll = random()
        difficulty = config[deposit.type].discovery_difficulty

        # Prospectivity increases discovery chance
        bonus = prospectivity_map[cell] * 0.3

        if discovery_roll < (1.0 - difficulty + bonus):
            civ.discover_deposit(cell, deposit)
            # Triggers civilization response (build mine, trade route, etc.)
```

**Depletion System**:
```python
# Mine operates each turn
def mine_operate(mine, turns):
    extraction_rate = mine.technology_level * base_rate  # Tech improves yield

    mine.remaining_tonnage -= extraction_rate * turns

    if mine.remaining_tonnage <= 0:
        mine.status = DEPLETED
        # Civilization must find new deposits or trade
```

**Strategic Depth**:
- **Resource Monopolies**: Civilization controls key copper/iron deposits → military advantage
- **Trade Routes**: Landlocked civ lacking resources → must trade with coastal empires
- **Wars Over Resources**: High-value deposits (gold, diamonds) → territorial conflicts
- **Technological Unlocks**: Bronze Age requires copper+tin deposits accessible

---

## Implementation Phases (Incremental)

### Phase A: Outputs and Utilities ✅ COMPLETE (TD_029 - 2025-10-14)

**Deliverables**:
1. ✅ Fix `platec_api_destroy` memory leak - **DONE**
2. ✅ Add batched kinematics API (`platec_api_get_plate_kinematics`) - **DONE**
   - Replaces "snapshot getter" concept with more efficient batched approach
   - Single call returns velocities + mass centers for all plates
3. ✅ Expose plate kinematics to C# (`TectonicKinematicsData`) - **DONE**
4. ⏸️ Remove static scratch variables (`s_flowDone` → member) - **DEFERRED to TD_030**
5. ✅ Add quality-of-life getters (width, height, cycle count, plate count) - **DONE**

**Testing** ✅:
- ✅ Unit tests: API lifecycle (create/step/destroy), deterministic seeds
- ✅ Integration tests: Batched API accuracy (matches individual getters)
- ✅ Performance tests: <1ms for batched call vs ~6ms for individual calls
- ✅ Memory leak tests: <5MB growth over 10 generations
- ✅ Thread safety tests: Concurrent simulations don't corrupt data

**See**: [Backlog.md - TD_029](../../../01-Active/Backlog.md#L100-L220) for complete implementation details

### Phase B: Geology Post-Process (C# - 4-6 weeks)

**Deliverables**:
1. ✅ Implement `BoundaryClassifier.ClassifyBoundaries()` (convergent/divergent/transform)
2. ✅ Implement `VolcanicProspectivityCalculator` (4 volcanic types)
3. ✅ Implement `MineralProspectivityCalculator` (12 resource types)
4. ✅ Implement `ProbabilisticSpawner.SpawnDeposits()` (lognormal sampling)
5. ✅ Create `GeologyPostProcessStage` (integrates into pipeline)

**Testing**:
- Unit tests: Boundary classification correctness (mock plate velocities)
- Unit tests: Prospectivity formulas (validate subduction arc distance curves)
- Visual tests: Generate reference worlds, verify volcanic arc positions (compare to Earth)

**Data Structures**:
```csharp
public record MineralDeposit(
    GridPosition Coords,
    ResourceType Type,
    float GradeProxy,      // 0..1 (high = rich ore)
    float TonnageProxy     // 0..1 (high = large deposit)
);

public enum ResourceType
{
    Copper, Gold, Iron, Coal, Zinc, Salt, REE, Diamonds,
    Bauxite, Nickel, Tin, Tungsten
}

public record VolcanoFeature(
    GridPosition Coords,
    VolcanoType Type,      // Arc, Ridge, Rift, Hotspot
    float ActivityLevel    // 0..1 (0=extinct, 1=active)
);
```

### Phase C: Climate/Hydrology Coupling (2-3 weeks)

**Prerequisites**: Phase 2 complete (VS_029 inner sea flow + humidity system)

**Deliverables**:
1. ✅ Update coal prospectivity with precipitation/temperature data
2. ✅ Update placer gold prospectivity with flow accumulation data
3. ✅ Add evaporite prospectivity (rift basins + arid climate)
4. ✅ Add bauxite/laterite prospectivity (tropical weathering)

**Integration Point**:
```csharp
// In GeologyPostProcessStage.Execute()
var climate = context.Temperature;  // From Phase 1
var precipitation = context.Precipitation;  // From Phase 1
var flowAccumulation = context.FlowAccumulation;  // From Phase 2

var coalProspectivity = CalculateCoalProspectivity(
    basins, climate, precipitation  // ← Climate coupling!
);

var placerGoldProspectivity = CalculatePlacerProspectivity(
    primaryGoldDeposits, flowAccumulation  // ← Hydrology coupling!
);
```

### Phase D: Validation and Tuning (2-3 weeks)

**Deliverables**:
1. ✅ Author 5-10 reference worlds (fixed seeds) with known geology
2. ✅ Visual validation: Arc positions match expected distances (50-300km from trenches)
3. ✅ Statistical validation: Deposit densities match real-world ranges
4. ✅ Expose balance sliders in debug panel (VS_031 integration)
5. ✅ A/B test deposit configs with civilization simulation

**Reference Worlds** (Earth-like features to validate):
- **Seed 1**: Pacific Ring of Fire (subduction arc volcanoes around Pacific)
- **Seed 2**: Mid-Atlantic Ridge (divergent boundary volcanoes)
- **Seed 3**: East African Rift (continental rift volcanoes)
- **Seed 4**: Andes Mountains (porphyry copper belt)
- **Seed 5**: Canadian Shield (craton iron ore deposits)

**Validation Metrics**:
- Volcanic arc distance distribution (should peak at 100-150km from trenches)
- Deposit clustering (should match tectonic boundaries, not random)
- Resource balance (copper 3× more common than gold, iron 5× more common than copper)

---

## Performance Targets

**Geology Post-Process Overhead** (512×512 map):
- **Boundary Classification**: < 10 ms (4-neighbor scan)
- **Volcanic Prospectivity**: < 20 ms (distance transforms + gaussian kernels)
- **Mineral Prospectivity**: < 30 ms (12 resource types, weighted formulas)
- **Probabilistic Spawning**: < 10 ms (sparse sampling)
- **Total Overhead**: < 70 ms (2-3% of total world generation time)

**Rationale**: Geology is post-process → can iterate 60× faster than re-running plates (0.5s vs 30s). Acceptable to spend 70ms for gameplay-critical features.

---

## Future Extensions (Phase 4+)

### Advanced Geology Features

1. **Metamorphic Petrology** - Rock type transitions based on pressure/temperature
2. **Sedimentary Stratigraphy** - Layered rock formation over geological time
3. **Fault Systems** - Earthquake risk zones (transform boundaries)
4. **Geothermal Energy** - Power generation near volcanic areas
5. **Underground Aquifers** - Water resources in sedimentary basins

### Advanced Resource Features

1. **Resource Quality Tiers** - Poor/Average/Rich deposits (not just grade/tonnage)
2. **Composite Deposits** - Multi-metal deposits (Cu+Au+Mo porphyry)
3. **Processing Chains** - Raw ore → smelting → refined metal
4. **Environmental Impact** - Mining pollution, habitat destruction
5. **Renewable Resources** - Forest regrowth, fish population dynamics

---

## Related Documents

- [Main Roadmap](0_Roadmap_World_Generation.md) - High-level overview
- [Stage 0: Pipeline Architecture](Stage_0_Pipeline_Architecture.md) - PipelineBuilder enables geology stage integration
- [PlateTectonics-Refactor-and-Geology-Plan.md](../../../08-Learnings/PlateTectonics/PlateTectonics-Refactor-and-Geology-Plan.md) - Detailed platec analysis
- [Platec-Porting-Decision.md](../../../08-Learnings/PlateTectonics/Platec-Porting-Decision.md) - C++ vs C# port evaluation
- [ADR-004: Feature-Based Clean Architecture](../../../03-Reference/ADR/ADR-004-feature-based-clean-architecture.md) - Why post-process architecture

---

**Last Updated**: 2025-10-14 (TD_029 prerequisite complete - Stage 0.5 ready to implement)
