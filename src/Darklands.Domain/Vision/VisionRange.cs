using System;
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;
using Darklands.Domain.Grid;

namespace Darklands.Domain.Vision
{
    /// <summary>
    /// Represents the vision range of an actor in grid tiles.
    /// Immutable value object that ensures valid vision ranges.
    /// </summary>
    public readonly record struct VisionRange
    {
        /// <summary>
        /// The vision range in tiles (integer distance).
        /// </summary>
        public int Value { get; }

        /// <summary>
        /// Minimum allowed vision range.
        /// </summary>
        public const int MinRange = 0;

        /// <summary>
        /// Maximum allowed vision range (prevents performance issues).
        /// </summary>
        public const int MaxRange = 20;

        /// <summary>
        /// Standard vision ranges for different actor types.
        /// </summary>
        public static readonly VisionRange Player = new(8);
        public static readonly VisionRange Goblin = new(5);
        public static readonly VisionRange Orc = new(6);
        public static readonly VisionRange Eagle = new(12);
        public static readonly VisionRange Blind = new(0);

        /// <summary>
        /// Creates a new vision range with the specified value.
        /// </summary>
        private VisionRange(int value)
        {
            Value = value;
        }

        /// <summary>
        /// Creates a vision range with validation.
        /// </summary>
        /// <param name="value">Vision range in tiles</param>
        /// <returns>Success with VisionRange or failure with validation error</returns>
        public static Fin<VisionRange> Create(int value)
        {
            if (value < MinRange)
                return FinFail<VisionRange>(Error.New($"Vision range cannot be negative (was {value})"));

            if (value > MaxRange)
                return FinFail<VisionRange>(Error.New($"Vision range cannot exceed {MaxRange} (was {value})"));

            return FinSucc(new VisionRange(value));
        }

        /// <summary>
        /// Checks if a position is within this vision range from an origin.
        /// Uses Euclidean distance for circular vision.
        /// </summary>
        public bool IsInRange(Position origin, Position target)
        {
            var dx = target.X - origin.X;
            var dy = target.Y - origin.Y;
            var distanceSquared = dx * dx + dy * dy;
            var rangeSquared = Value * Value;

            // Use squared values to avoid floating point math
            return distanceSquared <= rangeSquared;
        }

        /// <summary>
        /// Gets the bounding box for this vision range from an origin.
        /// Useful for optimization to limit FOV calculation area.
        /// </summary>
        public (Position min, Position max) GetBounds(Position origin)
        {
            var min = new Position(origin.X - Value, origin.Y - Value);
            var max = new Position(origin.X + Value, origin.Y + Value);
            return (min, max);
        }

        public override string ToString() => $"VisionRange({Value})";

        // Implicit conversion from common integer values for convenience
        public static implicit operator VisionRange(int value)
        {
            var result = Create(value);
            if (result.IsFail)
                throw new ArgumentException($"Invalid vision range: {value}");
            return result.IfFail(Blind);
        }
    }
}
