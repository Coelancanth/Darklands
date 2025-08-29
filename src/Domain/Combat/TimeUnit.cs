using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

namespace Darklands.Core.Domain.Combat;

/// <summary>
/// Represents a unit of time in combat, measured in milliseconds.
/// Time units are the fundamental currency of combat actions in Darklands.
/// All combat actions consume time units based on weapon speed, character agility, and encumbrance.
/// </summary>
public readonly record struct TimeUnit(int Value)
{
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
    /// Validates that the time unit value is within reasonable bounds
    /// </summary>
    public bool IsValid => Value >= 0 && Value <= Maximum.Value;
    
    /// <summary>
    /// Creates a time unit from milliseconds with validation
    /// </summary>
    public static Fin<TimeUnit> FromMilliseconds(int milliseconds)
    {
        if (milliseconds < 0)
            return Fin<TimeUnit>.Fail(Error.New($"Time units cannot be negative: {milliseconds}"));
            
        if (milliseconds > Maximum.Value)
            return Fin<TimeUnit>.Fail(Error.New($"Time units cannot exceed maximum: {milliseconds} > {Maximum.Value}"));
            
        return Fin<TimeUnit>.Succ(new TimeUnit(milliseconds));
    }
    
    /// <summary>
    /// Adds two time units together
    /// </summary>
    public static TimeUnit operator +(TimeUnit a, TimeUnit b) 
        => new(Math.Min(a.Value + b.Value, Maximum.Value));
    
    /// <summary>
    /// Subtracts time units, never going below zero
    /// </summary>
    public static TimeUnit operator -(TimeUnit a, TimeUnit b)
        => new(Math.Max(0, a.Value - b.Value));
    
    /// <summary>
    /// Multiplies time units by a factor
    /// </summary>
    public static TimeUnit operator *(TimeUnit time, double factor)
        => new((int)Math.Min(time.Value * factor, Maximum.Value));
        
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