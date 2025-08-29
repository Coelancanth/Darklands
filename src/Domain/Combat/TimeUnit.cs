using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

namespace Darklands.Core.Domain.Combat;

/// <summary>
/// Represents a unit of time in combat, measured in milliseconds.
/// Time units are the fundamental currency of combat actions in Darklands.
/// All combat actions consume time units based on weapon speed, character agility, and encumbrance.
/// 
/// SAFETY: Constructor is private to prevent invalid instances. Use Create() or FromMilliseconds().
/// </summary>
public readonly record struct TimeUnit
{
    /// <summary>
    /// The time value in milliseconds. Always valid due to private constructor.
    /// </summary>
    public int Value { get; }

    /// <summary>
    /// Private constructor ensures all instances are valid.
    /// Only accessible through validated factory methods.
    /// </summary>
    private TimeUnit(int value)
    {
        Value = value;
    }
    /// <summary>
    /// Zero time units - represents instantaneous actions or errors
    /// </summary>
    public static readonly TimeUnit Zero = new(0);

    /// <summary>
    /// Minimum valid time unit (1ms) - no action can be faster
    /// </summary>
    public static readonly TimeUnit Minimum = new(1);

    /// <summary>
    /// Maximum reasonable time unit for game balance (10 seconds)
    /// </summary>
    public static readonly TimeUnit Maximum = new(10_000);

    /// <summary>
    /// All TimeUnit instances are always valid due to private constructor.
    /// This property exists for interface compatibility but always returns true.
    /// </summary>
    public bool IsValid => true;

    /// <summary>
    /// Creates a TimeUnit with validation. Primary factory method.
    /// </summary>
    /// <param name="milliseconds">Time in milliseconds (0 to 10,000)</param>
    /// <returns>Success with TimeUnit or failure with validation error</returns>
    public static Fin<TimeUnit> Create(int milliseconds)
    {
        if (milliseconds < 0)
            return FinFail<TimeUnit>(Error.New($"Time units cannot be negative: {milliseconds}"));

        if (milliseconds > Maximum.Value)
            return FinFail<TimeUnit>(Error.New($"Time units cannot exceed maximum ({Maximum.Value}ms): {milliseconds}"));

        return FinSucc(new TimeUnit(milliseconds));
    }

    /// <summary>
    /// Creates a time unit from milliseconds with validation (alias for Create)
    /// </summary>
    public static Fin<TimeUnit> FromMilliseconds(int milliseconds)
        => Create(milliseconds);

    /// <summary>
    /// Factory for creating TimeUnits with known-valid values.
    /// Used by tests and Common combat actions.
    /// THROWS on invalid values - only use with compile-time known valid values.
    /// </summary>
    public static TimeUnit CreateUnsafe(int milliseconds)
    {
        var result = Create(milliseconds);
        return result.Match(
            Succ: timeUnit => timeUnit,
            Fail: error => throw new InvalidOperationException($"CreateUnsafe called with invalid value: {error}"));
    }

    /// <summary>
    /// Adds two time units together, clamping to maximum to prevent overflow
    /// </summary>
    public static TimeUnit operator +(TimeUnit a, TimeUnit b)
        => new(Math.Min(a.Value + b.Value, Maximum.Value));

    /// <summary>
    /// Adds two time units with overflow detection
    /// </summary>
    /// <returns>Success with result or failure if overflow occurs</returns>
    public static Fin<TimeUnit> Add(TimeUnit a, TimeUnit b)
    {
        var sum = (long)a.Value + b.Value;
        if (sum > Maximum.Value)
            return FinFail<TimeUnit>(Error.New($"Time addition overflow: {a.Value} + {b.Value} = {sum} > {Maximum.Value}"));

        return FinSucc(new TimeUnit((int)sum));
    }

    /// <summary>
    /// Subtracts time units, never going below zero
    /// </summary>
    public static TimeUnit operator -(TimeUnit a, TimeUnit b)
        => new(Math.Max(0, a.Value - b.Value));

    /// <summary>
    /// Multiplies time units by a factor with proper rounding
    /// </summary>
    public static TimeUnit operator *(TimeUnit time, double factor)
        => new((int)Math.Min(Math.Round(time.Value * factor), Maximum.Value));

    /// <summary>
    /// Multiplies time units by a factor
    /// </summary>
    public static TimeUnit operator *(double factor, TimeUnit time)
        => time * factor;

    /// <summary>
    /// Compares time units for ordering
    /// </summary>
    public static bool operator <(TimeUnit left, TimeUnit right)
        => left.Value < right.Value;

    /// <summary>
    /// Compares time units for ordering
    /// </summary>
    public static bool operator >(TimeUnit left, TimeUnit right)
        => left.Value > right.Value;

    /// <summary>
    /// Compares time units for ordering
    /// </summary>
    public static bool operator <=(TimeUnit left, TimeUnit right)
        => left.Value <= right.Value;

    /// <summary>
    /// Compares time units for ordering
    /// </summary>
    public static bool operator >=(TimeUnit left, TimeUnit right)
        => left.Value >= right.Value;

    /// <summary>
    /// Converts to a human-readable string with appropriate units
    /// </summary>
    public override string ToString()
    {
        return Value switch
        {
            < 1000 => $"{Value}ms",
            < 60000 => $"{Value / 1000.0:F1}s",
            _ => $"{Value / 60000.0:F1}min"
        };
    }
}
