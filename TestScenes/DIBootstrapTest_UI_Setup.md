# DIBootstrapTest - Phase 4 UI Setup Instructions

## Overview
The DIBootstrapTest scene now supports optional debug UI elements:
- **F12 Toggle**: Press F12 to show/hide the debug panel
- **Clear Button**: Clears the log output
- **Category Filters**: Checkboxes to enable/disable log categories in real-time

These are **optional** - the scene works fine without them (graceful degradation).

## Adding UI Nodes in Godot Editor

### Option 1: Quick Test (No UI Nodes)
Just run the scene as-is. The debug UI setup will detect missing nodes and skip gracefully.
- Press F12 → Console message explains DebugPanel node is missing (optional)

### Option 2: Add F12 Toggle (Recommended)

1. Open `TestScenes/DIBootstrapTest.tscn` in Godot Editor
2. Add a Control node (or Panel/MarginContainer) as a child of the root
3. Rename it to `DebugPanel`
4. Add all debug UI as children of this panel (LogOutput, ClearButton, CategoryFilters)
5. Position the panel where you want it
6. Optionally: Set its initial visibility to `false` (starts hidden)
7. Save and run

Now pressing F12 will toggle the entire debug panel visibility!

### Option 3: Add Clear Button

1. Open `TestScenes/DIBootstrapTest.tscn` in Godot Editor
2. Add a Button node as a child of the root
3. Rename it to `ClearButton`
4. Set button text: "Clear Logs"
5. Position it near the log output
6. Save and run

The code will automatically wire up the button to clear the RichTextLabel.

### Option 4: Add Category Filter Checkboxes

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

**Without F12 Toggle** (simple):
```
DIBootstrapTest (Node2D)
├─ StatusLabel (Label)          # Existing
├─ TestButton (Button)          # Existing
├─ LogOutput (RichTextLabel)    # Existing
├─ ClearButton (Button)         # NEW - Optional
└─ CategoryFilters (VBoxContainer)  # NEW - Optional
   └─ (CheckBoxes created dynamically)
```

**With F12 Toggle** (recommended):
```
DIBootstrapTest (Node2D)
├─ StatusLabel (Label)          # Existing
├─ TestButton (Button)          # Existing
└─ DebugPanel (Control/Panel)   # NEW - F12 toggles this!
   ├─ LogOutput (RichTextLabel) # Move inside DebugPanel
   ├─ ClearButton (Button)      # Move inside DebugPanel
   └─ CategoryFilters (VBoxContainer)  # Move inside DebugPanel
      └─ (CheckBoxes created dynamically)
```

**Key Insight**: Put all debug UI inside DebugPanel to toggle everything at once with F12!

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