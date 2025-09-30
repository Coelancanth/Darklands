using CSharpFunctionalExtensions;

namespace Darklands.Core.Domain.Common;

/// <summary>
/// Represents the health state of an actor.
/// Immutable value object with smart constructor validation.
/// </summary>
public sealed record Health
{
    public float Current { get; }
    public float Maximum { get; }

    /// <summary>
    /// Current health as a percentage of maximum (0.0 to 1.0).
    /// </summary>
    public float Percentage => Maximum > 0 ? Current / Maximum : 0;

    /// <summary>
    /// True if current health is zero or less.
    /// </summary>
    public bool IsDepleted => Current <= 0;

    private Health(float current, float maximum)
    {
        Current = current;
        Maximum = maximum;
    }

    /// <summary>
    /// Creates a new Health instance with validation.
    /// </summary>
    /// <param name="current">Current health value (must be >= 0 and <= maximum)</param>
    /// <param name="maximum">Maximum health value (must be > 0)</param>
    /// <returns>Result with Health on success, or failure message on validation error</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// If maximum is zero or negative (programmer error - contract violation)
    /// </exception>
    public static Result<Health> Create(float current, float maximum)
    {
        // PROGRAMMER ERROR: Maximum health must always be positive
        // This is a contract violation - no game should ever have non-positive max health
        if (maximum <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(maximum),
                maximum,
                "Maximum health must be positive");

        // DOMAIN ERROR: Business validation - current health cannot be negative
        if (current < 0)
            return Result.Failure<Health>("Current health cannot be negative");

        // DOMAIN ERROR: Business validation - current cannot exceed maximum
        if (current > maximum)
            return Result.Failure<Health>("Current health cannot exceed maximum");

        return Result.Success(new Health(current, maximum));
    }

    /// <summary>
    /// Reduces health by the specified amount, clamping at zero.
    /// Returns a new Health instance (immutable).
    /// </summary>
    /// <param name="amount">Damage amount (must be >= 0)</param>
    /// <returns>Result with new Health on success, or failure if amount is negative</returns>
    public Result<Health> Reduce(float amount)
    {
        // DOMAIN ERROR: Negative damage is unclear intent
        if (amount < 0)
            return Result.Failure<Health>("Damage cannot be negative");

        // Clamp to zero (can't have negative health)
        var newCurrent = Math.Max(0, Current - amount);
        return Result.Success(new Health(newCurrent, Maximum));
    }

    /// <summary>
    /// Increases health by the specified amount, clamping at maximum.
    /// Returns a new Health instance (immutable).
    /// </summary>
    /// <param name="amount">Heal amount (must be >= 0)</param>
    /// <returns>Result with new Health on success, or failure if amount is negative</returns>
    public Result<Health> Increase(float amount)
    {
        // DOMAIN ERROR: Negative heal amount is unclear intent
        if (amount < 0)
            return Result.Failure<Health>("Heal amount cannot be negative");

        // Clamp to maximum (can't overheal beyond max)
        var newCurrent = Math.Min(Maximum, Current + amount);
        return Result.Success(new Health(newCurrent, Maximum));
    }
}
