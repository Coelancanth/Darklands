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

### Presentation Layer Responsibilities (CRITICAL)

`✶ Architectural Principle ─────────────────────`
**Presentation layer should NEVER duplicate business logic!**

**Golden Rule**: If it requires logic or validation, **delegate to Core via Query/Command**.

**What Presentation SHOULD Do**:
- ✅ Capture user input (mouse clicks, keyboard)
- ✅ Call Core queries/commands via MediatR
- ✅ Display results (render sprites, update UI)
- ✅ Convert between Godot types and Core types (Vector2 → GridPosition)
- ✅ Handle Godot-specific events (_Ready, _Process, _Input)

**What Presentation SHOULD NOT Do**:
- ❌ Validate placement collision (use CanPlaceItemAtQuery)
- ❌ Calculate occupied cells (use ItemShape from Core)
- ❌ Check business rules (type compatibility, bounds checking)
- ❌ Iterate shapes to determine collision (Core owns this logic)
- ❌ Duplicate Domain/Application logic

**Red Flags in Presentation Code**:
```csharp
// ❌ BAD - Duplicating collision logic
for (int dy = 0; dy < height; dy++)
{
    for (int dx = 0; dx < width; dx++)
    {
        if (_itemsAtPositions.Contains(...)) // Business logic!
    }
}

// ✅ GOOD - Delegating to Core
var query = new CanPlaceItemAtQuery(actorId, itemId, position, rotation);
var result = await _mediator.Send(query);
bool canPlace = result.Value; // Simple boolean, no logic
```

**Why This Matters**:
1. **Single Source of Truth**: Logic lives in ONE place (Core)
2. **Testability**: Business logic tested in Core tests (fast, isolated)
3. **Maintainability**: Changes to rules don't require updating UI code
4. **Bug Prevention**: Can't have "works in Core but fails in UI" scenarios

**Example - BR_004 L-Shape Collision Bug**:
- **Problem**: Presentation iterated bounding box (4 cells) instead of OccupiedCells (3 cells)
- **Root Cause**: Duplicated collision logic that already existed in Domain
- **Fix**: Use CanPlaceItemAtQuery - Core owns validation, Presentation just displays result
- **Lesson**: Never duplicate logic "for performance" - measure first, optimize if needed

**Pattern for Validation in Presentation**:
```csharp
public override bool _CanDropData(Vector2 atPosition, Variant data)
{
    // Convert Godot types → Core types
    var targetPos = PixelToGridPosition(atPosition);
    var itemId = ExtractItemIdFromDragData(data);

    // Delegate validation to Core
    var query = new CanPlaceItemAtQuery(
        OwnerActorId!.Value,
        itemId,
        targetPos.Value,
        _sharedDragRotation);

    var result = _mediator.Send(query).Result; // Blocking OK for UI validation

    // Display result (green/red highlights)
    UpdateHighlights(result.Value);

    return result.Value; // Core tells us, we just listen
}
```

**Decision Rule**: "Does this require understanding game rules?" → If yes, delegate to Core!
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

**Last Updated**: 2025-10-03 22:40 (Added: Presentation Layer Responsibilities - Never duplicate business logic!)
