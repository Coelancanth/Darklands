using CSharpFunctionalExtensions;
using Darklands.Core.Features.Combat.Application.Queries;
using Darklands.Core.Features.Grid.Application.Commands;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.Movement.Application.Commands;

/// <summary>
/// Handler for MoveAlongPathCommand.
/// Orchestrates multi-step movement by delegating to MoveActorCommand for each waypoint.
/// Supports graceful cancellation via CancellationToken (right-click interrupt).
/// </summary>
/// <remarks>
/// Per ADR-003: Railway-oriented programming with Result&lt;T&gt;.
/// Per ADR-004 Rule 1: Commands orchestrate ALL work, events notify.
///
/// **Design Pattern**: Command Composition
/// - Reuses existing MoveActorCommand (no duplication of validation/FOV/events)
/// - MoveActorCommand emits ActorMovedEvent per step (existing event)
/// - Presentation layer animates each ActorMovedEvent (Godot Tween in Phase 4)
///
/// **Cancellation Strategy**:
/// - Check CancellationToken.ThrowIfCancellationRequested() BEFORE each step
/// - If cancelled: return Result.Success() with partial path completed (graceful stop)
/// - Actor remains at current tile (no rollback - movement is not transactional)
/// - Presentation layer cancels via CancellationTokenSource.Cancel() on right-click
///
/// **Error Handling**:
/// - If any step fails (impassable terrain, etc.): stop immediately, return failure
/// - Partial path executed before failure is NOT rolled back (movement is progressive)
/// </remarks>
public class MoveAlongPathCommandHandler : IRequestHandler<MoveAlongPathCommand, Result>
{
    private readonly IMediator _mediator;
    private readonly ILogger<MoveAlongPathCommandHandler> _logger;

    public MoveAlongPathCommandHandler(
        IMediator mediator,
        ILogger<MoveAlongPathCommandHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<Result> Handle(
        MoveAlongPathCommand request,
        CancellationToken cancellationToken)
    {
        // Validate input (programmer errors - fail fast per ADR-003)
        if (request.Path == null || request.Path.Count == 0)
            return Result.Failure("Path cannot be null or empty");

        _logger.LogInformation(
            "Starting path movement for actor {ActorId}: {StepCount} steps",
            request.ActorId,
            request.Path.Count);

        // Execute movement step-by-step
        // Skip first position (actor is already there)
        for (int i = 1; i < request.Path.Count; i++)
        {
            // Check for cancellation BEFORE each step (right-click interrupt)
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation(
                    "Movement cancelled for actor {ActorId} at step {Step}/{Total}",
                    request.ActorId,
                    i,
                    request.Path.Count);

                // Graceful stop: return success with partial completion
                // Actor stays at current position (no rollback)
                return Result.Success();
            }

            var targetPosition = request.Path[i];

            _logger.LogDebug(
                "Moving actor {ActorId} to position ({X}, {Y}) [step {Step}/{Total}]",
                request.ActorId,
                targetPosition.X,
                targetPosition.Y,
                i,
                request.Path.Count);

            // Delegate to existing MoveActorCommand (reuses validation, FOV, events)
            var moveResult = await _mediator.Send(
                new MoveActorCommand(request.ActorId, targetPosition),
                cancellationToken);

            // If step fails: stop immediately, return failure
            if (moveResult.IsFailure)
            {
                _logger.LogWarning(
                    "Movement failed for actor {ActorId} at step {Step}/{Total}: {Error}",
                    request.ActorId,
                    i,
                    request.Path.Count,
                    moveResult.Error);

                return Result.Failure(
                    $"Movement failed at step {i}/{request.Path.Count}: {moveResult.Error}");
            }

            // Success: MoveActorCommand emitted ActorMovedEvent
            // Presentation layer will animate this step via event handler

            // VS_007: Check if combat mode started (enemy detected) - auto-cancel exploration movement
            var isInCombatQuery = new IsInCombatQuery();
            var isInCombatResult = await _mediator.Send(isInCombatQuery, cancellationToken);
            if (isInCombatResult.IsSuccess && isInCombatResult.Value)
            {
                _logger.LogInformation(
                    "Combat detected! Auto-cancelling exploration movement for actor {ActorId} at step {Step}/{Total}",
                    request.ActorId,
                    i,
                    request.Path.Count);
                return Result.Success(); // Graceful stop when combat starts
            }

            // VS_006 Phase 4: Add delay between steps for visible movement
            // TODO: Replace with proper animation system in future iteration
            try
            {
                await Task.Delay(100, cancellationToken); // 100ms per tile = 10 tiles/second
            }
            catch (TaskCanceledException)
            {
                // Cancellation during delay is expected - return gracefully
                _logger.LogInformation(
                    "Movement cancelled for actor {ActorId} at step {Step}/{Total} (during delay)",
                    request.ActorId,
                    i,
                    request.Path.Count);
                return Result.Success(); // Graceful stop
            }
        }

        _logger.LogInformation(
            "Completed path movement for actor {ActorId}: {StepCount} steps",
            request.ActorId,
            request.Path.Count);

        return Result.Success();
    }
}
