using LanguageExt;
using LanguageExt.Common;
using Darklands.Core.Domain.Vision;
using Darklands.Core.Domain.Common;
using Darklands.Core.Domain.Grid;

namespace Darklands.Core.Application.Vision.Services
{
    /// <summary>
    /// Service for managing vision state persistence and caching.
    /// Handles explored tile accumulation and vision cache invalidation.
    /// Save-ready implementation supports persistence layer integration.
    /// </summary>
    public interface IVisionStateService
    {
        /// <summary>
        /// Gets the current vision state for an actor.
        /// Returns empty state if actor has no previous vision data.
        /// </summary>
        /// <param name="viewerId">Actor to get vision state for</param>
        /// <returns>Current vision state or error</returns>
        Fin<VisionState> GetVisionState(ActorId viewerId);

        /// <summary>
        /// Updates the vision state for an actor.
        /// Merges with existing explored tiles and updates cache.
        /// </summary>
        /// <param name="visionState">New vision state to store</param>
        /// <returns>Success or error</returns>
        Fin<Unit> UpdateVisionState(VisionState visionState);

        /// <summary>
        /// Clears vision state for an actor (when they die or become blinded).
        /// Preserves explored tiles for potential resurrection/recovery.
        /// </summary>
        /// <param name="viewerId">Actor to clear vision for</param>
        /// <param name="currentTurn">Current game turn</param>
        /// <returns>Success or error</returns>
        Fin<Unit> ClearVisionState(ActorId viewerId, int currentTurn);

        /// <summary>
        /// Invalidates vision cache for an actor (when they move).
        /// Forces recalculation on next FOV request.
        /// </summary>
        /// <param name="viewerId">Actor to invalidate cache for</param>
        /// <returns>Success or error</returns>
        Fin<Unit> InvalidateVisionCache(ActorId viewerId);

        /// <summary>
        /// Invalidates vision cache for all actors in the game.
        /// Used when significant map changes occur (walls destroyed, etc.).
        /// </summary>
        /// <returns>Success or error</returns>
        Fin<Unit> InvalidateAllVisionCaches();

        /// <summary>
        /// Gets vision statistics for all actors.
        /// Used for debugging and performance monitoring.
        /// </summary>
        /// <returns>Dictionary of actor ID to vision statistics</returns>
        Fin<IDictionary<ActorId, (int visible, int explored, bool needsRecalc)>> GetVisionStatistics();

        /// <summary>
        /// Merges vision states between actors (for shared vision mechanics).
        /// Creates combined vision state from multiple sources.
        /// </summary>
        /// <param name="viewerIds">Actors whose vision to merge</param>
        /// <param name="currentTurn">Current game turn</param>
        /// <returns>Merged vision state or error</returns>
        Fin<VisionState> MergeVisionStates(IEnumerable<ActorId> viewerIds, int currentTurn);
    }
}
