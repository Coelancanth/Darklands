using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.Combat.Application.Queries;

/// <summary>
/// Handler for IsActorScheduledQuery.
/// Checks if actor exists in turn queue.
/// </summary>
public class IsActorScheduledQueryHandler : IRequestHandler<IsActorScheduledQuery, Result<bool>>
{
    private readonly ITurnQueueRepository _turnQueue;
    private readonly ILogger<IsActorScheduledQueryHandler> _logger;

    public IsActorScheduledQueryHandler(
        ITurnQueueRepository turnQueue,
        ILogger<IsActorScheduledQueryHandler> logger)
    {
        _turnQueue = turnQueue;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(IsActorScheduledQuery request, CancellationToken cancellationToken)
    {
        var queueResult = await _turnQueue.GetAsync(cancellationToken);

        if (queueResult.IsFailure)
        {
            _logger.LogWarning(
                "Failed to get turn queue while checking if actor {ActorId} is scheduled: {Error}",
                request.ActorId,
                queueResult.Error);
            return Result.Failure<bool>(queueResult.Error);
        }

        var isScheduled = queueResult.Value.Contains(request.ActorId);

        _logger.LogDebug(
            "Actor {ActorId} scheduled check: {IsScheduled}",
            request.ActorId,
            isScheduled);

        return Result.Success(isScheduled);
    }
}
