using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Combat.Application;
using Darklands.Core.Features.Combat.Domain;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.Combat.Infrastructure;

/// <summary>
/// In-memory turn queue repository for MVP.
/// </summary>
/// <remarks>
/// SINGLETON PATTERN: Only ONE turn queue exists per game session (global combat state).
/// Unlike inventory (one per actor), there's a single shared turn queue.
///
/// THREAD SAFETY: Not thread-safe by design (single-player game, Godot main thread only).
///
/// PERSISTENCE: State lost on application restart (acceptable for MVP).
/// Combat state is ephemeral - resetting on app restart is expected behavior.
///
/// AUTO-CREATION: Lazy initialization on first GetAsync() call.
/// Requires InitializeWithPlayer() to be called once during game setup.
///
/// FUTURE: Replace with SQLite/JSON persistence without changing interface.
/// </remarks>
public sealed class InMemoryTurnQueueRepository : ITurnQueueRepository
{
    private TurnQueue? _turnQueue;
    private ActorId? _playerId;
    private readonly ILogger<InMemoryTurnQueueRepository> _logger;
    private readonly object _lock = new(); // Thread safety for future-proofing

    public InMemoryTurnQueueRepository(ILogger<InMemoryTurnQueueRepository> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Initializes the repository with the player's ActorId.
    /// Must be called once during game setup (typically in test scene initialization).
    /// </summary>
    /// <param name="playerId">Player character's ActorId</param>
    /// <remarks>
    /// WHY: Turn queue requires player ActorId for auto-creation.
    /// Test scenes call this in _Ready() before any combat interactions.
    /// Production game would call this after player character creation.
    /// </remarks>
    public void InitializeWithPlayer(ActorId playerId)
    {
        lock (_lock)
        {
            if (_playerId != null && !_playerId.Equals(playerId))
            {
                _logger.LogWarning(
                    "Turn queue already initialized with player {OldPlayerId}, re-initializing with {NewPlayerId}",
                    _playerId,
                    playerId);
            }

            _playerId = playerId;
            _turnQueue = null; // Force recreation on next GetAsync()
            _logger.LogDebug("Turn queue repository initialized with player {PlayerId}", playerId);
        }
    }

    public Task<Result<TurnQueue>> GetAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            // Auto-create on first access
            if (_turnQueue == null)
            {
                if (_playerId == null)
                {
                    return Task.FromResult(
                        Result.Failure<TurnQueue>(
                            "Turn queue repository not initialized. Call InitializeWithPlayer() first."));
                }

                _turnQueue = TurnQueue.CreateWithPlayer(_playerId.Value);
                _logger.LogDebug(
                    "Auto-created turn queue with player {PlayerId} at time=0",
                    _playerId);
            }

            return Task.FromResult(Result.Success(_turnQueue));
        }
    }

    public Task<Result> SaveAsync(TurnQueue turnQueue, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(turnQueue);

        lock (_lock)
        {
            // In-memory: Just update reference
            // Domain ensures immutability via value objects, so we store the latest aggregate state
            _turnQueue = turnQueue;

            _logger.LogDebug(
                "Saved turn queue state: {ActorCount} actors, IsInCombat={IsInCombat}",
                turnQueue.Count,
                turnQueue.IsInCombat);

            // Future implementations: Write to SQLite/JSON here
            return Task.FromResult(Result.Success());
        }
    }

    /// <summary>
    /// Resets the turn queue (test cleanup only).
    /// </summary>
    /// <remarks>
    /// TESTING ONLY: Allows tests to reset combat state between scenarios.
    /// Production code should never need this - queue manages its own lifecycle.
    /// </remarks>
    internal void Reset()
    {
        lock (_lock)
        {
            _turnQueue = null;
            _playerId = null;
            _logger.LogDebug("Turn queue repository reset");
        }
    }
}
