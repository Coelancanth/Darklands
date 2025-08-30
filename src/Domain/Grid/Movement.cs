using System;
using System.Linq;
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

namespace Darklands.Core.Domain.Grid
{
    /// <summary>
    /// Represents a movement from one position to another on the combat grid.
    /// Immutable value object that validates movement legality and calculates path properties.
    /// </summary>
    public readonly record struct Movement
    {
        /// <summary>
        /// Starting position of the movement.
        /// </summary>
        public Position From { get; }

        /// <summary>
        /// Destination position of the movement.
        /// </summary>
        public Position To { get; }

        /// <summary>
        /// Manhattan distance of this movement (for movement costs).
        /// </summary>
        public int ManhattanDistance => From.ManhattanDistanceTo(To);

        /// <summary>
        /// Euclidean distance of this movement (for range calculations).
        /// </summary>
        public double EuclideanDistance => From.EuclideanDistanceTo(To);

        /// <summary>
        /// Vector representing the direction and magnitude of movement.
        /// </summary>
        public Position Delta => To - From;

        /// <summary>
        /// Whether this movement is a diagonal move.
        /// </summary>
        public bool IsDiagonal => Math.Abs(Delta.X) > 0 && Math.Abs(Delta.Y) > 0;

        /// <summary>
        /// Whether this movement is orthogonal (not diagonal).
        /// </summary>
        public bool IsOrthogonal => !IsDiagonal;

        /// <summary>
        /// Whether this is a single-step movement (adjacent positions).
        /// </summary>
        public bool IsSingleStep => ManhattanDistance <= (IsDiagonal ? 2 : 1);

        /// <summary>
        /// Creates a movement from one position to another.
        /// </summary>
        public Movement(Position from, Position to)
        {
            From = from;
            To = to;
        }

        /// <summary>
        /// Creates a movement with validation.
        /// </summary>
        /// <param name="from">Starting position</param>
        /// <param name="to">Destination position</param>
        /// <returns>Success with Movement or failure with validation error</returns>
        public static Fin<Movement> Create(Position from, Position to)
        {
            if (from == to)
                return FinFail<Movement>(Error.New("Movement cannot have the same start and end position"));

            return FinSucc(new Movement(from, to));
        }

        /// <summary>
        /// Validates this movement against a grid, checking bounds and passability.
        /// </summary>
        /// <param name="grid">Grid to validate against</param>
        /// <returns>Success with Movement or failure with validation error</returns>
        public Fin<Movement> ValidateAgainst(Grid grid)
        {
            var movement = this;

            // Check if both positions are within grid bounds
            if (!grid.IsValidPosition(movement.From))
                return FinFail<Movement>(Error.New($"Starting position {movement.From} is out of bounds"));

            if (!grid.IsValidPosition(movement.To))
                return FinFail<Movement>(Error.New($"Destination position {movement.To} is out of bounds"));

            // Check if destination is passable
            return grid.GetTile(movement.To)
                .Bind(destinationTile =>
                {
                    if (!destinationTile.IsPassable)
                        return FinFail<Movement>(Error.New($"Destination {movement.To} is not passable (terrain: {destinationTile.TerrainType}, occupied: {destinationTile.IsOccupied})"));

                    return FinSucc(movement);
                });
        }

        /// <summary>
        /// Calculates a simple straight-line path between positions.
        /// Uses Bresenham-like algorithm to get intermediate positions.
        /// </summary>
        /// <returns>Sequence of positions from start to end (inclusive)</returns>
        public Seq<Position> CalculatePath()
        {
            var positions = new System.Collections.Generic.List<Position>();

            var dx = Math.Abs(Delta.X);
            var dy = Math.Abs(Delta.Y);
            var stepX = From.X < To.X ? 1 : -1;
            var stepY = From.Y < To.Y ? 1 : -1;

            var currentX = From.X;
            var currentY = From.Y;
            var error = dx - dy;

            while (true)
            {
                positions.Add(new Position(currentX, currentY));

                if (currentX == To.X && currentY == To.Y)
                    break;

                var error2 = error * 2;

                if (error2 > -dy)
                {
                    error -= dy;
                    currentX += stepX;
                }

                if (error2 < dx)
                {
                    error += dx;
                    currentY += stepY;
                }
            }

            return Seq(positions.AsEnumerable());
        }

        /// <summary>
        /// Validates that the entire path is passable on the given grid.
        /// </summary>
        /// <param name="grid">Grid to validate against</param>
        /// <returns>Success with path or failure with blocking position</returns>
        public Fin<Seq<Position>> ValidatePathPassability(Grid grid)
        {
            var path = CalculatePath();

            // Skip the starting position (assumed to be occupied by the moving actor)
            var pathToCheck = path.Skip(1);

            foreach (var position in pathToCheck)
            {
                var tileResult = grid.GetTile(position);
                if (tileResult.IsFail)
                    return FinFail<Seq<Position>>(Error.New($"Position {position} is out of bounds"));

                var tile = tileResult.IfFail(Tile.CreateEmpty(Position.Zero));
                if (!tile.IsPassable)
                    return FinFail<Seq<Position>>(Error.New($"Path blocked at position {position} (terrain: {tile.TerrainType}, occupied: {tile.IsOccupied})"));
            }

            return FinSucc(path);
        }

        /// <summary>
        /// Checks if there is a clear line of sight between the two positions.
        /// </summary>
        /// <param name="grid">Grid to check line of sight against</param>
        /// <returns>True if line of sight is clear, false if blocked</returns>
        public bool HasClearLineOfSight(Grid grid)
        {
            var path = CalculatePath();

            // Check all intermediate positions (excluding start and end)
            var intermediatePositions = path.Skip(1).Take(path.Count - 2);

            return intermediatePositions.All(position =>
            {
                var tileResult = grid.GetTile(position);
                if (tileResult.IsFail)
                    return false; // Out of bounds blocks line of sight

                var tile = tileResult.IfFail(Tile.CreateEmpty(Position.Zero));
                return !tile.BlocksLineOfSight;
            });
        }

        /// <summary>
        /// Calculates the movement cost based on distance and terrain.
        /// </summary>
        /// <param name="grid">Grid to calculate cost against</param>
        /// <returns>Movement cost or error if path is invalid</returns>
        public Fin<int> CalculateMovementCost(Grid grid)
        {
            var isDiagonal = this.IsDiagonal;
            var euclideanDistance = this.EuclideanDistance;
            var manhattanDistance = this.ManhattanDistance;

            return ValidatePathPassability(grid)
                .Map(path =>
                {
                    var baseCost = isDiagonal ? (int)Math.Ceiling(euclideanDistance) : manhattanDistance;

                    // Add terrain penalties for intermediate tiles
                    var terrainPenalty = 0;
                    foreach (var position in path.Skip(1)) // Skip starting position
                    {
                        var tileResult = grid.GetTile(position);
                        if (tileResult.IsSucc)
                        {
                            var tile = tileResult.IfFail(Tile.CreateEmpty(Position.Zero));
                            terrainPenalty += GetTerrainMovementPenalty(tile.TerrainType);
                        }
                    }

                    return baseCost + terrainPenalty;
                });
        }

        /// <summary>
        /// Gets the movement cost penalty for different terrain types.
        /// </summary>
        private static int GetTerrainMovementPenalty(TerrainType terrainType) => terrainType switch
        {
            TerrainType.Open => 0,
            TerrainType.Forest => 1,    // +1 movement cost
            TerrainType.Rocky => 1,     // +1 movement cost
            TerrainType.Hill => 2,      // +2 movement cost (uphill)
            TerrainType.Swamp => 3,     // +3 movement cost (difficult terrain)
            TerrainType.Water => 100,   // Effectively impassable
            TerrainType.Wall => 100,    // Effectively impassable
            _ => 0
        };

        public override string ToString() =>
            $"Movement({From} → {To}, distance: {ManhattanDistance}, {(IsDiagonal ? "diagonal" : "orthogonal")})";
    }
}
