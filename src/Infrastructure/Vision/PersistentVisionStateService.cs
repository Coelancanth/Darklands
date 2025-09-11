using LanguageExt;
using LanguageExt.Common;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Darklands.Core.Application.Vision.Services;
using Darklands.Core.Domain.Vision;
using Darklands.Core.Domain.Common;
using Darklands.Core.Domain.Grid;
using Darklands.Core.Infrastructure.Vision;
using static LanguageExt.Prelude;

namespace Darklands.Core.Infrastructure.Vision;

/// <summary>
/// Enhanced vision state service with save-ready persistence and performance monitoring.
/// Provides comprehensive cache management, metrics collection, and batched persistence.
/// Thread-safe implementation optimized for frequent FOV calculations.
/// </summary>
public sealed class PersistentVisionStateService : IVisionStateService
{
    private readonly ConcurrentDictionary<ActorId, VisionState> _visionStates;
    private readonly ConcurrentDictionary<ActorId, VisionCacheEntry> _visionCache;
    private readonly IVisionPerformanceMonitor _performanceMonitor;
    private readonly ILogger<PersistentVisionStateService> _logger;

    // Configuration
    private const int CacheExpirationTurns = 5;
    private const int MaxCacheSize = 100;
    private const int PersistenceBatchSize = 10;

    // Persistence tracking
    private readonly ConcurrentQueue<ActorId> _pendingPersistence;
    private readonly object _persistenceLock = new();
    private int _lastPersistedTurn = -1;

    public PersistentVisionStateService(
        IVisionPerformanceMonitor performanceMonitor,
        ILogger<PersistentVisionStateService> logger)
    {
        _visionStates = new ConcurrentDictionary<ActorId, VisionState>();
        _visionCache = new ConcurrentDictionary<ActorId, VisionCacheEntry>();
        _performanceMonitor = performanceMonitor;
        _logger = logger;
        _pendingPersistence = new ConcurrentQueue<ActorId>();
    }

    public Fin<VisionState> GetVisionState(ActorId viewerId)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Check cache first
            if (_visionCache.TryGetValue(viewerId, out var cacheEntry) && !cacheEntry.IsExpired)
            {
                stopwatch.Stop();
                _performanceMonitor.RecordFOVCalculation(
                    viewerId,
                    stopwatch.Elapsed.TotalMilliseconds,
                    cacheEntry.VisionState.CurrentlyVisible.Count,
                    0, // No tiles checked for cache hit
                    wasFromCache: true
                );

                _logger.LogDebug("Cache hit for Actor {ActorId}: {Visible} visible tiles",
                    viewerId.Value.ToString()[..8], cacheEntry.VisionState.CurrentlyVisible.Count);

                return cacheEntry.VisionState;
            }

            // Cache miss - get from persistent state
            var state = _visionStates.GetValueOrDefault(viewerId, VisionState.CreateEmpty(viewerId));

            stopwatch.Stop();
            _performanceMonitor.RecordFOVCalculation(
                viewerId,
                stopwatch.Elapsed.TotalMilliseconds,
                state.CurrentlyVisible.Count,
                state.PreviouslyExplored.Count,
                wasFromCache: false
            );

            _logger.LogDebug("Retrieved vision state for Actor {ActorId}: {Visible} visible, {Explored} explored",
                viewerId.Value.ToString()[..8], state.CurrentlyVisible.Count, state.PreviouslyExplored.Count);

            return state;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var error = Error.New("Failed to retrieve vision state", ex);
            _logger.LogError(ex, "Error retrieving vision state for Actor {ActorId}", viewerId.Value);
            return FinFail<VisionState>(error);
        }
    }

    public Fin<Unit> UpdateVisionState(VisionState visionState)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Get existing state to preserve explored tiles
            var existingState = _visionStates.GetValueOrDefault(visionState.ViewerId);

            VisionState mergedState;
            if (existingState != null)
            {
                // Merge with existing explored tiles (save-ready accumulation)
                var allExplored = existingState.PreviouslyExplored.Union(visionState.CurrentlyVisible);
                mergedState = visionState with
                {
                    PreviouslyExplored = allExplored.ToImmutableHashSet()
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

            // Update persistent state
            _visionStates.AddOrUpdate(visionState.ViewerId, mergedState, (_, _) => mergedState);

            // Update cache with expiration
            var cacheEntry = new VisionCacheEntry(mergedState, visionState.LastCalculatedTurn + CacheExpirationTurns);
            _visionCache.AddOrUpdate(visionState.ViewerId, cacheEntry, (_, _) => cacheEntry);

            // Queue for batch persistence
            _pendingPersistence.Enqueue(visionState.ViewerId);
            TriggerBatchPersistence(visionState.LastCalculatedTurn);

            // Maintain cache size
            MaintainCacheSize();

            stopwatch.Stop();
            _performanceMonitor.RecordFOVCalculation(
                visionState.ViewerId,
                stopwatch.Elapsed.TotalMilliseconds,
                mergedState.CurrentlyVisible.Count,
                mergedState.PreviouslyExplored.Count,
                wasFromCache: false
            );

            _logger.LogDebug("Updated vision state for Actor {ActorId}: {Visible} visible, {Explored} total explored, cached until turn {ExpirationTurn}",
                visionState.ViewerId.Value.ToString()[..8],
                mergedState.CurrentlyVisible.Count,
                mergedState.PreviouslyExplored.Count,
                cacheEntry.ExpirationTurn);

            return unit;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var error = Error.New("Failed to update vision state", ex);
            _logger.LogError(ex, "Error updating vision state for Actor {ActorId}", visionState.ViewerId.Value);
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
                // Merge current visible tiles into explored before clearing (save-ready pattern)
                var updatedExplored = existingState.PreviouslyExplored.Union(existingState.CurrentlyVisible).ToImmutableHashSet();
                var clearedState = existingState.ClearVisibility(currentTurn) with
                {
                    PreviouslyExplored = updatedExplored
                };
                _visionStates.AddOrUpdate(viewerId, clearedState, (_, _) => clearedState);

                // Clear from cache
                _visionCache.TryRemove(viewerId, out _);

                _logger.LogDebug("Cleared vision state for Actor {ActorId}, preserved {Explored} explored tiles",
                    viewerId.Value.ToString()[..8], clearedState.PreviouslyExplored.Count);
            }

            return unit;
        }
        catch (Exception ex)
        {
            var error = Error.New("Failed to clear vision state", ex);
            _logger.LogError(ex, "Error clearing vision state for Actor {ActorId}", viewerId.Value);
            return FinFail<Unit>(error);
        }
    }

    public Fin<Unit> InvalidateVisionCache(ActorId viewerId)
    {
        try
        {
            // Remove from cache to force recalculation
            _visionCache.TryRemove(viewerId, out _);

            // Mark persistent state for recalculation
            var existingState = _visionStates.GetValueOrDefault(viewerId);
            if (existingState != null)
            {
                var invalidatedState = existingState with { LastCalculatedTurn = -1 };
                _visionStates.AddOrUpdate(viewerId, invalidatedState, (_, _) => invalidatedState);
            }

            _logger.LogDebug("Invalidated vision cache for Actor {ActorId}", viewerId.Value.ToString()[..8]);
            return unit;
        }
        catch (Exception ex)
        {
            var error = Error.New("Failed to invalidate vision cache", ex);
            _logger.LogError(ex, "Error invalidating vision cache for Actor {ActorId}", viewerId.Value);
            return FinFail<Unit>(error);
        }
    }

    public Fin<Unit> InvalidateAllVisionCaches()
    {
        try
        {
            var invalidated = 0;

            // Clear all cache entries
            _visionCache.Clear();

            // Mark all persistent states for recalculation
            foreach (var kvp in _visionStates)
            {
                var invalidatedState = kvp.Value with { LastCalculatedTurn = -1 };
                _visionStates.TryUpdate(kvp.Key, invalidatedState, kvp.Value);
                invalidated++;
            }

            _logger.LogDebug("Invalidated all vision caches for {Count} actors", invalidated);
            return unit;
        }
        catch (Exception ex)
        {
            var error = Error.New("Failed to invalidate all vision caches", ex);
            _logger.LogError(ex, "Error invalidating all vision caches");
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
                var needsRecalc = state.LastCalculatedTurn < 0 || !_visionCache.ContainsKey(kvp.Key);

                stats[kvp.Key] = (
                    visible: state.CurrentlyVisible.Count,
                    explored: state.PreviouslyExplored.Count,
                    needsRecalc: needsRecalc
                );
            }

            _logger.LogDebug("Generated vision statistics for {Count} actors, {CacheSize} cached",
                stats.Count, _visionCache.Count);

            return stats;
        }
        catch (Exception ex)
        {
            var error = Error.New("Failed to get vision statistics", ex);
            _logger.LogError(ex, "Error getting vision statistics");
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

            // Merge with other viewers (shared vision mechanics)
            var mergedState = viewerIdsList.Skip(1).Aggregate(baseState, (current, viewerId) =>
            {
                var otherState = _visionStates.GetValueOrDefault(viewerId, VisionState.CreateEmpty(viewerId));
                return current.MergeWith(otherState, currentTurn);
            });

            _logger.LogDebug("Merged vision states from {Count} viewers: {Visible} visible, {Explored} explored",
                viewerIdsList.Count, mergedState.CurrentlyVisible.Count, mergedState.PreviouslyExplored.Count);

            return mergedState;
        }
        catch (Exception ex)
        {
            var error = Error.New("Failed to merge vision states", ex);
            _logger.LogError(ex, "Error merging vision states");
            return FinFail<VisionState>(error);
        }
    }

    /// <summary>
    /// Gets comprehensive performance metrics from the monitor.
    /// </summary>
    public Fin<VisionPerformanceReport> GetPerformanceReport() =>
        _performanceMonitor.GetPerformanceReport();

    /// <summary>
    /// Forces immediate persistence of all pending vision states.
    /// Used for save operations or testing.
    /// </summary>
    public Fin<Unit> FlushPendingPersistence()
    {
        try
        {
            lock (_persistenceLock)
            {
                var flushed = 0;
                while (_pendingPersistence.TryDequeue(out var actorId))
                {
                    // In a real implementation, this would write to database/file
                    // For now, we just track that we would persist
                    flushed++;
                }

                _logger.LogDebug("Flushed {Count} pending vision state persistence operations", flushed);
                return unit;
            }
        }
        catch (Exception ex)
        {
            var error = Error.New("Failed to flush pending persistence", ex);
            _logger.LogError(ex, "Error flushing pending persistence");
            return FinFail<Unit>(error);
        }
    }

    private void TriggerBatchPersistence(int currentTurn)
    {
        if (_pendingPersistence.Count >= PersistenceBatchSize || currentTurn > _lastPersistedTurn + 10)
        {
            // In production, this would trigger async batch persistence
            // For now, we just log that we would persist
            lock (_persistenceLock)
            {
                if (_pendingPersistence.Count > 0)
                {
                    _logger.LogDebug("Would trigger batch persistence for {Count} vision states at turn {Turn}",
                        _pendingPersistence.Count, currentTurn);
                    _lastPersistedTurn = currentTurn;
                }
            }
        }
    }

    private void MaintainCacheSize()
    {
        if (_visionCache.Count <= MaxCacheSize) return;

        // Remove expired entries first
        var expiredKeys = _visionCache
            .Where(kvp => kvp.Value.IsExpired)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _visionCache.TryRemove(key, out _);
        }

        // If still over limit, remove oldest non-expired entries
        if (_visionCache.Count > MaxCacheSize)
        {
            var toRemove = _visionCache.Count - MaxCacheSize;
            var oldestKeys = _visionCache
                .OrderBy(kvp => kvp.Value.VisionState.LastCalculatedTurn)
                .Take(toRemove)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in oldestKeys)
            {
                _visionCache.TryRemove(key, out _);
            }

            _logger.LogDebug("Pruned {Expired} expired and {Oldest} oldest cache entries",
                expiredKeys.Count, oldestKeys.Count);
        }
    }
}

/// <summary>
/// Vision cache entry with expiration tracking.
/// </summary>
internal sealed record VisionCacheEntry(VisionState VisionState, int ExpirationTurn)
{
    public bool IsExpired => VisionState.LastCalculatedTurn >= 0 && VisionState.LastCalculatedTurn >= ExpirationTurn;
}
