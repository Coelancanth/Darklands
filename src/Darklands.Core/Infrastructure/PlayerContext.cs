using CSharpFunctionalExtensions;
using Darklands.Core.Application;
using Darklands.Core.Domain.Common;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Infrastructure;

/// <summary>
/// In-memory implementation of IPlayerContext.
/// </summary>
/// <remarks>
/// THREAD SAFETY: Not thread-safe by design (single-player game, Godot main thread only).
///
/// PERSISTENCE: State lost on application restart.
/// Player ID is set during game initialization (test scene _Ready() or game start).
///
/// FUTURE: Load/save player ID with save game data.
/// </remarks>
public sealed class PlayerContext : IPlayerContext
{
    private ActorId? _playerId;
    private readonly ILogger<PlayerContext> _logger;
    private readonly object _lock = new(); // Thread safety for future-proofing

    public PlayerContext(ILogger<PlayerContext> _logger)
    {
        this._logger = _logger ?? throw new ArgumentNullException(nameof(_logger));
    }

    public void SetPlayerId(ActorId playerId)
    {
        lock (_lock)
        {
            if (_playerId != null && !_playerId.Equals(playerId))
            {
                _logger.LogWarning(
                    "Player context already initialized with {OldPlayerId}, re-initializing with {NewPlayerId}",
                    _playerId,
                    playerId);
            }

            _playerId = playerId;
            _logger.LogDebug("Player context initialized with {PlayerId}", playerId);
        }
    }

    public Result<ActorId> GetPlayerId()
    {
        lock (_lock)
        {
            return _playerId != null
                ? Result.Success(_playerId.Value)
                : Result.Failure<ActorId>("Player context not initialized. Call SetPlayerId() first.");
        }
    }

    public bool IsPlayer(ActorId actorId)
    {
        lock (_lock)
        {
            return _playerId != null && _playerId.Equals(actorId);
        }
    }
}
