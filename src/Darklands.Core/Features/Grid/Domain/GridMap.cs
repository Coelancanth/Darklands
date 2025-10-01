using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;

namespace Darklands.Core.Features.Grid.Domain;

/// <summary>
/// Represents a fixed-size grid map with terrain types.
/// Enforces grid bounds (30x30) and provides terrain query operations.
/// </summary>
public sealed class GridMap
{
    public const int Width = 30;
    public const int Height = 30;

    private readonly TerrainType[,] _terrain;

    /// <summary>
    /// Creates a new 30x30 grid map with all cells initialized to Floor terrain.
    /// </summary>
    public GridMap()
    {
        _terrain = new TerrainType[Width, Height];

        // Initialize entire grid to Floor (walkable, transparent)
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                _terrain[x, y] = TerrainType.Floor;
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
    /// Gets the terrain type at the specified position.
    /// </summary>
    /// <param name="pos">Grid position to query</param>
    /// <returns>Success with TerrainType, or Failure if position is out of bounds</returns>
    public Result<TerrainType> GetTerrain(Position pos)
    {
        if (!IsValidPosition(pos))
        {
            return Result.Failure<TerrainType>(
                $"Position ({pos.X}, {pos.Y}) is outside grid bounds (0-{Width - 1}, 0-{Height - 1})");
        }

        return Result.Success(_terrain[pos.X, pos.Y]);
    }

    /// <summary>
    /// Sets the terrain type at the specified position.
    /// </summary>
    /// <param name="pos">Grid position to modify</param>
    /// <param name="terrain">Terrain type to set</param>
    /// <returns>Success if updated, or Failure if position is out of bounds</returns>
    public Result SetTerrain(Position pos, TerrainType terrain)
    {
        if (!IsValidPosition(pos))
        {
            return Result.Failure(
                $"Position ({pos.X}, {pos.Y}) is outside grid bounds (0-{Width - 1}, 0-{Height - 1})");
        }

        _terrain[pos.X, pos.Y] = terrain;
        return Result.Success();
    }

    /// <summary>
    /// Checks if an actor can move through the terrain at the specified position.
    /// </summary>
    /// <param name="pos">Grid position to query</param>
    /// <returns>
    /// Success with true if passable (Floor, Smoke),
    /// Success with false if impassable (Wall),
    /// or Failure if position is out of bounds
    /// </returns>
    public Result<bool> IsPassable(Position pos) =>
        GetTerrain(pos)
            .Map(terrain => terrain.IsPassable());

    /// <summary>
    /// Checks if the terrain blocks vision (FOV calculation).
    /// </summary>
    /// <param name="pos">Grid position to query</param>
    /// <returns>
    /// Success with true if opaque/blocks vision (Wall, Smoke),
    /// Success with false if transparent (Floor),
    /// or Failure if position is out of bounds
    /// </returns>
    public Result<bool> IsOpaque(Position pos) =>
        GetTerrain(pos)
            .Map(terrain => terrain.IsOpaque());
}
