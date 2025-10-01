using CSharpFunctionalExtensions;
using Darklands.Core.Features.Grid.Application.Services;
using Darklands.Core.Features.Grid.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.Grid.Application.Commands;

/// <summary>
/// Handler for MoveActorCommand.
/// Validates passability and delegates position update to IActorPositionService.
/// </summary>
public class MoveActorCommandHandler : IRequestHandler<MoveActorCommand, Result>
{
    private readonly GridMap _gridMap;
    private readonly IActorPositionService _actorPositionService;
    private readonly ILogger<MoveActorCommandHandler> _logger;

    public MoveActorCommandHandler(
        GridMap gridMap,
        IActorPositionService actorPositionService,
        ILogger<MoveActorCommandHandler> logger)
    {
        _gridMap = gridMap;
        _actorPositionService = actorPositionService;
        _logger = logger;
    }

    public Task<Result> Handle(MoveActorCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Attempting to move actor {ActorId} to position ({X}, {Y})",
            request.ActorId,
            request.TargetPosition.X,
            request.TargetPosition.Y);

        // Validate target position is passable (railway-oriented programming)
        var passabilityResult = _gridMap.IsPassable(request.TargetPosition);

        if (passabilityResult.IsFailure)
        {
            _logger.LogWarning(
                "Move failed for actor {ActorId}: {Error}",
                request.ActorId,
                passabilityResult.Error);
            return Task.FromResult(Result.Failure(passabilityResult.Error));
        }

        if (!passabilityResult.Value)
        {
            _logger.LogWarning(
                "Move failed for actor {ActorId}: Target position ({X}, {Y}) is impassable",
                request.ActorId,
                request.TargetPosition.X,
                request.TargetPosition.Y);
            return Task.FromResult(Result.Failure(
                $"Cannot move to ({request.TargetPosition.X}, {request.TargetPosition.Y}): terrain is impassable"));
        }

        // Update actor position
        var updateResult = _actorPositionService.SetPosition(request.ActorId, request.TargetPosition);

        if (updateResult.IsSuccess)
        {
            _logger.LogInformation(
                "Successfully moved actor {ActorId} to position ({X}, {Y})",
                request.ActorId,
                request.TargetPosition.X,
                request.TargetPosition.Y);
        }

        return Task.FromResult(updateResult);
    }
}
