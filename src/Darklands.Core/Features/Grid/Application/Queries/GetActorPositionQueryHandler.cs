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
        _logger.LogDebug("Querying position for actor {ActorId}", request.ActorId);

        var result = _actorPositionService.GetPosition(request.ActorId);

        if (result.IsSuccess)
        {
            _logger.LogDebug(
                "Actor {ActorId} is at position ({X}, {Y})",
                request.ActorId,
                result.Value.X,
                result.Value.Y);
        }
        else
        {
            _logger.LogWarning(
                "Failed to get position for actor {ActorId}: {Error}",
                request.ActorId,
                result.Error);
        }

        return Task.FromResult(result);
    }
}
