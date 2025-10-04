using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Combat.Application;
using Darklands.Core.Features.Combat.Domain;

namespace Darklands.Core.Tests.Features.Combat.Application;

/// <summary>
/// In-memory fake repository for testing Application layer handlers.
/// Simulates persistence without actual I/O.
/// </summary>
public class FakeTurnQueueRepository : ITurnQueueRepository
{
    private TurnQueue? _queue;
    private readonly ActorId _playerId;

    public FakeTurnQueueRepository(ActorId playerId)
    {
        _playerId = playerId;
    }

    public Task<Result<TurnQueue>> GetAsync(CancellationToken cancellationToken = default)
    {
        // Auto-create with player if doesn't exist (matches real repository behavior)
        _queue ??= TurnQueue.CreateWithPlayer(_playerId);

        return Task.FromResult(Result.Success(_queue));
    }

    public Task<Result> SaveAsync(TurnQueue turnQueue, CancellationToken cancellationToken = default)
    {
        _queue = turnQueue;
        return Task.FromResult(Result.Success());
    }

    /// <summary>
    /// Test helper: Reset repository to empty state.
    /// </summary>
    public void Reset()
    {
        _queue = null;
    }
}
