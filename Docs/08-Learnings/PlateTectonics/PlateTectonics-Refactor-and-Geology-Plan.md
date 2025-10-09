## Plate Tectonics (platec) – Refactor and Geology Extension Plan

Date: 2025-10-09
Scope: 1) Assess platec implementation and propose actionable refactors to improve speed, ergonomics, and reliability; 2) Define a practical path to support geology/mineral spawning on top of platec results.

---

### 1) What do we think of platec? Can/should we refactor it to accelerate/improve it?

#### Assessment (Strengths)
- Proven simulation core: stable plate motion with collisions/subduction, buoyancy, and restarts.
- Cache-friendly recomposition: plate-wise world map overlay minimizes random access.
- Useful outputs: heightmap (`hmap`), plate owner map (`imap`), crust age (`amap`).
- Clear separation: world orchestrator (`lithosphere`) vs. per-plate logic (`plate`, `Movement`, `Mass`, `Segments`).

#### Pain Points / Risks
- API issues: `platec_api_destroy` erases handle but does not `delete` the `lithosphere*` (leak). Mixed pointer vs. id usage (`get_agemap`).
- Determinism and state: per-simulation RNGs but some static locals (e.g., `s_flowDone`) and assert-exit error handling.
- Parallelism: the hottest passes are single-threaded (per-plate loops amenable to parallelization).
- Maintainability: extensive `ASSERT` calls can terminate host processes; magic constants embedded; complex inner loops blend ownership/subduction/collisions.

#### Acceleration/Improvement Plan

Quick Wins (low-risk)
- Fix API lifecycle:
  - Free memory in destroy; align API to pointer-based accessors or expose `get_id(void*)`.
  - Add const getters for width/height, cycle/iteration counts.
- Determinism ergonomics:
  - Thread RNG via constructor and expose seed in API; remove static `s_flowDone` by reusing a member scratch buffer sized to world area (resized once), not static.
- Build/runtime switches:
  - Add a compile-time option to disable `ASSERT` aborts in release builds, return error codes on API boundaries.

Parallelization (medium effort)
- Plate overlay pass:
  - Parallelize the outer loop over plates in `updateHeightAndPlateIndexMaps` with work stealing (OpenMP/TBB/std::execution). Each thread writes to `hmap/imap/amap`; conflicts are resolved in the same logic (ownership/subduction/resolve). To keep determinism across threads, optionally process fixed plate index order but using stripe-based partitioning with local buffers, then reduce (see below).
- Safer deterministic variant:
  - Per-thread partial buffers (`hmap_t`, `imap_t`, `amap_t`) over fixed tile partitions, then a deterministic reduce that replays resolve/subduction where overlaps exist. This adds memory but guarantees ordering.
- Subduction/collision application:
  - The post-collection phases (`subductions[i]`, `collisions[i]`) are per-plate; process their vectors in parallel, then clear.

Memory/loop micro-optimizations
- Avoid re-zeroing full maps every frame where possible:
  - Track a dirty region bounding box per plate movement; zero only the union area; or keep a generation tag array instead of zeroing.
- Precompute neighbor indexes (N,E,S,W offsets) to avoid recomputing modulo and index math in inner loops.
- Reduce branches in ownership resolve by hoisting oceanic checks and timestamp comparisons; use bitwise forms already present but simplify where safe.
- Consider SoA layout for per-plate maps to help vectorization.

API/Ergonomics
- Add a single “snapshot” struct (C API) to retrieve:
  - `heightmap`, `platesmap`, `agemap`
  - Per-plate kinematics (unit velocity vectors), counts, and bounds
- Optional event hooks: user-supplied callback for “collision”/“subduction” with cell coords to support higher-level systems.

Testing & Tooling
- Add unit tests for API lifecycle (create/step/destroy), restart criteria, collision/subduction invariants, deterministic seed.
- Benchmarks: instrument plate overlay and collision handling; evaluate parallel variants.

#### Suggested Code Edits (concise)

Fix memory leak in destroy:
```cpp
// platecapi.cpp
void platec_api_destroy(void* pointer) {
  lithosphere* litho = static_cast<lithosphere*>(pointer);
  for (uint32_t i = 0; i < lithospheres.size(); ++i) {
    if (lithospheres[i].data == litho) {
      // free first, then erase handle
      delete lithospheres[i].data;
      lithospheres.erase(lithospheres.begin() + i);
      break;
    }
  }
}
```

Remove static scratch and make it a member:
```cpp
// lithosphere.hpp
class lithosphere {
  // ...
  std::vector<bool> flowDoneScratch; // sized to bounds_area when needed
};

// lithosphere.cpp (in erode)
if (flowDoneScratch.size() < bounds_area) flowDoneScratch.resize(bounds_area);
std::fill(flowDoneScratch.begin(), flowDoneScratch.begin() + bounds_area, false);
```

Expose RNG seeding via API and store seed in `lithosphere`.

Parallelize plate overlay (sketch):
```cpp
// In updateHeightAndPlateIndexMaps
#pragma omp parallel for schedule(dynamic)
for (int i = 0; i < (int)num_plates; ++i) {
  // same loop body; beware of shared writes to hmap/imap/amap
  // Option A: atomic/critical sections at resolve points (slower)
  // Option B: partition world into tiles and assign plates to tiles (preferred)
}
```

Add consolidated snapshot getter:
```cpp
// platecapi.hpp
PLATEC_API void platec_api_get_snapshot(void* p,
  float** heightmap, uint32_t** platesmap, const uint32_t** agemap,
  uint32_t* width, uint32_t* height);
```

---

### 2) Geology/Mineral Spawning – Should/Can we modify it to support?

Short answer
- Yes, we should support geology as a layer on top of platec. We can avoid invasive changes by adding derived maps and lightweight event capture, then run a post-process “geology simulator” to place deposits based on tectonic context, climate, and time.
- We can also add minimal hooks to platec to expose the signals we need (boundaries, velocities, ages) efficiently.

#### Inputs we already have (or can cheaply extract)
- `hmap` (topography/elevation)
- `imap` (plate ownership per cell)
- `amap` (crust timestamps → infer crust age and “young oceanic” vs. older continental)
- Plate unit velocities (via `platec_api_velocity_unity_vector_{x,y}`)

#### Derived tectonic layers (post-process)
- Plate boundary map with types for each boundary cell/pair:
  - Convergent–Subduction (ocean→continent; ocean→ocean)
  - Convergent–Collision (continent↔continent)
  - Divergent (rift/mid-ocean ridge)
  - Transform (shear)
- Arc belt map: offset on overriding side from subduction trenches (configurable trench–arc distance band)
- Orogenic belt map: buffered distance around continent–continent convergence zones
- Rift zones: buffered distance around divergent boundaries on continents
- Crustal thickness proxy: from platec’s `height/density` heuristics and/or smoothed elevation; use `amap` for oceanic crust aging

Classification sketch:
```text
For each world cell c:
  For 4-neighbors n with imap[n] != imap[c]:
    pA = imap[c], pB = imap[n]
    v_rel = vA - vB (unit vectors scaled by typical speed)
    n_hat = unit((pos[n] - pos[c]))  // boundary normal
    s = dot(v_rel, n_hat)
    if s > +tau:  convergent
    else if s < -tau: divergent
    else: transform
  For convergent:
    Determine subducting side by lower crustal height (oceanic) or age (younger oceanic subducts) → set arc offset on overriding side
```

#### Deposit rulebook (examples)
- Mid-ocean ridges (divergent, oceanic): VMS, massive sulfides, pillow basalts; young `amap` values
- Subduction arcs (convergent with oceanic subduction): porphyry Cu/Au/Mo belts within arc offset band
- Orogenic belts (continent–continent): orogenic gold along shear/transform intersections near belts
- Rifts (continental divergent): evaporites (with arid climate), REEs/alkaline complexes, sedimentary basins
- Stable cratons (old continental crust by `amap`): banded iron formations (if ancient eras modeled or by rule), kimberlites (random seed within craton mask)
- Placer deposits: derive from primary deposits + hydrology (rivers) once our hydrology pipeline is available

Each rule produces a scalar prospectivity map (0..1) per deposit type:
```text
prospect = w1 * boundary_type_match
         + w2 * distance_kernel(boundary/belt)
         + w3 * crust_age_match(amap)
         + w4 * elevation/slope constraints
         + w5 * climate/humidity (when available)
```

#### Minimal additions to platec (optional but helpful)
- Boundary extraction utility (C API) that returns a compact edge list between plates per frame or at snapshot time.
- Plate centroid positions and unit velocities exposed via a batched getter to avoid N FFI calls.
- Optionally tag subduction events in a per-iteration ring buffer (indices and masses) to feed the arc builder (can also be inferred from boundary classification + oceanic age).

#### Implementation Plan (incremental)

Phase A – Outputs and utilities (platec)
- Add snapshot getter and batched plate kinematics API.
- Provide a helper in C# to compute boundary types from `imap` + kinematics; store `BoundaryType[,]` and distance transforms for belts.

Phase B – Geology post-process (C#)
- Build derived masks (arcs, orogeny, rifts) with configurable distances; compute prospectivity rasters per deposit type.
- Sample probabilistically to place deposits with tunable density and size; save a resource layer (e.g., `MineralDeposit[]` with type, coords, grade/tonnage proxies).

Phase C – Climate/hydrology coupling
- After TD_009 (hydrology + humidity), refine rules for evaporites, laterites, placers, bauxites using moisture/temperature and river networks.

Phase D – Validation and tuning
- Author reference worlds (seeds) and visually validate belts and deposits; expose sliders for rule weights.

#### “Should we?”
- Yes—this approach is low-risk, modular, and leverages platec’s strengths without deep invasive changes. It keeps platec focused and puts gameplay-heavy geology in a separate, testable layer.

---

### Summary of Recommended Actions

- Apply quick API fixes (destroy, snapshot, RNG) and remove static scratch.
- Parallelize plate overlay/collision where safe; measure and gate by a compile-time flag.
- Add boundary/kinematics getters; implement a C# geology post-process producing derived belts and mineral prospectivity maps.
- Defer heavy physics changes; if needed, pursue later (e.g., better collision detection via spatial acceleration, toroidal wrap support).

**Outcome**: Faster, more ergonomic platec; a robust geology layer that generates believable mineral systems using tectonic context, extensible with climate/hydrology.
