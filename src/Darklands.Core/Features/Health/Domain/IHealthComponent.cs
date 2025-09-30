using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using HealthValue = Darklands.Core.Domain.Common.Health;

namespace Darklands.Core.Features.Health.Domain;

/// <summary>
/// Represents the health management capability for an actor.
/// Component-based architecture - actors have health via this component.
/// </summary>
public interface IHealthComponent
{
    /// <summary>
    /// The actor that owns this health component.
    /// </summary>
    ActorId OwnerId { get; }

    /// <summary>
    /// Current health state (immutable value object).
    /// </summary>
    HealthValue CurrentHealth { get; }

    /// <summary>
    /// True if the actor is alive (health > 0).
    /// </summary>
    bool IsAlive { get; }

    /// <summary>
    /// Applies damage to this actor's health.
    /// </summary>
    /// <param name="amount">Damage amount (must be >= 0)</param>
    /// <returns>Result with new health value on success, or failure message</returns>
    Result<HealthValue> TakeDamage(float amount);

    /// <summary>
    /// Restores health to this actor.
    /// </summary>
    /// <param name="amount">Heal amount (must be >= 0)</param>
    /// <returns>Result with new health value on success, or failure message</returns>
    Result<HealthValue> Heal(float amount);
}
