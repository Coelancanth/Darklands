using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;

namespace Darklands.Core.Application;

/// <summary>
/// Provides access to the current player character's identity.
/// </summary>
/// <remarks>
/// SINGLETON PATTERN: One player per game session (MVP scope).
///
/// INITIALIZATION: Test scenes/game setup must call SetPlayerId() once during initialization.
///
/// WHY: Multiple systems need to know "who is the player":
/// - EnemyDetectionEventHandler: Filter FOV events (only respond to player's vision)
/// - TurnQueue: Initialize with player at time=0
/// - UI Systems: Display player-specific data
///
/// FUTURE: Extend to support party-based gameplay (multiple player-controlled actors).
/// </remarks>
public interface IPlayerContext
{
    /// <summary>
    /// Sets the current player character's ActorId.
    /// Must be called once during game initialization.
    /// </summary>
    /// <param name="playerId">The player character's ActorId</param>
    void SetPlayerId(ActorId playerId);

    /// <summary>
    /// Gets the current player character's ActorId.
    /// </summary>
    /// <returns>Result containing PlayerId or Failure if not initialized</returns>
    Result<ActorId> GetPlayerId();

    /// <summary>
    /// Checks if the given ActorId is the player.
    /// </summary>
    /// <param name="actorId">ActorId to check</param>
    /// <returns>True if this is the player, false otherwise</returns>
    bool IsPlayer(ActorId actorId);
}
