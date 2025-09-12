using Darklands.SharedKernel.Domain;

namespace Darklands.Diagnostics.Domain.Performance;

/// <summary>
/// Individual vision operation metric for Diagnostics context.
/// Allows non-deterministic DateTime and double types per ADR-004 exemption.
/// </summary>
public sealed record VisionMetric(
    EntityId ActorId,      // Uses SharedKernel EntityId, not Domain ActorId
    DateTime Timestamp,    // ✅ Allowed in Diagnostics context
    double CalculationTimeMs,  // ✅ Allowed in Diagnostics context
    int TilesVisible,
    int TilesChecked,
    bool WasFromCache
);

/// <summary>
/// Performance statistics for a specific actor in Diagnostics context.
/// </summary>
public sealed record VisionStats(
    int TotalOperations,
    int CacheHits,
    double TotalCalculationTimeMs,  // ✅ Allowed in Diagnostics context
    double AverageTilesVisible,     // ✅ Allowed in Diagnostics context
    DateTime LastOperationTime      // ✅ Allowed in Diagnostics context
)
{
    public double CacheHitRate => TotalOperations > 0 ? (double)CacheHits / TotalOperations : 0.0;
    public double AverageCalculationTimeMs => (TotalOperations - CacheHits) > 0 ? TotalCalculationTimeMs / (TotalOperations - CacheHits) : 0.0;
}

/// <summary>
/// Comprehensive vision system performance report for Diagnostics context.
/// </summary>
public sealed record VisionPerformanceReport(
    int TotalCalculations,
    double CacheHitRate,           // ✅ Allowed in Diagnostics context
    double AverageCalculationTimeMs,  // ✅ Allowed in Diagnostics context
    double MedianCalculationTimeMs,   // ✅ Allowed in Diagnostics context
    double P95CalculationTimeMs,      // ✅ Allowed in Diagnostics context
    double SlowestCalculationMs,      // ✅ Allowed in Diagnostics context
    double FastestCalculationMs,      // ✅ Allowed in Diagnostics context
    IReadOnlyDictionary<EntityId, VisionStats> ActorStats  // EntityId, not ActorId
);
