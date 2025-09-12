using LanguageExt;
using LanguageExt.Common;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using Darklands.Core.Domain.Debug;
using Darklands.Core.Domain.Vision;
using Darklands.Core.Domain.Common;
using Darklands.Core.Domain.Grid;
using static LanguageExt.Prelude;

namespace Darklands.Core.Application.Vision.Services
{
    /// <summary>
    /// In-memory implementation of vision state service.
    /// Thread-safe using ConcurrentDictionary for multiplayer/async scenarios.
    /// Provides caching and explored tile accumulation for fog of war.
    /// </summary>
    public class InMemoryVisionStateService : IVisionStateService
    {
        private readonly ConcurrentDictionary<ActorId, VisionState> _visionStates;
        private readonly ICategoryLogger _logger;

        public InMemoryVisionStateService(ICategoryLogger logger)
        {
            _visionStates = new ConcurrentDictionary<ActorId, VisionState>();
            _logger = logger;
        }

        public Fin<VisionState> GetVisionState(ActorId viewerId)
        {
            try
            {
                var state = _visionStates.GetValueOrDefault(viewerId, VisionState.CreateEmpty(viewerId));
                _logger.Log(LogLevel.Debug, LogCategory.Vision, "Retrieved vision state for Actor {ActorId}: {Visible} visible, {Explored} explored",
                    viewerId.Value.ToString()[..8], state.CurrentlyVisible.Count, state.PreviouslyExplored.Count);
                return state;
            }
            catch (Exception ex)
            {
                var error = Error.New("Failed to retrieve vision state", ex);
                _logger.Log(LogLevel.Error, LogCategory.Vision, "Error retrieving vision state for Actor {ActorId}: {Exception}", viewerId.Value, ex.Message);
                return FinFail<VisionState>(error);
            }
        }

        public Fin<Unit> UpdateVisionState(VisionState visionState)
        {
            try
            {
                // Get existing state to preserve explored tiles
                var existingState = _visionStates.GetValueOrDefault(visionState.ViewerId);

                VisionState mergedState;
                if (existingState != null)
                {
                    // Merge with existing explored tiles
                    mergedState = visionState with
                    {
                        PreviouslyExplored = existingState.PreviouslyExplored.Union(visionState.CurrentlyVisible)
                    };
                }
                else
                {
                    // First time for this actor
                    mergedState = visionState with
                    {
                        PreviouslyExplored = visionState.CurrentlyVisible
                    };
                }

                _visionStates.AddOrUpdate(visionState.ViewerId, mergedState, (_, _) => mergedState);

                _logger.Log(LogLevel.Debug, LogCategory.Vision, "Updated vision state for Actor {ActorId}: {Visible} visible, {Explored} total explored",
                    visionState.ViewerId.Value.ToString()[..8],
                    mergedState.CurrentlyVisible.Count,
                    mergedState.PreviouslyExplored.Count);

                return unit;
            }
            catch (Exception ex)
            {
                var error = Error.New("Failed to update vision state", ex);
                _logger.Log(LogLevel.Error, LogCategory.Vision, "Error updating vision state for Actor {ActorId}: {Exception}", visionState.ViewerId.Value, ex.Message);
                return FinFail<Unit>(error);
            }
        }

        public Fin<Unit> ClearVisionState(ActorId viewerId, int currentTurn)
        {
            try
            {
                var existingState = _visionStates.GetValueOrDefault(viewerId);
                if (existingState != null)
                {
                    // Clear current visibility but preserve explored tiles
                    var clearedState = existingState.ClearVisibility(currentTurn);
                    _visionStates.AddOrUpdate(viewerId, clearedState, (_, _) => clearedState);

                    _logger.Log(LogLevel.Debug, LogCategory.Vision, "Cleared vision state for Actor {ActorId}, preserved {Explored} explored tiles",
                        viewerId.Value.ToString()[..8], clearedState.PreviouslyExplored.Count);
                }

                return unit;
            }
            catch (Exception ex)
            {
                var error = Error.New("Failed to clear vision state", ex);
                _logger.Log(LogLevel.Error, LogCategory.Vision, "Error clearing vision state for Actor {ActorId}: {Exception}", viewerId.Value, ex.Message);
                return FinFail<Unit>(error);
            }
        }

        public Fin<Unit> InvalidateVisionCache(ActorId viewerId)
        {
            try
            {
                var existingState = _visionStates.GetValueOrDefault(viewerId);
                if (existingState != null)
                {
                    // Mark for recalculation by setting turn to -1
                    var invalidatedState = existingState with { LastCalculatedTurn = -1 };
                    _visionStates.AddOrUpdate(viewerId, invalidatedState, (_, _) => invalidatedState);

                    _logger.Log(LogLevel.Debug, LogCategory.Vision, "Invalidated vision cache for Actor {ActorId}", viewerId.Value.ToString()[..8]);
                }

                return unit;
            }
            catch (Exception ex)
            {
                var error = Error.New("Failed to invalidate vision cache", ex);
                _logger.Log(LogLevel.Error, LogCategory.Vision, "Error invalidating vision cache for Actor {ActorId}: {Exception}", viewerId.Value, ex.Message);
                return FinFail<Unit>(error);
            }
        }

        public Fin<Unit> InvalidateAllVisionCaches()
        {
            try
            {
                var invalidated = 0;
                foreach (var kvp in _visionStates)
                {
                    var invalidatedState = kvp.Value with { LastCalculatedTurn = -1 };
                    _visionStates.TryUpdate(kvp.Key, invalidatedState, kvp.Value);
                    invalidated++;
                }

                _logger.Log(LogLevel.Debug, LogCategory.Vision, "Invalidated vision caches for {Count} actors", invalidated);
                return unit;
            }
            catch (Exception ex)
            {
                var error = Error.New("Failed to invalidate all vision caches", ex);
                _logger.Log(LogLevel.Error, LogCategory.Vision, "Error invalidating all vision caches: {Exception}", ex.Message);
                return FinFail<Unit>(error);
            }
        }

        public Fin<IDictionary<ActorId, (int visible, int explored, bool needsRecalc)>> GetVisionStatistics()
        {
            try
            {
                var stats = new Dictionary<ActorId, (int visible, int explored, bool needsRecalc)>();

                foreach (var kvp in _visionStates)
                {
                    var state = kvp.Value;
                    stats[kvp.Key] = (
                        visible: state.CurrentlyVisible.Count,
                        explored: state.PreviouslyExplored.Count,
                        needsRecalc: state.LastCalculatedTurn < 0
                    );
                }

                _logger.Log(LogLevel.Debug, LogCategory.Vision, "Generated vision statistics for {Count} actors", stats.Count);
                return stats;
            }
            catch (Exception ex)
            {
                var error = Error.New("Failed to get vision statistics", ex);
                _logger.Log(LogLevel.Error, LogCategory.Vision, "Error getting vision statistics: {Exception}", ex.Message);
                return FinFail<IDictionary<ActorId, (int visible, int explored, bool needsRecalc)>>(error);
            }
        }

        public Fin<VisionState> MergeVisionStates(IEnumerable<ActorId> viewerIds, int currentTurn)
        {
            try
            {
                var viewerIdsList = viewerIds.ToList();
                if (!viewerIdsList.Any())
                    return FinFail<VisionState>(Error.New("No viewer IDs provided for vision merge"));

                // Use first viewer as base
                var primaryViewerId = viewerIdsList.First();
                var baseState = _visionStates.GetValueOrDefault(primaryViewerId, VisionState.CreateEmpty(primaryViewerId));

                // Merge with other viewers
                var mergedState = viewerIdsList.Skip(1).Aggregate(baseState, (current, viewerId) =>
                {
                    var otherState = _visionStates.GetValueOrDefault(viewerId, VisionState.CreateEmpty(viewerId));
                    return current.MergeWith(otherState, currentTurn);
                });

                _logger.Log(LogLevel.Debug, LogCategory.Vision, "Merged vision states from {Count} viewers: {Visible} visible, {Explored} explored",
                    viewerIdsList.Count, mergedState.CurrentlyVisible.Count, mergedState.PreviouslyExplored.Count);

                return mergedState;
            }
            catch (Exception ex)
            {
                var error = Error.New("Failed to merge vision states", ex);
                _logger.Log(LogLevel.Error, LogCategory.Vision, "Error merging vision states: {Exception}", ex.Message);
                return FinFail<VisionState>(error);
            }
        }
    }
}
