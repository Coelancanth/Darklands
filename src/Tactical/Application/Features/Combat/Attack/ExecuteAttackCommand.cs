using Darklands.SharedKernel.Domain;
using Darklands.Tactical.Domain.ValueObjects;
using LanguageExt;
using MediatR;

namespace Darklands.Tactical.Application.Features.Combat.Attack;

/// <summary>
/// Command to execute an attack action from one actor to another.
/// </summary>
public sealed record ExecuteAttackCommand : IRequest<Fin<AttackResult>>
{
    /// <summary>
    /// The ID of the attacking actor.
    /// </summary>
    public EntityId AttackerId { get; }

    /// <summary>
    /// The ID of the target actor.
    /// </summary>
    public EntityId TargetId { get; }

    /// <summary>
    /// The base damage to attempt (before defense calculation).
    /// </summary>
    public int BaseDamage { get; }

    /// <summary>
    /// When this attack occurs in game time.
    /// </summary>
    public TimeUnit OccurredAt { get; }

    /// <summary>
    /// Optional special attack type or ability name.
    /// </summary>
    public string? AttackType { get; }

    public ExecuteAttackCommand(
        EntityId attackerId,
        EntityId targetId,
        int baseDamage,
        TimeUnit occurredAt,
        string? attackType = null)
    {
        AttackerId = attackerId;
        TargetId = targetId;
        BaseDamage = baseDamage;
        OccurredAt = occurredAt;
        AttackType = attackType ?? "Basic Attack";
    }
}

/// <summary>
/// Result of an attack action.
/// </summary>
public sealed record AttackResult
{
    /// <summary>
    /// The actual damage dealt after defense calculation.
    /// </summary>
    public int DamageDealt { get; }

    /// <summary>
    /// The remaining health of the target after the attack.
    /// </summary>
    public int TargetRemainingHealth { get; }

    /// <summary>
    /// Whether the target died from this attack.
    /// </summary>
    public bool TargetKilled { get; }

    /// <summary>
    /// Any special effects or status changes from the attack.
    /// </summary>
    public IReadOnlyList<string> Effects { get; }

    public AttackResult(
        int damageDealt,
        int targetRemainingHealth,
        bool targetKilled,
        IReadOnlyList<string>? effects = null)
    {
        DamageDealt = damageDealt;
        TargetRemainingHealth = targetRemainingHealth;
        TargetKilled = targetKilled;
        Effects = effects ?? Array.Empty<string>();
    }
}
