using CSharpFunctionalExtensions;

namespace Darklands.Core.Features.Combat.Domain;

/// <summary>
/// Represents time units for turn-based combat scheduling.
/// Value type with value semantics - two TimeUnits with the same Value are equal.
/// </summary>
/// <remarks>
/// DESIGN: Relative time per combat session (resets to 0 when combat starts/ends).
/// All actions cost time units (movement = 100 units by default for MVP).
///
/// WHY: Type safety prevents mixing time with other integers.
/// Encapsulates time arithmetic and prevents negative time.
/// </remarks>
public readonly record struct TimeUnits : IComparable<TimeUnits>
{
    public int Value { get; }

    private TimeUnits(int value)
    {
        Value = value;
    }

    /// <summary>
    /// Zero time - exploration mode and combat start state.
    /// </summary>
    public static TimeUnits Zero => new(0);

    /// <summary>
    /// Standard movement cost (10 time units per tile).
    /// </summary>
    /// <remarks>
    /// WHY 10: Smaller numbers are easier to reason about mentally (3 moves = 30 time).
    ///
    /// FUTURE: Diagonal movement cost consideration
    /// - Current: Uniform cost (orthogonal and diagonal both cost 10)
    /// - Realistic option: Diagonal = 14 (approximates √2 ≈ 1.41× distance)
    /// - Design decision: Start simple (uniform), add differential cost if tactical AI needs it
    /// </remarks>
    public static TimeUnits MovementCost => new(10);

    /// <summary>
    /// Creates a TimeUnits from a non-negative integer.
    /// </summary>
    /// <param name="value">Time units value (must be >= 0)</param>
    /// <returns>Success with TimeUnits, or Failure if negative</returns>
    public static Result<TimeUnits> Create(int value)
    {
        if (value < 0)
            return Result.Failure<TimeUnits>("Time units cannot be negative");

        return Result.Success(new TimeUnits(value));
    }

    /// <summary>
    /// Adds time units (for action costs).
    /// </summary>
    public Result<TimeUnits> Add(TimeUnits other) =>
        Create(Value + other.Value);

    /// <summary>
    /// Subtracts time units (rarely needed, but included for completeness).
    /// </summary>
    public Result<TimeUnits> Subtract(TimeUnits other) =>
        Create(Value - other.Value);

    /// <summary>
    /// Compares two TimeUnits for ordering in priority queue.
    /// </summary>
    public int CompareTo(TimeUnits other) => Value.CompareTo(other.Value);

    public static bool operator <(TimeUnits left, TimeUnits right) => left.Value < right.Value;
    public static bool operator >(TimeUnits left, TimeUnits right) => left.Value > right.Value;
    public static bool operator <=(TimeUnits left, TimeUnits right) => left.Value <= right.Value;
    public static bool operator >=(TimeUnits left, TimeUnits right) => left.Value >= right.Value;

    public override string ToString() => $"{Value} time units";
}
