using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Grid.Application.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.Grid.Application.Queries;

/// <summary>
/// Handler for GetActorPositionQuery.
/// Delegates to IActorPositionService for position lookup.
/// </summary>
public class GetActorPositionQueryHandler : IRequestHandler<GetActorPositionQuery, Result<Position>>
{
    private readonly IActorPositionService _actorPositionService;
    private readonly ILogger<GetActorPositionQueryHandler> _logger;

    public GetActorPositionQueryHandler(
        IActorPositionService actorPositionService,
        ILogger<GetActorPositionQueryHandler> logger)
    {
        _actorPositionService = actorPositionService;
        _logger = logger;
    }

    public Task<Result<Position>> Handle(GetActorPositionQuery request, CancellationToken cancellationToken)
    {
        // HOT PATH: Called frequently (hover events, pathfinding, movement)
        // Only log warnings/errors, not debug info
        var result = _actorPositionService.GetPosition(request.ActorId);

        if (result.IsFailure)
        {
            _logger.LogWarning(
                "Failed to get position for actor {ActorId}: {Error}",
                request.ActorId,
                result.Error);
        }

        return Task.FromResult(result);
    }
}
