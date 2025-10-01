using Darklands.Core.Domain.Common;
using MediatR;

namespace Darklands.Core.Features.Grid.Domain.Events;

/// <summary>
/// Domain event raised when an actor successfully moves to a new grid position.
/// Presentation layer subscribes to update sprite position in Godot scene.
/// </summary>
/// <param name="ActorId">Unique identifier of the actor that moved</param>
/// <param name="OldPosition">Previous grid position before the move</param>
/// <param name="NewPosition">New grid position after successful move</param>
/// <remarks>
/// Per ADR-004 Event Rules:
/// - Rule 2: Past tense (MovedEvent, not MoveEvent) - describes a complete fact
/// - Rule 3: Terminal subscriber (Presentation updates UI, no further events/commands)
/// - Rule 4: Depth = 1 (MoveActorCommand → ActorMovedEvent → UI, no cascading)
///
/// Event Design: Includes BOTH old and new positions so Presentation can clear the old cell
/// and render at the new cell without maintaining its own position state. The event is a
/// complete fact: "Actor X moved FROM position A TO position B".
/// </remarks>
public record ActorMovedEvent(ActorId ActorId, Position OldPosition, Position NewPosition) : INotification;
