using System.Linq;
using CSharpFunctionalExtensions;
using Darklands.Core.Application;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Grid.Application.Services;
using Darklands.Core.Infrastructure.Logging;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.Grid.Application.Queries;

/// <summary>
/// Handler for GetVisibleActorsQuery.
/// Orchestrates: get observer position → calculate FOV → get all actors → filter by visibility.
/// Demonstrates query composition pattern (delegates to CalculateFOVQuery).
/// </summary>
public class GetVisibleActorsQueryHandler : IRequestHandler<GetVisibleActorsQuery, Result<List<ActorId>>>
{
    private readonly IActorPositionService _actorPositionService;
    private readonly IMediator _mediator;
    private readonly IPlayerContext _playerContext;
    private readonly ILogger<GetVisibleActorsQueryHandler> _logger;

    public GetVisibleActorsQueryHandler(
        IActorPositionService actorPositionService,
        IMediator mediator,
        IPlayerContext playerContext,
        ILogger<GetVisibleActorsQueryHandler> logger)
    {
        _actorPositionService = actorPositionService;
        _mediator = mediator;
        _playerContext = playerContext;
        _logger = logger;
    }

    public async Task<Result<List<ActorId>>> Handle(GetVisibleActorsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Getting visible actors for observer {ObserverId} with radius {Radius}",
            request.ObserverId,
            request.Radius);

        // Step 1: Get observer's position
        var observerPosResult = _actorPositionService.GetPosition(request.ObserverId);
        if (observerPosResult.IsFailure)
        {
            _logger.LogWarning(
                "Failed to get visible actors: Observer {ObserverId} not found",
                request.ObserverId);
            return Result.Failure<List<ActorId>>(observerPosResult.Error);
        }

        var observerPos = observerPosResult.Value;

        // Step 2: Calculate FOV (query composition - delegates to CalculateFOVQuery)
        var fovQuery = new CalculateFOVQuery(observerPos, request.Radius);
        var fovResult = await _mediator.Send(fovQuery, cancellationToken);

        if (fovResult.IsFailure)
        {
            _logger.LogWarning(
                "Failed to calculate FOV for observer {ObserverId}: {Error}",
                request.ObserverId,
                fovResult.Error);
            return Result.Failure<List<ActorId>>(fovResult.Error);
        }

        var visiblePositions = fovResult.Value;

        // Step 3: Get all actors
        var allActorsResult = _actorPositionService.GetAllActors();
        if (allActorsResult.IsFailure)
        {
            return Result.Failure<List<ActorId>>(allActorsResult.Error);
        }

        var allActors = allActorsResult.Value;

        // Step 4: Filter actors by visibility (exclude observer from results)
        var visibleActors = new List<ActorId>();

        foreach (var actorId in allActors)
        {
            // Skip the observer
            if (actorId == request.ObserverId)
                continue;

            // Get actor position
            var actorPosResult = _actorPositionService.GetPosition(actorId);
            if (actorPosResult.IsFailure)
            {
                _logger.LogWarning(
                    "Skipping actor {ActorId}: Failed to get position",
                    actorId);
                continue; // Skip actors with invalid positions
            }

            // Check if actor's position is visible
            if (visiblePositions.Contains(actorPosResult.Value))
            {
                visibleActors.Add(actorId);
            }
        }

        // Format visible actors list (empty if none visible)
        // TODO (VS_020): Replace with actor names from templates via IActorNameResolver
        // Future: "Observer {8c2de643 [type: Player]} can see 2 actors: [{bdb71a68 [type: Goblin]}, {b66288f5 [type: Orc]}]"
        var visibleActorIds = visibleActors.Count > 0
            ? string.Join(", ", visibleActors.Select(a => a.ToLogString(_playerContext)))
            : "none";

        _logger.LogInformation(
            "[Grid] Observer {ObserverId} can see {VisibleCount} actors: [{VisibleActors}] (out of {TotalCount} total actors)",
            request.ObserverId.ToLogString(_playerContext),
            visibleActors.Count,
            visibleActorIds, // "none" or "shortId [type: Enemy/Player]"
            allActors.Count - 1); // -1 excludes observer

        return Result.Success(visibleActors);
    }
}
