using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.Combat.Application.Queries;

/// <summary>
/// Handler for GetTurnQueueStateQuery.
/// Returns current turn queue state for UI display and combat logic.
/// </summary>
public class GetTurnQueueStateQueryHandler : IRequestHandler<GetTurnQueueStateQuery, Result<TurnQueueStateDto>>
{
    private readonly ITurnQueueRepository _turnQueue;
    private readonly ILogger<GetTurnQueueStateQueryHandler> _logger;

    public GetTurnQueueStateQueryHandler(
        ITurnQueueRepository turnQueue,
        ILogger<GetTurnQueueStateQueryHandler> logger)
    {
        _turnQueue = turnQueue;
        _logger = logger;
    }

    public async Task<Result<TurnQueueStateDto>> Handle(
        GetTurnQueueStateQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting turn queue state");

        var queueResult = await _turnQueue.GetAsync(cancellationToken);

        if (queueResult.IsFailure)
        {
            _logger.LogWarning("Failed to get turn queue: {Error}", queueResult.Error);
            return Result.Failure<TurnQueueStateDto>(queueResult.Error);
        }

        var queue = queueResult.Value;

        // Convert domain ScheduledActor to DTO
        var scheduledActorDtos = queue.GetScheduledActors()
            .Select(actor => new ScheduledActorDto(
                actor.ActorId,
                actor.NextActionTime,
                actor.IsPlayer))
            .ToList();

        var dto = new TurnQueueStateDto(
            IsInCombat: queue.IsInCombat,
            QueueSize: queue.Count,
            ScheduledActors: scheduledActorDtos);

        _logger.LogDebug(
            "Turn queue state: combat={IsInCombat}, size={QueueSize}",
            dto.IsInCombat,
            dto.QueueSize);

        return Result.Success(dto);
    }
}
