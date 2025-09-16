using System;
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

namespace Darklands.Domain.Combat;

/// <summary>
/// Represents a unit of time in combat, measured in abstract Time Units (TU).
/// Time units are the fundamental currency of combat actions in Darklands.
/// All combat actions consume time units based on weapon speed, character agility, and encumbrance.
/// 
/// SAFETY: Constructor is private to prevent invalid instances. Use Create() or FromTU().
/// </summary>
public readonly record struct TimeUnit
{
    /// <summary>
    /// The time value in Time Units (TU). Always valid due to private constructor.
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
    /// Minimum valid time unit (1 TU) - no action can be faster
    /// </summary>
    public static readonly TimeUnit Minimum = new(1);

    /// <summary>
    /// Maximum reasonable time unit for game balance (10,000 TU)
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
    /// <param name="timeUnits">Time in Time Units (0 to 10,000)</param>
    /// <returns>Success with TimeUnit or failure with validation error</returns>
    public static Fin<TimeUnit> Create(int timeUnits)
    {
        if (timeUnits < 0)
            return FinFail<TimeUnit>(Error.New($"Time units cannot be negative: {timeUnits}"));

        if (timeUnits > Maximum.Value)
            return FinFail<TimeUnit>(Error.New($"Time units cannot exceed maximum ({Maximum.Value} TU): {timeUnits}"));

        return FinSucc(new TimeUnit(timeUnits));
    }

    /// <summary>
    /// Creates a time unit from Time Units with validation (alias for Create)
    /// </summary>
    public static Fin<TimeUnit> FromTU(int timeUnits)
        => Create(timeUnits);

    /// <summary>
    /// Factory for creating TimeUnits with known-valid values.
    /// Used by tests and Common combat actions.
    /// THROWS on invalid values - only use with compile-time known valid values.
    /// </summary>
    public static TimeUnit CreateUnsafe(int timeUnits)
    {
        var result = Create(timeUnits);
        return result.Match(
            Succ: timeUnit => timeUnit,
            Fail: error => throw new InvalidOperationException($"CreateUnsafe called with invalid value: {error}"));
    }

    /// <summary>
    /// Adds two time units together, clamping to maximum to prevent overflow
    /// </summary>
    public static TimeUnit operator +(TimeUnit a, TimeUnit b)
        => new(a.Value + b.Value > Maximum.Value ? Maximum.Value : a.Value + b.Value);

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
        => new(a.Value > b.Value ? a.Value - b.Value : 0);

    /// <summary>
    /// Multiplies time units by an integer factor with overflow protection.
    /// For fractional scaling, use ScaleBy method with explicit numerator/denominator.
    /// </summary>
    public static TimeUnit operator *(TimeUnit time, int factor)
    {
        if (factor <= 0) return Zero;
        var result = (long)time.Value * factor;
        return new(result > Maximum.Value ? Maximum.Value : (int)result);
    }

    /// <summary>
    /// Multiplies time units by an integer factor
    /// </summary>
    public static TimeUnit operator *(int factor, TimeUnit time)
        => time * factor;

    /// <summary>
    /// Scales time units by a fraction (numerator/denominator) with proper rounding.
    /// This replaces floating-point multiplication with deterministic integer math.
    /// </summary>
    public static TimeUnit ScaleBy(TimeUnit time, int numerator, int denominator)
    {
        if (denominator <= 0) throw new ArgumentException("Denominator must be positive", nameof(denominator));
        if (numerator <= 0) return Zero;

        var scaled = (long)time.Value * numerator;
        var result = (scaled + denominator / 2) / denominator; // Round to nearest
        return new(result > Maximum.Value ? Maximum.Value : (int)result);
    }

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
    /// Converts to a human-readable string with Time Units
    /// </summary>
    public override string ToString()
    {
        return $"{Value} TU";
    }
}
