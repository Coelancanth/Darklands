using LanguageExt;
using LanguageExt.Common;
using MediatR;
using Darklands.Application.Infrastructure.Vision;
using static LanguageExt.Prelude;

namespace Darklands.Application.Vision.Commands
{
    /// <summary>
    /// Console command for viewing vision system performance metrics.
    /// Provides comprehensive reporting for FOV calculation timings, cache hit rates, and optimization insights.
    /// Part of Phase 3 infrastructure enhancement for VS_011.
    /// </summary>
    public sealed record VisionPerformanceConsoleCommand : IRequest<Fin<string>>
    {
        /// <summary>
        /// Type of performance report to generate.
        /// </summary>
        public VisionPerformanceReportType ReportType { get; init; } = VisionPerformanceReportType.Summary;

        /// <summary>
        /// Whether to include per-actor detailed statistics.
        /// </summary>
        public bool IncludeActorDetails { get; init; } = false;

        /// <summary>
        /// Whether to reset performance metrics after generating the report.
        /// </summary>
        public bool ResetAfterReport { get; init; } = false;
    }

    /// <summary>
    /// Types of performance reports available.
    /// </summary>
    public enum VisionPerformanceReportType
    {
        /// <summary>Summary statistics only.</summary>
        Summary,
        /// <summary>Detailed performance breakdown.</summary>
        Detailed,
        /// <summary>Quick status check for monitoring.</summary>
        Quick
    }
}
