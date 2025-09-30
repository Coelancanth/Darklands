# DIBootstrapTest - Phase 4 UI Setup Instructions

## Overview
The DIBootstrapTest scene now supports optional debug UI elements:
- **Clear Button**: Clears the log output
- **Category Filters**: Checkboxes to enable/disable log categories in real-time

These are **optional** - the scene works fine without them (graceful degradation).

## Adding UI Nodes in Godot Editor

### Option 1: Quick Test (No UI Nodes)
Just run the scene as-is. The debug UI setup will detect missing nodes and skip gracefully.

### Option 2: Add Clear Button

1. Open `TestScenes/DIBootstrapTest.tscn` in Godot Editor
2. Add a Button node as a child of the root
3. Rename it to `ClearButton`
4. Set button text: "Clear Logs"
5. Position it near the log output
6. Save and run

The code will automatically wire up the button to clear the RichTextLabel.

### Option 3: Add Category Filter Checkboxes

1. Open `TestScenes/DIBootstrapTest.tscn` in Godot Editor
2. Add a VBoxContainer node as a child of the root
3. Rename it to `CategoryFilters`
4. Position it (e.g., left side of screen)
5. Optionally add a Label above it: "Log Categories:"
6. Save and run

The code will automatically:
- Discover all categories from the codebase (Combat, Movement, AI, Infrastructure, etc.)
- Create a CheckBox for each category
- Set initial state based on enabled categories
- Wire up toggle handlers to enable/disable filtering

## Node Structure Example

```
DIBootstrapTest (Node2D)
├─ StatusLabel (Label)          # Existing
├─ TestButton (Button)          # Existing
├─ LogOutput (RichTextLabel)    # Existing
├─ ClearButton (Button)         # NEW - Optional
└─ CategoryFilters (VBoxContainer)  # NEW - Optional
   └─ (CheckBoxes created dynamically)
```

## How It Works

The `SetupDebugUI()` method uses `GetNodeOrNull()` which means:
- If nodes exist → UI features are enabled
- If nodes missing → Scene runs normally without those features
- No crashes, no warnings, just works

## Testing

### Without UI Nodes
```bash
# Just run the scene
# You'll see: "Setting up Debug UI (VS_003 Phase 4)"
# UI elements won't be created (nodes not found)
```

### With Clear Button
```bash
# Click "Clear Logs" button
# Log output clears
# Console shows: "✅ Log output cleared"
```

### With Category Filters
```bash
# Uncheck "Combat" checkbox
# Combat logs disappear from output
# Console shows: "❌ Disabled category: Combat"

# Check "Combat" checkbox again
# Combat logs resume appearing
# Console shows: "✅ Enabled category: Combat"
```

## Progressive Enhancement

This is a **progressive enhancement** pattern:
1. **Base functionality**: Logging works without any UI
2. **Enhanced with Clear**: Add clear button for convenience
3. **Full debug power**: Add category filters for granular control

You can add these incrementally as needed!

## Categories Auto-Discovered

The system automatically finds categories from your codebase:
- Scans `Darklands.Core` assembly
- Looks for `Commands.{Category}` and `Queries.{Category}` patterns
- Creates checkboxes for each unique category found
- No manual configuration needed!

Current categories:
- **Infrastructure**: DI, logging, core services
- **Combat**: Attack, damage, combat mechanics (when implemented)
- **Movement**: Position, pathfinding (when implemented)
- **AI**: Behavior, decisions (when implemented)
- **Network**: Multiplayer, sync (when implemented)

New categories appear automatically as you add CQRS handlers!