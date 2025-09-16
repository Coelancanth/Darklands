using System;

namespace Darklands.Domain.Grid
{
    /// <summary>
    /// Represents a 2D integer coordinate position on the combat grid.
    /// Immutable value object that provides safe coordinate operations for tactical combat.
    /// </summary>
    public readonly record struct Position(int X, int Y)
    {
        public static readonly Position Zero = new(0, 0);
        public static readonly Position One = new(1, 1);
        public static readonly Position North = new(0, 1);
        public static readonly Position South = new(0, -1);
        public static readonly Position West = new(-1, 0);
        public static readonly Position East = new(1, 0);

        /// <summary>
        /// Calculates the Manhattan distance between two positions.
        /// Used for movement cost calculations in tactical combat.
        /// </summary>
        public int ManhattanDistanceTo(Position other) =>
            Math.Abs(X - other.X) + Math.Abs(Y - other.Y);

        /// <summary>
        /// Calculates the Euclidean distance between two positions.
        /// Used for range calculations and line of sight.
        /// </summary>
        public double EuclideanDistanceTo(Position other)
        {
            var dx = X - other.X;
            var dy = Y - other.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// Checks if this position is adjacent to another position (including diagonals).
        /// </summary>
        public bool IsAdjacentTo(Position other) =>
            ManhattanDistanceTo(other) <= 2 && this != other &&
            Math.Abs(X - other.X) <= 1 && Math.Abs(Y - other.Y) <= 1;

        /// <summary>
        /// Checks if this position is orthogonally adjacent (not diagonal).
        /// Used for movement validation in grid-based combat.
        /// </summary>
        public bool IsOrthogonallyAdjacentTo(Position other) =>
            ManhattanDistanceTo(other) == 1;

        /// <summary>
        /// Gets all orthogonally adjacent positions (North, South, East, West).
        /// </summary>
        public Position[] GetOrthogonallyAdjacentPositions() => new[]
        {
            this + North,
            this + South,
            this + East,
            this + West
        };

        /// <summary>
        /// Gets all adjacent positions including diagonals.
        /// </summary>
        public Position[] GetAllAdjacentPositions() => new[]
        {
            // Orthogonal
            this + North,
            this + South,
            this + East,
            this + West,
            // Diagonal
            this + North + West,  // Northwest
            this + North + East,  // Northeast
            this + South + West,  // Southwest
            this + South + East   // Southeast
        };

        public static Position operator +(Position a, Position b) => new(a.X + b.X, a.Y + b.Y);
        public static Position operator -(Position a, Position b) => new(a.X - b.X, a.Y - b.Y);
        public static Position operator *(Position a, int scalar) => new(a.X * scalar, a.Y * scalar);

        public override string ToString() => $"({X}, {Y})";
    }
}
