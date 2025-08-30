# ADR-007: TileMap Variant Selection Strategy

**Date**: 2025-08-30
**Status**: Proposed
**Deciders**: Tech Lead
**Tags**: #tilemap #autotiling #architecture

## Context

For VS_008 (Grid Scene and Player Sprite), we need to decide how to handle tile variant selection during dungeon generation. We have analyzed two approaches:

1. **SPD Approach**: Manual calculation of which tile variant to place based on neighbors
2. **Godot Native**: TileMapLayers with terrain sets that automatically select variants

Both approaches can achieve visually consistent, professional-looking dungeons, but with different trade-offs.

**Important Clarification**: This ADR is about **tile placement/variant selection** (one-time during generation), NOT about rendering (drawing pixels every frame).

## Decision Drivers

- **Generation Speed**: Must generate 50x50+ grids quickly
- **Flexibility**: Support procedural generation at runtime
- **Maintainability**: Easy to add new tile types and visual variants
- **Visual Quality**: Seamless tile connections without manual variant placement
- **Clean Architecture**: Separation between domain logic and presentation

## Considered Options

### Option 1: Pure SPD Approach (Manual Variant Selection)

Implement SPD's tile variant calculation in C#:
```csharp
public class TileVariantSelector {
    // During generation, calculate which variant to use
    public int GetWallVariant(int[] neighbors) {
        int variant = BaseWall;
        if (!IsWall(neighbors[RIGHT])) variant += 1;
        if (!IsWall(neighbors[LEFT])) variant += 2;
        // Returns sprite index (e.g., wall_variant_3)
    }
}
```

**Pros:**
- Complete control over variant selection
- Deterministic and testable
- Works identically to proven SPD system
- No Godot-specific dependencies in Core

**Cons:**
- Must implement all neighbor-checking logic manually
- Requires creating 49+ tile variants for corners/sides
- More code to maintain
- Duplicates what Godot's autotiling does automatically

### Option 2: Godot Terrain Sets (Native Autotiling)

Use Godot's built-in terrain matching for variant selection:
```gdscript
# During generation: Tell Godot "place a wall here"
# Godot automatically selects the correct variant based on neighbors
tilemap_layer.set_cells_terrain_connect(
    cells_array,
    terrain_set_id,
    terrain_id  # e.g., WALL_TERRAIN
)
# Godot checks neighbors and places wall_corner, wall_edge, etc.
```

**Pros:**
- Godot automatically selects correct variant based on neighbors
- No manual neighbor checking needed
- Less code to maintain
- Built-in alternative tiles with probability weights
- Custom data layers for tile properties

**Cons:**
- Generation-time performance concerns with large maps
- API can be tricky for procedural generation
- Less control over exact variant selection
- Some users report "tricky behaviors"

### Option 3: Hybrid Approach (Recommended)

Use domain model for logic, Godot for variant selection:

```csharp
// Domain Layer (Core project) - Decides WHAT goes WHERE
public class GridGenerator {
    private Grid grid;
    
    public void GenerateDungeon() {
        // Domain logic determines tile types
        PlaceRoom(5, 5, 10, 10, TileType.Floor);
        PlaceWalls(5, 5, 10, 10, TileType.Wall);
        PlaceDoor(7, 5, TileType.Door);
        // Grid contains logical types (Wall, Floor, Door)
    }
}

// Presentation Layer (Godot project) - ONE-TIME conversion
public partial class GridView : Node2D {
    private TileMapLayer wallLayer;
    
    public void BuildTilemap(Grid grid) {
        // One-time generation: Convert logical grid to visual tiles
        for (int i = 0; i < grid.Size; i++) {
            var pos = grid.IndexToPosition(i);
            var type = grid.GetTile(pos);
            
            // Godot autotiling selects variant (corner, edge, etc.)
            if (type == TileType.Wall) {
                wallLayer.SetCellsTerrainConnect(
                    new[] { pos.ToVector2I() },
                    0, WALL_TERRAIN
                );
                // Godot picks wall_corner_left, wall_straight, etc.
            }
        }
    }
}
```

**Pros:**
- Clean separation: Domain decides WHAT, Godot decides WHICH variant
- Domain logic stays pure C# (testable)
- Leverages Godot's autotiling for variant selection
- Can switch variant selection strategies later
- One-time work during generation

**Cons:**
- Need to map between domain types and Godot terrains
- Two systems to understand
- Generation-time performance depends on Godot's terrain system

## Decision

**Adopt Option 3: Hybrid Approach**

### Rationale

1. **Follows Clean Architecture**: Domain model independent of Godot
2. **Best of both worlds**: Domain controls logic, Godot handles variant selection
3. **Future-proof**: Can change variant selection strategy without touching domain
4. **Pragmatic**: Uses engine's autotiling where it excels
5. **Simplicity**: No need to manually calculate which variant to use

### Implementation Strategy

#### Phase 1: Domain Model (Pure C#)
```csharp
public enum TileType {
    Empty = 0,
    Floor = 1,
    Wall = 2,
    Door = 3,
    Water = 4
}

public interface IGrid {
    TileType GetTile(Position pos);
    void SetTile(Position pos, TileType type);
    event Action<Position, TileType> TileChanged;
}
```

#### Phase 2: Godot Tile Placement (One-Time Generation)
1. Create TileSet resource with terrain sets:
   - Terrain 0: Floors (auto-connects to other floors)
   - Terrain 1: Walls (auto-connects to other walls)
   - Terrain 2: Water (auto-connects edges with floor)

2. Use multiple TileMapLayers:
   - Layer 0: Floor tiles
   - Layer 1: Wall tiles (with raised perspective variants)
   - Layer 2: Features (doors, stairs)
   - Layer 3: Decorations

3. Generation flow:
   ```csharp
   // ONE-TIME during level generation
   var generator = new DungeonGenerator();
   var grid = generator.Generate(50, 50);
   
   // ONE-TIME conversion to Godot tilemap
   gridView.BuildFromDomain(grid);
   // After this, tiles are static - no logic runs per frame
   ```

### Alternative Tiles Strategy

Use Godot's alternative tiles for visual variety:
- Create 2-3 variants per tile type
- Set probability weights (50% normal, 45% alt1, 5% alt2)
- Let Godot randomly select during placement

### Custom Data Layers

Define tile properties in Godot TileSet:
- `MovementCost`: float (1.0 normal, 2.0 difficult terrain)
- `BlocksSight`: bool
- `BlocksMovement`: bool
- `TerrainType`: string (for gameplay effects)

## Consequences

### Positive
- Clean separation between game logic and tile placement
- Can unit test domain logic without Godot
- Leverages engine's autotiling for variant selection
- Easy to add new tile types
- Visual consistency handled automatically by Godot
- One-time work - no per-frame logic needed

### Negative
- Must maintain mapping between domain types and Godot terrains
- Learning curve for Godot's terrain system
- Generation-time performance concerns at 100x100+ grids
- Less fine control over exact variant selection

### Mitigations
- Document the domain↔Godot mapping clearly
- Consider "Better Terrain" plugin if native system inadequate
- Implement grid chunking if performance degrades
- Keep tile stitching logic in separate class for potential fallback

## Notes

### Terminology Clarification
- **Tile Placement**: The one-time decision of what tile type goes where (Wall at position 5,5)
- **Variant Selection**: The one-time decision of which visual variant to use (wall_corner_left)
- **Rendering**: Drawing pixels to screen every frame (handled automatically by Godot)

### Key Insight
SPD manually calculates variants because libGDX doesn't have autotiling. Godot's terrain sets do this automatically - we just say "place a wall here" and Godot selects the correct corner/edge variant based on neighbors.

### Additional Notes
- SPD's approach works great for Java/libGDX but would be redundant in Godot
- Godot 4's terrain system has known issues, but plugins exist as alternatives
- The hybrid approach aligns with our VSA and Clean Architecture principles
- Can always implement manual variant selection later if Godot's system proves inadequate

## References

- [SPD Dungeon Generation Analysis](../../../Docs/08-Learning/SPD-Dungeon-Generation-Analysis.md)
- [SPD Visual Consistency System](../../../Docs/08-Learning/SPD-Visual-Consistency-System.md)
- [Godot TileMap Documentation](https://docs.godotengine.org/en/stable/tutorials/2d/using_tilemaps.html)
- [Better Terrain Plugin](https://github.com/Portponky/better-terrain) (potential fallback)