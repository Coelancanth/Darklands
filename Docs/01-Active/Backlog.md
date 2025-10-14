# Darklands Development Backlog


**Last Updated**: 2025-10-14 15:55 (Tech Lead: TD_027 updated - Added PipelineBuilder + feedback loop architecture)

**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 010
- **Next TD**: 028
- **Next VS**: 031


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

### TD_027: WorldGen Pipeline Refactoring (Strategy + Builder + Feedback Loops)
**Status**: Proposed (unblocks plate lib rewrite + particle erosion + feedback loops)
**Owner**: Tech Lead → Dev Engineer (approved for implementation)
**Size**: L (12-14h)
**Priority**: Critical (blocks particle-based erosion + alternative plate implementations + iterative refinement)
**Markers**: [ARCHITECTURE] [WORLDGEN] [REFACTORING] [PARTICLE-EROSION] [FEEDBACK-LOOPS]

**What**: Refactor `GenerateWorldPipeline` from monolithic 330-line orchestrator to stage-based architecture with PipelineBuilder, supporting BOTH single-pass and iterative feedback loop modes.

**Why**:
- **Plate lib rewrite requirement** - Need to A/B test alternative plate algorithms (`platec` vs WorldEngine port vs FastNoise vs custom)
- **Erosion reordering requirement** - Particle erosion must run BEFORE D-8 flow (erosion modifies terrain, flow reads it)
- **Feedback loop requirement** - Support iterative climate-erosion cycles (climate → erosion → climate → ... until convergence)
- **Pipeline mode selection** - Single-pass (fast preview, 2s) vs Iterative (high quality, 6-10s) with different stage orders
- **Preset system** - VS_031 debug panel needs "Fast Preview" vs "High Quality" presets
- **Current blocker** - Hardcoded dependencies + fixed stage order + no iteration support

**How** (5-phase refactoring with PipelineBuilder):

**Phase 1: Core Abstractions** (2-3h)
- Create `IPipelineStage` interface in Application/Abstractions:
  ```csharp
  public interface IPipelineStage
  {
      string StageName { get; }
      Result<PipelineContext> Execute(PipelineContext input, int iterationIndex = 0);
  }
  ```
- Create `PipelineContext` record DTO in Application/DTOs (immutable data flow with 20+ optional fields)
- Create `PipelineMode` enum in Application/Common (SinglePass, Iterative)
- Add `FeedbackIterations` property to `PlateSimulationParams` (default: 3)
- Note: `IPlateSimulator` already exists (Strategy pattern foundation ✅)

**Phase 2: Extract Pipeline Stages** (3-4h)
- Create Infrastructure/Pipeline/Stages/ folder with 7 stage implementations:
  1. `PlateGenerationStage` - Wraps IPlateSimulator (swappable!)
  2. `ElevationPostProcessStage` - Wraps ElevationPostProcessor static helper
  3. `TemperatureStage` - Climate Stage 2 (iteration-aware logging)
  4. `PrecipitationStage` - Climate Stage 3 (base precip)
  5. `RainShadowStage` - Climate Stage 4 (orographic blocking)
  6. `CoastalMoistureStage` - Climate Stage 5 (maritime enhancement)
  7. `D8FlowStage` - Flow calculation Stage 7 (runs after erosion)
- Each stage ~60-80 lines (focused, single responsibility)
- Stages accept `iterationIndex` parameter (for feedback loop awareness)
- ParticleErosionStage (future): Supports uniform spawning (iteration 0) vs weighted (iteration 1+)

**Phase 3: Pipeline Orchestrators** (2-3h)
- Create `SinglePassPipeline` class (current order: Climate → Erosion):
  - Foundation stages → Feedback stages (single pass) → Analysis stages
  - Optimized for speed (2s generation time)
- Create `IterativePipeline` class (expert's order: Erosion → Climate):
  - Foundation stages → Feedback loop (Erosion → Climate × N iterations) → Analysis stages
  - Optimized for quality (6-10s with 3-5 iterations)
- Both implement `IWorldGenerationPipeline`
- Delete monolithic `GenerateWorldPipeline.cs` (replaced by two specialized orchestrators)

**Phase 3.5: PipelineBuilder Implementation** (2-3h)
- Create `PipelineBuilder` class in Infrastructure/Pipeline with fluent API:
  - `UsePlateGenerator(IPlateTectonicsGenerator)` - Strategy pattern for plate algorithms
  - `UseSinglePassMode()` / `UseIterativeMode(int iterations)` - Mode selection
  - `AddFoundationStage()` / `AddFeedbackStage()` / `AddAnalysisStage()` - Low-level stage control
  - `UseDefaultStages(IServiceProvider)` - Auto-configure stages for selected mode
  - `UseFastPreviewPreset(IServiceProvider)` - Single-pass preset (fast, 2s)
  - `UseHighQualityPreset(IServiceProvider)` - Iterative preset (slow, 6-10s, 5 iterations)
  - `Build(ILogger)` - Constructs appropriate pipeline (Single or Iterative)
- Update `GameStrapper.cs` DI registration:
  - Register all 7 stages as Transient
  - Register pipeline via builder with config-based preset selection
  - Example: `new PipelineBuilder().UseFastPreviewPreset(sp).Build(logger)`

**Phase 4: Validation & Tests** (2-3h)
- Integration test: SinglePassPipeline produces identical result to old monolithic pipeline (same seed)
- Integration test: IterativePipeline converges (iteration 3 similar to iteration 5)
- Unit tests: Each stage in isolation (mock PipelineContext inputs)
- Unit tests: Builder produces correct pipeline configurations (preset validation)
- Regression: All 495+ existing tests GREEN (no changes needed)
- Verify backward compatibility: `IWorldGenerationPipeline` interface unchanged

**Enables Future Work:**
- ✅ **Feedback loop experimentation** (A/B test Single-Pass vs Iterative):
  ```csharp
  // Fast Preview: Climate → Erosion (single pass, 2s)
  var fast = new PipelineBuilder().UseFastPreviewPreset(sp).Build(logger);

  // High Quality: (Erosion → Climate) × 5 iterations (6-10s)
  var quality = new PipelineBuilder().UseHighQualityPreset(sp).Build(logger);

  // Generate same seed with both, compare visual quality
  CompareWorlds(fast.Generate(params), quality.Generate(params));
  ```
- ✅ **Algorithm A/B testing** (swap plate simulators via builder):
  ```csharp
  // Test platec vs WorldEngine port
  var pipelines = new[] {
      new PipelineBuilder()
          .UsePlateGenerator(new NativePlateSimulator(...))
          .UseSinglePassMode().UseDefaultStages(sp).Build(logger),
      new PipelineBuilder()
          .UsePlateGenerator(new WorldEngineSimulator(...))  // C# port
          .UseSinglePassMode().UseDefaultStages(sp).Build(logger)
  };
  ```
- ✅ **VS_031 debug panel presets** (dropdown: "Fast Preview" | "High Quality"):
  ```csharp
  // In debug panel UI:
  var preset = _presetDropdown.SelectedValue;
  var pipeline = preset == "Fast"
      ? new PipelineBuilder().UseFastPreviewPreset(services).Build(logger)
      : new PipelineBuilder().UseHighQualityPreset(services).Build(logger);
  ```
- ✅ **Custom experimental pipelines** (researchers can try custom orders):
  ```csharp
  // Experiment: Run erosion TWICE per climate cycle
  var experimental = new PipelineBuilder()
      .UseIterativeMode(iterations: 3)
      .AddFeedbackStage(new ParticleErosionStage(...))  // First erosion
      .AddFeedbackStage(new ParticleErosionStage(...))  // Second erosion
      .AddFeedbackStage(new TemperatureStage(...))
      .Build(logger);
  ```

**Done When**:
1. ✅ `IPipelineStage` interface exists with `iterationIndex` parameter (Application/Abstractions)
2. ✅ `PipelineContext` record exists (Application/DTOs) with 20+ optional fields
3. ✅ `PipelineMode` enum exists (Application/Common: SinglePass, Iterative)
4. ✅ 7 stages implemented in Infrastructure/Pipeline/Stages/ folder (iteration-aware)
5. ✅ `SinglePassPipeline` class exists (Climate → Erosion order, ~100 lines)
6. ✅ `IterativePipeline` class exists (Erosion → Climate loop, ~120 lines)
7. ✅ `PipelineBuilder` class exists with fluent API (Infrastructure/Pipeline)
8. ✅ Builder presets work: `UseFastPreviewPreset()` and `UseHighQualityPreset()`
9. ✅ `GameStrapper.cs` updated with builder-based registration
10. ✅ Plate generator swappable via builder (IPlateSimulator strategy)
11. ✅ Integration test: SinglePassPipeline == Old monolithic pipeline (bit-identical)
12. ✅ Integration test: IterativePipeline converges (iterations 3-5 stabilize)
13. ✅ Unit tests: Builder produces correct configurations (preset validation)
14. ✅ All 495+ existing tests GREEN (zero regressions)
15. ✅ Backward compatible: `IWorldGenerationPipeline` interface unchanged

**Depends On**: None (refactoring only - no new features)

**Presentation Layer Implications** (Godot UI changes - OPTIONAL for Phase 1):

**WorldMapUINode.cs** (add ~50 lines):
- Add pipeline mode dropdown after seed input:
  ```csharp
  _pipelineModeDropdown = new OptionButton();
  _pipelineModeDropdown.AddItem("Fast Preview (2s)", (int)PipelineMode.SinglePass);
  _pipelineModeDropdown.AddItem("High Quality (6-10s)", (int)PipelineMode.Iterative);
  ```
- Add iteration count slider (visible only when Iterative mode selected):
  ```csharp
  _iterationSlider = new HSlider { MinValue = 2, MaxValue = 5, Value = 3, Step = 1 };
  // Hide/show based on pipeline mode selection
  ```

**WorldMapOrchestratorNode.cs** (line 267):
- Update `GenerateWorldCommand` to include pipeline mode:
  ```csharp
  var command = new GenerateWorldCommand(
      seed, worldSize: 512, plateCount: 10,
      pipelineMode: _currentPipelineMode,      // From UI
      feedbackIterations: _currentIterations   // From slider (default: 3)
  );
  ```

**GenerateWorldCommand.cs** (Core layer):
- Add `PipelineMode Mode` and `int FeedbackIterations` properties

**UI Implementation Decision**: Presentation layer UI changes are **OPTIONAL** for TD_027 initial delivery. Pipeline builder works headless via DI config/environment variables. UI controls can be added as **Phase 5** (post-validation) or deferred to **VS_031** debug panel integration where they better fit the parameter tuning workflow.

**Blocks**:
- Particle-based erosion implementation (needs reorderable stages + iterative mode)
- Alternative plate implementations (WorldEngine port, FastNoise, custom - needs strategy swapping)
- VS_031 debug panel (needs preset system via builder + UI integration for semantic params)
- Feedback loop validation (climate-erosion co-evolution experiments)

**Pipeline Mode Trade-Offs**:

| Mode | Stage Order | Use Case | Speed | Quality | Erosion Realism |
|------|-------------|----------|-------|---------|----------------|
| **Single-Pass** | Climate → Erosion | Fast preview, real-time iteration | 2s (512×512) | Good approximation | ✅ Precipitation-weighted spawning |
| **Iterative (3×)** | (Erosion → Climate) × 3 | Balanced quality | 6s (512×512) | High convergence | ❌ Uniform iteration 0, ✅ weighted 1+ |
| **Iterative (5×)** | (Erosion → Climate) × 5 | Final production worlds | 10s (512×512) | Maximum fidelity | ❌ Uniform iteration 0, ✅ weighted 1+ |

**Circular Dependency Analysis**:
- **Problem**: Climate needs eroded terrain (accurate rain shadows) BUT Erosion needs precipitation (weighted spawning)
- **Single-Pass Solution**: Climate BEFORE erosion (one-shot approximation, prioritizes erosion intensity realism)
- **Iterative Solution**: Erosion → Climate loop (converges to equilibrium, prioritizes climate accuracy after iteration 1)
- **Insight**: Both approaches valid - trade-off between first-shot accuracy (Single-Pass) vs convergence quality (Iterative)
- **Expert's Order Validated**: For feedback loops, Erosion → Climate is physically correct (erosion modifies terrain, climate responds)

**Tech Lead Decision** (2025-10-14 15:55 - UPDATED with PipelineBuilder + feedback loops):
- **Architecture evolved**: Initial assessment (no builder) was wrong - feedback loops require:
  1. **Multiple pipeline variants** (Single-Pass vs Iterative with different stage orders)
  2. **Preset system** (Fast Preview vs High Quality for VS_031 debug panel)
  3. **A/B testing** (compare pipeline modes and plate algorithms)
  4. **Result**: Builder pattern NOW justified (wasn't needed for simple reordering, IS needed for mode selection)
- **Patterns validated**: Strategy (swappable algorithms) + Builder (fluent configuration) + Chain of Responsibility (stage execution)
- **SOLID compliance**: All 5 principles satisfied
- **Complexity justified**: +600 lines (~500 stages + 100 builder) BUT enables THREE real requirements:
  1. Plate lib rewrite (strategy swapping via builder)
  2. Particle erosion reordering (stage-based architecture)
  3. Feedback loops (iterative mode with climate-erosion co-evolution)
- **Size increase**: 12-14h (was 8-10h, +4h for builder + iterative pipeline + convergence tests)
- **IPlateSimulator exists**: Strategy pattern foundation in place (see IPlateSimulator.cs:10) ✅
- **Folder structure**:
  - Application/Abstractions: IPipelineStage (iteration-aware)
  - Application/Common: PipelineMode enum (SinglePass, Iterative)
  - Application/DTOs: PipelineContext (immutable data flow)
  - Infrastructure/Pipeline: SinglePassPipeline, IterativePipeline, PipelineBuilder
  - Infrastructure/Pipeline/Stages/: 7 stage implementations (iteration-aware logging)
  - ElevationPostProcessor, HydraulicErosionProcessor remain as static utilities
- **Pipeline mode decision**: Support BOTH orders (not mutually exclusive):
  - Single-Pass (Climate → Erosion): Fast, one-shot approximation, prioritizes precipitation-weighted spawning
  - Iterative (Erosion → Climate × N): Slow, converges to equilibrium, prioritizes rain shadow accuracy
- **Expert's insight adopted**: For feedback loops, Erosion → Climate is correct (mirrors physical causality)
- **Circular dependency broken**: Both modes break the Climate ↔ Erosion feedback loop differently
- **Backward compatibility guaranteed**: IWorldGenerationPipeline unchanged, all 495+ tests pass
- **Risk**: Low-medium (refactoring + new mode, mitigated by 100% test coverage + integration tests)
- **Next step**: Approve for Dev Engineer implementation (12-14h estimate with builder + feedback loops)

---

### TD_024: Basin Detection & Inner Sea Depth System
**Status**: Proposed (Foundation for VS_030 water body classification)
**Owner**: Tech Lead
**Size**: M (6-8h)
**Priority**: Ideas (prerequisite for VS_030 Phase 1)
**Markers**: [ARCHITECTURE] [WORLDGEN] [REFACTORING]

**What**:

**Why**: VS_030 requires basin metadata (pour points, water surface elevation, boundaries) to classify ocean vs inner seas. Current code only calculates ocean depth; inner seas need depth maps for thalweg pathfinding cost function.

**How**:

**Done When**:


**Depends On**: TD_023 ✅ (pit-filling basin preservation complete)

**Blocks**: VS_030 Phase 1 (water body classification needs basin metadata)

---

---

## 📈 Important (Do Next)
*Core features for current milestone, technical debt affecting velocity*

### VS_030: Inner Sea Flow via Lake Thalweg Pathfinding
**Status**: Proposed (Requires TD_021 foundation)
**Owner**: Dev Engineer
**Size**: L (14-18h)
**Priority**: Important (fixes D-8 flow topology for inner seas)
**Markers**: [WORLDGEN] [ALGORITHM] [PATHFINDING] [HYDRAULIC-EROSION]

**What**: Implement Dijkstra-based pathfinding to compute river thalweg (deepest channel) paths through inner seas, enabling correct D-8 flow topology for landlocked water bodies.

**Why**:
- **D-8 Problem**: Inner seas are flat surfaces (elevation < sea level) → D-8 has no gradient → becomes sink → breaks flow accumulation
- **Current State**: Rivers terminate incorrectly at inner sea boundaries (flow "stuck" at lakes like Caspian Sea)
- **Physical Reality**: Rivers flow THROUGH lakes along deepest channels (thalweg) to outlets, not terminate at inlets
- **Pathfinding Solution**: Pre-compute optimal paths (inlet → outlet) using depth-based cost, hard-code flow directions for inner sea cells
- **CRITICAL CORRECTION**: Water body classification MUST happen AFTER pit-filling (pit-filling DEFINES lakes with boundaries/water level/outlets)

**How** (5-phase implementation with CORRECT pipeline order):

**Phase 1: Water Body Classification from Pit-Filling Results** (3-4h)
- **CRITICAL**: This happens AFTER pit-filling, which DEFINES lakes hydrologically
- Create `WaterBodyMasks.cs` DTOs (OceanMask, InnerSeaMask, InnerSeaRegions)
- Implement `WaterBodyClassifier.ClassifyFromPitFilling()`:
  - Input: FilledHeightmap + OriginalHeightmap + PitFillingMetadata (basins identified by pit-filling)
  - Algorithm:
    - For each basin from pit-filling (has boundary, water surface elevation, pour point):
      - If pour point at sea level AND connected to map edges → Ocean
      - If pour point above sea level OR landlocked → Inner Sea
    - Group inner seas into regions (connected components)
    - Detect inlets: Boundary cells where land D-8 flow enters lake
    - Detect outlet: The pour point identified by pit-filling
  - Output: WaterBodyMasks (ocean mask, inner sea mask, inner sea regions with inlets/outlets)
- Why this works: Pit-filling already solved the "leaky lake" and "archipelago of sinks" problems by finding pour points

**Phase 2: Lake Thalweg Pathfinder** (3-4h)
- Implement `LakeThalwegPathfinder.ComputePaths()` (Dijkstra with depth-based cost)
- Cost function: `Cost = 1 / (lakeSurfaceElevation - originalHeightmap[cell] + ε)` → Prefers deeper water
- For each inner sea region (from Phase 1):
  - For each inlet → Find path to outlet (Dijkstra)
  - Result: List of path segments (x, y, nextX, nextY)
- Output: `Dictionary<InnerSeaId, List<PathSegment>>` (thalweg paths for all inner seas)
- Optimization: Use BFS/distance transform to pre-compute "flow to nearest thalweg" for non-path lake cells

**Phase 3: Hybrid Flow Direction Calculator** (2-3h)
- Modify `FlowDirectionCalculator.CalculateWithThalwegs()`:
  - Land cells: D-8 steepest descent (unchanged)
  - Ocean cells: dir = -1 (sink)
  - Inner sea cells ON thalweg path: Use pre-computed direction from pathfinding
  - Inner sea cells NOT on thalweg: Flow toward nearest thalweg cell (from Phase 2 optimization)
- Result: Hybrid flow directions (D-8 + pathfinding + sinks)

**Phase 4: Pipeline Integration** (2-3h)
- **CORRECTED PIPELINE ORDER**:
  ```
  Stage 2: Pit-filling (FIRST - DEFINES lakes)
    → Output: FilledHeightmap + PitFillingMetadata (basins with pour points)

  Stage 2.5 (NEW): Water Body Classification
    → Input: FilledHeightmap + OriginalHeightmap + PitFillingMetadata
    → Output: WaterBodyMasks (ocean/inner sea + regions with inlets/outlets)

  Stage 2.6 (NEW): Thalweg Pathfinding
    → Input: InnerSeaRegions (from Stage 2.5)
    → Output: ThalwegPaths (Dijkstra-computed paths)

  Stage 3 (MODIFIED): Hybrid Flow Direction Calculator
    → Input: FilledHeightmap + WaterBodyMasks + ThalwegPaths
    → Output: FlowDirections (D-8 + thalweg + sinks)

  Stage 4-5: Topological Sort + Flow Accumulation (unchanged)
  ```
- Update `PitFillingCalculator` to expose basin metadata (pour points, boundaries)
- Update `WorldGenerationResult` to include `WaterBodyMasks` and `ThalwegPaths`
- Wire through to rendering

**Phase 5: Visualization and Testing** (3-4h)
- Add view modes:
  - `OceanMask`: Show ocean cells (blue)
  - `InnerSeaMask`: Show inner sea cells (teal/cyan - distinguish from ocean)
  - `LakeThalwegs`: Show computed paths through inner seas (red lines on water)
  - `ThalwegDepths`: Heat map of lake depths (deeper=red, shallow=blue)
- Unit tests: Water classification correctness (ocean vs inner sea after pit-filling)
- Unit tests: Pathfinding finds valid paths (inlet → outlet)
- Visual validation: Flow accumulation shows rivers flowing THROUGH inner seas
- Visual validation: Thalweg paths curve naturally (follow deepest channels)
- Performance testing: Total overhead <50ms (classification + pathfinding)

**Done When**:
1. ✅ Pit-filling exposes basin metadata (pour points, boundaries, water surface elevations)
2. ✅ `WaterBodyClassifier` classifies basins AFTER pit-filling (ocean vs inner sea)
3. ✅ Inner sea regions have correct inlets (where land flow enters) and outlets (pour points)
4. ✅ `LakeThalwegPathfinder` computes depth-based paths for all inner sea regions
5. ✅ Dijkstra cost function uses depth (prefers thalweg channels)
6. ✅ `FlowDirectionCalculator` hybrid approach works (D-8 + thalweg + sinks)
7. ✅ Flow accumulation shows rivers passing THROUGH inner seas (not terminating at boundaries)
8. ✅ 4 new view modes render correctly (OceanMask, InnerSeaMask, LakeThalwegs, ThalwegDepths)
9. ✅ Unit tests: Pathfinding correctness (finds paths, follows depth gradient)
10. ✅ Visual validation: No "leaky lake" errors (all classified lakes are true basins per pit-filling)
11. ✅ All existing tests GREEN (no regression)
12. ✅ Performance: <50ms total overhead for 512×512 map (~5-10 inner seas)

**Depends On**:
- TD_021 ✅ (must be complete - provides SSOT constant and normalized scale)
- TD_023 ✅ (must be complete - provides basin metadata for water classification)
- VS_029 ✅ (flow visualization exists for validation)

**Blocks**: Nothing (hydraulic erosion foundation - enables future particle erosion)

**Enables**:
- Future particle-based erosion (rivers erode along thalweg paths)
- Future sediment deposition (sediment accumulates in deep lake basins)
- Future navigation system (ships follow thalweg channels)

**Dev Engineer Decision** (2025-10-13 18:38):
- **CRITICAL CORRECTION**: Original VS_030 had water classification BEFORE pit-filling → CAUSALITY INVERSION ERROR
- **Key Insight from tmp.md**:
  - "Pit-filling is not just filling - it's a DEFINITION tool"
  - Pour point defines outlet, pour point elevation defines water surface, basin below pour point defines lake boundary
  - Cannot classify "what is a lake?" before pit-filling answers this question
- **Correct Order**: Pit-filling (defines) → Classify (ocean vs inner sea) → Pathfind (thalweg) → Flow calculation (hybrid)
- **Leaky Lake Prevention**: Classifying from pit-filling results prevents misclassifying river valleys as lakes
- **Archipelago Prevention**: Pit-filling already merged micro-sinks into coherent basins with single pour points
- **Size Increase**: 14-18h (was 12-16h) due to Phase 1 added (water classification moved from TD_021)
- **Approach Still Valid**: Pathfinding over land bridge (excellent visual quality + reusable paths)
- **Next Step**: Implement TD_021 (foundation), expose pit-filling metadata, then implement VS_030 with correct pipeline

---

## 💡 Ideas (Future Work)
*Future features, nice-to-haves, deferred work*



### VS_033: MVP Item Editor (Weapons + Armor Focus)
**Status**: Proposed (Build AFTER manual item creation phase)
**Owner**: Product Owner → Tech Lead (breakdown) → Dev Engineer (implement)
**Size**: L (15-20h)
**Priority**: Ideas (deferred until designer pain validated)
**Markers**: [TOOLING] [DESIGNER-UX]

**What**: Minimal viable Godot EditorPlugin for creating weapon/armor ItemTemplates with auto-wired i18n and component-based UI.

**Why**:
- **Eliminate CSV pain** - Designer never manually edits CSV files (auto-generates translation keys, auto-syncs en.csv)
- **Component selection UI** - Check boxes for Equippable + Weapon/Armor (vs manual SubResource creation in Inspector)
- **Validation before save** - Catch errors (duplicate keys, missing components) BEFORE runtime
- **80% of content** - Weapons + armor are most items, validates tooling investment

**Strategic Phasing** (do NOT skip ahead):
1. **Phase 1: Validate Pain** (2-4h manual work) - REQUIRED FIRST
   - After VS_032 complete, designer creates 10-20 items manually in Godot Inspector
   - Designer documents pain points: "Inspector tedious", "CSV editing error-prone", "No validation until runtime"
   - **Trigger**: Designer reports "Inspector workflow is too tedious for 20+ items"

2. **Phase 2: Build MVP** (15-20h) - ONLY if Phase 1 pain validated
   - Build EditorPlugin with 5 core features (see "How" below)
   - Focus: Weapons + armor ONLY (defer consumables/tools to Phase 3)
   - Effort: 15-20h (vs 30h full Item Editor by deferring advanced features)

3. **Phase 3: Expand** (when >30 items exist) - Future
   - Add consumables, tools, containers support
   - Add balance tools (DPS calculator, usage tracking)
   - Add batch operations (create 10 variants)

**How** (MVP scope - 5 core features):
1. **Component Selection UI** (3-4h) - Checkboxes: ☑ Equippable, ☑ Weapon, ☑ Armor (auto-show properties)
2. **Quick Templates** (2-3h) - Presets: "Weapon" (Equippable+Weapon), "Armor" (Equippable+Armor), "Shield" (Equippable+Armor+Weapon)
3. **Auto-Wired i18n** (4-5h) - Designer types "Iron Sword" → auto-generates ITEM_IRON_SWORD → auto-writes en.csv (ZERO manual CSV editing!)
4. **Component Validation** (3-4h) - "Weapon requires Equippable", "Duplicate key ITEM_IRON_SWORD", offer auto-fix
5. **Live Preview** (2-3h) - Show sprite + stats + translation key preview

**Deferred for MVP** (add in Phase 3):
- ❌ Balance comparison (DPS calculator, power curves)
- ❌ Usage tracking (which ActorTemplates use this item)
- ❌ Batch operations (create N variants)
- ❌ Consumables/tools support (weapons + armor = 80% of content)

**Done When** (Phase 2 - MVP):
- ✅ Designer creates iron_sword.tres in 2 minutes (vs 5+ minutes Inspector)
- ✅ Component selection via checkboxes (no manual SubResource creation)
- ✅ Zero manual CSV editing (auto-generates ITEM_IRON_SWORD, writes to en.csv)
- ✅ Validation before save (catches duplicate keys, missing components)
- ✅ Works for weapons + armor (can create sword, plate armor, shield)
- ✅ Designer reports: "Item Editor is MUCH faster than Inspector"

**Depends On**:
- VS_032 ✅ (must be complete - validates equipment system works)
- Phase 1 manual item creation (10-20 items) - validates pain is REAL, not hypothetical

**Blocks**: Nothing (tooling is parallel track - doesn't block gameplay features)

**Product Owner Decision** (2025-10-10):
- **Do NOT build until Phase 1 pain validated** - Must create 10-20 items manually first to validate:
  1. Component-based ItemTemplate architecture works (can actors equip items?)
  2. Inspector workflow pain is REAL (not hypothetical)
  3. CSV editing pain is REAL (manual key management sucks)
- **Rationale**: Tools solve REAL pain, not imaginary pain. Must feel pain before building solution.
- **Risk mitigation**: If Phase 1 shows "Inspector is workable", defer Item Editor and create more items (avoid 15-20h investment for low ROI).
- **Scope discipline**: MVP focuses weapons + armor (80% of content). Defer consumables/tools until 30+ items exist (avoid premature generalization).

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