using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.Combat.Application.Queries;

/// <summary>
/// Handler for IsInCombatQuery.
/// Returns current combat state from turn queue.
/// </summary>
public class IsInCombatQueryHandler : IRequestHandler<IsInCombatQuery, Result<bool>>
{
    private readonly ITurnQueueRepository _turnQueue;
    private readonly ILogger<IsInCombatQueryHandler> _logger;

    public IsInCombatQueryHandler(
        ITurnQueueRepository turnQueue,
        ILogger<IsInCombatQueryHandler> logger)
    {
        _turnQueue = turnQueue;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(IsInCombatQuery request, CancellationToken cancellationToken)
    {
        // HOT PATH: Called frequently (every movement), only log warnings/errors
        var queueResult = await _turnQueue.GetAsync(cancellationToken);

        if (queueResult.IsFailure)
        {
            _logger.LogWarning("Failed to get turn queue: {Error}", queueResult.Error);
            return Result.Failure<bool>(queueResult.Error);
        }

        return Result.Success(queueResult.Value.IsInCombat);
    }
}
