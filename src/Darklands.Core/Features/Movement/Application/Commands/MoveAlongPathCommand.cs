using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using MediatR;

namespace Darklands.Core.Features.Movement.Application.Commands;

/// <summary>
/// Command to move an actor along a predetermined path with waypoints.
/// Executes movement step-by-step, delegating to MoveActorCommand for each tile.
/// Supports cancellation via CancellationToken (right-click to stop movement).
/// </summary>
/// <param name="ActorId">Actor to move</param>
/// <param name="Path">
/// Ordered list of positions from current position to goal (inclusive).
/// Must include starting position at index 0.
/// </param>
/// <remarks>
/// Per ADR-003: Returns Result for functional error handling.
/// Per ADR-004: Command orchestrates work, events notify subscribers.
///
/// **Architecture Decision** (VS_006):
/// - Delegates to existing MoveActorCommand per step (reuses validation, FOV, events)
/// - Emits ActorMovedEvent PER STEP (enables discrete tile animation)
/// - Respects CancellationToken (checks before each step for right-click interrupt)
/// - If cancelled mid-path: actor stays at current tile (graceful stop, not rollback)
///
/// **Event Flow**:
/// ```
/// MoveAlongPathCommand → for each step:
///   → MoveActorCommand → validates + updates position
///     → ActorMovedEvent (existing)
///       → Presentation animates tile-to-tile (Godot Tween)
///       → FOVCalculatedEvent (if position changed)
/// ```
///
/// **Cancellation Behavior**:
/// - CancellationToken checked BEFORE each MoveActorCommand
/// - If cancelled: return success with partial path completed
/// - Presentation layer cancels via CancellationTokenSource.Cancel() on right-click
/// </remarks>
public record MoveAlongPathCommand(
    ActorId ActorId,
    IReadOnlyList<Position> Path
) : IRequest<Result>;
