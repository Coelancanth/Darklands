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
**ALWAYS add temporary info messages during user testing, remove when confirmed!**

**Why This Matters**:
- User can't see internal logs during Godot testing
- Need confirmation that operations actually executed
- Silent success feels like silent failure to users
- Helps identify WHEN bugs occur (before/after which step)

**Pattern - Temporary GD.Print Messages**:
```csharp
// Add during user testing phase
_logger.LogInformation("Swap initiated...");
GD.Print("🔄 SWAP: Starting swap operation");  // ← USER SEES THIS

// ... operation code ...

GD.Print("✅ SWAP: Completed successfully");  // ← CONFIRMATION
_logger.LogInformation("Swap completed");

// Remove after testing confirms feature works
```

**When to Add Info Messages**:
- ✅ Complex operations (swap, multi-step transactions)
- ✅ Silent operations (no visual feedback yet)
- ✅ Critical data operations (prevent data loss)
- ✅ During bug investigation (trace execution flow)
- ✅ New feature validation (confirm it actually runs)

**When to Remove Info Messages**:
- ✅ After user confirms feature works correctly
- ✅ Before PR/merge (keep codebase clean)
- ✅ Keep logger statements (for debugging), remove GD.Print

**Example - VS_018 Swap Testing**:
```csharp
// TEMPORARY: User testing confirmation messages
GD.Print($"🔄 Removing {sourceItemId} from source...");
var removeSourceResult = await _mediator.Send(removeSourceCmd);
if (removeSourceResult.IsSuccess)
    GD.Print($"✅ Source item removed");
else
    GD.Print($"❌ FAILED to remove source: {removeSourceResult.Error}");

// After testing confirms swap works → Delete GD.Print lines, keep _logger
```

**Red Flag**: User says "I tried it but nothing happened" → Add info messages to show execution

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
- ❌ Never use `GD.Print()` or `GD.PrintErr()`
- ✅ Always use `ILogger<T>` from Microsoft.Extensions.Logging
- Retrieve via ServiceLocator in `_Ready()`

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

**Last Updated**: 2025-10-03
