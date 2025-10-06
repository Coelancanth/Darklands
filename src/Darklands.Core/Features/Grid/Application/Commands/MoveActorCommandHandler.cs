using CSharpFunctionalExtensions;
using Darklands.Core.Application;
using Darklands.Core.Features.Combat.Application;
using Darklands.Core.Features.Combat.Application.Queries;
using Darklands.Core.Features.Combat.Domain;
using Darklands.Core.Features.Grid.Application.Queries;
using Darklands.Core.Features.Grid.Application.Services;
using Darklands.Core.Features.Grid.Domain;
using Darklands.Core.Features.Grid.Domain.Events;
using Darklands.Core.Infrastructure.Events;
using Darklands.Core.Infrastructure.Logging;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.Grid.Application.Commands;

/// <summary>
/// Handler for MoveActorCommand.
/// Validates passability, updates position, calculates FOV, and emits events.
/// </summary>
/// <remarks>
/// Per ADR-004: Command orchestrates ALL work (move + FOV calc), then emits events as facts.
/// Events notify Presentation layer (sprite position + FOV overlay updates).
/// </remarks>
public class MoveActorCommandHandler : IRequestHandler<MoveActorCommand, Result>
{
    private readonly GridMap _gridMap;
    private readonly IActorPositionService _actorPositionService;
    private readonly IMediator _mediator;
    private readonly IGodotEventBus _eventBus;
    private readonly ITurnQueueRepository _turnQueue;
    private readonly IPlayerContext _playerContext;
    private readonly ILogger<MoveActorCommandHandler> _logger;

    public MoveActorCommandHandler(
        GridMap gridMap,
        IActorPositionService actorPositionService,
        IMediator mediator,
        IGodotEventBus eventBus,
        ITurnQueueRepository turnQueue,
        IPlayerContext playerContext,
        ILogger<MoveActorCommandHandler> logger)
    {
        _gridMap = gridMap;
        _actorPositionService = actorPositionService;
        _mediator = mediator;
        _eventBus = eventBus;
        _turnQueue = turnQueue;
        _playerContext = playerContext;
        _logger = logger;
    }

    public async Task<Result> Handle(MoveActorCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Attempting to move actor {ActorId} to position ({X}, {Y})",
            request.ActorId,
            request.TargetPosition.X,
            request.TargetPosition.Y);

        // Get current position BEFORE moving (needed for complete event)
        var oldPositionResult = _actorPositionService.GetPosition(request.ActorId);
        if (oldPositionResult.IsFailure)
        {
            _logger.LogWarning(
                "Move failed for actor {ActorId}: {Error}",
                request.ActorId,
                oldPositionResult.Error);
            return Result.Failure(oldPositionResult.Error);
        }

        var oldPosition = oldPositionResult.Value;

        // Validate target position is passable (railway-oriented programming)
        var passabilityResult = _gridMap.IsPassable(request.TargetPosition);

        if (passabilityResult.IsFailure)
        {
            _logger.LogWarning(
                "Move failed for actor {ActorId}: {Error}",
                request.ActorId,
                passabilityResult.Error);
            return Result.Failure(passabilityResult.Error);
        }

        if (!passabilityResult.Value)
        {
            _logger.LogWarning(
                "Move failed for actor {ActorId}: Target position ({X}, {Y}) is impassable",
                request.ActorId,
                request.TargetPosition.X,
                request.TargetPosition.Y);
            return Result.Failure(
                $"Cannot move to ({request.TargetPosition.X}, {request.TargetPosition.Y}): terrain is impassable");
        }

        // Update actor position
        var updateResult = _actorPositionService.SetPosition(request.ActorId, request.TargetPosition);

        if (updateResult.IsFailure)
        {
            return updateResult;
        }

        _logger.LogInformation(
            "Actor {ActorId} moved to ({X}, {Y})",
            request.ActorId.ToLogString(_playerContext),
            request.TargetPosition.X,
            request.TargetPosition.Y);

        // Calculate FOV for new position
        // TODO: Replace with per-actor vision (VisionConstants → GetActorVisionRadiusQuery when implementing racial bonuses)
        var fovResult = await _mediator.Send(
            new CalculateFOVQuery(request.TargetPosition, VisionConstants.DefaultVisionRadius),
            cancellationToken);

        if (fovResult.IsFailure)
        {
            // Log but don't fail the move - FOV is secondary to movement success
            _logger.LogWarning(
                "FOV calculation failed after move for actor {ActorId}: {Error}",
                request.ActorId,
                fovResult.Error);
        }

        // Emit events (facts): Actor moved + FOV calculated
        // Presentation layer subscribes to update UI
        // Event contains complete fact: moved FROM oldPosition TO newPosition
        await _eventBus.PublishAsync(new ActorMovedEvent(request.ActorId, oldPosition, request.TargetPosition));

        if (fovResult.IsSuccess)
        {
            // Dual publishing: FOVCalculatedEvent goes to BOTH buses
            // GodotEventBus → Presentation layer (UI subscribers: FOV overlay, test scenes)
            await _eventBus.PublishAsync(new FOVCalculatedEvent(request.ActorId, fovResult.Value));

            // MediatR → Application layer (business logic: EnemyDetectionEventHandler)
            await _mediator.Publish(new FOVCalculatedEvent(request.ActorId, fovResult.Value));

            _logger.LogDebug(
                "FOV calculated for actor {ActorId}: {VisibleCount} positions visible",
                request.ActorId,
                fovResult.Value.Count);
        }

        // Advance turn time if in combat mode (exploration movement is instant)
        var isInCombatQuery = new IsInCombatQuery();
        var isInCombatResult = await _mediator.Send(isInCombatQuery, cancellationToken);

        if (isInCombatResult.IsSuccess && isInCombatResult.Value)
        {
            // In combat - movement costs time
            var queueResult = await _turnQueue.GetAsync(cancellationToken);

            if (queueResult.IsSuccess)
            {
                var queue = queueResult.Value;
                var scheduledActor = queue.ScheduledActors
                    .FirstOrDefault(a => a.ActorId == request.ActorId);

                if (scheduledActor.ActorId != default)
                {
                    // Calculate new action time: current time + movement cost
                    var currentTime = scheduledActor.NextActionTime;
                    var newActionTimeResult = currentTime.Add(TimeUnits.MovementCost);

                    if (newActionTimeResult.IsFailure)
                    {
                        _logger.LogWarning(
                            "Failed to calculate new action time for {ActorId}: {Error}",
                            request.ActorId,
                            newActionTimeResult.Error);
                        return Result.Success(); // Move succeeded, just skip time advancement
                    }

                    var newActionTime = newActionTimeResult.Value;
                    var rescheduleResult = queue.Reschedule(request.ActorId, newActionTime);

                    if (rescheduleResult.IsSuccess)
                    {
                        await _turnQueue.SaveAsync(queue, cancellationToken);

                        _logger.LogInformation(
                            "[Grid] Combat turn: {ActorId} moved (time: {OldTime} -> {NewTime}, cost: {Cost})",
                            request.ActorId.ToLogString(_playerContext),
                            currentTime,
                            newActionTime,
                            TimeUnits.MovementCost);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Failed to reschedule actor {ActorId} after movement: {Error}",
                            request.ActorId,
                            rescheduleResult.Error);
                    }
                }
                else
                {
                    _logger.LogDebug(
                        "Actor {ActorId} not in turn queue (combat started mid-move?), skipping time advancement",
                        request.ActorId);
                }
            }
            else
            {
                _logger.LogWarning(
                    "Failed to get turn queue for time advancement: {Error}",
                    queueResult.Error);
            }
        }
        else
        {
            // Exploration mode - movement is instant (no time cost)
            _logger.LogDebug(
                "Exploration mode: Movement for {ActorId} is instant (no time cost)",
                request.ActorId);
        }

        return Result.Success();
    }
}
