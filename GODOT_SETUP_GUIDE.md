# Godot Setup Guide - VS_005 Grid FOV Test Scene

## Overview
This guide helps you complete the Godot editor setup for the Grid FOV test scene.
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
5. Click **Close**

### 1.2 Verify Asset Import
1. Navigate to `assets/micro-roguelike/` in FileSystem dock
2. Click `colored_tilemap.png`
3. In Import dock (right side), verify:
   - **Import As**: Texture2D ✅
   - **Compress Mode**: Lossless ✅
   - **Fix Alpha Border**: On ✅
   - (Filter is controlled by Project Settings, not here)

### 1.3 Create TileSet Resource
1. In FileSystem dock, right-click `assets/` → **New Resource**
2. Search for "TileSet" → Click "Create"
3. Save as `assets/grid_tileset.tres`
4. Double-click `grid_tileset.tres` to open TileSet editor (bottom panel)

### 1.4 Configure TileSet Atlas
1. In TileSet editor bottom panel, click **"+ Create a new atlas"**
2. Select `assets/micro-roguelike/colored_tilemap.png`
3. **Atlas settings** (in right Inspector):
   - Texture Region Size: `8x8` (tile size)
   - Separation: `1x1` (spacing between tiles)
   - Use Texture Padding: OFF
4. The grid should now overlay the tilemap correctly

### 1.5 Add Terrain Tiles
The atlas has 160 tiles (16 wide × 10 tall). You need to select 3 tiles:

**Floor Tile** (ID 16 - row 1, col 0):
1. In TileSet atlas view, **click tile at row 1, column 0** (small dot/floor)
2. This creates tile ID 16 automatically

**Wall Tile** (ID 0 - row 0, col 0):
1. Click tile at row 0, column 0 (solid block)
2. This is tile ID 0

**Smoke Tile** (ID 96 - row 6, col 0):
1. Click tile at row 6, column 0 (cloud/fog sprite)
2. This is tile ID 96

**FOV Overlay Tile** (any bright tile):
1. Pick any visually distinct tile (e.g., bright yellow square)
2. Remember its ID for FOV visualization

### 1.6 Verify TileSet
- You should see at least 4 tiles selected in the atlas
- IDs should be: 0 (wall), 16 (floor), 96 (smoke), + 1 for FOV
- **Save** (Ctrl+S)

---

## Step 2: Configure Test Scene (~5 minutes)

### 2.1 Open Scene
1. Open `TestScenes/GridTestScene.tscn`
2. Scene tree should show:
   ```
   GridTestScene (Node2D)
   ├─ TerrainLayer (TileMapLayer)
   ├─ FOVLayer (TileMapLayer)
   ├─ Player (Sprite2D)
   └─ Dummy (Sprite2D)
   ```

### 2.2 Assign TileSet to Layers
1. **Select "TerrainLayer" node**
2. In Inspector → **Tile Set**: Drag `assets/grid_tileset.tres` into this field
3. **Select "FOVLayer" node**
4. In Inspector → **Tile Set**: Drag `assets/grid_tileset.tres` into this field

### 2.3 Configure FOV Layer Visual
1. Select "FOVLayer" node
2. In Inspector → **CanvasItem** section:
   - Modulate: Yellow-ish with alpha (e.g., `RGBA(255, 255, 128, 128)`)
   - This makes FOV tiles semi-transparent overlay

### 2.4 Verify Sprites
- Player and Dummy sprites should already be configured
- Player: Green tint
- Dummy: Red tint
- Both use region from tilemap (8x8 square)

### 2.5 Save Scene
- **Ctrl+S** to save

---

## Step 3: Update GridTestSceneController Tile IDs (~2 minutes)

The controller needs to know which tile IDs to use for rendering.

### 3.1 Open Controller Script
Open `GridTestSceneController.cs` in your IDE.

### 3.2 Add Tile ID Constants
Find the constants at the top of the class and add tile mappings:

```csharp
private const int TileSize = 8;
private const int VisionRadius = 8;

// Tile IDs from grid_tileset.tres
private const int FloorTileId = 16;   // Row 1, Col 0
private const int WallTileId = 0;     // Row 0, Col 0
private const int SmokeTileId = 96;   // Row 6, Col 0
private const int FOVTileId = 1;      // Any bright tile for overlay
```

### 3.3 Update InitializeGameState() Rendering
Find the terrain initialization section and add TileMap rendering:

```csharp
// Initialize test terrain: Walls around edges, smoke patches
for (int x = 0; x < 30; x++)
{
    await _mediator.Send(new SetTerrainCommand(new Position(x, 0), TerrainType.Wall));
    await _mediator.Send(new SetTerrainCommand(new Position(x, 29), TerrainType.Wall));

    // Render walls on TileMap
    _terrainLayer.SetCell(new Vector2I(x, 0), 0, new Vector2I(WallTileId % 16, WallTileId / 16));
    _terrainLayer.SetCell(new Vector2I(x, 29), 0, new Vector2I(WallTileId % 16, WallTileId / 16));
}
```

**OR** simpler approach - query GridMap and render all terrain:

```csharp
// After all terrain commands, render entire grid
for (int x = 0; x < 30; x++)
{
    for (int y = 0; y < 30; y++)
    {
        var terrainQuery = await _mediator.Send(new GetTerrainQuery(new Position(x, y)));
        if (terrainQuery.IsSuccess)
        {
            int tileId = terrainQuery.Value switch
            {
                TerrainType.Floor => FloorTileId,
                TerrainType.Wall => WallTileId,
                TerrainType.Smoke => SmokeTileId,
                _ => FloorTileId
            };
            _terrainLayer.SetCell(new Vector2I(x, y), 0, new Vector2I(tileId % 16, tileId / 16));
        }
    }
}
```

### 3.4 Update OnFOVCalculated() Rendering
Find the `OnFOVCalculated()` method and fix tile rendering:

```csharp
private void OnFOVCalculated(FOVCalculatedEvent evt)
{
    if (!evt.ActorId.Equals(_activeActorId))
        return; // Only show active actor's FOV

    // Clear previous FOV overlay
    _fovLayer.Clear();

    // Highlight visible tiles
    foreach (var pos in evt.VisiblePositions)
    {
        // Source ID 0 = first atlas, Vector2I = tile coords in atlas
        _fovLayer.SetCell(
            new Vector2I(pos.X, pos.Y),  // Grid position
            0,                            // Source ID (atlas index)
            new Vector2I(FOVTileId % 16, FOVTileId / 16) // Tile atlas coords
        );
    }

    GD.Print($"FOV updated: {evt.VisiblePositions.Count} positions visible");
}
```

### 3.5 Add GetTerrainQuery (if using full render approach)
You'll need to create this query in Core (similar to GetActorPositionQuery):

```csharp
// In Core/Features/Grid/Application/Queries/GetTerrainQuery.cs
public record GetTerrainQuery(Position Position) : IRequest<Result<TerrainType>>;

// Handler: Delegates to GridMap.GetTerrain()
```

---

## Step 4: Manual Testing (~10 minutes)

### 4.1 Run Scene
1. Press **F6** (Run Scene) or click "Run Current Scene" button
2. The Grid Test Scene should launch

### 4.2 Test Checklist
- [ ] **Terrain renders** - Walls around edges, smoke patches visible
- [ ] **Player sprite visible** - Green square at starting position
- [ ] **Dummy sprite visible** - Red square at starting position
- [ ] **FOV overlay renders** - Yellow highlighted tiles around player
- [ ] **Player movement** - Arrow keys move player (blocked by walls)
- [ ] **Dummy movement** - WASD keys move dummy (blocked by walls)
- [ ] **Smoke passable** - Can walk through smoke tiles
- [ ] **Smoke blocks vision** - Hide player behind smoke → Tab → Dummy's FOV doesn't show player
- [ ] **Tab switches FOV** - Press Tab → FOV overlay switches between player/dummy perspective
- [ ] **Console logging** - Check Output panel for event logs

### 4.3 Expected Behavior
```
[Movement Test]
1. Press Right Arrow → Player moves east
2. Console: "Actor moved to (6, 5)"
3. Console: "FOV updated: ~201 positions visible"
4. FOV overlay updates around player

[Vision Blocking Test]
1. Move player behind smoke patch (position 10, 10)
2. Press Tab to switch to Dummy's FOV
3. Player position should NOT be highlighted in FOV overlay
4. Move dummy closer → Player becomes visible when line-of-sight clears
```

### 4.4 Troubleshooting

**Problem**: No terrain visible
- **Fix**: Check TileSet assigned to TerrainLayer
- **Fix**: Verify tile IDs match atlas (print debug in InitializeGameState)

**Problem**: FOV overlay not showing
- **Fix**: Check FOVLayer has TileSet assigned
- **Fix**: Verify modulate color has alpha < 1.0 (semi-transparent)

**Problem**: Sprites don't move
- **Fix**: Check ServiceLocator initialized (should see error in console if not)
- **Fix**: Verify Main scene calls `GameStrapper.Initialize()`

**Problem**: Movement blocked everywhere
- **Fix**: GridMap might not be initialized as Floor by default
- **Fix**: Check SetTerrainCommand is working (add debug logs)

**Problem**: Compilation errors
- **Fix**: Rebuild solution: `dotnet build Darklands.csproj`
- **Fix**: Check all `using` statements in GridTestSceneController.cs

---

## Step 5: Architecture Verification

### Event Flow Validation
When you press an arrow key, this should happen:

1. **Godot Input** → `_UnhandledInput()` detects key press
2. **Query Position** → `GetActorPositionQuery(actorId)` gets current position
3. **Calculate Target** → `newPos = currentPos + direction`
4. **Send Command** → `MoveActorCommand(actorId, newPos)`
5. **Core Processing**:
   - Validates terrain passability
   - Updates position in ActorPositionService
   - Calculates FOV via CalculateFOVQuery
   - Emits ActorMovedEvent
   - Emits FOVCalculatedEvent
6. **Event Handlers**:
   - `OnActorMoved()` → Updates sprite.Position
   - `OnFOVCalculated()` → Updates FOV overlay tiles

**Zero polling! Pure event-driven!**

### Console Output Example
```
[INFO] Attempting to move actor <guid> to position (6, 5)
[INFO] Successfully moved actor <guid> to position (6, 5)
[DEBUG] FOV calculated successfully: 201 positions visible
Actor moved to (6, 5)
FOV updated: 201 positions visible
```

---

## Complete!

If all tests pass, VS_005 Phase 4 is complete! The grid + FOV + terrain system is fully functional with:
- ✅ Event-driven architecture (ADR-002, ADR-004 compliant)
- ✅ Custom shadowcasting FOV (libtcod algorithm)
- ✅ Terrain variety (Floor, Wall, Smoke)
- ✅ Player + Dummy actors
- ✅ FOV visualization
- ✅ Manual testing validated

Next steps:
- Commit Phase 4 completion
- Update backlog
- Move to VS_006 (next feature)
