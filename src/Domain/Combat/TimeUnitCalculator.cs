using LanguageExt;
using LanguageExt.Common;

namespace Darklands.Core.Domain.Combat;

/// <summary>
/// Core business logic for calculating time units in combat.
/// This is the heart of the Darklands combat system, determining how long actions take
/// based on weapon speed, character agility, and equipment encumbrance.
/// 
/// Formula: FinalTime = (BaseTime * 100 * (10 + Encumbrance)) / (Agility * 10)
/// Uses deterministic integer arithmetic to ensure identical results across all platforms.
/// </summary>
public static class TimeUnitCalculator
{
    /// <summary>
    /// Minimum valid agility score (prevents division by zero)
    /// </summary>
    public const int MinimumAgility = 1;

    /// <summary>
    /// Maximum reasonable agility score for game balance
    /// </summary>
    public const int MaximumAgility = 100;

    /// <summary>
    /// Minimum encumbrance (unencumbered)
    /// </summary>
    public const int MinimumEncumbrance = 0;

    /// <summary>
    /// Maximum encumbrance before actions become prohibitively slow
    /// </summary>
    public const int MaximumEncumbrance = 50;

    /// <summary>
    /// Calculates the actual time units required to perform a combat action.
    /// Takes into account the base action cost, character agility, and current encumbrance.
    /// </summary>
    /// <param name="action">The combat action being performed</param>
    /// <param name="agility">Character's agility score (1-100)</param>
    /// <param name="encumbrance">Current encumbrance level (0-50)</param>
    /// <returns>Calculated time units or error if parameters are invalid</returns>
    public static Fin<TimeUnit> CalculateActionTime(
        CombatAction action,
        int agility,
        int encumbrance)
    {
        // Validate inputs
        if (!action.IsValid)
            return Fin<TimeUnit>.Fail(Error.New($"Invalid combat action: {action.Name}"));

        if (agility < MinimumAgility || agility > MaximumAgility)
            return Fin<TimeUnit>.Fail(Error.New($"Agility must be between {MinimumAgility} and {MaximumAgility}, got: {agility}"));

        if (encumbrance < MinimumEncumbrance || encumbrance > MaximumEncumbrance)
            return Fin<TimeUnit>.Fail(Error.New($"Encumbrance must be between {MinimumEncumbrance} and {MaximumEncumbrance}, got: {encumbrance}"));

        try
        {
            // Deterministic integer arithmetic - same result on all platforms
            var baseTime = action.BaseCost.Value;

            // Formula: (BaseTime * 100 * (10 + Encumbrance)) / (Agility * 10)
            // This avoids all floating-point operations while preserving the original formula's intent
            var numerator = baseTime * 100 * (10 + encumbrance);
            var denominator = agility * 10;

            // Integer division with proper rounding (add half denominator for round-to-nearest)
            var finalTime = (numerator + denominator / 2) / denominator;

            // Ensure result is within valid bounds
            return TimeUnit.FromTU(finalTime);
        }
        catch (Exception ex)
        {
            return Fin<TimeUnit>.Fail(Error.New($"Failed to calculate action time: {ex.Message}", ex));
        }
    }

    /// <summary>
    /// Calculates the encumbrance factor for time calculations.
    /// Higher encumbrance means slower actions. Returns integer factor for deterministic math.
    /// </summary>
    /// <param name="encumbrance">Encumbrance level (0-50)</param>
    /// <returns>Encumbrance factor as integer (10 = no penalty, 60 = maximum penalty)</returns>
    public static Fin<int> CalculateEncumbranceFactor(int encumbrance)
    {
        if (encumbrance < MinimumEncumbrance || encumbrance > MaximumEncumbrance)
            return Fin<int>.Fail(Error.New($"Encumbrance must be between {MinimumEncumbrance} and {MaximumEncumbrance}, got: {encumbrance}"));

        return Fin<int>.Succ(10 + encumbrance);
    }

    /// <summary>
    /// Validates agility is within acceptable bounds for calculations.
    /// Higher agility means faster actions.
    /// </summary>
    /// <param name="agility">Agility score (1-100)</param>
    /// <returns>Success if valid, error if out of bounds</returns>
    public static Fin<int> ValidateAgilityScore(int agility)
    {
        if (agility < MinimumAgility || agility > MaximumAgility)
            return Fin<int>.Fail(Error.New($"Agility must be between {MinimumAgility} and {MaximumAgility}, got: {agility}"));

        return Fin<int>.Succ(agility);
    }

    /// <summary>
    /// Calculates comparative action speeds for tactical decision making.
    /// Returns which action is faster and by how much.
    /// </summary>
    public static Fin<ActionComparison> CompareActionSpeeds(
        CombatAction actionA,
        CombatAction actionB,
        int agility,
        int encumbrance)
    {
        return from timeA in CalculateActionTime(actionA, agility, encumbrance)
               from timeB in CalculateActionTime(actionB, agility, encumbrance)
               select new ActionComparison(
                   actionA.Name,
                   timeA,
                   actionB.Name,
                   timeB,
                   timeA < timeB ? actionA.Name : actionB.Name,
                   timeA.Value > timeB.Value ? timeA.Value - timeB.Value : timeB.Value - timeA.Value);
    }
}

/// <summary>
/// Result of comparing two combat actions for speed
/// </summary>
public readonly record struct ActionComparison(
    string ActionAName,
    TimeUnit TimeA,
    string ActionBName,
    TimeUnit TimeB,
    string FasterAction,
    int TimeDifferenceMs);
