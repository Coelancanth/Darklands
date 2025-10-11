## Platec Porting Decision – C++ Interop vs C# Port vs Rust Rewrite

Date: 2025-10-09
Context: We consume platec’s heightmap (and maps) as inputs to our PCG pipeline. We’re evaluating whether to keep the current native approach, fully port to C#, or re-implement in Rust.

---

### Goals and Constraints
- Deterministic outputs per seed (stable inputs to PCG pipeline).
- Good throughput for 512×512 to 2048×2048 worlds.
- Cross-platform build (Windows now; potentially Linux/macOS later).
- Maintainability and debugging inside our Godot C# stack.
- Low risk of output drift (biome thresholds, hydrology inputs rely on elevation distributions).

---

### Option A — Keep C++ platec with Interop (current pattern)
- How: Prebuilt DLL + C API (P/Invoke) or C++/CLI wrapper. Provide snapshot getters; marshal arrays into C#.
- Pros:
  - Zero algorithm risk; retains current performance and fidelity.
  - Minimal work: just tighten API (destroy leak, snapshot function, RNG seed) and packaging.
  - Easy to keep using in PCG pipeline; stable heightmap statistics.
- Cons:
  - Native build toolchain/packaging friction; CI needed for platforms.
  - Debugging across FFI is clunkier than managed code.
  - Memory copy overhead when marshaling large arrays (mitigate with pinned buffers or file-backed snapshots if needed).

When to choose: Now, for production stability and speed. Best risk/benefit today.

---

### Option B — Full C# Port
- How: Port `lithosphere/plate/movement/mass/segments` to idiomatic C#. Use `float[]` (row-major), `Parallel.For` for per-plate passes, `System.Numerics.Vector` for hot loops.
- Pros:
  - First-class debugging, profiling, and testing in our C# stack.
  - Simplified distribution (no native DLLs). Cross-platform via .NET.
  - Easier determinism control and reproducible tests.
- Cons:
  - Significant engineering effort; high risk of subtle output drift.
  - Performance work may be needed to match C++ on large worlds.
  - Must port and validate all restart/collision/subduction edge cases.

When to choose: Later, if we need deep customization, easier maintenance, and can afford parity work. Guard with golden-image tests.

---

### Option C — Rust Rewrite (FFI to C#)
- How: Re-implement in Rust; expose C ABI via `cbindgen`; consume via P/Invoke.
- Pros:
  - Performance + safety; good cross-platform story.
  - Clean modern codebase for long-term.
- Cons:
  - Highest upfront cost and FFI plumbing; steepest ramp-up.
  - Same interop/debug friction as C++ (and a new toolchain).
  - Parity risk similar to C# port.

When to choose: Only if we plan broader native reuse beyond C# and want Rust across the generation stack.

---

### Trade-off Summary
- Value/Speed-to-Production: C++ interop > C# port ≈ Rust.
- Maintainability in our stack: C# > Rust ≈ C++.
- Performance confidence: C++ ≥ Rust ≥ tuned C#.
- Output stability (low drift risk): C++ interop.

---

### Recommendation
- Short term (MVP through TD_009/010/011): Keep C++ platec and refactor the API minimally.
  - Add: `destroy` fix, snapshot getter (height/plates/age + dims), RNG seed parameterization, optional boundary/kinematics batch getters.
  - Package: CI-produced DLLs; version and checksum snapshots; write golden-image tests for elevation histograms.
- Medium term: Experiment with a C# port behind a feature flag when pipeline stabilizes.
  - Build golden comparisons (mean/variance/quantiles/edge cases) to ensure parity.
  - Parallelize cautiously; lock in determinism gates before swapping.
- Rust: Not recommended now; revisit only with a broader native agenda.

---

### Migration Plan (Practical)
1) Stabilize native API
   - Snapshot getter; fix destroy; seed injection; optional batched getters for plate velocities and IDs.
2) Guardrails
   - Golden tests: elevation quantiles per biome-relevant thresholds; restart behavior; plate counts over time.
3) Packaging
   - CI build matrix for Win (and Linux/macOS if needed); publish artifacts; load per-platform in Godot C#.
4) Optional C# prototype
   - Port `Movement`/`Mass` + overlay path; compare outputs at 512×512 for N seeds; keep off by default.

---

### Bottom Line
- It’s okay to port to C#, but not necessary now. Given we already consume platec’s heightmap for PCG, the least risky and most efficient path is: keep C++ platec with a cleaned-up API, add guardrail tests, and revisit a managed port after our worldgen pipeline is complete and stable.
