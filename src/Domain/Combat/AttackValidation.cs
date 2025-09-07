using LanguageExt;
using LanguageExt.Common;
using Darklands.Core.Domain.Grid;
using static LanguageExt.Prelude;

namespace Darklands.Core.Domain.Combat;

/// <summary>
/// Represents the validation result for a melee attack attempt.
/// Validates adjacency rules, target state, and basic attack preconditions.
/// 
/// SAFETY: Constructor is private to prevent invalid instances. Use Create().
/// </summary>
public readonly record struct AttackValidation
{
    /// <summary>
    /// The actor performing the attack
    /// </summary>
    public ActorId AttackerId { get; }

    /// <summary>
    /// Position of the attacking actor
    /// </summary>
    public Position AttackerPosition { get; }

    /// <summary>
    /// The target actor being attacked
    /// </summary>
    public ActorId TargetId { get; }

    /// <summary>
    /// Position of the target actor
    /// </summary>
    public Position TargetPosition { get; }

    /// <summary>
    /// Whether the target is alive (required for attacks)
    /// </summary>
    public bool IsTargetAlive { get; }

    /// <summary>
    /// Private constructor ensures all instances are valid.
    /// Only accessible through validated factory methods.
    /// </summary>
    private AttackValidation(
        ActorId attackerId,
        Position attackerPosition,
        ActorId targetId,
        Position targetPosition,
        bool isTargetAlive)
    {
        AttackerId = attackerId;
        AttackerPosition = attackerPosition;
        TargetId = targetId;
        TargetPosition = targetPosition;
        IsTargetAlive = isTargetAlive;
    }

    /// <summary>
    /// All AttackValidation instances are always valid due to private constructor.
    /// This property exists for interface compatibility but always returns true.
    /// </summary>
    public bool IsValid => true;

    /// <summary>
    /// Creates an attack validation with comprehensive rule checking.
    /// Primary factory method for attack validation.
    /// </summary>
    /// <param name="attackerId">The attacking actor</param>
    /// <param name="attackerPosition">Position of the attacker</param>
    /// <param name="targetId">The target actor</param>
    /// <param name="targetPosition">Position of the target</param>
    /// <param name="isTargetAlive">Whether the target is alive</param>
    /// <returns>Success with validation or failure with specific error</returns>
    public static Fin<AttackValidation> Create(
        ActorId attackerId,
        Position attackerPosition,
        ActorId targetId,
        Position targetPosition,
        bool isTargetAlive)
    {
        // Validate attacker ID
        if (attackerId == ActorId.Empty)
            return FinFail<AttackValidation>(Error.New("Invalid attacker ID: cannot be empty"));

        // Validate target ID  
        if (targetId == ActorId.Empty)
            return FinFail<AttackValidation>(Error.New("Invalid target ID: cannot be empty"));

        // Prevent self-targeting
        if (attackerId == targetId)
            return FinFail<AttackValidation>(Error.New("Actor cannot attack itself"));

        // Validate target is alive
        if (!isTargetAlive)
            return FinFail<AttackValidation>(Error.New("Cannot attack dead target: target is dead"));

        // Validate adjacency for melee attacks (includes diagonals)
        if (!attackerPosition.IsAdjacentTo(targetPosition))
            return FinFail<AttackValidation>(Error.New(
                $"Target is not adjacent: melee attacks require adjacent positions"));

        return FinSucc(new AttackValidation(
            attackerId,
            attackerPosition,
            targetId,
            targetPosition,
            isTargetAlive));
    }

    /// <summary>
    /// Factory for creating AttackValidations with known-valid values.
    /// Used by tests and scenarios where validation has already occurred.
    /// THROWS on invalid values - only use with compile-time known valid values.
    /// </summary>
    public static AttackValidation CreateUnsafe(
        ActorId attackerId,
        Position attackerPosition,
        ActorId targetId,
        Position targetPosition,
        bool isTargetAlive = true)
    {
        var result = Create(attackerId, attackerPosition, targetId, targetPosition, isTargetAlive);
        return result.Match(
            Succ: validation => validation,
            Fail: error => throw new InvalidOperationException($"CreateUnsafe called with invalid values: {error}"));
    }

    /// <summary>
    /// Gets the attack distance in grid units (Manhattan distance).
    /// </summary>
    public int AttackDistance => AttackerPosition.ManhattanDistanceTo(TargetPosition);

    /// <summary>
    /// Determines if this is a valid melee attack (adjacent only).
    /// </summary>
    public bool IsValidMeleeRange => AttackerPosition.IsAdjacentTo(TargetPosition);

    /// <summary>
    /// Returns a human-readable description of the attack validation.
    /// </summary>
    public override string ToString()
    {
        var status = IsValid ? "Valid" : "Invalid";
        return $"{status} Attack: {AttackerId} -> {TargetId} (distance: {AttackDistance})";
    }
}
