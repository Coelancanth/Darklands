using LanguageExt;
using LanguageExt.Common;

namespace Darklands.Core.Domain.Combat;

/// <summary>
/// Core business logic for calculating time units in combat.
/// This is the heart of the Darklands combat system, determining how long actions take
/// based on weapon speed, character agility, and equipment encumbrance.
/// 
/// Formula: FinalTime = BaseTime * (100/Agility) * (1 + Encumbrance * 0.1)
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
            // Core time calculation formula
            var baseTime = action.BaseCost.Value;
            var agilityModifier = 100.0 / agility;
            var encumbranceModifier = 1.0 + (encumbrance * 0.1);

            var finalTime = (int)Math.Round(baseTime * agilityModifier * encumbranceModifier);

            // Ensure result is within valid bounds
            return TimeUnit.FromMilliseconds(finalTime);
        }
        catch (Exception ex)
        {
            return Fin<TimeUnit>.Fail(Error.New($"Failed to calculate action time: {ex.Message}", ex));
        }
    }

    /// <summary>
    /// Calculates the time penalty multiplier for a given encumbrance level.
    /// Higher encumbrance means slower actions.
    /// </summary>
    /// <param name="encumbrance">Encumbrance level (0-50)</param>
    /// <returns>Multiplier for time calculations (1.0 = no penalty, 6.0 = maximum penalty)</returns>
    public static Fin<double> CalculateEncumbrancePenalty(int encumbrance)
    {
        if (encumbrance < MinimumEncumbrance || encumbrance > MaximumEncumbrance)
            return Fin<double>.Fail(Error.New($"Encumbrance must be between {MinimumEncumbrance} and {MaximumEncumbrance}, got: {encumbrance}"));

        return Fin<double>.Succ(1.0 + (encumbrance * 0.1));
    }

    /// <summary>
    /// Calculates the time bonus multiplier for a given agility score.
    /// Higher agility means faster actions.
    /// </summary>
    /// <param name="agility">Agility score (1-100)</param>
    /// <returns>Multiplier for time calculations (1.0 = average, 0.1 = maximum bonus)</returns>
    public static Fin<double> CalculateAgilityBonus(int agility)
    {
        if (agility < MinimumAgility || agility > MaximumAgility)
            return Fin<double>.Fail(Error.New($"Agility must be between {MinimumAgility} and {MaximumAgility}, got: {agility}"));

        return Fin<double>.Succ(100.0 / agility);
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
                   Math.Abs(timeA.Value - timeB.Value));
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
