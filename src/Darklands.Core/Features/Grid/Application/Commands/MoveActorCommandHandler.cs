using CSharpFunctionalExtensions;
using Darklands.Core.Features.Grid.Application.Queries;
using Darklands.Core.Features.Grid.Application.Services;
using Darklands.Core.Features.Grid.Domain;
using Darklands.Core.Features.Grid.Domain.Events;
using Darklands.Core.Infrastructure.Events;
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
    private readonly ILogger<MoveActorCommandHandler> _logger;

    public MoveActorCommandHandler(
        GridMap gridMap,
        IActorPositionService actorPositionService,
        IMediator mediator,
        IGodotEventBus eventBus,
        ILogger<MoveActorCommandHandler> logger)
    {
        _gridMap = gridMap;
        _actorPositionService = actorPositionService;
        _mediator = mediator;
        _eventBus = eventBus;
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
            "Successfully moved actor {ActorId} to position ({X}, {Y})",
            request.ActorId,
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

        return Result.Success();
    }
}
