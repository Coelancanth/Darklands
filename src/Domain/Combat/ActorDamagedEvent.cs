using MediatR;
using Darklands.Core.Domain.Grid;
using Darklands.Core.Domain.Actor;

namespace Darklands.Core.Domain.Combat;

/// <summary>
/// Domain event published when an actor takes damage during combat.
/// Replaces static callback anti-pattern with proper MediatR notification.
/// 
/// Used for:
/// - Live health bar updates
/// - Damage visualization effects
/// - Combat logging and statistics
/// </summary>
/// <param name="ActorId">The ID of the actor that was damaged</param>
/// <param name="OldHealth">The actor's health before damage</param>
/// <param name="NewHealth">The actor's health after damage</param>
public sealed record ActorDamagedEvent(
    ActorId ActorId,
    Health OldHealth,
    Health NewHealth
) : INotification
{
    /// <summary>
    /// Creates an ActorDamagedEvent for the specified actor with health transition.
    /// </summary>
    /// <param name="actorId">The actor that was damaged</param>
    /// <param name="oldHealth">Health before damage</param>
    /// <param name="newHealth">Health after damage</param>
    /// <returns>A new ActorDamagedEvent</returns>
    public static ActorDamagedEvent Create(ActorId actorId, Health oldHealth, Health newHealth) =>
        new(actorId, oldHealth, newHealth);

    /// <summary>
    /// Gets the amount of damage that was dealt (positive value).
    /// </summary>
    public int DamageAmount => OldHealth.Current - NewHealth.Current;

    public override string ToString() =>
        $"ActorDamagedEvent(ActorId: {ActorId}, {OldHealth} → {NewHealth}, Damage: {DamageAmount})";
}
