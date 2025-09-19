using LanguageExt;
using Darklands.Domain.Grid;
using Darklands.Application.Infrastructure.Vision;

namespace Darklands.Application.Vision.Services
{
    /// <summary>
    /// Interface for vision system performance monitoring.
    /// Provides performance tracking and reporting capabilities for FOV calculations.
    /// Part of Phase 3 infrastructure enhancement for VS_011.
    /// </summary>
    public interface IVisionPerformanceMonitor
    {
        /// <summary>
        /// Records the performance of an FOV calculation operation.
        /// </summary>
        /// <param name="actorId">Actor the calculation was for</param>
        /// <param name="calculationTimeMs">Time taken in milliseconds</param>
        /// <param name="tilesVisible">Number of tiles made visible</param>
        /// <param name="tilesChecked">Total tiles checked during calculation</param>
        /// <param name="wasFromCache">Whether result came from cache</param>
        void RecordFOVCalculation(ActorId actorId, double calculationTimeMs, int tilesVisible, int tilesChecked, bool wasFromCache);

        /// <summary>
        /// Gets comprehensive performance statistics for all actors.
        /// </summary>
        Fin<VisionPerformanceReport> GetPerformanceReport();

        /// <summary>
        /// Clears all performance data. Used for testing or periodic cleanup.
        /// </summary>
        void Reset();
    }
}
