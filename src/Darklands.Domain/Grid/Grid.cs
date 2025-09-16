using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Immutable;
using LanguageExt;
using LanguageExt.Common;
using Darklands.Domain.Common;
using System.Text.Json.Serialization;
using static LanguageExt.Prelude;

namespace Darklands.Domain.Grid
{
    /// <summary>
    /// Represents the tactical combat grid containing all tiles and their states.
    /// Immutable domain model that provides safe access to grid positions and tile information.
    /// 
    /// Save-ready entity per ADR-005:
    /// - Implements IPersistentEntity for save/load compatibility
    /// - Uses ImmutableArray for true immutability
    /// - Contains ModData for future modding support
    /// - Unique GridId for stable references
    /// </summary>
    public sealed record Grid(
        GridId Id,
        int Width,
        int Height,
        ImmutableArray<Tile> Tiles,
        ImmutableDictionary<string, string> ModData
    ) : IPersistentEntity
    {
        /// <summary>
        /// IPersistentEntity implementation - exposes ID for save system.
        /// </summary>
        IEntityId IPersistentEntity.Id => Id;

        /// <summary>
        /// Total number of tiles in the grid.
        /// </summary>
        public int TileCount => Width * Height;

        /// <summary>
        /// Transient state that doesn't save (cached pathfinding, visual effects, etc.).
        /// Kept separate from persistent state and reconstructed after loading.
        /// </summary>
        [JsonIgnore]
        public ITransientState? TransientState { get; init; }

        /// <summary>
        /// Creates a new empty grid with the specified dimensions and default terrain.
        /// Uses IStableIdGenerator for save-ready grid creation.
        /// </summary>
        /// <param name="ids">ID generator for creating stable grid identifier</param>
        /// <param name="width">Width of the grid (must be > 0)</param>
        /// <param name="height">Height of the grid (must be > 0)</param>
        /// <param name="defaultTerrain">Default terrain for all tiles</param>
        /// <param name="modData">Optional mod data</param>
        /// <returns>Success with new Grid or failure with validation error</returns>
        public static Fin<Grid> Create(IStableIdGenerator ids, int width, int height, TerrainType defaultTerrain = TerrainType.Open, ImmutableDictionary<string, string>? modData = null)
        {
            if (width <= 0)
                return FinFail<Grid>(Error.New($"Grid width must be positive: {width}"));

            if (height <= 0)
                return FinFail<Grid>(Error.New($"Grid height must be positive: {height}"));

            if (width > 1000 || height > 1000)
                return FinFail<Grid>(Error.New($"Grid dimensions too large: {width}x{height} (max 1000x1000)"));

            var tilesBuilder = ImmutableArray.CreateBuilder<Tile>(width * height);

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var position = new Position(x, y);
                    tilesBuilder.Add(Tile.CreateEmpty(position, defaultTerrain));
                }
            }

            return FinSucc(new Grid(
                GridId.NewId(ids),
                width,
                height,
                tilesBuilder.ToImmutable(),
                modData ?? ImmutableDictionary<string, string>.Empty));
        }

        /// <summary>
        /// Creates a grid from an existing collection of tiles.
        /// Used for deserialization, testing, and migration scenarios.
        /// </summary>
        /// <param name="ids">ID generator for creating stable grid identifier</param>
        /// <param name="width">Width of the grid</param>
        /// <param name="height">Height of the grid</param>
        /// <param name="tiles">Pre-configured tiles (must match dimensions)</param>
        /// <param name="modData">Optional mod data</param>
        /// <returns>Success with new Grid or failure with validation error</returns>
        public static Fin<Grid> FromTiles(IStableIdGenerator ids, int width, int height, ImmutableArray<Tile> tiles, ImmutableDictionary<string, string>? modData = null)
        {
            if (width <= 0 || height <= 0)
                return FinFail<Grid>(Error.New($"Invalid dimensions: {width}x{height}"));

            if (tiles.Length != width * height)
                return FinFail<Grid>(Error.New($"Tile count {tiles.Length} doesn't match dimensions {width}x{height} = {width * height}"));

            return FinSucc(new Grid(
                GridId.NewId(ids),
                width,
                height,
                tiles,
                modData ?? ImmutableDictionary<string, string>.Empty));
        }

        /// <summary>
        /// Checks if a position is within the grid bounds.
        /// </summary>
        public bool IsValidPosition(Position position) =>
            position.X >= 0 && position.X < Width &&
            position.Y >= 0 && position.Y < Height;

        /// <summary>
        /// Gets the tile at the specified position.
        /// </summary>
        /// <param name="position">Position to query</param>
        /// <returns>Success with Tile or failure if position is out of bounds</returns>
        public Fin<Tile> GetTile(Position position)
        {
            if (!IsValidPosition(position))
                return FinFail<Tile>(Error.New($"Position {position} is out of bounds for {Width}x{Height} grid"));

            return FinSucc(Tiles[ToIndex(position, Width)]);
        }

        /// <summary>
        /// Sets a tile at the specified position, returning a new Grid.
        /// </summary>
        /// <param name="position">Position to update</param>
        /// <param name="tile">New tile to place</param>
        /// <returns>Success with new Grid or failure if position is invalid</returns>
        public Fin<Grid> SetTile(Position position, Tile tile)
        {
            if (!IsValidPosition(position))
                return FinFail<Grid>(Error.New($"Position {position} is out of bounds for {Width}x{Height} grid"));

            if (tile.Position != position)
                return FinFail<Grid>(Error.New($"Tile position {tile.Position} doesn't match target position {position}"));

            var newTiles = Tiles.SetItem(ToIndex(position, Width), tile);

            return FinSucc(this with { Tiles = newTiles });
        }

        /// <summary>
        /// Places an actor at the specified position, returning a new Grid.
        /// </summary>
        /// <param name="position">Position to place actor</param>
        /// <param name="actorId">Actor to place</param>
        /// <returns>Success with new Grid or failure if position is invalid/occupied</returns>
        public Fin<Grid> PlaceActor(Position position, ActorId actorId)
        {
            return GetTile(position)
                .Bind(tile =>
                {
                    if (tile.IsOccupied)
                        return FinFail<Grid>(Error.New($"Position {position} is already occupied"));

                    return SetTile(position, tile.WithOccupant(actorId));
                });
        }

        /// <summary>
        /// Removes an actor from the specified position, returning a new Grid.
        /// </summary>
        /// <param name="position">Position to clear</param>
        /// <returns>Success with new Grid or failure if position is invalid/empty</returns>
        public Fin<Grid> RemoveActor(Position position)
        {
            return GetTile(position)
                .Bind(tile =>
                {
                    if (tile.IsEmpty)
                        return FinFail<Grid>(Error.New($"Position {position} is not occupied"));

                    return SetTile(position, tile.WithoutOccupant());
                });
        }

        /// <summary>
        /// Sets the terrain type at the specified position, returning a new Grid.
        /// </summary>
        /// <param name="position">Position to update</param>
        /// <param name="terrainType">New terrain type</param>
        /// <returns>Success with new Grid or failure if position is invalid</returns>
        public Fin<Grid> SetTerrain(Position position, TerrainType terrainType)
        {
            return GetTile(position)
                .Bind(tile => SetTile(position, tile.WithTerrain(terrainType)));
        }

        /// <summary>
        /// Gets all orthogonal neighbors of a position that are within bounds.
        /// </summary>
        /// <param name="position">Center position</param>
        /// <returns>Collection of neighbor tiles</returns>
        public Seq<Tile> GetOrthogonalNeighbors(Position position)
        {
            var neighborPositions = position.GetOrthogonallyAdjacentPositions();

            return Seq(neighborPositions
                .Where(IsValidPosition)
                .Select(pos => GetTile(pos))
                .Where(result => result.IsSucc)
                .Select(result => result.IfFail(Tile.CreateEmpty(Position.Zero)))); // Should never happen due to bounds check
        }

        /// <summary>
        /// Gets all adjacent neighbors (including diagonals) of a position that are within bounds.
        /// </summary>
        /// <param name="position">Center position</param>
        /// <returns>Collection of neighbor tiles</returns>
        public Seq<Tile> GetAllNeighbors(Position position)
        {
            var neighborPositions = position.GetAllAdjacentPositions();

            return Seq(neighborPositions
                .Where(IsValidPosition)
                .Select(pos => GetTile(pos))
                .Where(result => result.IsSucc)
                .Select(result => result.IfFail(Tile.CreateEmpty(Position.Zero)))); // Should never happen due to bounds check
        }

        /// <summary>
        /// Gets all tiles that are currently occupied by actors.
        /// </summary>
        /// <returns>Sequence of occupied tiles</returns>
        public Seq<Tile> GetOccupiedTiles() =>
            Seq(Tiles.Where(tile => tile.IsOccupied));

        /// <summary>
        /// Gets all empty tiles that can be moved to.
        /// </summary>
        /// <returns>Sequence of passable empty tiles</returns>
        public Seq<Tile> GetPassableTiles() =>
            Seq(Tiles.Where(tile => tile.IsPassable));

        /// <summary>
        /// Finds the position of an actor on the grid.
        /// </summary>
        /// <param name="actorId">Actor to find</param>
        /// <returns>Position of the actor, or None if not found</returns>
        public Option<Position> FindActor(ActorId actorId)
        {
            var tile = Tiles.FirstOrDefault(t =>
                t.Occupant.Match(
                    Some: occupant => occupant.Value == actorId.Value,
                    None: () => false));

            return tile.Position == Position.Zero && !IsValidPosition(Position.Zero)
                ? None
                : Some(tile.Position);
        }

        /// <summary>
        /// Converts 2D position to 1D array index.
        /// Uses row-major ordering (Y * width + X).
        /// </summary>
        private static int ToIndex(Position position, int width) =>
            position.Y * width + position.X;

        /// <summary>
        /// Converts 1D array index to 2D position.
        /// </summary>
        private static Position FromIndex(int index, int width) =>
            new(index % width, index / width);

        public override string ToString() =>
            $"Grid({Width}x{Height}, {GetOccupiedTiles().Count} occupied)";
    }
}
