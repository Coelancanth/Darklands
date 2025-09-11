using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Immutable;
using Darklands.Core.Infrastructure.Vision;
using Darklands.Core.Domain.Vision;
using Darklands.Core.Domain.Common;
using Darklands.Core.Domain.Grid;
using Darklands.Core.Infrastructure.Identity;
using Darklands.Core.Tests.TestUtilities;

namespace Darklands.Core.Tests.Infrastructure.Vision;

/// <summary>
/// Tests for PersistentVisionStateService - Phase 3 enhanced infrastructure component.
/// Validates enhanced persistence, performance monitoring integration, and cache management.
/// </summary>
[Trait("Category", "Phase3")]
[Trait("Component", "Infrastructure")]
public class PersistentVisionStateServiceTests
{
    private readonly PersistentVisionStateService _service;
    private readonly VisionPerformanceMonitor _performanceMonitor;
    private readonly ActorId _testActorId;

    public PersistentVisionStateServiceTests()
    {
        var logger = NullLogger<PersistentVisionStateService>.Instance;
        var monitorLogger = NullLogger<VisionPerformanceMonitor>.Instance;

        _performanceMonitor = new VisionPerformanceMonitor(monitorLogger);
        _service = new PersistentVisionStateService(_performanceMonitor, logger);
        _testActorId = ActorId.NewId(TestIdGenerator.Instance);
    }

    [Fact]
    public void GetVisionState_NewActor_ShouldReturnEmptyState()
    {
        // Act
        var result = _service.GetVisionState(_testActorId);

        // Assert
        result.IsSucc.Should().BeTrue();

        var state = result.IfFail(_ => throw new InvalidOperationException());
        state.ViewerId.Should().Be(_testActorId);
        state.CurrentlyVisible.Should().BeEmpty();
        state.PreviouslyExplored.Should().BeEmpty();
        state.LastCalculatedTurn.Should().Be(0);
    }

    [Fact]
    public void UpdateVisionState_NewState_ShouldStoreAndMergeExplored()
    {
        // Arrange
        var visibleTiles = ImmutableHashSet.Create(
            new Position(1, 1),
            new Position(1, 2),
            new Position(2, 1)
        );

        var visionState = new VisionState(
            ViewerId: _testActorId,
            CurrentlyVisible: visibleTiles,
            PreviouslyExplored: ImmutableHashSet<Position>.Empty,
            LastCalculatedTurn: 5
        );

        // Act
        var updateResult = _service.UpdateVisionState(visionState);
        var getResult = _service.GetVisionState(_testActorId);

        // Assert
        updateResult.IsSucc.Should().BeTrue();
        getResult.IsSucc.Should().BeTrue();

        var retrievedState = getResult.IfFail(_ => throw new InvalidOperationException());
        retrievedState.CurrentlyVisible.Should().BeEquivalentTo(visibleTiles);
        retrievedState.PreviouslyExplored.Should().BeEquivalentTo(visibleTiles); // First time, so visible becomes explored
    }

    [Fact]
    public void UpdateVisionState_ExistingState_ShouldAccumulateExploredTiles()
    {
        // Arrange
        var firstVisible = ImmutableHashSet.Create(new Position(1, 1), new Position(1, 2));
        var secondVisible = ImmutableHashSet.Create(new Position(2, 1), new Position(2, 2));

        var firstState = new VisionState(_testActorId, firstVisible, ImmutableHashSet<Position>.Empty, 1);
        var secondState = new VisionState(_testActorId, secondVisible, ImmutableHashSet<Position>.Empty, 2);

        // Act
        _service.UpdateVisionState(firstState);
        _service.UpdateVisionState(secondState);
        var result = _service.GetVisionState(_testActorId);

        // Assert
        result.IsSucc.Should().BeTrue();

        var finalState = result.IfFail(_ => throw new InvalidOperationException());
        finalState.CurrentlyVisible.Should().BeEquivalentTo(secondVisible);
        finalState.PreviouslyExplored.Should().BeEquivalentTo(firstVisible.Union(secondVisible)); // Accumulated exploration
    }

    [Fact]
    public void GetVisionState_WithCaching_ShouldRecordPerformanceMetrics()
    {
        // Arrange
        var visionState = new VisionState(
            _testActorId,
            ImmutableHashSet.Create(new Position(1, 1)),
            ImmutableHashSet<Position>.Empty,
            1
        );

        _service.UpdateVisionState(visionState);

        // Act - First call should be cache miss, second should be cache hit
        _service.GetVisionState(_testActorId);  // Cache population
        _service.GetVisionState(_testActorId);  // Cache hit

        // Assert
        var performanceReport = _performanceMonitor.GetPerformanceReport();
        performanceReport.IsSucc.Should().BeTrue();

        var report = performanceReport.IfFail(_ => throw new InvalidOperationException());
        report.ActorStats.Should().ContainKey(_testActorId);

        var actorStats = report.ActorStats[_testActorId];
        actorStats.TotalOperations.Should().BeGreaterThan(0);
        actorStats.CacheHitRate.Should().BeGreaterThan(0); // Should have some cache hits
    }

    [Fact]
    public void ClearVisionState_ExistingState_ShouldPreserveExploredTiles()
    {
        // Arrange - Build up explored tiles through normal usage
        var exploredTiles = ImmutableHashSet.Create(new Position(3, 3), new Position(3, 4));
        var initialState = new VisionState(_testActorId, exploredTiles, ImmutableHashSet<Position>.Empty, 1);
        _service.UpdateVisionState(initialState);

        // Then update with new visible tiles
        var visibleTiles = ImmutableHashSet.Create(new Position(1, 1), new Position(1, 2));
        var visionState = new VisionState(_testActorId, visibleTiles, ImmutableHashSet<Position>.Empty, 5);
        _service.UpdateVisionState(visionState);

        // Act
        var clearResult = _service.ClearVisionState(_testActorId, 10);
        var getResult = _service.GetVisionState(_testActorId);

        // Assert
        clearResult.IsSucc.Should().BeTrue();
        getResult.IsSucc.Should().BeTrue();

        var clearedState = getResult.IfFail(_ => throw new InvalidOperationException());
        clearedState.CurrentlyVisible.Should().BeEmpty(); // Cleared
        clearedState.PreviouslyExplored.Should().BeEquivalentTo(exploredTiles.Union(visibleTiles)); // Preserved and merged
    }

    [Fact]
    public void InvalidateVisionCache_ExistingState_ShouldForceRecalculation()
    {
        // Arrange
        var visionState = new VisionState(_testActorId, ImmutableHashSet.Create(new Position(1, 1)), ImmutableHashSet<Position>.Empty, 5);
        _service.UpdateVisionState(visionState);

        // Act
        var invalidateResult = _service.InvalidateVisionCache(_testActorId);
        var getResult = _service.GetVisionState(_testActorId);

        // Assert
        invalidateResult.IsSucc.Should().BeTrue();
        getResult.IsSucc.Should().BeTrue();

        var state = getResult.IfFail(_ => throw new InvalidOperationException());
        state.LastCalculatedTurn.Should().Be(-1); // Marked for recalculation
    }

    [Fact]
    public void InvalidateAllVisionCaches_MultipleActors_ShouldInvalidateAll()
    {
        // Arrange
        var actor1 = ActorId.NewId(TestIdGenerator.Instance);
        var actor2 = ActorId.NewId(TestIdGenerator.Instance);

        var state1 = new VisionState(actor1, ImmutableHashSet.Create(new Position(1, 1)), ImmutableHashSet<Position>.Empty, 5);
        var state2 = new VisionState(actor2, ImmutableHashSet.Create(new Position(2, 2)), ImmutableHashSet<Position>.Empty, 5);

        _service.UpdateVisionState(state1);
        _service.UpdateVisionState(state2);

        // Act
        var invalidateResult = _service.InvalidateAllVisionCaches();

        // Assert
        invalidateResult.IsSucc.Should().BeTrue();

        var getResult1 = _service.GetVisionState(actor1);
        var getResult2 = _service.GetVisionState(actor2);

        getResult1.IsSucc.Should().BeTrue();
        getResult2.IsSucc.Should().BeTrue();

        getResult1.IfFail(_ => throw new InvalidOperationException()).LastCalculatedTurn.Should().Be(-1);
        getResult2.IfFail(_ => throw new InvalidOperationException()).LastCalculatedTurn.Should().Be(-1);
    }

    [Fact]
    public void GetVisionStatistics_MultipleActors_ShouldReturnCorrectStats()
    {
        // Arrange
        var actor1 = ActorId.NewId(TestIdGenerator.Instance);
        var actor2 = ActorId.NewId(TestIdGenerator.Instance);

        // First, add actor1 with initial exploration
        var initialState1 = new VisionState(actor1, ImmutableHashSet.Create(new Position(0, 0)), ImmutableHashSet<Position>.Empty, 1);
        _service.UpdateVisionState(initialState1);

        // Then update with new vision
        var state1 = new VisionState(actor1, ImmutableHashSet.Create(new Position(1, 1), new Position(1, 2)), ImmutableHashSet<Position>.Empty, 5);
        var state2 = new VisionState(actor2, ImmutableHashSet.Create(new Position(2, 2)), ImmutableHashSet<Position>.Empty, 3);

        _service.UpdateVisionState(state1);
        _service.UpdateVisionState(state2);

        // Act
        var statsResult = _service.GetVisionStatistics();

        // Assert
        statsResult.IsSucc.Should().BeTrue();

        var stats = statsResult.IfFail(_ => throw new InvalidOperationException());
        stats.Should().ContainKey(actor1);
        stats.Should().ContainKey(actor2);

        stats[actor1].visible.Should().Be(2);
        stats[actor1].explored.Should().Be(3); // Original explored + new visible

        stats[actor2].visible.Should().Be(1);
        stats[actor2].explored.Should().Be(1); // New visible becomes explored
    }

    [Fact]
    public void MergeVisionStates_MultipleActors_ShouldCombineVision()
    {
        // Arrange
        var actor1 = ActorId.NewId(TestIdGenerator.Instance);
        var actor2 = ActorId.NewId(TestIdGenerator.Instance);

        var visible1 = ImmutableHashSet.Create(new Position(1, 1), new Position(1, 2));
        var visible2 = ImmutableHashSet.Create(new Position(2, 1), new Position(2, 2));

        var state1 = new VisionState(actor1, visible1, ImmutableHashSet<Position>.Empty, 5);
        var state2 = new VisionState(actor2, visible2, ImmutableHashSet<Position>.Empty, 5);

        _service.UpdateVisionState(state1);
        _service.UpdateVisionState(state2);

        // Act
        var mergeResult = _service.MergeVisionStates(new[] { actor1, actor2 }, 5);

        // Assert
        mergeResult.IsSucc.Should().BeTrue();

        var mergedState = mergeResult.IfFail(_ => throw new InvalidOperationException());
        mergedState.ViewerId.Should().Be(actor1); // Primary viewer
        mergedState.CurrentlyVisible.Should().Contain(visible1.Union(visible2)); // Combined vision
    }

    [Fact]
    public void MergeVisionStates_EmptyViewerList_ShouldReturnError()
    {
        // Act
        var result = _service.MergeVisionStates(Array.Empty<ActorId>(), 5);

        // Assert
        result.IsFail.Should().BeTrue();
    }

    [Fact]
    public void GetPerformanceReport_ShouldReturnMonitorReport()
    {
        // Arrange - Generate some activity to record metrics
        var visionState = new VisionState(_testActorId, ImmutableHashSet.Create(new Position(1, 1)), ImmutableHashSet<Position>.Empty, 1);
        _service.UpdateVisionState(visionState);
        _service.GetVisionState(_testActorId);

        // Act
        var reportResult = _service.GetPerformanceReport();

        // Assert
        reportResult.IsSucc.Should().BeTrue();

        var report = reportResult.IfFail(_ => throw new InvalidOperationException());
        report.Should().NotBeNull();
        report.ActorStats.Should().ContainKey(_testActorId);
    }

    [Fact]
    public void FlushPendingPersistence_ShouldCompleteSuccessfully()
    {
        // Arrange - Create some state that would need persistence
        var visionState = new VisionState(_testActorId, ImmutableHashSet.Create(new Position(1, 1)), ImmutableHashSet<Position>.Empty, 1);
        _service.UpdateVisionState(visionState);

        // Act
        var flushResult = _service.FlushPendingPersistence();

        // Assert
        flushResult.IsSucc.Should().BeTrue();
    }
}
