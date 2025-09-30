using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using HealthValue = Darklands.Core.Domain.Common.Health;

namespace Darklands.Core.Features.Health.Domain;

/// <summary>
/// Manages health state for an actor.
/// Mutable component that tracks health changes over time.
/// </summary>
public sealed class HealthComponent : IHealthComponent
{
    public ActorId OwnerId { get; }
    public HealthValue CurrentHealth { get; private set; }
    public bool IsAlive => !CurrentHealth.IsDepleted;

    /// <summary>
    /// Creates a new HealthComponent for an actor.
    /// </summary>
    /// <param name="ownerId">The actor that owns this component</param>
    /// <param name="initialHealth">Initial health state</param>
    /// <exception cref="ArgumentNullException">If ownerId is default or initialHealth is null</exception>
    public HealthComponent(ActorId ownerId, HealthValue initialHealth)
    {
        if (ownerId.Value == Guid.Empty)
            throw new ArgumentException("OwnerId cannot be empty", nameof(ownerId));

        OwnerId = ownerId;
        CurrentHealth = initialHealth ?? throw new ArgumentNullException(nameof(initialHealth));
    }

    /// <summary>
    /// Applies damage to this actor's health.
    /// Uses .Tap() to mutate internal state while preserving railway-oriented composition.
    /// </summary>
    /// <param name="amount">Damage amount (must be >= 0)</param>
    /// <returns>Result with updated health on success, or failure message</returns>
    public Result<HealthValue> TakeDamage(float amount)
    {
        return CurrentHealth.Reduce(amount)
            .Tap(newHealth => CurrentHealth = newHealth);
    }

    /// <summary>
    /// Restores health to this actor.
    /// Uses .Tap() to mutate internal state while preserving railway-oriented composition.
    /// </summary>
    /// <param name="amount">Heal amount (must be >= 0)</param>
    /// <returns>Result with updated health on success, or failure message</returns>
    public Result<HealthValue> Heal(float amount)
    {
        return CurrentHealth.Increase(amount)
            .Tap(newHealth => CurrentHealth = newHealth);
    }
}
