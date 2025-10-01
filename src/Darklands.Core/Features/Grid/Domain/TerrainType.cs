namespace Darklands.Core.Features.Grid.Domain;

/// <summary>
/// Defines the type of terrain occupying a grid cell.
/// Each type has distinct movement and vision properties.
/// </summary>
public enum TerrainType
{
    /// <summary>
    /// Standard walkable terrain. Passable and transparent.
    /// </summary>
    Floor,

    /// <summary>
    /// Solid obstacle. Impassable and opaque (blocks both movement and vision).
    /// </summary>
    Wall,

    /// <summary>
    /// Obscuring gas/fog. Passable but opaque (allows movement, blocks vision).
    /// Enables tactical hide/ambush mechanics.
    /// </summary>
    Smoke
}

/// <summary>
/// Extension methods for querying TerrainType behavior properties.
/// </summary>
public static class TerrainTypeExtensions
{
    /// <summary>
    /// Determines if an actor can move through this terrain type.
    /// </summary>
    /// <param name="terrain">The terrain type to check</param>
    /// <returns>True if actors can walk through this terrain</returns>
    public static bool IsPassable(this TerrainType terrain) =>
        terrain switch
        {
            TerrainType.Floor => true,
            TerrainType.Wall => false,
            TerrainType.Smoke => true,  // Passable but opaque!
            _ => throw new ArgumentOutOfRangeException(nameof(terrain), terrain, "Unknown terrain type")
        };

    /// <summary>
    /// Determines if this terrain type blocks vision (FOV calculation).
    /// </summary>
    /// <param name="terrain">The terrain type to check</param>
    /// <returns>True if this terrain blocks line-of-sight</returns>
    public static bool IsOpaque(this TerrainType terrain) =>
        terrain switch
        {
            TerrainType.Floor => false,
            TerrainType.Wall => true,
            TerrainType.Smoke => true,  // Opaque but passable!
            _ => throw new ArgumentOutOfRangeException(nameof(terrain), terrain, "Unknown terrain type")
        };
}
