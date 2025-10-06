using Darklands.Core.Application;
using Darklands.Core.Application.Repositories;
using Darklands.Core.Domain.Common;

namespace Darklands.Core.Infrastructure.Logging;

/// <summary>
/// Extension methods for enriching ActorId with contextual information for logging.
/// </summary>
/// <remarks>
/// **PURPOSE**: Make logs human-readable while preserving unique IDs for debugging.
///
/// **FORMAT**: "shortId [type: ActorType, name: ACTOR_KEY]"
/// - Example: "8c2de643 [type: Player, name: ACTOR_PLAYER]"
/// - Example: "bdb71a68 [type: Enemy, name: ACTOR_GOBLIN]"
///
/// **ARCHITECTURE**:
/// - Uses IPlayerContext to determine Player vs Enemy
/// - Uses IActorRepository to get actor name key
/// - Fallback to shorter format if services unavailable
///
/// **VS_020 UPDATE**: Now includes actor name from repository
/// </remarks>
public static class ActorIdLoggingExtensions
{
    /// <summary>
    /// Formats ActorId with type and name information for logging.
    /// </summary>
    /// <param name="actorId">The ActorId to format</param>
    /// <param name="playerContext">Player context service (optional - for type detection)</param>
    /// <param name="actorRepository">Actor repository (optional - for name lookup)</param>
    /// <returns>Formatted string: "shortId [type: ActorType, name: ACTOR_KEY]"</returns>
    public static string ToLogString(
        this ActorId actorId,
        IPlayerContext? playerContext = null,
        IActorRepository? actorRepository = null)
    {
        var shortId = actorId.Value.ToString().Substring(0, 8);

        // No context - just ID
        if (playerContext == null && actorRepository == null)
        {
            return shortId;
        }

        // Build format parts
        var actorType = playerContext?.IsPlayer(actorId) == true ? "Player" : "Enemy";

        // Try to get actor name from repository
        string? actorName = null;
        if (actorRepository != null)
        {
            var actorResult = actorRepository.GetByIdAsync(actorId).Result;
            if (actorResult.IsSuccess)
            {
                actorName = actorResult.Value.NameKey;
            }
        }

        // Format: "shortId [type: Type, name: Name]" or "shortId [type: Type]"
        if (actorName != null)
        {
            return $"{shortId} [type: {actorType}, name: {actorName}]";
        }
        else if (playerContext != null)
        {
            return $"{shortId} [type: {actorType}]";
        }
        else
        {
            return shortId;
        }
    }
}
