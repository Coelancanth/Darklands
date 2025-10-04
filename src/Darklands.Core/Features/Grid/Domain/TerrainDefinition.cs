using CSharpFunctionalExtensions;

namespace Darklands.Core.Features.Grid.Domain;

/// <summary>
/// Immutable terrain catalog entry defining properties and rendering data.
/// </summary>
/// <remarks>
/// ARCHITECTURE (ADR-002 Compliance):
/// - Stored directly in GridMap cells (zero-cost property access during FOV/pathfinding)
/// - Loaded once at startup from TileSet (catalog data, not runtime state)
/// - AtlasX/AtlasY are primitives (int, not Godot Vector2I) for Core layer purity
///
/// SSOT PATTERN (VS_009):
/// - TileSet is single source of truth (custom data: name, can_pass, can_see_through)
/// - Infrastructure reads TileSet → creates TerrainDefinition
/// - Core never touches Godot types
///
/// IMMUTABILITY:
/// - TerrainDefinition is immutable record
/// - Safe to share references across GridMap cells
/// - No copying overhead (reference semantics)
/// </remarks>
public sealed record TerrainDefinition
{
    /// <summary>
    /// Unique terrain name (e.g., "floor", "wall", "smoke", "tree").
    /// Maps to TileSet custom_data layer 0 (terrain_name).
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Can actors move through this terrain?
    /// Maps to TileSet custom_data layer 1 (can_pass).
    /// </summary>
    public bool CanPass { get; }

    /// <summary>
    /// Is this terrain transparent for vision/FOV?
    /// Maps to TileSet custom_data layer 2 (can_see_through).
    /// </summary>
    /// <remarks>
    /// Opposite of legacy IsOpaque:
    /// - Floor: can_see_through=true (was IsOpaque=false)
    /// - Wall/Smoke: can_see_through=false (was IsOpaque=true)
    /// </remarks>
    public bool CanSeeThrough { get; }

    /// <summary>
    /// TileSet atlas X coordinate (for rendering).
    /// Primitive int for ADR-002 compliance (Core has zero Godot dependencies).
    /// </summary>
    public int AtlasX { get; }

    /// <summary>
    /// TileSet atlas Y coordinate (for rendering).
    /// Primitive int for ADR-002 compliance (Core has zero Godot dependencies).
    /// </summary>
    public int AtlasY { get; }

    private TerrainDefinition(
        string name,
        bool canPass,
        bool canSeeThrough,
        int atlasX,
        int atlasY)
    {
        Name = name;
        CanPass = canPass;
        CanSeeThrough = canSeeThrough;
        AtlasX = atlasX;
        AtlasY = atlasY;
    }

    /// <summary>
    /// Creates a terrain definition with validation.
    /// </summary>
    /// <param name="name">Unique terrain identifier (lowercase, alphanumeric + underscore)</param>
    /// <param name="canPass">Movement allowed?</param>
    /// <param name="canSeeThrough">Vision allowed?</param>
    /// <param name="atlasX">TileSet X coordinate</param>
    /// <param name="atlasY">TileSet Y coordinate</param>
    /// <returns>Result containing TerrainDefinition or validation error</returns>
    public static Result<TerrainDefinition> Create(
        string name,
        bool canPass,
        bool canSeeThrough,
        int atlasX,
        int atlasY)
    {
        // Validate name
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<TerrainDefinition>("Terrain name cannot be empty");
        }

        // Atlas coordinates can be any value (TileSet determines valid range)
        // Negative values are valid for autotiling terrains (Godot convention)

        return Result.Success(new TerrainDefinition(
            name.ToLowerInvariant(),  // Normalize to lowercase
            canPass,
            canSeeThrough,
            atlasX,
            atlasY));
    }

    /// <summary>
    /// Legacy compatibility: Determines if terrain blocks vision.
    /// </summary>
    /// <remarks>
    /// Kept for smooth migration from IsOpaque() extension method.
    /// Equivalent to: !CanSeeThrough
    /// </remarks>
    public bool IsOpaque() => !CanSeeThrough;

    /// <summary>
    /// Legacy compatibility: Determines if terrain allows movement.
    /// </summary>
    /// <remarks>
    /// Kept for smooth migration from IsPassable() extension method.
    /// Equivalent to: CanPass
    /// </remarks>
    public bool IsPassable() => CanPass;

    /// <summary>
    /// Returns terrain name for logging and debugging.
    /// </summary>
    public override string ToString() => Name;
}
