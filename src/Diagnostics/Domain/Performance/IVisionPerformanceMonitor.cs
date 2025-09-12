using LanguageExt;
using Darklands.SharedKernel.Domain;

namespace Darklands.Diagnostics.Domain.Performance;

/// <summary>
/// Interface for vision system performance monitoring in Diagnostics context.
/// Allows non-deterministic types (DateTime, double) per ADR-004 exemption.
/// Uses EntityId for cross-context compatibility.
/// </summary>
public interface IVisionPerformanceMonitor
{
    /// <summary>
    /// Records the performance of an FOV calculation operation.
    /// </summary>
    /// <param name="actorId">Actor the calculation was for (as EntityId)</param>
    /// <param name="calculationTimeMs">Time taken in milliseconds</param>
    /// <param name="tilesVisible">Number of tiles made visible</param>
    /// <param name="tilesChecked">Total tiles checked during calculation</param>
    /// <param name="wasFromCache">Whether result came from cache</param>
    void RecordFOVCalculation(EntityId actorId, double calculationTimeMs, int tilesVisible, int tilesChecked, bool wasFromCache);

    /// <summary>
    /// Gets comprehensive performance statistics for all actors.
    /// </summary>
    Fin<VisionPerformanceReport> GetPerformanceReport();

    /// <summary>
    /// Clears all performance data. Used for testing or periodic cleanup.
    /// </summary>
    void Reset();
}
