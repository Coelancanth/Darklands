# SPD Dungeon Generation Analysis

**Date**: 2025-08-30
**Source**: Shattered Pixel Dungeon v2.5.x
**Purpose**: Extract dungeon generation patterns for Darklands grid-based gameplay

## Executive Summary

SPD uses a sophisticated multi-pass generation system that creates organic, interconnected dungeons through:
1. **Room-based architecture** with various types and sizes
2. **Builder patterns** for different dungeon layouts (loops, branches, linear)
3. **Painter system** for tile placement and decoration
4. **Post-generation passes** for traps, items, and mobs

## Core Architecture

### 1. Generation Pipeline

```
Level.build() → Builder.build() → Painter.paint() → Post-processing
     ↓              ↓                  ↓                    ↓
  Rooms init    Connect rooms    Place tiles         Add entities
```

### 2. Key Classes

- **Level**: Base class, holds map data as int[] array
- **RegularLevel**: Standard dungeon levels with rooms
- **Builder**: Connects rooms into layouts
- **Painter**: Places actual terrain tiles
- **Room**: Rectangle with connections and metadata
- **Terrain**: Constants for tile types (WALL, EMPTY, DOOR, etc.)

## Room Generation System

### Room Types Hierarchy
```
Room (abstract)
├── StandardRoom (regular gameplay spaces)
│   ├── EntranceRoom
│   ├── ExitRoom
│   └── Various sized rooms
├── SpecialRoom (unique features)
│   ├── ShopRoom
│   ├── StatueRoom
│   ├── PitRoom
│   └── SacrificeRoom
└── SecretRoom (hidden areas)
```

### Room Initialization Process
```java
// From RegularLevel.initRooms()
ArrayList<Room> initRooms = new ArrayList<>();
initRooms.add(roomEntrance = EntranceRoom.createEntrance());
initRooms.add(roomExit = ExitRoom.createExit());

// Add standard rooms based on level size
int standards = standardRooms(feeling == Feeling.LARGE);
for (int i = 0; i < standards; i++) {
    StandardRoom s = StandardRoom.createRoom();
    s.setSizeCat(standards - i);
    initRooms.add(s);
}
```

## Builder Pattern System

### Builder Types
1. **LoopBuilder**: Creates circular layouts
2. **FigureEightBuilder**: Two interconnected loops
3. **BranchesBuilder**: Tree-like structures
4. **LineBuilder**: Linear progression
5. **RegularBuilder**: Standard mixed layouts

### Connection Algorithm
```java
// Core room placement logic from Builder.placeRoom()
protected static float placeRoom(ArrayList<Room> collision, 
                                Room prev, Room next, float angle) {
    // 1. Calculate target position using angle
    // 2. Find free space avoiding collisions
    // 3. Size room to fit available space
    // 4. Connect to previous room
    // 5. Return actual angle achieved
}
```

### Spatial Logic
- Rooms have **neighbors** (adjacent) and **connected** (with doors)
- Connections respect room boundaries
- Builder ensures all rooms are reachable

## Tile Placement System

### Terrain Types (from Terrain.java)
```java
public static final int CHASM = 0;      // Pit tiles
public static final int EMPTY = 1;      // Walkable floor
public static final int GRASS = 2;      // Flammable floor
public static final int WALL = 4;       // Solid, blocks LOS
public static final int DOOR = 5;       // Openable barrier
public static final int ENTRANCE = 7;   // Level start
public static final int EXIT = 8;       // Level end
```

### Terrain Flags (Bitwise Properties)
```java
public static final int PASSABLE = 0x01;     // Can walk through
public static final int LOS_BLOCKING = 0x02; // Blocks line of sight
public static final int FLAMABLE = 0x04;     // Can burn
public static final int SECRET = 0x08;       // Hidden
public static final int SOLID = 0x10;        // Blocks movement
public static final int AVOID = 0x20;        // AI avoids
public static final int LIQUID = 0x40;       // Water/lava
public static final int PIT = 0x80;          // Fall hazard
```

### Painter Methods
```java
// Basic tile operations
Painter.set(level, x, y, value);              // Set single tile
Painter.fill(level, x, y, w, h, value);       // Fill rectangle
Painter.fillEllipse(level, rect, value);      // Fill ellipse
Painter.fillDiamond(level, rect, value);      // Fill diamond
Painter.drawLine(level, from, to, value);     // Draw line
```

## Map Storage Architecture

### Single Array Design
```java
// Level.java core storage
public int[] map;        // Main terrain data
public boolean[] visited;    // Player has seen
public boolean[] mapped;     // Player knows about
public boolean[] heroFOV;    // Currently visible
public boolean[] passable;   // Movement cache
public boolean[] solid;      // Collision cache
```

### Index Calculation
```java
// Convert 2D coordinates to 1D array index
int cell = x + y * level.width();

// Convert cell back to coordinates
int x = cell % level.width();
int y = cell / level.width();
```

## Post-Generation Processing

### Decoration Phases
1. **Grass placement**: Random grass tiles for variety
2. **Trap placement**: Based on level depth and feeling
3. **Item placement**: Loot in rooms
4. **Mob spawning**: Enemies placed avoiding entrance
5. **Secret areas**: Hidden doors and rooms

### Special Features
- **Feelings**: Level modifiers (DARK, LARGE, TRAPS, etc.)
- **Landmarks**: Special rooms guaranteed per floor
- **Connections**: Ensure all rooms reachable

## Key Insights for Darklands

### 1. Efficient Tile Storage
- **Single int[] array** for all terrain (not 2D array)
- **Bitwise flags** for properties instead of multiple arrays
- **Cell indexing** for fast lookups

### 2. Room-First Generation
- Generate abstract room graph first
- Then place tiles based on room data
- Allows easy re-generation and validation

### 3. Builder/Painter Separation
- **Builder**: Logical layout (room connections)
- **Painter**: Visual representation (actual tiles)
- Clean separation of concerns

### 4. Flexible Room System
- Rooms know min/max sizes
- Can resize to fit available space
- Support various shapes (rect, ellipse, diamond)

### 5. Multi-Pass Generation
- Each pass has single responsibility
- Easy to add/remove features
- Deterministic with seeds

## Implementation Recommendations for Darklands

### Phase 1: Core Grid System
```csharp
public class Grid {
    private int[] tiles;      // Terrain types
    private int width, height;
    
    public int GetCell(int x, int y) => tiles[x + y * width];
    public void SetCell(int x, int y, int value) => tiles[x + y * width] = value;
}
```

### Phase 2: Room Abstraction
```csharp
public abstract class Room {
    public Rect Bounds { get; set; }
    public List<Room> Connected { get; set; }
    
    public abstract int MinWidth();
    public abstract int MinHeight();
    public abstract void Paint(Grid grid);
}
```

### Phase 3: Builder Pattern
```csharp
public interface ILevelBuilder {
    List<Room> Build(List<Room> rooms);
}

public class SimpleBuilder : ILevelBuilder {
    // Connect entrance to exit through other rooms
}
```

### Phase 4: Painter System
```csharp
public static class GridPainter {
    public static void Fill(Grid grid, Rect area, TileType tile);
    public static void DrawWalls(Grid grid, Room room);
    public static void PlaceDoor(Grid grid, Point location);
}
```

## Critical Patterns to Adopt

1. **Terrain as integers, not enums** - Faster array operations
2. **Flags for properties** - One lookup gives all tile info
3. **Room-based generation** - Not pure noise/cellular automata
4. **Separate logical/visual** - Rooms exist before tiles
5. **Multi-pass decoration** - Clean, testable phases

## Performance Considerations

- SPD generates 32x32 to 85x85 levels instantly
- Uses primitive int[] for speed (no boxing)
- Caches frequently checked properties (passable, solid)
- Pre-calculates FOV and pathfinding data

## Next Steps for VS_008

1. Create Grid domain model with int[] storage
2. Define TileType constants matching SPD pattern
3. Implement basic Painter static methods
4. Create simple room placement logic
5. Add Godot scene to visualize grid

## References

- Level.java:152 - Map storage design
- Builder.java:41 - Room connection algorithm  
- Painter.java:42-97 - Tile placement methods
- Terrain.java:26-125 - Tile types and flags
- RegularLevel.java:104-121 - Generation pipeline