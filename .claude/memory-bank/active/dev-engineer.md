# Dev Engineer Memory Bank

**Purpose**: Reusable implementation patterns and technical reminders. NOT for current tasks or session logs.

---

## 🔄 Workflow Protocol

**MANDATORY: Update Backlog BEFORE Committing Each Phase**

1. Complete phase implementation + tests
2. Update `Docs/01-Active/Backlog.md` with progress (5-7 lines)
3. Stage files: `git add` (code + tests + backlog)
4. Commit: `feat(feature): Description [Phase X/4]`

---

## 🎯 Root Cause First Principle (CRITICAL)

**Added**: 2025-10-04 (TD_005 - Lessons from BR_006/BR_007)

**Before implementing ANY bug fix, ask:**
1. **"Is this a workaround or a root cause fix?"**
2. **"What's the actual underlying problem?"**
3. **"Will this fix prevent similar bugs, or just patch this one?"**

### Workaround vs Root Cause Fix

**Workaround** (Tactical Patch):
- ✅ **When to use**: Critical production bug, need immediate fix
- ✅ **Pattern**: Fix symptom → Document as TD → Schedule proper fix
- ⚠️ **Warning**: Accumulates technical debt, doesn't prevent recurrence
- **Examples from BR_006/BR_007**:
  - Mouse warp hack (0.1px movement to trigger _CanDropData)
  - Shape override in Presentation (equipment slot 1×1 force)
  - CustomMinimumSize for sprite centering

**Root Cause Fix** (Architectural Solution):
- ✅ **When to use**: Have time to fix properly, architectural issue identified
- ✅ **Benefits**: Prevents entire class of bugs, reduces complexity
- ✅ **Examples**: TD_003 (separate components), TD_004 (move logic to Core)

### BR_006/BR_007 Case Study

**Symptoms**:
- BR_006: Rotation highlights didn't update during cross-container drag
- BR_007: Equipment slots showed L-shape highlights instead of 1×1

**Tactical Fixes** (Workarounds):
- BR_006: Mouse warp hack (`Input.WarpMouse(mousePos + Vector2(0.1, 0))`)
- BR_007: Shape override + CustomMinimumSize in Presentation

**Root Cause** (Architectural):
- Single Responsibility Principle violation: `SpatialInventoryContainerNode` (1372 lines) combines TWO incompatible UI patterns
  - Equipment Slot: Single-item swap, 1×1 display, centered sprites
  - Inventory Grid: Multi-cell Tetris, rotation, collision detection
- Business logic in Presentation: 500+ lines of shape calculation, equipment slot rules, swap logic (7 distinct leaks)

**Proper Fix** (Already Planned):
- **TD_003**: Separate `EquipmentSlotNode` (~400 lines) from `InventoryContainerNode` (~800 lines)
- **TD_004**: Move ALL business logic to Core (queries return results, Presentation just renders)
- **Result**: Workarounds disappear naturally when components are separated and logic is in Core

### Implementation Protocol

**When you encounter a bug:**

1. **Understand the symptom** (what the user sees)
2. **Identify the root cause** (why it happens architecturally)
3. **Choose approach**:
   - **Emergency path**: Workaround now + TD for proper fix
   - **Proper path**: Fix root cause (if time permits)
4. **Document decision**:
   - Workaround: Add comment `// WORKAROUND (BR_XXX): [explanation] - See TD_YYY for proper fix`
   - Root cause: Add comment `// ROOT CAUSE FIX (BR_XXX): [architectural change]`

**Example - BR_006 Documentation**:
```csharp
// WORKAROUND (BR_006): Force _CanDropData re-run via mouse warp
// Root cause: Cross-container drag state management complexity
// Proper fix: TD_003 (separate components) + TD_004 (query-based highlights)
Input.WarpMouse(mousePos + new Vector2(0.1f, 0));
```

### UX Pattern Recognition

**ADDED**: 2025-10-04 (TD_005)

**When implementing UI features, watch for these red flags:**

1. **Combining Incompatible Patterns**:
   - ❌ Equipment slot (swap, centered) + Inventory grid (Tetris, multi-cell) in ONE component
   - ❌ Different collision rules in same validation method (`if (isEquipmentSlot)` branches scattered)
   - ✅ Separate components for different interaction patterns

2. **Business Logic in Presentation**:
   - ❌ Shape rotation in Presentation
   - ❌ Equipment slot override logic in rendering code
   - ❌ Collision detection duplicated from Core
   - ✅ Query Core for results, render what Core provides

3. **Special Case Accumulation**:
   - ⚠️ If you add 2nd `if (containerType == X)` branch → time to separate components
   - ⚠️ If you add 3rd business rule → time to move logic to Core
   - **Rule**: 2+ special cases = architectural smell

**Pattern**: When you find yourself writing "but equipment slots need special handling here too..." → STOP. That's a sign of SRP violation. Consider:
- Should this be a separate component? (TD_003 pattern)
- Should this logic be in Core? (TD_004 pattern)

---

## 📋 Requirement Clarification Protocol (CRITICAL)

**ADDED**: 2025-10-04 (TD_005)

**Before starting ANY implementation, repeat the requirement back to the user in your own words.**

### Why This Matters
- Prevents misunderstandings early (cheaper to clarify now than fix later)
- Ensures you understand the actual need (not just the stated request)
- Builds trust (user sees you're listening and thinking)

### Pattern

**User says**: "Fix the equipment slot highlights"

**You respond**:
> "Just to confirm: You want equipment slots to show a single 1×1 highlight when dragging items, instead of showing the item's full L-shape (3 cells). Is that correct?"
>
> "Also, should this apply to ALL equipment slots (weapon, armor, ring), or just weapon slots?"

**User confirms/corrects**: "Yes, exactly. All equipment slots should work this way."

**Then implement**: Now you know the exact scope and requirements.

### When to Ask Clarifying Questions

**Always ask when:**
- ✅ Requirement mentions visual/UX changes (subjective - confirm the vision)
- ✅ Multiple ways to interpret the request (ambiguous - clarify scope)
- ✅ Edge cases not specified (boundaries - define expected behavior)
- ✅ Performance requirements unclear (optimization - confirm targets)

**Examples of good clarifying questions**:
- "Should this work during cross-container drag, or just within the same container?"
- "What should happen if the user rotates while over an invalid position?"
- "Do we need to preserve rotation when swapping items?"

**Red flag**: If you're guessing how a feature should work → ASK FIRST.

---

## 🎓 Core Patterns

### TDD Discipline
- Write failing test first (RED)
- Minimal implementation (GREEN)
- Refactor if needed
- Use `[Trait("Category", "PhaseX")]` for phase-specific runs
- Phase 1 tests must run <10ms (pure domain)

### User Testing Protocol (CRITICAL)
**ALWAYS add temporary verbose logging during user testing, reduce when confirmed!**

**Why This Matters**:
- Need confirmation that operations actually executed (especially silent multi-step operations)
- Helps identify WHEN bugs occur (before/after which step)
- Rich formatting (Serilog) provides structured data for debugging

**Pattern - Temporary LogInformation (Upgrade to LogDebug After Testing)**:
```csharp
// TEMPORARY: User testing - upgrade to LogInformation for visibility
_logger.LogInformation("🔄 SWAP: Starting swap operation for {SourceItem} ↔ {TargetItem}",
    sourceItemId, targetItemId);

// ... operation code ...

_logger.LogInformation("✅ SWAP: Completed successfully");

// After testing confirms feature works → Downgrade to LogDebug
_logger.LogDebug("Swap operation for {SourceItem} ↔ {TargetItem}", sourceItemId, targetItemId);
```

**When to Add Verbose Logging (LogInformation)**:
- ✅ Complex operations (swap, multi-step transactions)
- ✅ Silent operations (no visual feedback yet)
- ✅ Critical data operations (prevent data loss)
- ✅ During bug investigation (trace execution flow)
- ✅ New feature validation (confirm it actually runs)

**When to Reduce Verbosity (LogInformation → LogDebug)**:
- ✅ After user confirms feature works correctly
- ✅ Before PR/merge (reduce log noise in production)
- ✅ Keep statements, just change log level

**Example - VS_018 Swap Testing**:
```csharp
// TEMPORARY: User testing confirmation (LogInformation for visibility)
_logger.LogInformation("🔄 Removing {SourceItem} from source container", sourceItemId);
var removeSourceResult = await _mediator.Send(removeSourceCmd);

if (removeSourceResult.IsSuccess)
    _logger.LogInformation("✅ Source item removed successfully");
else
    _logger.LogError("❌ FAILED to remove source: {Error}", removeSourceResult.Error);

// After testing confirms swap works:
// Change LogInformation → LogDebug for production cleanliness
_logger.LogDebug("Removed {SourceItem} from source", sourceItemId);
```

**Benefits of Logger over GD.Print**:
- ✅ Structured logging (parameters captured separately)
- ✅ Rich formatting (colors, emojis, structured data)
- ✅ Persistent (logged to files, not just console)
- ✅ Filterable (log levels, categories)
- ✅ Professional (no need to delete, just adjust level)

**Red Flag**: User says "I tried but nothing happened" → Upgrade relevant logs to LogInformation

### Regression Tests (CRITICAL)
**ALWAYS create regression tests for bug fixes!**

**When to Create Regression Test**:
- ✅ Data loss bugs (items disappearing, state corruption)
- ✅ Logic errors that passed existing tests (test coverage gap)
- ✅ User-reported bugs (real-world scenarios missed by unit tests)
- ✅ Race conditions or timing issues
- ✅ Edge cases discovered during manual testing

**Regression Test Pattern**:
```csharp
[Fact]
public async Task Handle_BugScenario_ShouldNotCauseDataLoss()
{
    // REGRESSION TEST: Brief description of bug
    // WHY: Explain what was broken and why it matters
    // Bug scenario (pre-fix):
    // 1. Step-by-step reproduction
    // 2. What went wrong
    // 3. Impact (data loss, crash, etc.)
    // Fix: Brief description of solution

    // Arrange: Set up exact bug scenario
    // Act: Execute the operation that previously failed
    // Assert: Verify BOTH success AND data integrity
}
```

**Example** (VS_018 Data Loss Bug):
- **Bug**: Items disappeared when type validation failed
- **Test Name**: `Handle_FailedTypeValidation_ShouldNotRemoveItemFromSource`
- **Key Assertions**:
  - Command fails as expected
  - Item **remains in source** (data preserved!)
  - Item **not in target** (no side effects)

**Pragmatic Approach**:
1. Fix critical bug first (emergency response)
2. Add regression test immediately after (prevents recurrence)
3. Commit separately: `fix(...)` then `test(...): Add regression test`

**Red Flag**: If existing tests pass but bug exists → test coverage gap → regression test needed!

### Railway-Oriented Programming
```csharp
// Functional composition eliminates manual error checking
public Result<bool> IsPassable(Position pos) =>
    GetTerrain(pos)                  // Result<TerrainType>
        .Map(t => t.IsPassable());   // Transform to Result<bool>
// Failure propagates automatically
```

---

## 🎨 TileSet Custom Data Pattern

**When to Use**: Entity catalogs with designer-modifiable properties (items, terrain, enemies).

**Key Steps**:
1. **Domain**: `CreateFromTileSet(atlasSource, x, y)` factory reads metadata
2. **Infrastructure**: Auto-discover via `GetTilesCount()` + `GetTileId(i)` loop
3. **Validate**: Check metadata exists, return `Result.Failure` if missing
4. **Store primitives**: Atlas coords as ints (ADR-002)

**Example**:
```csharp
var tileData = atlasSource.GetTileData(coords, 0);
var name = (string)tileData.GetCustomData("item_name");
if (string.IsNullOrEmpty(name))
    return Result.Failure<Item>("Missing metadata");
```

---

## 🔧 Godot C# Integration

### Node References
- ❌ Don't rely on `[Export] NodePath` auto-population
- ✅ Use explicit `GetNode<T>()` in `_Ready()`

### ServiceLocator Pattern
- ✅ Acceptable at Godot boundary (Godot instantiates nodes)
- ❌ Never use in Core layer
- Pattern: `ServiceLocator.Get<T>()` ONLY in `_Ready()`

### Logging (ADR-001)
- ❌ **NEVER use `GD.Print()` or `GD.PrintErr()`** - Not structured, no filtering, hard to maintain
- ❌ **NEVER use `System.Console.WriteLine()`** in Core - Bypasses logging infrastructure
- ✅ **ALWAYS use `ILogger<T>` from Microsoft.Extensions.Logging** - Structured, filterable, professional
- Retrieve via ServiceLocator in `_Ready()` (Presentation layer)
- Use constructor injection (Core layer)

**Why ILogger > GD.Print**:
- ✅ Structured logging (capture parameters separately for querying)
- ✅ Log levels (Debug/Info/Warning/Error filtering)
- ✅ Multiple outputs (console + file + external systems)
- ✅ Production-ready (can adjust verbosity without code changes)
- ✅ Testable (can verify logging in unit tests)

**Temp Debug Logging Protocol** (from User Testing section above):
- Use `LogInformation` during active development/debugging
- Downgrade to `LogDebug` after feature confirmed working
- NEVER use `GD.Print` or `Console.WriteLine` - use ILogger always

### Production Log Formatting (VS_007 Lessons)

**ADDED**: 2025-10-04 19:15 (Combat system logging cleanup)

**Clean Production Logs (Machine-Parseable)**:
- ❌ **NO emojis** in log messages (grep-friendly, tool-parseable)
- ❌ **NO Unicode arrows** (`→` breaks grep patterns)
- ✅ **ASCII arrows** (`->` for transitions, easy to grep)
- ✅ **Numeric values** (TimeUnits.ToString() returns "10" not "10 time units")
- ✅ **Structured parameters** (capture separately, not embedded in strings)

**Before (Development Logs)**:
```csharp
_logger.LogInformation("⏱️ Combat turn: {ActorId} moved (time: {OldTime} → {NewTime}, cost: {Cost})",
    actorId, "10 time units", "20 time units", "10 time units"); // Verbose, emojis, Unicode
```

**After (Production Logs)**:
```csharp
_logger.LogInformation("Combat turn: {ActorId} moved (time: {OldTime} -> {NewTime}, cost: {Cost})",
    actorId, oldTime, newTime, cost); // Clean, parseable, ASCII
// Output: "Combat turn: Player moved (time: 10 -> 20, cost: 10)"
```

**Value Object ToString() Pattern**:
```csharp
// ✅ CORRECT - Clean output for logging
public override string ToString() => Value.ToString(); // Returns "10" not "10 time units"
```

**Benefits**:
- ✅ `grep "Combat turn.*moved"` works (no emoji noise)
- ✅ `awk -F'time: ' '{print $2}'` extracts time values
- ✅ Log parsers don't choke on Unicode
- ✅ Professional production logs (easier for ops teams)

**When to Use Emojis**: NEVER in production code. Demo/tutorial logs only (temporary).

**Red Flag**: If your log message has `→`, `✅`, `🎯`, etc. → Replace with ASCII equivalent before merge.

### Node2D vs Control Hierarchy (CRITICAL)
**Rule**: Control containers (CenterContainer, VBoxContainer, etc.) ONLY layout Control children!

**Common Mistake**:
```csharp
// ❌ WRONG - Sprite2D (Node2D) in Control container
var sprite = new Sprite2D();
var center = new CenterContainer();
center.AddChild(sprite); // Centering won't work!
```

**Solution**:
```csharp
// ✅ CORRECT - TextureRect (Control) in Control container
var texture = new TextureRect { StretchMode = KeepAspectCentered };
var center = new CenterContainer();
center.AddChild(texture); // Centering works perfectly!
```

**When to use each**:
- `Sprite2D`: Game world objects (physics, 2D space positioning)
- `TextureRect`: UI elements (HUD, menus, inventory grids)
- Symptom of mixing: Sprites stuck at (0,0), layout properties ignored

---

## 🏗️ Architecture (ADRs)

**ADR-002**: Core has zero Godot dependencies (primitives only)
**ADR-003**: Use `Result<T>` for failable operations
**ADR-004**: Feature-based organization (Domain/Application/Infrastructure per feature)

### 🎯 SSOT Principle - Single Source of Truth (CRITICAL)

**Added**: 2025-10-04 (TD_004 - 7 logic leaks eliminated, 500+ lines of business logic moved from Presentation to Core)

`✶ Core Principle ─────────────────────────────────`
**Business logic must exist in EXACTLY ONE place - the Core layer.**

If Presentation calculates business logic, it WILL diverge from Core over time.
`─────────────────────────────────────────────────`

**SSOT in Practice**:

**❌ VIOLATION (Logic Duplication)**:
```csharp
// Core/Domain/Inventory.cs
public Result PlaceItemAt(ItemId itemId, GridPosition pos, Rotation rotation)
{
    var rotatedShape = item.Shape.RotateClockwise(rotation);  // Business logic
    foreach (var offset in rotatedShape.OccupiedCells) { ... }
}

// Components/InventoryNode.cs - DUPLICATE LOGIC!
private void RenderHighlight(ItemId itemId, Rotation rotation)
{
    var rotatedShape = _itemShapes[itemId].RotateClockwise(rotation);  // ❌ DUPLICATED!
    foreach (var offset in rotatedShape.OccupiedCells) { ... }         // ❌ DUPLICATED!
}
```

**Problem**: Shape rotation logic exists in TWO places → Will diverge when Core changes!

**✅ CORRECT (Core is SSOT)**:
```csharp
// Core/Application/Queries/CalculateHighlightCellsQuery.cs
public class CalculateHighlightCellsQueryHandler
{
    public async Task<Result<List<GridPosition>>> Handle(...)
    {
        var item = await _items.GetByIdAsync(cmd.ItemId);

        // SSOT: Shape rotation logic lives ONLY in Core
        var rotatedShape = item.Shape.RotateClockwise(cmd.Rotation);

        // Return RESULTS (what to render), not raw data
        return rotatedShape.OccupiedCells
            .Select(offset => new GridPosition(cmd.Position.X + offset.X, ...))
            .ToList();
    }
}

// Components/InventoryNode.cs - ZERO business logic
private async void RenderHighlight(ItemId itemId, Rotation rotation)
{
    // Query Core for RESULTS
    var query = new CalculateHighlightCellsQuery(itemId, position, rotation);
    var cells = await _mediator.Send(query);

    // ONLY rendering (pixel math + Godot APIs)
    foreach (var cellPos in cells.Value)
    {
        var pixelX = cellPos.X * CellSize;
        var pixelY = cellPos.Y * CellSize;
        RenderSprite(pixelX, pixelY);
    }
}
```

**Benefits**:
- ✅ Change shape rotation logic → Update ONE file (Core query handler)
- ✅ Test shape rotation → Unit test (no Godot required)
- ✅ Zero drift → Presentation always uses Core's current logic
- ✅ Complexity reduction → Presentation shrinks (TD_004: 1372 → 1208 lines, -12%)

**TD_004 Real-World Impact**:
- **Before**: 500+ lines of business logic in Presentation (7 distinct leaks)
- **After**: 164 lines eliminated, 3 Core queries created
- **Lesson**: If Presentation calculates anything related to game rules → LEAK!

**Enforcement Rule**:
> **Before merging Presentation code**: Grep for business logic patterns
> - `grep "RotateClockwise\|RotateCounterclockwise" Components/` → Must return 0
> - `grep "OccupiedCells.*foreach" Components/` → Must return 0
> - If found → Create Core query, delegate calculation

**See**: [ADR-002: Presentation/Logic Boundary](../../Docs/03-Reference/ADR/ADR-002-godot-integration-architecture.md#presentationlogic-boundary---ssot-principle) for 7 real leak examples from TD_004

### Presentation Layer Architecture: "Dumb Rendering" Principle (CRITICAL)

**CONSOLIDATED 2025-10-04**: Merged from analysis of SpatialInventoryContainerNode (1372 lines, 7 logic leaks, 500+ lines of business logic). This section codifies ALL lessons learned about Presentation/Core boundaries.

`✶ Core Principle ─────────────────────────────────`
**Presentation should be as "dumb" as possible - NEVER duplicate business logic!**

**The Boundary**:
- Presentation **renders** (HOW to display) - pixel math, Godot APIs, visual feedback
- Core **decides** (WHAT to display) - business rules, calculations, validation

**Golden Rules**:
1. **Never**: Cache Core's state and recalculate Core's logic
2. **Always**: Query Core for results, render what Core tells you
3. **Decision Rule**: "Does this require understanding game rules?" → If yes, delegate to Core!

**What Presentation SHOULD Do**:
- ✅ Capture user input (mouse clicks, keyboard) → Convert to Core types (Vector2 → GridPosition)
- ✅ Send queries/commands to Core via MediatR
- ✅ Display results from Core (render sprites, update UI, show highlights)
- ✅ Handle Godot-specific APIs (_Ready, _Process, _Input, AddChild, TextureRect)
- ✅ Pixel math (Grid → Pixel coordinates, rotation in radians)

**What Presentation SHOULD NOT Do**:
- ❌ Validate placement collision → Query Core's `CanPlaceItemAtQuery`
- ❌ Calculate occupied cells → Query Core's `GetItemRenderDataQuery`
- ❌ Rotate shapes → Core rotates, returns rotated cells
- ❌ Determine equipment slot centering → Core provides render position
- ❌ Decide swap vs move → Core command handles decision
- ❌ Check business rules (type compatibility, bounds) → Core validates
- ❌ Iterate shapes to calculate cells → Core calculates, Presentation renders
`─────────────────────────────────────────────────`

#### The Cache-Driven Architecture Anti-Pattern ❌

**What it looks like** (from SpatialInventoryContainerNode.cs):
```csharp
// ❌ ANTI-PATTERN: Caching Core state in Presentation
private Dictionary<ItemId, ItemShape> _itemShapes = new();
private Dictionary<ItemId, Rotation> _itemRotations = new();
private Dictionary<ItemId, (int, int)> _itemDimensions = new();

// Lines 640-683: Then RECALCULATING business logic using cache
var rotatedShape = shape;
for (int i = 0; i < (int)rotation; i++)
{
    rotatedShape = rotatedShape.RotateClockwise().Value; // Business logic!
}

foreach (var offset in rotatedShape.OccupiedCells)
{
    _itemsAtPositions[occupiedCell] = itemId; // Building collision map - business logic!
}
```

**Why this is wrong**:
- ❌ Presentation duplicates Core's shape rotation logic
- ❌ Presentation duplicates Core's occupied cell calculation
- ❌ If Core's logic changes, Presentation breaks (divergence risk)
- ❌ Cannot test this logic without Godot (business logic should be testable)

**The correct pattern** (Query-Based):
```csharp
// ✅ CORRECT: Query Core for RESULTS, don't recalculate
var query = new GetItemRenderDataQuery(containerId, itemId);
var result = await _mediator.Send(query);

// Presentation ONLY renders what Core calculated
foreach (var cellPos in result.OccupiedCells)
{
    RenderSprite(cellPos); // Pure rendering, no logic
}
```

**Historical Example - BR_004 L-Shape Collision Bug** (Real bug from project):
- **Problem**: Presentation iterated bounding box (4 cells) instead of OccupiedCells (3 cells)
- **Root Cause**: Cache-Driven Anti-Pattern - Presentation duplicated collision logic from Core
- **Fix**: Query Core's `CanPlaceItemAtQuery` - Core owns validation, Presentation just displays result
- **Lesson**: Never duplicate logic "for performance" - measure first, optimize if needed
- **Impact**: This same anti-pattern reappeared in 6 OTHER places (TD_004 found 500+ lines of duplicate logic!)

#### Common Logic Leaks (From TD_004 Analysis - 7 Found!)

**Leak #1: Shape Rotation**
```csharp
// ❌ WRONG - Presentation rotates shapes
var rotatedShape = baseShape;
for (int i = 0; i < rotation; i++)
    rotatedShape = rotatedShape.RotateClockwise().Value;

// ✅ RIGHT - Core calculates rotated shape
var query = new CalculateHighlightCellsQuery(itemId, position, rotation);
var cells = await _mediator.Send(query); // Core did rotation
```

**Leak #2: Occupied Cell Calculation**
```csharp
// ❌ WRONG - Presentation calculates which cells items occupy
foreach (var offset in rotatedShape.OccupiedCells)
{
    var occupiedCell = new GridPosition(origin.X + offset.X, origin.Y + offset.Y);
    _itemsAtPositions[occupiedCell] = itemId;
}

// ✅ RIGHT - Query Core for occupied cells
var renderData = await _mediator.Send(new GetItemRenderDataQuery(containerId, itemId));
foreach (var cellPos in renderData.OccupiedCells)
{
    MarkOccupied(cellPos); // Just rendering, no calculation
}
```

**Leak #3: Equipment Slot Centering**
```csharp
// ❌ WRONG - Presentation decides "equipment slots center items" (business rule!)
bool isEquipmentSlot = containerType == ContainerType.WeaponOnly && gridWidth == 1;
if (isEquipmentSlot)
{
    pixelX = (CellSize - effectiveW) / 2f; // Centering logic - business rule!
}

// ✅ RIGHT - Core tells Presentation where to render
var renderData = await _mediator.Send(new GetItemRenderDataQuery(containerId, itemId));
var pixelPos = renderData.RenderPosition; // Core decided center vs origin
RenderAt(pixelPos); // Presentation just uses the position
```

**Leak #4: Equipment Slot Detection**
```csharp
// ❌ WRONG - Business rule "what is an equipment slot?" in Presentation
bool isEquipmentSlot = _containerType == ContainerType.WeaponOnly && _gridWidth == 1 && _gridHeight == 1;
if (isEquipmentSlot) { /* special case */ }

// ✅ RIGHT - Core exposes property
var renderData = await _mediator.Send(new GetItemRenderDataQuery(containerId, itemId));
if (renderData.IsEquipmentSlot) { /* Presentation uses flag from Core */ }
```

**Leak #5: Swap vs Move Decision**
```csharp
// ❌ WRONG - Presentation decides swap vs move (business decision!)
bool isOccupied = _itemsAtPositions.TryGetValue(targetPos, out var targetItemId);
bool isEquipmentSlot = _containerType == ContainerType.WeaponOnly;

if (isOccupied && isEquipmentSlot)
    SwapItemsSafeAsync(...); // 78 lines of swap logic in Presentation!
else
    MoveItemAsync(...);

// ✅ RIGHT - Core command handles decision
await _mediator.Send(new SwapOrMoveItemCommand(
    sourceContainer, sourceItem, targetContainer, targetPos, rotation));
// Core decides swap vs move, Presentation just waits for result
```

**Leak #6: Type Validation**
```csharp
// ❌ WRONG - Business rule "weapon slots only accept weapons" in Presentation
private bool CanAcceptItemType(string itemType)
{
    if (_containerType == ContainerType.WeaponOnly)
        return itemType == "weapon"; // Business rule!
    return true;
}

// ✅ RIGHT - Already delegated to Core via CanPlaceItemAtQuery
// Remove this method entirely (dead code)
```

**Leak #7: Fallback Rectangle Calculation**
```csharp
// ❌ WRONG - Presentation handles items without shapes
var (effectiveWidth, effectiveHeight) = RotationHelper.GetRotatedDimensions(baseWidth, baseHeight, rotation);
for (int dy = 0; dy < effectiveHeight; dy++)
    for (int dx = 0; dx < effectiveWidth; dx++)
        _itemsAtPositions[new GridPosition(origin.X + dx, origin.Y + dy)] = itemId;

// ✅ RIGHT - Core handles ALL shape types (L-shapes AND rectangles)
var renderData = await _mediator.Send(new GetItemRenderDataQuery(containerId, itemId));
foreach (var cellPos in renderData.OccupiedCells) // Core handled fallback
    MarkOccupied(cellPos);
```

#### Red Flags: Logic Leaks in Presentation

**If you see these patterns, move logic to Core:**

1. ❌ **Iterating shapes to calculate cells**: `foreach (var offset in shape.OccupiedCells)`
   - **Fix**: Query Core for cell positions

2. ❌ **Rotating shapes**: `shape.RotateClockwise()`
   - **Fix**: Core rotates, returns rotated cells

3. ❌ **Equipment slot special cases**: `if (isEquipmentSlot) { ... }`
   - **Fix**: Core returns `IsEquipmentSlot` flag, Presentation uses it

4. ❌ **Swap/move decision logic**: `if (isOccupied && isEquipmentSlot) SwapAsync() else MoveAsync()`
   - **Fix**: Single Core command handles decision

5. ❌ **Caching Core state**: `_itemShapes`, `_itemRotations`, `_itemDimensions`
   - **Fix**: Query Core for results when needed (performance is <1ms!)

#### The Query-Based Pattern (Correct Approach)

**Principle**: Presentation queries Core for **results**, not **state**.

**Example - Rendering Items**:
```csharp
// Step 1: Query Core for render data
var query = new GetItemRenderDataQuery(containerId, itemId);
var renderData = await _mediator.Send(query);

// Result from Core:
// - OccupiedCells: List<GridPosition> (Core calculated!)
// - RenderPosition: GridPosition (Core decided center vs origin!)
// - IsEquipmentSlot: bool (Core determined!)
// - Rotation: Rotation (Core's source of truth!)

// Step 2: Presentation ONLY does pixel math and rendering
var pixelPos = GridToPixel(renderData.RenderPosition);
var sprite = CreateSprite(itemId);
sprite.Position = pixelPos;
sprite.Rotation = ToRadians(renderData.Rotation);

// Step 3: Render occupied cells (for debugging/highlights)
foreach (var cellPos in renderData.OccupiedCells)
{
    var highlight = CreateHighlight();
    highlight.Position = GridToPixel(cellPos);
    AddChild(highlight);
}
```

**What Presentation did**:
- ✅ Grid → Pixel conversion (presentation concern)
- ✅ Creating Godot nodes (presentation API)
- ✅ Setting positions/rotation (rendering)

**What Presentation did NOT do**:
- ❌ Calculate which cells are occupied (Core did it)
- ❌ Rotate the shape (Core did it)
- ❌ Decide center vs origin (Core did it)
- ❌ Determine equipment slot status (Core did it)

**Godot-Specific Pattern: _CanDropData Integration**

**How to integrate with Godot's drag-drop API** (complete example):
```csharp
public override bool _CanDropData(Vector2 atPosition, Variant data)
{
    // STEP 1: Convert Godot types → Core types (Presentation responsibility)
    var targetPos = PixelToGridPosition(atPosition); // Pixel → Grid conversion
    var itemId = ExtractItemIdFromDragData(data); // Parse Godot variant

    if (targetPos == null || itemId == null)
        return false; // Invalid Godot data

    // STEP 2: Delegate ALL validation to Core (zero business logic in Presentation!)
    var query = new CanPlaceItemAtQuery(
        OwnerActorId!.Value,
        itemId,
        targetPos.Value,
        _sharedDragRotation);

    var result = _mediator.Send(query).Result; // Blocking OK for UI validation
    bool isValid = result.IsSuccess && result.Value;

    // STEP 3: Display visual feedback (Presentation responsibility)
    UpdateHighlights(targetPos.Value, itemId, _sharedDragRotation, isValid);
    // ↳ Even UpdateHighlights should query Core for WHAT cells to highlight!
    //   See CalculateHighlightCellsQuery pattern

    return isValid; // Core decided, Presentation just returns result
}
```

**Key Points**:
- ✅ Presentation handles Godot API (Vector2, Variant parsing)
- ✅ Presentation does pixel → grid conversion (presentation concern)
- ✅ Core validates placement (business logic)
- ✅ Presentation shows visual feedback (rendering)
- ❌ Presentation does NOT duplicate validation logic

#### Performance: "But Queries Are Slow!"

**Reality check** (from TD_004 analysis):
```
Query overhead: ~0.5ms per call (in-memory repository)
Mouse move events: ~30/second during drag
Frame budget (60 FPS): 16.67ms

Query cost: 0.5ms × 30 = 15ms/second
Percentage of budget: 15ms / 16.67ms = ~3% per frame

Verdict: IMPERCEPTIBLE ✅
```

**Trade-off**:
- 3% performance cost (imperceptible)
- 100% architectural purity (SSOT, testable, maintainable)

**Decision**: Architecture > Micro-optimization

**When to optimize**: If profiling shows queries >50% of frame time (unlikely for in-memory repositories).

#### Concrete Impact: TD_004 Cleanup

**Before TD_004** (Cache-Driven Anti-Pattern):
- 1372 lines in SpatialInventoryContainerNode
- 7 logic leaks (500+ lines of business logic)
- Equipment slot logic in 4 different places
- Cannot test without Godot

**After TD_004** (Query-Based Pattern):
- ~800 lines (500+ lines deleted!)
- Zero business logic (verified by grep)
- All equipment slot logic in Core (single place)
- Business logic testable without Godot

**Lesson**: "Dumb Rendering" isn't just cleaner - it **halves your Presentation code**!

**Why This Architecture Matters** (Summary):
1. **Single Source of Truth (SSOT)**: Business logic lives in ONE place (Core) - change once, works everywhere
2. **Testability**: Business logic tested in Core tests (fast, isolated, no Godot dependency)
3. **Maintainability**: Rule changes don't require updating UI code (Core query handles it)
4. **Bug Prevention**: Can't have "works in Core but fails in UI" scenarios (Core is source of truth)
5. **Code Reduction**: TD_004 proved this - 1372 lines → 800 lines (500+ lines of duplicate logic removed!)
6. **Performance**: 3% overhead for 100% architectural purity - worth the trade-off

#### Summary: The Dumb Rendering Checklist

**Before writing Presentation code, ask:**

1. ❓ "Am I calculating something?" → Query Core instead
2. ❓ "Am I making a business decision?" → Query/Command to Core
3. ❓ "Am I iterating shapes/cells?" → Query Core for results
4. ❓ "Do I have `if (isEquipmentSlot)`?" → Core should expose flag
5. ❓ "Am I caching Core's state?" → Query for results when needed

**If answer is YES to any**: Move logic to Core, query for results.

**Remember**: Presentation should feel **boring** - just pixel math and `AddChild()` calls. If it feels clever, you're probably doing Core's job!

`─────────────────────────────────────────────────`

---

## 📝 Test Naming

```
MethodName_Scenario_ExpectedBehavior

Examples:
- Create_ValidValues_ShouldReturnSuccess
- GetTerrain_OutOfBounds_ShouldReturnFailure
```

**When to Comment Tests**:
- ✅ Business rules, bug regressions, edge cases, architecture constraints
- ❌ Don't comment obvious behavior

---

## 🔗 Quick Commands

```bash
# Test execution
dotnet test --filter "Category=Phase1"
dotnet test tests/Darklands.Core.Tests/Darklands.Core.Tests.csproj

# Git workflow
git checkout -b feat/VS_XXX-description
./scripts/git/branch-status-check.ps1
git commit -m "feat(feature): Description [Phase X/4]"
```

---

**Last Updated**: 2025-10-04 19:15 (VS_007 follow-ups: Added Combat Log Formatting guidelines - clean production logs without emojis, TimeUnits.ToString() returns numeric values, ASCII arrows for machine-parseable output)
