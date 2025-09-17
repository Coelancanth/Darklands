using LanguageExt;
using LanguageExt.Common;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using Darklands.Application.Common;
using Darklands.Domain.Vision;
using Darklands.Domain.Common;
using Darklands.Domain.Grid;
using static LanguageExt.Prelude;

namespace Darklands.Application.Vision.Services
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
            var state = _visionStates.GetValueOrDefault(viewerId, VisionState.CreateEmpty(viewerId));
            var actorIdStr = viewerId.Value.ToString();
            var shortActorId = actorIdStr.Length >= 8 ? actorIdStr[..8] : actorIdStr;

            _logger.Log(LogLevel.Debug, LogCategory.Vision, "Retrieved vision state for Actor {ActorId}: {Visible} visible, {Explored} explored",
                shortActorId, state.CurrentlyVisible.Count, state.PreviouslyExplored.Count);

            return FinSucc(state);
        }

        public Fin<Unit> UpdateVisionState(VisionState visionState)
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

            var actorIdStr = visionState.ViewerId.Value.ToString();
            var shortActorId = actorIdStr.Length >= 8 ? actorIdStr[..8] : actorIdStr;

            _logger.Log(LogLevel.Debug, LogCategory.Vision, "Updated vision state for Actor {ActorId}: {Visible} visible, {Explored} total explored",
                shortActorId,
                mergedState.CurrentlyVisible.Count,
                mergedState.PreviouslyExplored.Count);

            return FinSucc(unit);
        }

        public Fin<Unit> ClearVisionState(ActorId viewerId, int currentTurn)
        {
            var existingState = _visionStates.GetValueOrDefault(viewerId);
            if (existingState != null)
            {
                // Clear current visibility but preserve explored tiles
                var clearedState = existingState.ClearVisibility(currentTurn);
                _visionStates.AddOrUpdate(viewerId, clearedState, (_, _) => clearedState);

                var actorIdStr = viewerId.Value.ToString();
                var shortActorId = actorIdStr.Length >= 8 ? actorIdStr[..8] : actorIdStr;

                _logger.Log(LogLevel.Debug, LogCategory.Vision, "Cleared vision state for Actor {ActorId}, preserved {Explored} explored tiles",
                    shortActorId, clearedState.PreviouslyExplored.Count);
            }

            return FinSucc(unit);
        }

        public Fin<Unit> InvalidateVisionCache(ActorId viewerId)
        {
            var existingState = _visionStates.GetValueOrDefault(viewerId);
            if (existingState != null)
            {
                // Mark for recalculation by setting turn to -1
                var invalidatedState = existingState with { LastCalculatedTurn = -1 };
                _visionStates.AddOrUpdate(viewerId, invalidatedState, (_, _) => invalidatedState);

                var actorIdStr = viewerId.Value.ToString();
                var shortActorId = actorIdStr.Length >= 8 ? actorIdStr[..8] : actorIdStr;

                _logger.Log(LogLevel.Debug, LogCategory.Vision, "Invalidated vision cache for Actor {ActorId}", shortActorId);
            }

            return FinSucc(unit);
        }

        public Fin<Unit> InvalidateAllVisionCaches()
        {
            var invalidated = 0;
            foreach (var kvp in _visionStates)
            {
                var invalidatedState = kvp.Value with { LastCalculatedTurn = -1 };
                _visionStates.TryUpdate(kvp.Key, invalidatedState, kvp.Value);
                invalidated++;
            }

            _logger.Log(LogLevel.Debug, LogCategory.Vision, "Invalidated vision caches for {Count} actors", invalidated);
            return FinSucc(unit);
        }

        public Fin<IDictionary<ActorId, (int visible, int explored, bool needsRecalc)>> GetVisionStatistics()
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
            return FinSucc<IDictionary<ActorId, (int visible, int explored, bool needsRecalc)>>(stats);
        }

        public Fin<VisionState> MergeVisionStates(IEnumerable<ActorId> viewerIds, int currentTurn)
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

            return FinSucc(mergedState);
        }
    }
}
