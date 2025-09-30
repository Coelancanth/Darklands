using Darklands.Core.Domain.Common;
using MediatR;

namespace Darklands.Core.Features.Health.Application.Events;

/// <summary>
/// Domain event published when an actor's health changes.
/// Past tense (fact, not command) per ADR-004 Rule 2.
/// Contains complete context for subscribers per ADR-004 Rule 1.
/// </summary>
/// <param name="ActorId">The actor whose health changed</param>
/// <param name="OldHealth">Health value before the change</param>
/// <param name="NewHealth">Health value after the change</param>
/// <param name="IsDead">True if the actor died (health reached zero)</param>
/// <param name="IsCritical">True if health is below 25% threshold</param>
public sealed record HealthChangedEvent(
    ActorId ActorId,
    float OldHealth,
    float NewHealth,
    bool IsDead,
    bool IsCritical
) : INotification;
