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

**Last Updated**: 2025-10-02
