## Plate Tectonics (platec) – Implementation Review (Control Flow Focus)

Date: 2025-10-09
Scope: Deep control-flow and lifecycle review of `References/plate-tectonics` (Mindwerks/plate-tectonics fork). Emphasis on update loop, orchestration between modules, state transitions, and API surface.

---

### Executive Summary

- The simulation is driven by a central `lithosphere` object that owns world-scale maps and an array of `plate` objects. Control flow is a repeated step function: compute kinematics → reconcile plate coverage into world maps → handle subduction/collisions → spawn new oceanic crust at divergences → buoyancy aging → cull empty plates → potentially restart a cycle → terminate when no plates remain (or cycles exhausted).
- The public C API (`platecapi.*`) exposes an opaque pointer, a step function, a finish predicate, and accessors to height/plate maps. Examples demonstrate a simple loop: create → while(!finished) step → read map → destroy.
- The plate-level control flow separates movement/physics (`Movement`), mass/centroid (`Mass`), bounds management (`Bounds`), and continental segmentation (`Segments` + `SegmentData` + `MySegmentCreator`) for collision bookkeeping and targeted mass transfer.
- Restarts are part of normal flow: after low-activity or long iterations, the world “bakes in” accumulated state, recreates plates, and continues up to a configured cycle count.

---

### Public API and Example Control Flow

Entry points live in `src/platecapi.hpp/.cpp`. Clients create a simulation, advance it step-by-step, poll for completion, then fetch results.

Key API surface:
- `void* platec_api_create(seed, width, height, sea_level, erosion_period, folding_ratio, aggr_overlap_abs, aggr_overlap_rel, cycle_count, num_plates)` → returns `lithosphere*` as opaque pointer
- `void platec_api_step(void*)` → advances one iteration
- `uint32_t platec_api_is_finished(void*)` → 1 if done, 0 otherwise
- `float* platec_api_get_heightmap(void*)` → world heightmap (topography)
- `uint32_t* platec_api_get_platesmap(void*)` → plate index per world cell
- `float platec_api_velocity_unity_vector_{x,y}(void*, plate_index)` → unit velocity components per plate

Example end-to-end loop (`examples/simulation.cpp`):
- Parse params (seed, dims, step cadence)
- `p = platec_api_create(...)`
- Optionally snapshot initial map
- Loop: `while (!finished) { platec_api_step(p); maybe_snapshot(); }`
- Save final map and exit

---

### Core Engine Control Flow (lithosphere)

The `lithosphere` orchestrates the entire simulation and owns global state. Its constructor seeds initial terrain and instantiates plates; `update()` performs one simulation step.

#### Initialization
1) Generate base noise field (slow noise) into `tmp` and normalize to [0, 1].
2) Binary search a `sea_threshold` to match target `sea_level` proportion, then stamp into world map as base crust values:
   - Land cells: `CONTINENTAL_BASE`
   - Ocean cells: `OCEANIC_BASE`
3) Create plate containers and call `createPlates()`.

#### Plate creation (`createPlates`)
- Select `max_plates` random unique origins.
- Grow each plate’s ownership frontier via `growPlates()` until all world cells are assigned to a plate (wrap-aware neighborhood growth).
- For each plate:
  - Copy its local submap (`pmap`) from the global heightmap (masking cells by ownership) and instantiate a `plate` with: random seed, `pmap`, local bounds, world origin, and plate age.
- Initialize simulation counters: `iter_count`, `peak_Ek`, `last_coll_count`.

#### Per-step update (`update`)
High-level sequence per iteration:
1) Accumulate system kinematics
   - Sum plate velocities and momentum across all plates.
   - Track `peak_Ek` (max observed momentum).
2) Early restart condition (activity guardrails)
   - Restart if any holds:
     - Total velocity < `RESTART_SPEED_LIMIT`
     - `systemKineticEnergy / peak_Ek < RESTART_ENERGY_RATIO`
     - `last_coll_count > NO_COLLISION_TIME_LIMIT`
     - `iter_count > RESTART_ITERATIONS`
3) Apply per-plate forces and movement
   - For each plate:
     - `resetSegments()` to prune stale collision bookkeeping
     - Optional `erode(CONTINENTAL_BASE)` when `erosion_period` divides `iter_count`
     - `move()` to advance bounds by current velocity (unit vector × scalar speed), including rotational perturbation
4) Recompose world maps and record interactions
   - Reset global `hmap` (height) and `imap` (owner indices)
   - For each plate, copy local crust into global maps with wrap-aware addressing, resolving overlaps:
     - Subduction: oceanic under continental prioritization with sediment transfer recorded as subductions (deferred)
     - Continental juxtapositions: resolve ownership and folding using `folding_ratio`; record collisions for both plates
5) Apply subduction effects
   - For each plate, for each recorded subduction, call `addCrustBySubduction(...)` to add crust inland along motion vectors (with randomized offsets)
6) Update collisions and momentum
   - For each collision pair, apply friction proportionally to deformed mass and aggregate crust if thresholds met (`aggr_overlap_abs` / `aggr_overlap_rel`) via `aggregateCrust` and `collide`
7) Fill divergent boundaries with new oceanic crust
   - Cells without owners after movement inherit previous owner; assign young oceanic crust with buoyancy bonus (`amap` timestamps, higher `hmap` for young ocean)
8) Cull empty plates
   - Remove plates with no owned cells and reindex owners
9) Apply buoyancy aging globally
   - Add small buoyancy proportional to inverted age for recent oceanic crust
10) Tick iteration counter

If a restart was triggered at step (2), control transfers to `restart()` instead of continuing the normal flow:

#### Restart control flow (`restart`)
- Increment `cycle_count`, stop if exceeding `max_cycles` (unless “eternity” mode)
- Bake all plates into the world height map and age map (sum heights, weighted age mean)
- `clearPlates()` (delete all `plate` objects), then `createPlates()` again if cycles remain
- Restore per-plate ages from global `amap` into each new plate’s local age map
- If cycles are exhausted, apply a final buoyancy pass

#### Termination
- `isFinished()` returns true when `num_plates == 0`. Consumers poll `platec_api_is_finished()` to end the loop.

---

### Plate-Level Control Flow (`plate`)

Each `plate` composes:
- `Movement`: plate velocity vector (unit + scalar), impulses, circular drift; friction and collision response
- `Mass`: total crust mass and centroid; maintained during erosion and crust transfer
- `Bounds`: plate’s position and size within world; handles wrapping and growth
- `Segments`/`SegmentData`/`MySegmentCreator`: partition continental crust into connected components for targeted collision aggregation and stats
- Local maps: `map` (height), `age_map` (timestamps) sized to `Bounds`

Key flows:
- Movement: `move()` updates velocity from impulses, normalizes direction, adjusts scalar speed, applies rotational delta, then shifts bounds by `velocityOnX/Y()`
- Erosion: `erode(lower_bound)`
  - Identify river sources (local maxima) above bound
  - Flow downhill along steepest neighbor, carving into a temp map; accumulate noise; then redistribute crust nonlinearly to neighbors based on height deltas, update `Mass`
- Collision bookkeeping:
  - `addCollision(wx, wy)` selects the continent segment at world coords, increments coll-count, returns area used by callers to weigh responses
  - `aggregateCrust(...)` transfers the entirety of the collided continental segment to the receiver plate at aligned world coords; updates mass, marks donor segment non-existent
  - `applyFriction(deformed_mass)` reduces velocity proportional to deformed mass and plate mass
  - `collide(...)` computes impulse exchange along the collision normal using restitution 0 (sticking), adjusting both plates’ impulses inversely by mass
- Subduction deposition: `addCrustBySubduction(...)` chooses an inland position along relative motion, mixes ages by mass, adds mass to height and plate `Mass`
- Crust edits: `setCrust(x, y, z, t)` will grow bounds when necessary (wrapping-aware), copy old content into new bounds, reassign segment IDs, and adjust the local age map by mass weighting

---

### Data Structures and Responsibilities

- World-scale:
  - `HeightMap hmap` (float), `IndexMap imap` (uint32 owner), `AgeMap amap` (uint32 timestamp), `WorldDimension` (wrap-aware indexing/helpers)
  - `vector<vector<plateCollision>> collisions/subductions` per plate per step
- Plate-scale:
  - `HeightMap map`, `AgeMap age_map`, `Bounds` (position + size), `Segments` (segment IDs per local cell) with `SegmentData` stats
- Physics:
  - `Movement` (vx, vy unit vector, scalar `velocity`, impulses dx/dy, small circular drift based on world size)
  - `Mass` (total mass, center of mass cx/cy)

---

### Orchestration Diagram (One Step)

Pseudocode summarizing `lithosphere::update()`:
```cpp
// compute activity
float totalVelocity = sum(plate.getVelocity());
float systemKineticEnergy = sum(plate.getMomentum());
if (shouldRestart(totalVelocity, systemKineticEnergy, peak_Ek, last_coll_count, iter_count)) {
  restart(); return;
}
// advance plates
for plate in plates:
  plate.resetSegments();
  if (erosion_due) plate.erode(CONTINENTAL_BASE);
  plate.move();
// rebuild world, detect interactions
hmap.clear(); imap.clear();
for plate in plates:
  copy_local_to_world_with_wrap_and_resolve(plate, hmap, imap, amap,
      collisions, subductions, folding_ratio);
// apply subductions
for plate_index:
  for subduction in subductions[plate_index]:
    plates[plate_index].addCrustBySubduction(subduction, iter_count);
// collision friction/aggregation/impulse
updateCollisions();
// fill divergent boundaries with young oceanic crust and buoyancy
spawn_new_oceanic_crust(imap, prev_imap, amap, hmap, iter_count);
// cleanup
removeEmptyPlates();
apply_oceanic_buoyancy(hmap, amap, iter_count);
iter_count++;
```

---

### Notable Control-Flow Details and Invariants

- Wrap-aware addressing: All world-to-plate and plate-to-world indexing is done through `WorldDimension` and `Bounds`, guarding against out-of-range drift.
- Order dependence: Collision bookkeeping assumes the sequence: update maps → record collisions → apply subduction → apply collision friction/aggregation. Deviating the order may corrupt momentum or double-apply crust.
- Segment lifecycle: Segments are reset at each step (`resetSegments`) to avoid accumulating stale connected-component artifacts before collision processing. Aggregated segments are marked non-existent to avoid repeated transfers in a single step.
- Buoyancy effects are time-based and applied both during divergence fill and as a final global pass, biasing young ocean to be more buoyant (slightly higher elevation) before aging levels it.
- Restart preserves mass and ages by baking the global map back into new plates and restoring local `age_map` from baked `amap`.

---

### Risks, Bugs, and Observations

- Memory lifecycle in API destroy:
  - `platec_api_destroy(void* litho)` erases the pointer from the internal vector but does not `delete` the `lithosphere*` → memory leak for clients using API in long-lived processes.
- API inconsistency (agemap):
  - `platec_api_get_agemap(uint32_t id)` expects an integer ID, unlike other accessors that accept the opaque pointer. There is no public function to retrieve the ID for a given pointer in the C API; Python bindings may use a different path. Consider harmonizing signatures.
- Global state/`static` data:
  - `lithosphere.cpp` uses a `static vector<bool> s_flowDone` sized to the largest plate area seen; reused across `erode` calls. Thread-safety concerns; also retains memory.
- Error handling:
  - Heavy use of `ASSERT(...)` macros that `exit(1)` or log-only in release; library consumers can be terminated from within. Consider propagating errors via return codes/exceptions on API boundary.
- Determinism:
  - RNG (`SimpleRandom`) is per `lithosphere`/`plate`, advancing in many locations (growth, movement, subduction, erosion noise). Control-flow dependent randomness (e.g., collision counts) can vary by floating-point rounding/order if code changes.
- Performance:
  - `updateHeightAndPlateIndexMaps` is hot and touches large contiguous memory per-plate; it is designed for cache-friendly plate-wise writes, but subductions and collisions add branching and per-cell logic. Consider parallelizing per-plate loops if thread safety is addressed.

---

### Recommendations

- Fix API destroy to free resources:
  - Change `platec_api_destroy` to actually `delete` casted `lithosphere*` and erase from registry.
- Align C API signatures:
  - Make all getters accept `void*` or expose `platec_api_get_id(void*)` to support id-based calls consistently.
- Guard thread-safety (future):
  - Remove/avoid static buffers inside step functions; move to member state or stack.
- Replace process-terminating assertions at library boundary with error returns; reserve `ASSERT` for internal invariants in debug builds.
- Add tests exercising: restart conditions, aggregation thresholds, subduction deposition distribution, and API lifecycle (create/step/destroy).

---

### File Map (Reviewed)

- API: `src/platecapi.hpp`, `src/platecapi.cpp`
- Orchestrator: `src/lithosphere.hpp`, `src/lithosphere.cpp`
- Plate composition: `src/plate.hpp`, `src/plate.cpp`
- Physics: `src/movement.hpp`, `src/movement.cpp`; `src/mass.hpp`, `src/mass.cpp`
- Spatial/indexing: `src/bounds.*`, `src/geometry.*`, `src/world_point.*`, `src/rectangle.hpp`, `src/heightmap.*`
- Segmentation: `src/segments.hpp`, `src/segment_data.hpp`, `src/segment_creator.hpp`
- Helpers: `src/plate_functions.hpp`, `src/utils.hpp`, noise sources
- Example CLI: `examples/simulation.cpp`

---

### Appendix: One-Turn Event Ordering Rationale

- Move then overlay: updates must happen on plates before projecting to the world map, otherwise collisions are computed from stale positions.
- Record then apply subduction: sediment transfers depend on identifying the “receiver” plate before deposition; applying subduction during overlay would double count.
- Friction/aggregation before divergence fill: collision impulses and mass transfers affect ownership; creating new oceanic crust should not bias collision results.
- Buoyancy last: visual shaping and stability; ensures young ocean stands out only after ownership and masses are final.
