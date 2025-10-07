### Civs Simulation (ftomassetti/civs) – Implementation Review

This document provides a deep code-level review of the Clojure implementation found in `References/civs-simulation`. It covers architecture, data flow, correctness, performance, test coverage, and concrete, actionable fixes.

---

### Executive Summary

- **Strengths**: Clear domain model via records; modular logic split (demographics, choices, core); deterministic testing hooks; practical world/language serialization; simple facts/event system; concise CLI entrypoint and a small rendering utility.
- **Main Risks**: Several correctness bugs (typos, wrong API usage, likely logic errors) that can alter outcomes or fail at runtime; outdated modules; global mutable state; some anti-idiomatic/fragile Clojure patterns.
- **Priority Fixes**: see “Bugs and Correctness Issues” for specific locations and patches. Highest impact: activity choice sorting, population/numbers usage of `.size`, settlement updates, society evolution typo, migration probability `case`, demographics range checks, I/O result format flag.

---

### Architecture Overview

- **Entrypoint & CLI**
  - `civs.core`: program lifecycle (`simulate`, `run`, `-main`), orchestrates turns and snapshot/facts capture.
  - `civs.cli`: command-line options (world, bands, turns, output file, readable flag).

- **Domain Model** (`civs.model.*`)
  - `core`: core records and utilities.
  - `society`: social evolution rules and queries (band → tribe → chiefdom).
  - `language`, `history`, `politic` (placeholder), `culture` (placeholder).

- **Simulation Logic** (`civs.logic.*`)
  - `basic`: randomness, math helpers, small numeric utilities.
  - `demographics`: world-biome-based prosperity, environmental modifiers, population transitions.
  - `tribe-choices`: event probabilities and applications (migrate, split, settle, evolve, learn).
  - `core`: world/game generation and per-turn processing pipeline.

- **I/O & Visualization**
  - `civs.io`: fressian-based serialization with custom handlers; history compression via :unchanged markers.
  - `civs.graphics`: basic population heatmap renderer (ARGB buffered image).

- **Activities (stubs)**
  - `civs.activities.commerce`, `civs.activities.war` are placeholders.

- **Build & Tests**
  - `project.clj` declares dependencies (`lands-java-lib`, `langgen`, `fressian`, etc.).
  - Unit and acceptance tests cover core, IO, logic, and societal rules with deterministic RNG via `with-redefs`.

---

### Data Model (records)

```clojure
(defrecord PoliticalEntity [id name society groups culture])
(defrecord Game [world settlements groups political-entities next_id])
(defrecord Culture [nomadism knowledge language])
(defrecord Group [id name position population political-entity-id])
(defrecord Settlement [id name foundation-turn position owner])
(defrecord Population [children young-men young-women old-men old-women])
```

- `Game` state is a bag of maps keyed by ids (`groups`, `settlements`, `political-entities`) plus a monotonic `next_id` allocator and a Java `world`.
- `PoliticalEntity` owns groups and culture (nomadism level, knowledge flags, language ref).
- `Population` splits cohorts to model demographics and lifecycle transitions.

---

### Simulation Pipeline

- `civs.core/simulate` drives the N-turn loop, updates global `current-turn`, stores `game-snapshots` per turn, and collects `facts` emitted by events.
- `civs.logic.core/turn` reduces `group-turn` across all groups and prunes dead groups from their political entities.
- `civs.logic.tribe-choices/group-turn`:
  - Recomputes population via `civs.logic.demographics/update-population` (births, aging, mortality based on prosperity).
  - Updates `game` with the new group, then considers events in order: nomadism/agriculture shifts, settlement creation, migration, splitting, societal evolution.
  - Each successful event records a `fact` tagged with the group and parameters.

Pseudocode for one turn of the whole world:

```clojure
(let [groups (groups game)]
  (->> groups
       (reduce (fn [g grp]
                 (-> g
                     (update-population-for grp)
                     (apply-possible-events-for grp)))
               game)
       (remove-dead-groups)))
```

---

### Environment and Prosperity Model

- Base prosperity per activity depends on biome category (per-activity tables).
- Temperature and humidity modifiers nudge prosperity within tight bands (≈ [0.975, 1.025]) to remain near biome baselines.
- `crowding-per-activity` caps supportable population based on actives and activity type (hunting vs agriculture).
- Actual prosperity for a position is the max of per-activity prosperity among known activities.
- Choices like migration and splitting hinge on current prosperity and crowding.

---

### Determinism & RNG

- Random functions are centralized in `civs.logic.basic` with a process-wide `java.util.Random` instance and wrappers `crand-int`/`crand-float`.
- Tests override these with `with-redefs` for reproducibility.
- Recommendation: inject RNG through `game` or function args to eliminate global state and enable reproducible, parallelizable runs.

---

### Facts and History

- Turn-local `facts` atom records structured events.
- Simulation returns `{:facts {turn -> [facts...]}, :game-snapshots {turn -> game}}`.
- IO layer compresses history by substituting unchanged cultures with `:unchanged`, then restores while deserializing.

---

### Bugs and Correctness Issues (high priority first)

1) Activity selection chooses the worst option
- File: `civs.logic.demographics` (function `chosen-activity`)
- Issue: sorts ascending and takes first, picking the lowest prosperity instead of the highest.
- Fix: select max, e.g. `(->> activities (apply max-key :prosperity) :activity)`.

2) `.size` called on lazy seqs (multiple locations)
- Files: `civs.model.core` (`group-in-pos`, `n-groups-alive`, `n-ghost-cities`), and similar patterns elsewhere.
- Issue: `(filter ...)` returns a seq; `.size` is a Java `Collection` method and will fail. Should use `count`.
- Fix examples:
  - `group-in-pos`: replace `(.size groups)` with `(count groups)`.
  - `n-groups-alive`: replace `(.size (groups-alive game))` with `(count (groups-alive game))`.
  - `n-ghost-cities`: same pattern with `(count ...)`.

3) Settlement update uses wrong key
- File: `civs.model.core` (`update-settlement`)
- Issue: uses `:settelements` instead of `:settlements`, so updates are dropped.
- Effect: downstream updates (e.g., renaming settlements on language development) do not persist.
- Fix: replace `:settelements` with `:settlements`.

4) Chiefdom evolution typo
- File: `civs.model.society` (`evolve-in-chiefdom`)
- Issue: sets `:chiefdm` instead of `:chiefdom`.
- Effect: `chiefdom-society?` checks will fail; acceptance tests rely on “no chiefdom yet”, so may not trigger here but will break later.
- Fix: `:chiefdom`.

5) Migration probability uses malformed `case`
- File: `civs.logic.tribe-choices` (event `migrate` probability function)
- Issue: `(case ...)` used without matching constants; should be `cond`. Current form won’t dispatch correctly.
- Fix:
  ```clojure
  (* ip (cond
          (sedentary? game group)      0.0
          (semi-sedentary? game group) 0.15
          (nomadic? game group)        0.85
          :else                        0.0))
  ```

6) Range check helper has unbalanced parens
- File: `civs.logic.basic` (`check-in-range`)
- Issue: second `(when ...)` is nested incorrectly; potential compile error.
- Fix: ensure two independent `when` forms are both closed properly.

7) Throwing exceptions without constructing them
- File: `civs.logic.demographics` (`base-prosperity-per-activity-in-biome`, `crowding-per-activity`)
- Issue: `(throw Exception ...)` should be `(throw (Exception. ...))`.

8) Outdated `civs.logic.stats`
- Calls `group-turn` with a world instead of a game; appears to predate refactors and is likely unused.
- Fix: update or remove; if kept, rewire to current API.

9) Global state and `def` inside functions
- Files: `civs.core/simulate` uses `(def current-game ...)`; `civs.logic.globals` uses `def` to mutate `current-turn`.
- Issue: side-effectful globals impede testability and parallelism.
- Fix: use locals/atoms in scope, or thread state via args/return values.

10) History serialization options not respected
- File: `civs.io/save-simulation-result`
- Issue: ignores `use-fressian`; always writes fressian. CLI `--readable-format` is not honored.
- Fix: implement readable path (e.g., tagged EDN) when requested.

11) Minor: stray vector in `restore-history-from-serialization`
- File: `civs.io`
- Issue: stray `[history]` expression; harmless but confusing.

12) Naming/style nits
- `next_id` vs idiomatic `next-id`; field/property access via `(.field obj)` mixed with keyword access; could be normalized.

---

### Performance and Complexity

- The simulation performs per-group per-turn computations with local searches (`land-cells-around`, `cells-around`) using cartesian products. For modest map sizes and group counts this is fine. For large simulations, consider:
  - Memoizing environmental lookups at turn granularity (biome/temperature/humidity) where immutable.
  - Using counted collections to avoid repeated traversals.
  - Pre-filtering candidate migration targets with heuristics or sampling when the candidate set is large.

---

### Test Coverage & Determinism

- Acceptance tests validate population bounds across biomes and long runs; unit tests cover helpers, IO roundtrips, and basic model behaviors.
- RNG overrides via `with-redefs` provide determinism.
- Gaps: no tests exercise settlement update path after language evolution (would catch the `:settelements` bug), migration probability structure, and chiefdom evolution typo.

---

### Design Improvements (Recommended)

- **Inject RNG** via `game` or a passed-in RNG to avoid globals and allow isolated, parallel simulations.
- **Eliminate `def` mutation** in hot paths; prefer locals, atoms, or threading state.
- **Normalize collections**: always use pure Clojure data ops (`count` over `.size`, `assoc`/`update` over dotted Java-style when not needed).
- **Event system**: consider tagging events with explicit types and schemas; make event order configurable; emit probabilities for observability.
- **I/O paths**: honor readable vs binary flag; document world resolution and failure modes; avoid global `print-method` redefinition side effects by scoping.
- **Societal evolution**: add intermediate forms and rules as needed (currently minimal, with placeholders in `activities/*`, `culture`, `politic`).

---

### Suggested Patches (concise)

- Fix activity choice:
```clojure
(defn chosen-activity [game group pos]
  (->> (known-activities game group)
       (map (fn [a] {:activity a
                     :prosperity (prosperity-in-pos-per-activity game group pos a)}))
       (apply max-key :prosperity)
       :activity))
```

- Replace `.size` with `count` in sequence contexts; audit `group-in-pos`, `n-groups-alive`, `n-ghost-cities`, acceptance tests call paths.

- `update-settlement` key:
```clojure
(assoc game :settlements settlements)
```

- Chiefdom typo:
```clojure
(set-society game group :chiefdom)
```

- Migration probability `cond` (see above) and same sorting issue for destination preferences (should select the highest preference):
```clojure
(->> possible-destinations
     (map (fn [p] {:preference (perturbate-low (prosperity-in-pos game group p))
                   :pos p}))
     (apply max-key :preference)
     :pos)
```

- `check-in-range` parentheses; ensure both bounds are checked.

- Throw constructions `(Exception.)` wherever used.

- Honor `--readable-format` in IO by branching to a tagged-edn path via `miner.tagged`.

---

### Portability Notes (if porting into Godot/.NET)

- Records map well to C# classes/records; prefer immutable structs for value-like types.
- Thread RNG via dependency injection to match test determinism.
- Replace fressian with .NET serializer (e.g., MessagePack or System.Text.Json with custom converters) and keep the :unchanged delta approach if needed.
- Preserve turn facts for debugging/telemetry in-engine; a ring buffer per turn can limit memory.

---

### Conclusion

The project is a compact, well-structured simulation baseline with a clear domain model and useful test scaffolding. Addressing the listed correctness issues will substantially improve fidelity and reliability. Beyond fixes, modest refactors around state handling, RNG injection, and IO option honoring will make the system more maintainable and portable.
