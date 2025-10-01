using Darklands.Core.Domain.Common;
using MediatR;

namespace Darklands.Core.Features.Grid.Domain.Events;

/// <summary>
/// Domain event raised when Field of View is calculated for an actor.
/// Presentation layer subscribes to update FOV visualization overlay.
/// </summary>
/// <param name="ActorId">Actor whose FOV was calculated</param>
/// <param name="VisiblePositions">Set of all grid positions visible to the actor</param>
/// <remarks>
/// Typically emitted after ActorMovedEvent (vision changes when actor moves).
/// Can also be emitted independently (e.g., terrain changes like smoke dissipating).
///
/// Per ADR-004 Event Rules:
/// - Rule 2: Past tense (CalculatedEvent, not CalculateEvent) - describes a fact
/// - Rule 3: Terminal subscriber (Presentation updates FOV overlay, no further events)
/// - Rule 4: Depth = 1 (no cascading events)
/// </remarks>
public record FOVCalculatedEvent(ActorId ActorId, HashSet<Position> VisiblePositions) : INotification;
