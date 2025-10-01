using Darklands.Core.Domain.Common;
using MediatR;

namespace Darklands.Core.Features.Grid.Domain.Events;

/// <summary>
/// Domain event raised when an actor successfully moves to a new grid position.
/// Presentation layer subscribes to update sprite position in Godot scene.
/// </summary>
/// <param name="ActorId">Unique identifier of the actor that moved</param>
/// <param name="NewPosition">New grid position after successful move</param>
/// <remarks>
/// Per ADR-004 Event Rules:
/// - Rule 2: Past tense (MovedEvent, not MoveEvent) - describes a fact
/// - Rule 3: Terminal subscriber (Presentation updates UI, no further events/commands)
/// - Rule 4: Depth = 1 (MoveActorCommand → ActorMovedEvent → UI, no cascading)
/// </remarks>
public record ActorMovedEvent(ActorId ActorId, Position NewPosition) : INotification;
