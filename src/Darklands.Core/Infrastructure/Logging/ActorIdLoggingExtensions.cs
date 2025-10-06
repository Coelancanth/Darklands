using Darklands.Core.Application;
using Darklands.Core.Domain.Common;

namespace Darklands.Core.Infrastructure.Logging;

/// <summary>
/// Extension methods for enriching ActorId with contextual information for logging.
/// </summary>
/// <remarks>
/// **PURPOSE**: Make logs human-readable while preserving unique IDs for debugging.
///
/// **FORMAT**: "shortId [type: ActorType]"
/// - Example: "8c2de643 [type: Player]"
/// - Example: "bdb71a68 [type: Enemy]"
///
/// **ARCHITECTURE**:
/// - Uses IPlayerContext to determine Player vs Enemy
/// - Fallback to "Unknown" if context unavailable
/// - TODO (VS_020): Replace with IActorNameResolver when Actor entities exist
///   Future format: "8c2de643 [type: Player, name: Warrior]"
/// </remarks>
public static class ActorIdLoggingExtensions
{
    /// <summary>
    /// Formats ActorId with type information for logging.
    /// </summary>
    /// <param name="actorId">The ActorId to format</param>
    /// <param name="playerContext">Player context service (optional - for type detection)</param>
    /// <returns>Formatted string: "shortId [type: ActorType]"</returns>
    public static string ToLogString(this ActorId actorId, IPlayerContext? playerContext = null)
    {
        var shortId = actorId.Value.ToString().Substring(0, 8);

        if (playerContext == null)
        {
            // No context available - show ID only
            return shortId;
        }

        // Determine actor type using player context
        var actorType = playerContext.IsPlayer(actorId) ? "Player" : "Enemy";

        return $"{shortId} [type: {actorType}]";
    }
}
