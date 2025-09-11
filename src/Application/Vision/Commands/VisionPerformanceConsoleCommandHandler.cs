using LanguageExt;
using LanguageExt.Common;
using MediatR;
using Serilog;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Darklands.Core.Application.Vision.Services;
using Darklands.Core.Infrastructure.Vision;
using static LanguageExt.Prelude;

namespace Darklands.Core.Application.Vision.Commands
{
    /// <summary>
    /// Handler for VisionPerformanceConsoleCommand - Generates performance reports for vision system.
    /// Provides detailed metrics analysis, optimization recommendations, and performance monitoring.
    /// Part of Phase 3 infrastructure enhancement for comprehensive FOV performance tracking.
    /// </summary>
    public class VisionPerformanceConsoleCommandHandler : IRequestHandler<VisionPerformanceConsoleCommand, Fin<string>>
    {
        private readonly IVisionPerformanceMonitor _performanceMonitor;
        private readonly ILogger _logger;

        public VisionPerformanceConsoleCommandHandler(
            IVisionPerformanceMonitor performanceMonitor,
            ILogger logger)
        {
            _performanceMonitor = performanceMonitor;
            _logger = logger;
        }

        public Task<Fin<string>> Handle(VisionPerformanceConsoleCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger?.Debug("Generating vision performance report: {ReportType}, Actor details: {IncludeActorDetails}, Reset: {Reset}",
                    request.ReportType, request.IncludeActorDetails, request.ResetAfterReport);

                var reportResult = _performanceMonitor.GetPerformanceReport();

                return Task.FromResult(reportResult.Match(
                    Succ: report =>
                    {
                        var output = GenerateReport(report, request);

                        if (request.ResetAfterReport)
                        {
                            _performanceMonitor.Reset();
                            _logger?.Debug("Performance metrics reset after report generation");
                        }

                        return Fin<string>.Succ(output);
                    },
                    Fail: error =>
                    {
                        var errorMessage = $"Failed to generate vision performance report: {error.Message}";
                        _logger?.Warning("Vision performance report generation failed: {Error}", error.Message);
                        return Fin<string>.Fail(Error.New(errorMessage, error));
                    }
                ));
            }
            catch (Exception ex)
            {
                var error = Error.New("Vision performance console command failed", ex);
                _logger?.Error(ex, "Vision performance console command failed");
                return Task.FromResult(Fin<string>.Fail(error));
            }
        }

        private string GenerateReport(VisionPerformanceReport report, VisionPerformanceConsoleCommand request)
        {
            var output = new StringBuilder();

            switch (request.ReportType)
            {
                case VisionPerformanceReportType.Quick:
                    GenerateQuickReport(output, report);
                    break;

                case VisionPerformanceReportType.Summary:
                    GenerateSummaryReport(output, report);
                    break;

                case VisionPerformanceReportType.Detailed:
                    GenerateDetailedReport(output, report, request.IncludeActorDetails);
                    break;
            }

            return output.ToString();
        }

        private void GenerateQuickReport(StringBuilder output, VisionPerformanceReport report)
        {
            output.AppendLine("🔍 Vision Performance Quick Status");
            output.AppendLine($"Calculations: {report.TotalCalculations}");
            output.AppendLine($"Cache Hit Rate: {report.CacheHitRate:P1}");
            output.AppendLine($"Average Time: {report.AverageCalculationTimeMs:F2}ms");

            if (report.AverageCalculationTimeMs > 10)
                output.AppendLine("⚠️  Performance warning: Average calculation time > 10ms");
            else if (report.P95CalculationTimeMs > 20)
                output.AppendLine("⚠️  Performance warning: 95th percentile > 20ms");
            else
                output.AppendLine("✅ Performance: Good");
        }

        private void GenerateSummaryReport(StringBuilder output, VisionPerformanceReport report)
        {
            output.AppendLine("🔍 Vision System Performance Report");
            output.AppendLine("=====================================");
            output.AppendLine();

            // Overall statistics
            output.AppendLine("📊 Overall Statistics:");
            output.AppendLine($"  Total FOV Calculations: {report.TotalCalculations}");
            output.AppendLine($"  Cache Hit Rate: {report.CacheHitRate:P1}");
            output.AppendLine($"  Active Actors: {report.ActorStats.Count}");
            output.AppendLine();

            // Timing statistics
            output.AppendLine("⏱️  Timing Analysis:");
            output.AppendLine($"  Average: {report.AverageCalculationTimeMs:F2}ms");
            output.AppendLine($"  Median: {report.MedianCalculationTimeMs:F2}ms");
            output.AppendLine($"  95th Percentile: {report.P95CalculationTimeMs:F2}ms");
            output.AppendLine($"  Fastest: {report.FastestCalculationMs:F2}ms");
            output.AppendLine($"  Slowest: {report.SlowestCalculationMs:F2}ms");
            output.AppendLine();

            // Performance assessment
            output.AppendLine("📈 Performance Assessment:");
            if (report.CacheHitRate < 0.5)
            {
                output.AppendLine("  ⚠️  Low cache hit rate - consider longer cache expiration");
            }
            else if (report.CacheHitRate > 0.8)
            {
                output.AppendLine("  ✅ Excellent cache efficiency");
            }
            else
            {
                output.AppendLine("  ✅ Good cache efficiency");
            }

            if (report.AverageCalculationTimeMs > 10)
            {
                output.AppendLine("  ⚠️  Average calculation time exceeds 10ms target");
                output.AppendLine("      Consider optimizing shadowcasting algorithm");
            }
            else
            {
                output.AppendLine("  ✅ Calculation times within acceptable range");
            }

            if (report.P95CalculationTimeMs > 20)
            {
                output.AppendLine("  ⚠️  95th percentile exceeds 20ms - investigate outliers");
            }
        }

        private void GenerateDetailedReport(StringBuilder output, VisionPerformanceReport report, bool includeActorDetails)
        {
            GenerateSummaryReport(output, report);
            output.AppendLine();

            // Detailed timing breakdown
            output.AppendLine("📊 Detailed Performance Metrics:");
            output.AppendLine($"  Calculation Range: {report.FastestCalculationMs:F2}ms - {report.SlowestCalculationMs:F2}ms");

            var variance = report.P95CalculationTimeMs - report.MedianCalculationTimeMs;
            output.AppendLine($"  Performance Variance: {variance:F2}ms (95th - Median)");

            if (variance > 5)
            {
                output.AppendLine("    ⚠️  High variance indicates inconsistent performance");
            }
            else
            {
                output.AppendLine("    ✅ Low variance - consistent performance");
            }

            output.AppendLine();

            // Actor-specific details if requested
            if (includeActorDetails && report.ActorStats.Any())
            {
                output.AppendLine("👤 Per-Actor Statistics:");
                output.AppendLine("Actor ID     | Operations | Cache Rate | Avg Time | Avg Visible");
                output.AppendLine("-------------|------------|------------|----------|------------");

                foreach (var (actorId, stats) in report.ActorStats.OrderByDescending(kvp => kvp.Value.TotalOperations))
                {
                    var actorIdShort = actorId.Value.ToString()[..8];
                    output.AppendLine($"{actorIdShort} | {stats.TotalOperations,10} | {stats.CacheHitRate,9:P1} | {stats.AverageCalculationTimeMs,7:F2}ms | {stats.AverageTilesVisible,10:F1}");
                }
                output.AppendLine();
            }

            // Optimization recommendations
            output.AppendLine("💡 Optimization Recommendations:");

            if (report.CacheHitRate < 0.3)
            {
                output.AppendLine("  • Increase cache expiration time to improve hit rate");
            }

            if (report.AverageCalculationTimeMs > 5)
            {
                output.AppendLine("  • Profile shadowcasting algorithm for optimization opportunities");
                output.AppendLine("  • Consider early termination for distant tiles");
            }

            if (report.ActorStats.Count > 50)
            {
                output.AppendLine("  • Monitor memory usage with high actor count");
                output.AppendLine("  • Consider periodic cache cleanup for inactive actors");
            }

            var slowActors = report.ActorStats.Where(kvp => kvp.Value.AverageCalculationTimeMs > 10).Count();
            if (slowActors > 0)
            {
                output.AppendLine($"  • {slowActors} actors have slow calculation times - investigate positioning");
            }

            if (report.ActorStats.All(kvp => kvp.Value.AverageTilesVisible < 10))
            {
                output.AppendLine("  • All actors see few tiles - consider increasing vision ranges");
            }
        }
    }
}
