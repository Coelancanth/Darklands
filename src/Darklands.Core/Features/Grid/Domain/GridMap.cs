using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;

namespace Darklands.Core.Features.Grid.Domain;

/// <summary>
/// Represents a fixed-size grid map with terrain definitions.
/// Enforces grid bounds (30x30) and provides terrain query operations.
/// </summary>
/// <remarks>
/// ARCHITECTURE CHANGE (VS_019 Phase 1):
/// - OLD: Stored TerrainType enum (hardcoded properties)
/// - NEW: Stores TerrainDefinition records (data-driven properties from TileSet)
///
/// ZERO-COST PROPERTY ACCESS:
/// - TerrainDefinitions stored directly in cells (not ItemId references)
/// - IsPassable/IsOpaque queries are direct property access (no repository lookups)
/// - Immutable records safe to share references across cells
/// </remarks>
public sealed class GridMap
{
    public const int Width = 30;
    public const int Height = 30;

    private readonly TerrainDefinition[,] _terrain;

    /// <summary>
    /// Creates a new 30x30 grid map with all cells initialized to default terrain.
    /// </summary>
    /// <param name="defaultTerrain">Default terrain for initialization (typically "floor")</param>
    public GridMap(TerrainDefinition defaultTerrain)
    {
        if (defaultTerrain == null)
        {
            throw new ArgumentNullException(nameof(defaultTerrain), "Default terrain cannot be null");
        }

        _terrain = new TerrainDefinition[Width, Height];

        // Initialize entire grid to default terrain (typically floor: walkable, transparent)
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                _terrain[x, y] = defaultTerrain;
            }
        }
    }

    /// <summary>
    /// Checks if a position is within grid bounds.
    /// </summary>
    /// <param name="pos">Position to validate</param>
    /// <returns>True if position is within [0, Width) and [0, Height)</returns>
    public bool IsValidPosition(Position pos) =>
        pos.X >= 0 && pos.X < Width &&
        pos.Y >= 0 && pos.Y < Height;

    /// <summary>
    /// Gets the terrain definition at the specified position.
    /// </summary>
    /// <param name="pos">Grid position to query</param>
    /// <returns>Success with TerrainDefinition, or Failure if position is out of bounds</returns>
    public Result<TerrainDefinition> GetTerrain(Position pos)
    {
        if (!IsValidPosition(pos))
        {
            return Result.Failure<TerrainDefinition>(
                $"Position ({pos.X}, {pos.Y}) is outside grid bounds (0-{Width - 1}, 0-{Height - 1})");
        }

        return Result.Success(_terrain[pos.X, pos.Y]);
    }

    /// <summary>
    /// Sets the terrain definition at the specified position.
    /// </summary>
    /// <param name="pos">Grid position to modify</param>
    /// <param name="terrain">Terrain definition to set</param>
    /// <returns>Success if updated, or Failure if position is out of bounds</returns>
    public Result SetTerrain(Position pos, TerrainDefinition terrain)
    {
        if (!IsValidPosition(pos))
        {
            return Result.Failure(
                $"Position ({pos.X}, {pos.Y}) is outside grid bounds (0-{Width - 1}, 0-{Height - 1})");
        }

        if (terrain == null)
        {
            return Result.Failure("Terrain definition cannot be null");
        }

        _terrain[pos.X, pos.Y] = terrain;
        return Result.Success();
    }

    /// <summary>
    /// Checks if an actor can move through the terrain at the specified position.
    /// </summary>
    /// <param name="pos">Grid position to query</param>
    /// <returns>
    /// Success with true if passable (terrain.CanPass=true),
    /// Success with false if impassable (terrain.CanPass=false),
    /// or Failure if position is out of bounds
    /// </returns>
    public Result<bool> IsPassable(Position pos) =>
        GetTerrain(pos)
            .Map(terrain => terrain.IsPassable());  // Legacy method, equivalent to terrain.CanPass

    /// <summary>
    /// Checks if the terrain blocks vision (FOV calculation).
    /// </summary>
    /// <param name="pos">Grid position to query</param>
    /// <returns>
    /// Success with true if opaque/blocks vision (terrain.CanSeeThrough=false),
    /// Success with false if transparent (terrain.CanSeeThrough=true),
    /// or Failure if position is out of bounds
    /// </returns>
    public Result<bool> IsOpaque(Position pos) =>
        GetTerrain(pos)
            .Map(terrain => terrain.IsOpaque());  // Legacy method, equivalent to !terrain.CanSeeThrough
}
