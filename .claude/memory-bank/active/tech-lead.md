# Tech Lead Memory Bank

**Purpose**: High-level architectural principles and lessons learned. NOT for current tasks or session logs.

---

## 🔍 Core Principle: Search Godot-Native First

Before designing ANY system, ask:
1. "Has Godot solved this already?"
2. "Is there a built-in feature for this use case?"
3. "Can I search: 'Godot 4 [use case] tutorial'?"

**Pattern**:
- ❌ Don't immediately design custom solutions
- ✅ Research Godot docs/tutorials first
- ✅ Use native features when available
- ✅ Only build custom if no native solution exists

**Example**: TileSet custom data layers exist for tile metadata - no need to build custom JSON parsers or hardcoded registries.

---

## 🎨 TileSet is a Database, Not Just Sprites

**Key Features**:
- **Custom data layers**: Attach metadata per tile (string, float, int, bool)
- **Multi-cell tiles**: Tiles can span 2×1, 1×3, 2×2 cells
- **Atlas coordinates**: Reference by grid position, not pixels
- **Visual editor**: Designers modify without code

**Use Cases**:
- Item catalogs with properties (weight, stackable, etc.)
- Terrain properties (movement cost, damage)
- Enemy/NPC variants with stats
- Procedural generation data

**Pattern**: Store primitives in Core (atlas coords as ints), load TileSet in Infrastructure, maintain ADR-002 separation.

---

## 🏗️ Metadata-Driven Design Pattern

**When to Use**:
- Designer-modifiable data (item stats, terrain properties)
- Content that should be moddable without recompilation
- Properties that vary per entity instance

**Godot Tools**:
- TileSet custom data (for tile-based catalogs)
- Resource custom properties (for other entities)
- Inspector visual editing

**Benefits**: Designer independence, single source of truth, hot reload support

---

## 🔗 Key Godot Resources

**TileSet Documentation**:
- https://docs.godotengine.org/en/stable/tutorials/2d/using_tilesets.html
- Section: "Assigning custom metadata to the TileSet's tiles"

**Custom Data Tutorial**:
- https://gamedevartisan.com/tutorials/godot-fundamentals/tilemaps-and-custom-data-layers

**Key APIs**:
- `TileSetAtlasSource.GetTileData(coords, alt)` → TileData
- `TileData.GetCustomData(layer_name)` → Variant
- `TileSetAtlasSource.GetTileTextureRegion(coords)` → Rect2

---

## 🏛️ Feature Breakdown: Presentation/Core Separation (CRITICAL)

**ADDED 2025-10-04**: After TD_004 analysis found 500+ lines of business logic leaked into Presentation, Tech Lead must enforce architectural boundaries during task breakdown.

### Core Principle for Task Creation

`✶ Architectural Rule ─────────────────────────────`
**When breaking down features, ALWAYS ask:**
1. "What business logic is needed?" → **Core tasks** (queries/commands)
2. "What visual feedback is needed?" → **Presentation tasks** (rendering only)

**The Boundary**:
- **Core decides WHAT** (business rules, calculations, validation)
- **Presentation renders HOW** (pixel math, Godot APIs, visual feedback)
`─────────────────────────────────────────────────`

### Pattern: Feature Breakdown Template

**For EVERY feature requiring visual UI, create TWO task groups:**

**1. Core Tasks (Business Logic)** - Phase 1-3 in phased implementation:
- [ ] Create Domain entities/value objects (Phase 1)
- [ ] Create Command/Query + Handler (Phase 2)
- [ ] Create Repository/Service if needed (Phase 3)
- [ ] Unit tests for all business logic

**2. Presentation Tasks (Rendering Only)** - Phase 4:
- [ ] Create Godot node component
- [ ] Call Core queries/commands via MediatR
- [ ] Render results (pixel math + Godot APIs)
- [ ] NO business logic duplication

**Example - Drag-Drop Highlight Feature**:

❌ **WRONG Breakdown** (Causes Logic Leak):
```markdown
### Task: Implement drag-drop highlights
- [ ] Create highlight rendering in InventoryContainerNode
- [ ] Calculate which cells to highlight based on item shape
- [ ] Apply rotation to shape
- [ ] Handle equipment slot 1×1 override
- [ ] Render green/red sprites
```
**Problem**: ALL logic in Presentation (shape rotation, equipment override = business logic!)

✅ **CORRECT Breakdown** (Enforces Separation):
```markdown
### Core Tasks (Phase 1-2):
- [ ] Create `CalculateHighlightCellsQuery` (returns List<GridPosition>)
- [ ] Handler: Rotate item shape based on rotation parameter
- [ ] Handler: Apply equipment slot 1×1 override if needed
- [ ] Unit tests: Rotation, equipment slots, L-shapes

### Presentation Tasks (Phase 4):
- [ ] Update `RenderDragHighlight` to call CalculateHighlightCellsQuery
- [ ] For each cell in result, render TextureRect at pixel position
- [ ] Delete shape rotation logic (moved to Core)
- [ ] Delete equipment slot override (moved to Core)
```
**Result**: Business logic in Core (testable), Presentation just renders (dumb)

### Red Flags in Task Descriptions

**If you see these in Presentation tasks, move to Core:**

1. ❌ "Calculate occupied cells" → Create `GetOccupiedCellsQuery`
2. ❌ "Rotate shape" → Core query accepts rotation parameter
3. ❌ "Check if equipment slot" → Core exposes `IsEquipmentSlot` property
4. ❌ "Validate placement" → Use existing `CanPlaceItemAtQuery`
5. ❌ "Determine centering" → Core provides `RenderPosition`
6. ❌ "Decide swap vs move" → Create `SwapOrMoveItemCommand`
7. ❌ "Handle item type validation" → Core query validates

**Rule**: If task requires understanding **game rules**, it's a **Core task**.

### Common Feature Breakdown Patterns

#### Pattern 1: Visual Feedback (Highlights, Indicators)

**Feature**: "Show L-shape highlight when dragging item"

**Breakdown**:
- **Core**: `CalculateHighlightCellsQuery(itemId, position, rotation)` → `List<GridPosition>`
  - Logic: Get item shape, rotate, apply container rules, return cell positions
- **Presentation**: Call query, render TextureRect at each cell position
  - Logic: ONLY pixel math (Grid → Pixel coordinates)

#### Pattern 2: User Actions (Drag-Drop, Click)

**Feature**: "Swap items in equipment slots"

**Breakdown**:
- **Core**: `SwapOrMoveItemCommand(sourceContainer, targetContainer, sourceItem, targetPos, rotation)` → `Result`
  - Logic: Determine swap vs move, validate, execute with rollback
- **Presentation**: Call command on `_DropData`, show success/error feedback
  - Logic: ONLY Godot API integration (parse drag data, convert Vector2 → GridPosition)

#### Pattern 3: State Display (Health Bars, Inventory Grids)

**Feature**: "Display items in inventory grid"

**Breakdown**:
- **Core**: `GetInventoryStateQuery(actorId)` → `InventoryStateDto { Items, OccupiedCells, RenderPositions }`
  - Logic: Calculate occupied cells, determine render positions (centered vs origin)
- **Presentation**: Call query, render sprites at provided positions
  - Logic: ONLY rendering (create TextureRect, set position/texture)

### Task Breakdown Checklist

**Before finalizing task breakdown, verify:**

1. ✅ **Core tasks don't mention Godot APIs** (no Vector2, TextureRect, AddChild)
2. ✅ **Presentation tasks don't mention business rules** (no validation, calculation, decision logic)
3. ✅ **Each Core query/command has clear input/output** (strongly typed DTOs)
4. ✅ **Presentation tasks reference specific Core queries** ("Call CanPlaceItemAtQuery")
5. ✅ **No duplication between Core and Presentation** (logic lives in ONE place)

### Real Example: TD_004 Lessons

**What Happened**: SpatialInventoryContainerNode had **7 logic leaks** (500+ lines of business logic in Presentation)

**Root Cause**: Tasks were broken down as "Implement inventory rendering" without specifying Core queries needed.

**Fix**: TD_004 created **3 Core queries** to extract ALL business logic:
1. `CalculateHighlightCellsQuery` (shape rotation + equipment override)
2. `GetItemRenderDataQuery` (occupied cells + render positions)
3. `SwapOrMoveItemCommand` (swap/move decision + rollback)

**Result**: Presentation shrank from 1372 lines → 800 lines (500+ lines of business logic moved to Core)

**Lesson**: **Explicit Core tasks prevent logic leaks** - if task breakdown doesn't mention query creation, Dev Engineer defaults to putting logic in Presentation!

### When to Create New Queries vs Reuse

**Create NEW query when**:
- Feature needs data not provided by existing queries
- Existing query returns too much data (performance concern)
- Business logic calculation is unique to this feature

**Reuse existing query when**:
- Data already available (e.g., `CanPlaceItemAtQuery` for validation)
- Same business logic needed (e.g., occupied cell calculation)
- Adding parameter to existing query is simpler than new query

**Example**: Highlight rendering reuses `ItemShape` from Core but needs NEW query (`CalculateHighlightCellsQuery`) because existing placement query doesn't return rotated cell positions.

### Cross-Reference

**For detailed implementation patterns**, see:
- [dev-engineer.md: Presentation Layer Architecture](dev-engineer.md#presentation-layer-architecture-dumb-rendering-principle-critical) - Complete patterns for Presentation/Core separation
- Cache-Driven Anti-Pattern (what NOT to do)
- 7 Common Logic Leaks (specific patterns to avoid)
- Query-Based Pattern (correct approach with examples)

**Tech Lead's Role**: Create tasks that enforce these patterns, Dev Engineer implements following patterns.

---

**Last Updated**: 2025-10-04 08:22 (Added: Feature Breakdown: Presentation/Core Separation - task breakdown patterns from TD_004 lessons, prevents 500+ line logic leaks)
