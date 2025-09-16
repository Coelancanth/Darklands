using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using Darklands.Application.Vision.Services;
using Darklands.Domain.Common;
using Darklands.Domain.Grid;
using Darklands.Domain.Vision;
using static LanguageExt.Prelude;

namespace Darklands.Application.Infrastructure.Vision;

/// <summary>
/// Performance monitor for vision system operations.
/// Tracks FOV calculation times, cache hit rates, and provides metrics for optimization.
/// Thread-safe implementation with rolling averages and percentile tracking.
/// </summary>
public sealed class VisionPerformanceMonitor : IVisionPerformanceMonitor
{
    private readonly ILogger<VisionPerformanceMonitor> _logger;
    private readonly ConcurrentQueue<VisionMetric> _metrics;
    private readonly ConcurrentDictionary<ActorId, VisionStats> _actorStats;
    private readonly object _statsLock = new();

    // Configuration
    private const int MaxMetricsHistory = 1000;
    private const int WarningThresholdMs = 10;
    private const int ErrorThresholdMs = 50;

    public VisionPerformanceMonitor(ILogger<VisionPerformanceMonitor> logger)
    {
        _logger = logger;
        _metrics = new ConcurrentQueue<VisionMetric>();
        _actorStats = new ConcurrentDictionary<ActorId, VisionStats>();
    }

    /// <summary>
    /// Records the performance of an FOV calculation operation.
    /// </summary>
    /// <param name="actorId">Actor the calculation was for</param>
    /// <param name="calculationTimeMs">Time taken in milliseconds</param>
    /// <param name="tilesVisible">Number of tiles made visible</param>
    /// <param name="tilesChecked">Total tiles checked during calculation</param>
    /// <param name="wasFromCache">Whether result came from cache</param>
    public void RecordFOVCalculation(ActorId actorId, double calculationTimeMs, int tilesVisible, int tilesChecked, bool wasFromCache)
    {
        var timestamp = DateTime.UtcNow;
        var metric = new VisionMetric(
            ActorId: actorId,
            Timestamp: timestamp,
            CalculationTimeMs: calculationTimeMs,
            TilesVisible: tilesVisible,
            TilesChecked: tilesChecked,
            WasFromCache: wasFromCache
        );

        // Record metric
        _metrics.Enqueue(metric);

        // Maintain rolling window
        while (_metrics.Count > MaxMetricsHistory)
        {
            _metrics.TryDequeue(out _);
        }

        // Update actor stats
        UpdateActorStats(actorId, metric);

        // Log performance warnings
        if (calculationTimeMs > ErrorThresholdMs)
        {
            _logger.LogError("Vision calculation exceeded error threshold: {TimeMs}ms for Actor {ActorId} (visible: {Visible}, checked: {Checked}, cached: {Cached})",
                calculationTimeMs, actorId.Value.ToString()[..8], tilesVisible, tilesChecked, wasFromCache);
        }
        else if (calculationTimeMs > WarningThresholdMs)
        {
            _logger.LogWarning("Vision calculation slow: {TimeMs}ms for Actor {ActorId} (visible: {Visible}, checked: {Checked}, cached: {Cached})",
                calculationTimeMs, actorId.Value.ToString()[..8], tilesVisible, tilesChecked, wasFromCache);
        }
        else
        {
            _logger.LogDebug("Vision calculation: {TimeMs}ms for Actor {ActorId} (visible: {Visible}, checked: {Checked}, cached: {Cached})",
                calculationTimeMs, actorId.Value.ToString()[..8], tilesVisible, tilesChecked, wasFromCache);
        }
    }

    /// <summary>
    /// Gets comprehensive performance statistics for all actors.
    /// </summary>
    public Fin<VisionPerformanceReport> GetPerformanceReport()
    {
        try
        {
            lock (_statsLock)
            {
                var allMetrics = _metrics.ToArray();
                if (allMetrics.Length == 0)
                {
                    return new VisionPerformanceReport(
                        TotalCalculations: 0,
                        CacheHitRate: 0.0,
                        AverageCalculationTimeMs: 0.0,
                        MedianCalculationTimeMs: 0.0,
                        P95CalculationTimeMs: 0.0,
                        SlowestCalculationMs: 0.0,
                        FastestCalculationMs: 0.0,
                        ActorStats: new Dictionary<ActorId, VisionStats>()
                    );
                }

                var calculations = allMetrics.Where(m => !m.WasFromCache).Select(m => m.CalculationTimeMs).OrderBy(t => t).ToArray();
                var cacheHits = allMetrics.Count(m => m.WasFromCache);
                var totalOperations = allMetrics.Length;

                var report = new VisionPerformanceReport(
                    TotalCalculations: calculations.Length,
                    CacheHitRate: totalOperations > 0 ? (double)cacheHits / totalOperations : 0.0,
                    AverageCalculationTimeMs: calculations.Length > 0 ? calculations.Average() : 0.0,
                    MedianCalculationTimeMs: calculations.Length > 0 ? GetPercentile(calculations, 0.5) : 0.0,
                    P95CalculationTimeMs: calculations.Length > 0 ? GetPercentile(calculations, 0.95) : 0.0,
                    SlowestCalculationMs: calculations.Length > 0 ? calculations.Max() : 0.0,
                    FastestCalculationMs: calculations.Length > 0 ? calculations.Min() : 0.0,
                    ActorStats: new Dictionary<ActorId, VisionStats>(_actorStats)
                );

                _logger.LogInformation("Vision performance report: {Calculations} calculations, {CacheHitRate:P1} cache hit rate, {AverageMs:F2}ms average",
                    report.TotalCalculations, report.CacheHitRate, report.AverageCalculationTimeMs);

                return report;
            }
        }
        catch (Exception ex)
        {
            var error = Error.New("Failed to generate vision performance report", ex);
            _logger.LogError(ex, "Error generating vision performance report");
            return FinFail<VisionPerformanceReport>(error);
        }
    }

    /// <summary>
    /// Clears all performance data. Used for testing or periodic cleanup.
    /// </summary>
    public void Reset()
    {
        while (_metrics.TryDequeue(out _)) { }
        _actorStats.Clear();
        _logger.LogDebug("Vision performance monitor reset");
    }

    private void UpdateActorStats(ActorId actorId, VisionMetric metric)
    {
        _actorStats.AddOrUpdate(actorId,
            new VisionStats(
                TotalOperations: 1,
                CacheHits: metric.WasFromCache ? 1 : 0,
                TotalCalculationTimeMs: metric.WasFromCache ? 0.0 : metric.CalculationTimeMs,
                AverageTilesVisible: metric.TilesVisible,
                LastOperationTime: metric.Timestamp
            ),
            (_, existing) => new VisionStats(
                TotalOperations: existing.TotalOperations + 1,
                CacheHits: existing.CacheHits + (metric.WasFromCache ? 1 : 0),
                TotalCalculationTimeMs: existing.TotalCalculationTimeMs + (metric.WasFromCache ? 0.0 : metric.CalculationTimeMs),
                AverageTilesVisible: (existing.AverageTilesVisible * existing.TotalOperations + metric.TilesVisible) / (existing.TotalOperations + 1),
                LastOperationTime: metric.Timestamp
            )
        );
    }

    private static double GetPercentile(double[] sortedValues, double percentile)
    {
        if (sortedValues.Length == 0) return 0.0;
        if (sortedValues.Length == 1) return sortedValues[0];

        var index = percentile * (sortedValues.Length - 1);
        var lower = (int)Math.Floor(index);
        var upper = (int)Math.Ceiling(index);

        if (lower == upper) return sortedValues[lower];

        var weight = index - lower;
        return sortedValues[lower] * (1 - weight) + sortedValues[upper] * weight;
    }
}

/// <summary>
/// Individual vision operation metric.
/// </summary>
public sealed record VisionMetric(
    ActorId ActorId,
    DateTime Timestamp,
    double CalculationTimeMs,
    int TilesVisible,
    int TilesChecked,
    bool WasFromCache
);

/// <summary>
/// Performance statistics for a specific actor.
/// </summary>
public sealed record VisionStats(
    int TotalOperations,
    int CacheHits,
    double TotalCalculationTimeMs,
    double AverageTilesVisible,
    DateTime LastOperationTime
)
{
    public double CacheHitRate => TotalOperations > 0 ? (double)CacheHits / TotalOperations : 0.0;
    public double AverageCalculationTimeMs => (TotalOperations - CacheHits) > 0 ? TotalCalculationTimeMs / (TotalOperations - CacheHits) : 0.0;
}

/// <summary>
/// Comprehensive vision system performance report.
/// </summary>
public sealed record VisionPerformanceReport(
    int TotalCalculations,
    double CacheHitRate,
    double AverageCalculationTimeMs,
    double MedianCalculationTimeMs,
    double P95CalculationTimeMs,
    double SlowestCalculationMs,
    double FastestCalculationMs,
    IReadOnlyDictionary<ActorId, VisionStats> ActorStats
);
