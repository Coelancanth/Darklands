using System;

namespace Darklands.Tactical.Domain.ValueObjects;

/// <summary>
/// Represents a deterministic unit of time in the tactical combat system.
/// Uses integer values to ensure perfect reproducibility and avoid floating-point precision issues.
/// </summary>
public readonly record struct TimeUnit : IComparable<TimeUnit>, IComparable
{
    /// <summary>
    /// The raw integer value representing time units.
    /// </summary>
    public int Value { get; }

    /// <summary>
    /// Creates a new TimeUnit with the specified value.
    /// </summary>
    public TimeUnit(int value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Time unit cannot be negative");

        Value = value;
    }

    /// <summary>
    /// Represents zero time units.
    /// </summary>
    public static TimeUnit Zero => new(0);

    /// <summary>
    /// Represents one standard turn duration (100 time units).
    /// </summary>
    public static TimeUnit OneTurn => new(100);

    /// <summary>
    /// Represents half a turn duration (50 time units).
    /// </summary>
    public static TimeUnit HalfTurn => new(50);

    /// <summary>
    /// Represents a quick action duration (25 time units).
    /// </summary>
    public static TimeUnit QuickAction => new(25);

    /// <summary>
    /// Creates a TimeUnit from a number of turns.
    /// </summary>
    public static TimeUnit FromTurns(int turns) => new(turns * 100);

    /// <summary>
    /// Converts this TimeUnit to a number of turns (rounded down).
    /// </summary>
    public int ToTurns() => Value / 100;

    /// <summary>
    /// Adds two TimeUnits together.
    /// </summary>
    public static TimeUnit operator +(TimeUnit left, TimeUnit right) =>
        new(left.Value + right.Value);

    /// <summary>
    /// Subtracts one TimeUnit from another.
    /// </summary>
    public static TimeUnit operator -(TimeUnit left, TimeUnit right)
    {
        var result = left.Value - right.Value;
        if (result < 0)
            throw new InvalidOperationException("TimeUnit subtraction would result in negative value");

        return new TimeUnit(result);
    }

    /// <summary>
    /// Multiplies a TimeUnit by a scalar value.
    /// </summary>
    public static TimeUnit operator *(TimeUnit time, int scalar)
    {
        if (scalar < 0)
            throw new ArgumentOutOfRangeException(nameof(scalar), "Cannot multiply TimeUnit by negative value");

        return new TimeUnit(time.Value * scalar);
    }

    /// <summary>
    /// Multiplies a TimeUnit by a scalar value.
    /// </summary>
    public static TimeUnit operator *(int scalar, TimeUnit time) => time * scalar;

    /// <summary>
    /// Divides a TimeUnit by a scalar value.
    /// </summary>
    public static TimeUnit operator /(TimeUnit time, int divisor)
    {
        if (divisor <= 0)
            throw new ArgumentOutOfRangeException(nameof(divisor), "Cannot divide TimeUnit by zero or negative value");

        return new TimeUnit(time.Value / divisor);
    }

    /// <summary>
    /// Checks if one TimeUnit is less than another.
    /// </summary>
    public static bool operator <(TimeUnit left, TimeUnit right) => left.Value < right.Value;

    /// <summary>
    /// Checks if one TimeUnit is less than or equal to another.
    /// </summary>
    public static bool operator <=(TimeUnit left, TimeUnit right) => left.Value <= right.Value;

    /// <summary>
    /// Checks if one TimeUnit is greater than another.
    /// </summary>
    public static bool operator >(TimeUnit left, TimeUnit right) => left.Value > right.Value;

    /// <summary>
    /// Checks if one TimeUnit is greater than or equal to another.
    /// </summary>
    public static bool operator >=(TimeUnit left, TimeUnit right) => left.Value >= right.Value;

    /// <summary>
    /// Compares this TimeUnit to another.
    /// </summary>
    public int CompareTo(TimeUnit other) => Value.CompareTo(other.Value);

    /// <summary>
    /// Compares this TimeUnit to another object.
    /// </summary>
    public int CompareTo(object? obj)
    {
        if (obj is null) return 1;
        if (obj is TimeUnit other) return CompareTo(other);
        throw new ArgumentException($"Object must be of type {nameof(TimeUnit)}");
    }

    /// <summary>
    /// Returns a string representation of this TimeUnit.
    /// </summary>
    public override string ToString() => $"{Value} time units";

    /// <summary>
    /// Implicitly converts an integer to a TimeUnit.
    /// </summary>
    public static implicit operator TimeUnit(int value) => new(value);

    /// <summary>
    /// Explicitly converts a TimeUnit to an integer.
    /// </summary>
    public static explicit operator int(TimeUnit time) => time.Value;
}
