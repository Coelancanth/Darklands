using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Darklands.Application.Vision;
using Darklands.Application.Infrastructure.Vision;
using Darklands.Domain.Common;
using Darklands.Domain.Grid;
using Darklands.Application.Infrastructure.Identity;
using Darklands.Core.Tests.TestUtilities;

namespace Darklands.Core.Tests.Infrastructure.Vision;

/// <summary>
/// Tests for VisionPerformanceMonitor - Phase 3 infrastructure component.
/// Validates performance metrics collection, reporting, and statistical analysis.
/// </summary>
[Trait("Category", "Phase3")]
[Trait("Component", "Infrastructure")]
public class VisionPerformanceMonitorTests
{
    private readonly VisionPerformanceMonitor _monitor;
    private readonly ActorId _testActorId;

    public VisionPerformanceMonitorTests()
    {
        var logger = NullLogger<VisionPerformanceMonitor>.Instance;
        _monitor = new VisionPerformanceMonitor(logger);
        _testActorId = ActorId.NewId(TestIdGenerator.Instance);
    }

    [Fact]
    public void RecordFOVCalculation_WithValidMetrics_ShouldStoreCorrectly()
    {
        // Arrange
        const double calculationTime = 5.5;
        const int tilesVisible = 25;
        const int tilesChecked = 100;

        // Act
        _monitor.RecordFOVCalculation(_testActorId, calculationTime, tilesVisible, tilesChecked, wasFromCache: false);

        // Assert
        var report = _monitor.GetPerformanceReport();
        report.IsSucc.Should().BeTrue();

        var reportValue = report.IfFail(_ => throw new InvalidOperationException());
        reportValue.TotalCalculations.Should().Be(1);
        reportValue.AverageCalculationTimeMs.Should().Be(calculationTime);
        reportValue.CacheHitRate.Should().Be(0.0);
        reportValue.ActorStats.Should().ContainKey(_testActorId);
    }

    [Fact]
    public void RecordFOVCalculation_WithCacheHit_ShouldTrackCacheMetrics()
    {
        // Arrange & Act
        _monitor.RecordFOVCalculation(_testActorId, 1.0, 20, 0, wasFromCache: true);
        _monitor.RecordFOVCalculation(_testActorId, 5.0, 25, 100, wasFromCache: false);

        // Assert
        var report = _monitor.GetPerformanceReport();
        report.IsSucc.Should().BeTrue();

        var reportValue = report.IfFail(_ => throw new InvalidOperationException());
        reportValue.TotalCalculations.Should().Be(1); // Only non-cached calculations
        reportValue.CacheHitRate.Should().Be(0.5); // 1 cache hit out of 2 total operations

        var actorStats = reportValue.ActorStats[_testActorId];
        actorStats.TotalOperations.Should().Be(2);
        actorStats.CacheHits.Should().Be(1);
        actorStats.CacheHitRate.Should().Be(0.5);
    }

    [Fact]
    public void RecordFOVCalculation_MultipleActors_ShouldTrackSeparately()
    {
        // Arrange
        var actor1 = ActorId.NewId(TestIdGenerator.Instance);
        var actor2 = ActorId.NewId(TestIdGenerator.Instance);

        // Act
        _monitor.RecordFOVCalculation(actor1, 3.0, 20, 80, wasFromCache: false);
        _monitor.RecordFOVCalculation(actor2, 7.0, 30, 120, wasFromCache: false);

        // Assert
        var report = _monitor.GetPerformanceReport();
        report.IsSucc.Should().BeTrue();

        var reportValue = report.IfFail(_ => throw new InvalidOperationException());
        reportValue.TotalCalculations.Should().Be(2);
        reportValue.AverageCalculationTimeMs.Should().Be(5.0); // (3.0 + 7.0) / 2
        reportValue.ActorStats.Should().ContainKey(actor1);
        reportValue.ActorStats.Should().ContainKey(actor2);

        reportValue.ActorStats[actor1].AverageCalculationTimeMs.Should().Be(3.0);
        reportValue.ActorStats[actor2].AverageCalculationTimeMs.Should().Be(7.0);
    }

    [Fact]
    public void GetPerformanceReport_WithNoData_ShouldReturnEmptyReport()
    {
        // Act
        var report = _monitor.GetPerformanceReport();

        // Assert
        report.IsSucc.Should().BeTrue();

        var reportValue = report.IfFail(_ => throw new InvalidOperationException());
        reportValue.TotalCalculations.Should().Be(0);
        reportValue.CacheHitRate.Should().Be(0.0);
        reportValue.AverageCalculationTimeMs.Should().Be(0.0);
        reportValue.ActorStats.Should().BeEmpty();
    }

    [Fact]
    public void GetPerformanceReport_WithMultipleCalculations_ShouldCalculatePercentilesCorrectly()
    {
        // Arrange - Create a range of calculation times for statistical analysis
        var times = new[] { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0, 10.0 };

        foreach (var time in times)
        {
            _monitor.RecordFOVCalculation(_testActorId, time, 20, 100, wasFromCache: false);
        }

        // Act
        var report = _monitor.GetPerformanceReport();

        // Assert
        report.IsSucc.Should().BeTrue();

        var reportValue = report.IfFail(_ => throw new InvalidOperationException());
        reportValue.TotalCalculations.Should().Be(10);
        reportValue.AverageCalculationTimeMs.Should().Be(5.5); // Average of 1-10
        reportValue.MedianCalculationTimeMs.Should().Be(5.5); // Median of 1-10
        reportValue.FastestCalculationMs.Should().Be(1.0);
        reportValue.SlowestCalculationMs.Should().Be(10.0);

        // 95th percentile of [1,2,3,4,5,6,7,8,9,10] should be around 9.5
        reportValue.P95CalculationTimeMs.Should().BeInRange(9.0, 10.0);
    }

    [Fact]
    public void Reset_ShouldClearAllMetrics()
    {
        // Arrange
        _monitor.RecordFOVCalculation(_testActorId, 5.0, 25, 100, wasFromCache: false);

        var reportBefore = _monitor.GetPerformanceReport();
        reportBefore.IfFail(_ => throw new InvalidOperationException()).TotalCalculations.Should().Be(1);

        // Act
        _monitor.Reset();

        // Assert
        var reportAfter = _monitor.GetPerformanceReport();
        reportAfter.IsSucc.Should().BeTrue();

        var reportValue = reportAfter.IfFail(_ => throw new InvalidOperationException());
        reportValue.TotalCalculations.Should().Be(0);
        reportValue.ActorStats.Should().BeEmpty();
    }

    [Fact]
    public void RecordFOVCalculation_WithSlowCalculation_ShouldHandleCorrectly()
    {
        // Arrange - Simulate a slow calculation (>10ms warning threshold)
        const double slowTime = 15.5;

        // Act & Assert - Should not throw, just log warning
        var act = () => _monitor.RecordFOVCalculation(_testActorId, slowTime, 50, 200, wasFromCache: false);
        act.Should().NotThrow();

        var report = _monitor.GetPerformanceReport();
        report.IsSucc.Should().BeTrue();

        var reportValue = report.IfFail(_ => throw new InvalidOperationException());
        reportValue.AverageCalculationTimeMs.Should().Be(slowTime);
    }

    [Fact]
    public void RecordFOVCalculation_MixedCacheAndCalculation_ShouldMaintainCorrectStatistics()
    {
        // Arrange & Act - Mix of cache hits and calculations
        _monitor.RecordFOVCalculation(_testActorId, 0.1, 20, 0, wasFromCache: true);     // Cache hit
        _monitor.RecordFOVCalculation(_testActorId, 5.0, 25, 100, wasFromCache: false);  // Calculation
        _monitor.RecordFOVCalculation(_testActorId, 0.1, 25, 0, wasFromCache: true);     // Cache hit
        _monitor.RecordFOVCalculation(_testActorId, 8.0, 30, 150, wasFromCache: false);  // Calculation

        // Assert
        var report = _monitor.GetPerformanceReport();
        report.IsSucc.Should().BeTrue();

        var reportValue = report.IfFail(_ => throw new InvalidOperationException());

        // Total operations vs calculations
        reportValue.TotalCalculations.Should().Be(2); // Only non-cached
        reportValue.CacheHitRate.Should().Be(0.5); // 2 hits out of 4 operations
        reportValue.AverageCalculationTimeMs.Should().Be(6.5); // (5.0 + 8.0) / 2

        // Actor stats should include all operations
        var actorStats = reportValue.ActorStats[_testActorId];
        actorStats.TotalOperations.Should().Be(4);
        actorStats.CacheHits.Should().Be(2);
        actorStats.AverageCalculationTimeMs.Should().Be(6.5); // Average of actual calculations only
    }
}
