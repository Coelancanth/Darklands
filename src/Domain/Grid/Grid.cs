using System;
using System.Linq;
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

namespace Darklands.Core.Domain.Grid
{
    /// <summary>
    /// Represents the tactical combat grid containing all tiles and their states.
    /// Immutable domain model that provides safe access to grid positions and tile information.
    /// </summary>
    public sealed record Grid
    {
        /// <summary>
        /// Width of the grid (number of columns).
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// Height of the grid (number of rows).
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// Total number of tiles in the grid.
        /// </summary>
        public int TileCount => Width * Height;

        /// <summary>
        /// Internal tile storage as 1D array for performance.
        /// Access via ToIndex conversion from 2D coordinates.
        /// </summary>
        private readonly Tile[] _tiles;

        /// <summary>
        /// Creates a new grid with the specified dimensions.
        /// Private constructor - use factory methods for creation.
        /// </summary>
        private Grid(int width, int height, Tile[] tiles)
        {
            Width = width;
            Height = height;
            _tiles = tiles;
        }

        /// <summary>
        /// Creates a new empty grid with the specified dimensions and default terrain.
        /// </summary>
        /// <param name="width">Width of the grid (must be > 0)</param>
        /// <param name="height">Height of the grid (must be > 0)</param>
        /// <param name="defaultTerrain">Default terrain for all tiles</param>
        /// <returns>Success with new Grid or failure with validation error</returns>
        public static Fin<Grid> Create(int width, int height, TerrainType defaultTerrain = TerrainType.Open)
        {
            if (width <= 0)
                return FinFail<Grid>(Error.New($"Grid width must be positive: {width}"));

            if (height <= 0)
                return FinFail<Grid>(Error.New($"Grid height must be positive: {height}"));

            if (width > 1000 || height > 1000)
                return FinFail<Grid>(Error.New($"Grid dimensions too large: {width}x{height} (max 1000x1000)"));

            var tiles = new Tile[width * height];

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var position = new Position(x, y);
                    tiles[ToIndex(position, width)] = Tile.CreateEmpty(position, defaultTerrain);
                }
            }

            return FinSucc(new Grid(width, height, tiles));
        }

        /// <summary>
        /// Creates a grid from an existing array of tiles.
        /// Used for deserialization and testing.
        /// </summary>
        /// <param name="width">Width of the grid</param>
        /// <param name="height">Height of the grid</param>
        /// <param name="tiles">Pre-configured tiles (must match dimensions)</param>
        /// <returns>Success with new Grid or failure with validation error</returns>
        public static Fin<Grid> FromTiles(int width, int height, Tile[] tiles)
        {
            if (width <= 0 || height <= 0)
                return FinFail<Grid>(Error.New($"Invalid dimensions: {width}x{height}"));

            if (tiles.Length != width * height)
                return FinFail<Grid>(Error.New($"Tile count {tiles.Length} doesn't match dimensions {width}x{height} = {width * height}"));

            return FinSucc(new Grid(width, height, tiles));
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

            return FinSucc(_tiles[ToIndex(position, Width)]);
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

            var newTiles = new Tile[_tiles.Length];
            System.Array.Copy(_tiles, newTiles, _tiles.Length);
            newTiles[ToIndex(position, Width)] = tile;

            return FinSucc(new Grid(Width, Height, newTiles));
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
        /// Gets all orthogonal neighbors of a position that are within bounds.
        /// </summary>
        /// <param name="position">Center position</param>
        /// <returns>Collection of neighbor tiles</returns>
        public Seq<Tile> GetOrthogonalNeighbors(Position position)
        {
            var neighborPositions = position.GetOrthogonallyAdjacentPositions();

            return neighborPositions
                .Where(IsValidPosition)
                .Select(pos => GetTile(pos))
                .Where(result => result.IsSucc)
                .Select(result => result.IfFail(Tile.CreateEmpty(Position.Zero))) // Should never happen due to bounds check
                .ToSeq();
        }

        /// <summary>
        /// Gets all adjacent neighbors (including diagonals) of a position that are within bounds.
        /// </summary>
        /// <param name="position">Center position</param>
        /// <returns>Collection of neighbor tiles</returns>
        public Seq<Tile> GetAllNeighbors(Position position)
        {
            var neighborPositions = position.GetAllAdjacentPositions();

            return neighborPositions
                .Where(IsValidPosition)
                .Select(pos => GetTile(pos))
                .Where(result => result.IsSucc)
                .Select(result => result.IfFail(Tile.CreateEmpty(Position.Zero))) // Should never happen due to bounds check
                .ToSeq();
        }

        /// <summary>
        /// Gets all tiles that are currently occupied by actors.
        /// </summary>
        /// <returns>Sequence of occupied tiles</returns>
        public Seq<Tile> GetOccupiedTiles() =>
            _tiles.Where(tile => tile.IsOccupied).ToSeq();

        /// <summary>
        /// Gets all empty tiles that can be moved to.
        /// </summary>
        /// <returns>Sequence of passable empty tiles</returns>
        public Seq<Tile> GetPassableTiles() =>
            _tiles.Where(tile => tile.IsPassable).ToSeq();

        /// <summary>
        /// Finds the position of an actor on the grid.
        /// </summary>
        /// <param name="actorId">Actor to find</param>
        /// <returns>Position of the actor, or None if not found</returns>
        public Option<Position> FindActor(ActorId actorId)
        {
            var tile = _tiles.FirstOrDefault(t =>
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
