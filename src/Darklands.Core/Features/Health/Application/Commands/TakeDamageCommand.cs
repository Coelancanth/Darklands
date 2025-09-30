using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using MediatR;

namespace Darklands.Core.Features.Health.Application.Commands;

/// <summary>
/// Command to apply damage to an actor's health.
/// Pure DTO - no behavior, just data for the handler.
/// </summary>
/// <param name="TargetId">The actor receiving damage</param>
/// <param name="Amount">Damage amount (must be >= 0)</param>
public sealed record TakeDamageCommand(
    ActorId TargetId,
    float Amount
) : IRequest<Result<DamageResult>>;

/// <summary>
/// Result of applying damage to an actor.
/// Contains all information needed by subscribers (Rule 1 - Events have complete context).
/// </summary>
public sealed record DamageResult
{
    public ActorId ActorId { get; }
    public float OldHealth { get; }
    public float NewHealth { get; }
    public float DamageApplied { get; }
    public bool IsDead { get; }
    public bool IsCritical { get; }

    public DamageResult(
        ActorId actorId,
        float oldHealth,
        float newHealth,
        float damageApplied,
        bool isDead,
        bool isCritical)
    {
        ActorId = actorId;
        OldHealth = oldHealth;
        NewHealth = newHealth;
        DamageApplied = damageApplied;
        IsDead = isDead;
        IsCritical = isCritical;
    }
}
