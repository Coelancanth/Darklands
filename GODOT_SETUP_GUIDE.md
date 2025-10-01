# Godot 4.4 Setup Guide - VS_005 Grid FOV Test Scene

## Overview
This guide helps you complete the Godot 4.4 editor setup for the Grid FOV test scene.
The Core architecture is complete and event-driven - this is just the visual layer.

## Prerequisites
✅ Core architecture complete (all tests passing)
✅ GridTestSceneController.cs created
✅ GridTestScene.tscn template created
✅ Kenney assets in `assets/micro-roguelike/`

---

## Step 1: Configure Pixel-Perfect Rendering (~1 minute)

### 1.1 Set Global Texture Filter (For Pixel Art)
1. **Menu Bar** → **Project** → **Project Settings**
2. Search for: `default_texture_filter`
3. Find: **Rendering → Textures → Canvas Textures → Default Texture Filter**
4. Set to: **Nearest** (disables texture blurring for crisp pixels)
5. **Default Texture Repeat**: Should be **Disable**
6. Click **Close**

### 1.2 Verify Asset Import
The Kenney tilemap should already be imported correctly. Verify:

1. Navigate to `assets/micro-roguelike/` in FileSystem dock
2. Click `colored_tilemap.png`
3. In Import dock (right side), should show:
   - **Import As**: Texture2D ✅
   - **Compress Mode**: Lossless ✅
   - **Fix Alpha Border**: On ✅

**Note**: Texture filtering is controlled by Project Settings (Step 1.1), not import settings.

---

## Step 2: Create TileSet Resource (~3 minutes)

### 2.1 Create TileSet
1. In **FileSystem dock**, right-click `assets/` folder
2. Select **"New Resource"**
3. Search for **"TileSet"**
4. Click **"Create"**
5. Save as `assets/grid_tileset.tres`
6. **Double-click** `grid_tileset.tres` to open TileSet editor (bottom panel)

### 2.2 Add Atlas Source
1. In TileSet editor (bottom panel), click **"+ Add" button**
2. Select **"Atlas"** from the dropdown
3. A file browser opens - select `assets/micro-roguelike/colored_tilemap.png`
4. The atlas should now appear in the TileSet editor

### 2.3 Configure Atlas Grid (IMPORTANT!)

With the atlas selected in the left panel, configure these settings in the **right Inspector panel**:

**Base Tile Settings:**
- **Texture Region Size**: `8 x 8` (size of each tile)
- **Use Texture Padding**: OFF
- **Separation**: `1 x 1` (spacing between tiles in the source image)

**After setting these values**, the grid overlay should align perfectly with the tile boundaries in the preview.

### 2.4 Verify Grid Alignment
- You should see a grid overlay on the tilemap preview
- Grid lines should align with tile edges (not cutting through tile centers)
- 16 columns × 10 rows = 160 tiles total
- **Save** (Ctrl+S)

**Note**: In Godot 4.4, you don't manually "select" individual tiles. The entire atlas is automatically available once the grid is configured correctly.

---

## Step 3: Configure Test Scene (~5 minutes)

### 3.1 Open Scene
1. Open `TestScenes/GridTestScene.tscn`
2. Scene tree should show:
   ```
   GridTestScene (Node2D)
   ├─ TerrainLayer (TileMapLayer)
   ├─ FOVLayer (TileMapLayer)
   ├─ Player (Sprite2D)
   └─ Dummy (Sprite2D)
   ```

### 3.2 Assign TileSet to Layers

**TerrainLayer:**
1. Select **"TerrainLayer"** node in scene tree
2. In Inspector, find **TileMapLayer** section
3. **Tile Set** property: Click and select `assets/grid_tileset.tres` (or drag from FileSystem)

**FOVLayer:**
1. Select **"FOVLayer"** node in scene tree
2. In Inspector → **Tile Set**: Select `assets/grid_tileset.tres`

### 3.3 Configure FOV Layer Visual
1. Select **"FOVLayer"** node
2. In Inspector → **CanvasItem** section:
   - **Visibility** → **Modulate**: Set to semi-transparent yellow
     - R: 255, G: 255, B: 128, A: 80 (adjust to taste)
   - This makes FOV tiles visible as an overlay

### 3.4 Verify Sprites
- **Player** sprite should show green tint
- **Dummy** sprite should show red tint
- Both use an 8×8 region from the tilemap

### 3.5 Save Scene
- **Ctrl+S** to save

---

## Step 4: Update GridTestSceneController (~10 minutes)

The controller needs to use **atlas coordinates** instead of tile IDs. Godot 4.4 uses `(x, y)` coordinates to reference tiles in the atlas.

### 4.1 Understanding Atlas Coordinates

Looking at the Kenney tilemap (16 tiles wide):
- **Top-left tile**: Atlas coords `(0, 0)`
- **Row 1, Col 0**: Atlas coords `(0, 1)` ← Floor tile
- **Row 6, Col 0**: Atlas coords `(0, 6)` ← Smoke tile

### 4.2 Add Tile Constants

Open `GridTestSceneController.cs` and add these constants after line 35:

```csharp
private const int TileSize = 8;
private const int VisionRadius = 8;

// Atlas coordinates for terrain tiles (Kenney Micro Roguelike tileset)
// Format: new Vector2I(column, row)
private static readonly Vector2I WallAtlasCoord = new(0, 0);   // Top-left: solid gray block
private static readonly Vector2I FloorAtlasCoord = new(0, 1);  // Row 1, Col 0: simple floor
private static readonly Vector2I SmokeAtlasCoord = new(0, 6);  // Row 6, Col 0: cloud sprite
private static readonly Vector2I FOVAtlasCoord = new(4, 3);    // Row 3, Col 4: bright green tile
```

### 4.3 Add Terrain Rendering to InitializeGameState()

Find the `InitializeGameState()` method. After the terrain commands, add rendering code:

```csharp
private async void InitializeGameState()
{
    // ... existing code (CreateActorIDs, SetTerrainCommands, RegisterActors) ...

    // Register actors at starting positions
    var playerStartPos = new Position(5, 5);
    var dummyStartPos = new Position(20, 20);

    await _mediator.Send(new RegisterActorCommand(_playerId, playerStartPos));
    await _mediator.Send(new RegisterActorCommand(_dummyId, dummyStartPos));

    // Update sprites to initial positions
    _playerSprite.Position = GridToPixel(playerStartPos);
    _dummySprite.Position = GridToPixel(dummyStartPos);

    // === ADD THIS: Render all terrain to TileMap ===
    RenderAllTerrain();

    // Calculate initial FOV for player
    await _mediator.Send(new MoveActorCommand(_playerId, playerStartPos));

    GD.Print("Grid Test Scene initialized!");
    GD.Print("Controls: Arrow Keys = Player, WASD = Dummy, Tab = Switch FOV view");
}
```

### 4.4 Add RenderAllTerrain() Helper Method

Add this new method to the class (anywhere after `InitializeGameState`):

```csharp
/// <summary>
/// Renders the entire 30x30 grid terrain to the TileMap.
/// Called once during initialization.
/// </summary>
private void RenderAllTerrain()
{
    for (int x = 0; x < 30; x++)
    {
        for (int y = 0; y < 30; y++)
        {
            // Determine terrain type (matches SetTerrainCommand logic)
            Vector2I atlasCoord;

            // Edges are walls
            if (x == 0 || x == 29 || y == 0 || y == 29)
            {
                atlasCoord = WallAtlasCoord;
            }
            // Smoke patches
            else if ((x == 10 && y == 10) || (x == 10 && y == 11) || (x == 11 && y == 10))
            {
                atlasCoord = SmokeAtlasCoord;
            }
            // Interior walls
            else if (y == 15 && x >= 5 && x < 10)
            {
                atlasCoord = WallAtlasCoord;
            }
            // Everything else is floor
            else
            {
                atlasCoord = FloorAtlasCoord;
            }

            // Render to TileMap: SetCell(grid_position, source_id, atlas_coords)
            _terrainLayer.SetCell(new Vector2I(x, y), 0, atlasCoord);
        }
    }

    GD.Print("Terrain rendered: 30x30 grid");
}
```

### 4.5 Update OnFOVCalculated() Method

Find the `OnFOVCalculated()` method and replace it with:

```csharp
/// <summary>
/// Event handler: FOV calculated - update visibility overlay.
/// Only update if this is the active actor's FOV.
/// </summary>
private void OnFOVCalculated(FOVCalculatedEvent evt)
{
    if (!evt.ActorId.Equals(_activeActorId))
        return; // Only show active actor's FOV

    // Clear previous FOV overlay
    _fovLayer.Clear();

    // Highlight visible tiles using atlas coordinates
    foreach (var pos in evt.VisiblePositions)
    {
        // SetCell parameters: (grid_coords, source_id, atlas_coords)
        _fovLayer.SetCell(
            new Vector2I(pos.X, pos.Y),  // Grid position
            0,                            // Source ID (atlas index, 0 = first/only atlas)
            FOVAtlasCoord                // Atlas coordinates for FOV tile
        );
    }

    GD.Print($"FOV updated: {evt.VisiblePositions.Count} positions visible");
}
```

### 4.6 Verify Complete Controller

Your controller should now have:
- ✅ Atlas coordinate constants
- ✅ `RenderAllTerrain()` method
- ✅ Updated `OnFOVCalculated()` method
- ✅ Call to `RenderAllTerrain()` in `InitializeGameState()`

**Save the file** (Ctrl+S)

---

## Step 5: Build and Test (~5 minutes)

### 5.1 Build Solution
1. Open terminal in project root
2. Run: `dotnet build Darklands.csproj`
3. Should complete with **0 errors**

### 5.2 Run Test Scene in Godot
1. In Godot, open `TestScenes/GridTestScene.tscn`
2. Press **F6** (Run Current Scene) or click "Run Current Scene" button
3. The scene should launch

### 5.3 Manual Test Checklist

**Visual Checks:**
- [ ] **30×30 grid renders** - Walls around edges (gray blocks)
- [ ] **Interior walls visible** - Horizontal wall at row 15, columns 5-9
- [ ] **Smoke patches visible** - Cloud sprites at (10,10), (10,11), (11,10)
- [ ] **Floor tiles fill interior** - Simple dot/floor pattern
- [ ] **Player sprite visible** - Green-tinted square at position (5, 5)
- [ ] **Dummy sprite visible** - Red-tinted square at position (20, 20)
- [ ] **FOV overlay active** - Semi-transparent yellow tiles around player

**Movement Tests:**
- [ ] **Arrow Right** → Player moves east (sprite moves, FOV updates)
- [ ] **Arrow Left** → Player moves west
- [ ] **Arrow Down** → Player moves south
- [ ] **Arrow Up** → Player moves north
- [ ] **Movement blocked by walls** → Try moving into edge wall (no movement)
- [ ] **W/A/S/D** → Dummy moves (same as arrows for player)
- [ ] **Walk through smoke** → Can move into smoke tiles (they're passable)

**FOV Tests:**
- [ ] **Tab key** → FOV overlay switches to Dummy's perspective (different tiles highlighted)
- [ ] **Tab again** → Switches back to Player's FOV
- [ ] **Smoke blocks vision** → Move player to (11, 10) → Tab to Dummy → Player position NOT highlighted in FOV
- [ ] **Console logs events** → Check Output panel for "Actor moved" and "FOV updated" messages

### 5.4 Expected Console Output

```
Grid Test Scene initialized!
Controls: Arrow Keys = Player, WASD = Dummy, Tab = Switch FOV view
Terrain rendered: 30x30 grid
Actor moved to (5, 5)
FOV updated: ~201 positions visible
```

When you press Arrow Right:
```
Actor moved to (6, 5)
FOV updated: 201 positions visible
```

### 5.5 Troubleshooting

**Problem**: No terrain visible (black screen)
- **Check**: TerrainLayer has TileSet assigned (Inspector → Tile Set)
- **Check**: RenderAllTerrain() is being called
- **Debug**: Add `GD.Print("Rendering tile at " + x + "," + y);` in RenderAllTerrain loop

**Problem**: FOV overlay not showing
- **Check**: FOVLayer has TileSet assigned
- **Check**: FOVLayer Modulate alpha < 255 (semi-transparent)
- **Debug**: Print `evt.VisiblePositions.Count` in OnFOVCalculated

**Problem**: Sprites don't move when pressing keys
- **Check**: Console for errors (especially ServiceLocator errors)
- **Check**: Main scene calls `GameStrapper.Initialize()`
- **Debug**: Add `GD.Print("Key pressed: " + keyEvent.Keycode);` in _UnhandledInput

**Problem**: Movement blocked everywhere
- **Check**: GridMap initialized correctly (all Floor by default)
- **Debug**: Print terrain type in TryMoveActor before sending command

**Problem**: Tiles render at wrong positions/sizes
- **Check**: Atlas Separation is `1 x 1`
- **Check**: Texture Region Size is `8 x 8`
- **Fix**: Reopen TileSet, verify settings, Save

**Problem**: Compilation errors
- **Run**: `dotnet build Darklands.csproj` to see exact error
- **Check**: All `using` statements present in GridTestSceneController.cs
- **Check**: Vector2I (capital 'I') is used, not Vector2i

---

## Step 6: Architecture Verification

### Event Flow (Zero Polling!)

When you press Arrow Right:

```
1. Godot Input System
   ↓
2. _UnhandledInput(InputEvent) detects Key.Right
   ↓
3. GetActorPositionQuery sent to Core
   ↓
4. Calculate newPos = currentPos + (1, 0)
   ↓
5. MoveActorCommand sent to Core
   ↓
6. Core: MoveActorCommandHandler.Handle()
   ├─ Validates terrain passability
   ├─ Updates ActorPositionService
   ├─ Calculates FOV (CalculateFOVQuery)
   ├─ Emits ActorMovedEvent
   └─ Emits FOVCalculatedEvent
   ↓
7. Event Handlers (Godot main thread)
   ├─ OnActorMoved() → sprite.Position = GridToPixel(newPos)
   └─ OnFOVCalculated() → _fovLayer.SetCell(...) for each visible tile
```

**No _Process() polling! Pure event-driven updates!**

### ADR Compliance Check

✅ **ADR-002**: ServiceLocator used ONLY in `_Ready()` (Godot → DI bridge)
✅ **ADR-003**: Result<T> error handling throughout Core
✅ **ADR-004**: Events are terminal subscribers (no cascading)
✅ **ADR-001**: Core has zero Godot dependencies

---

## Complete!

If all tests pass, **VS_005 Phase 4 is COMPLETE!** 🎉

You now have a fully functional, event-driven grid + FOV system:
- ✅ Custom shadowcasting FOV algorithm (~220 LOC, <10ms)
- ✅ Terrain variety (Floor, Wall, Smoke with tactical depth)
- ✅ Event-driven architecture (zero polling)
- ✅ Player + Dummy actors for testing
- ✅ FOV visualization with switchable perspective
- ✅ 189 Core tests passing (54ms)

### Next Steps

1. **Play with the demo!** Try hiding behind smoke and seeing vision blocking
2. **Commit any controller changes** (if you modified GridTestSceneController.cs)
3. **Update backlog** to mark VS_005 as complete
4. **Move to VS_006** (next feature in roadmap)

---

## Quick Reference: Atlas Coordinates

For tweaking tile appearances, these are the atlas coords in use:

| Terrain | Atlas Coords | Visual Description |
|---------|--------------|-------------------|
| Wall    | (0, 0)       | Top-left gray block |
| Floor   | (0, 1)       | Row 1 simple floor |
| Smoke   | (0, 6)       | Row 6 cloud sprite |
| FOV     | (4, 3)       | Row 3 bright tile |

To change tiles, just update the constants in GridTestSceneController.cs:
```csharp
private static readonly Vector2I WallAtlasCoord = new(col, row);
```

Godot's SetCell signature:
```csharp
SetCell(
    Vector2I gridPosition,    // Where on the map (0-29, 0-29)
    int sourceId,             // Which atlas (always 0 for single atlas)
    Vector2I atlasCoords      // Which tile in atlas (col, row)
)
```
