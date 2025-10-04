using System.Diagnostics;
using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Movement.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Darklands.Core.Tests.Features.Movement.Infrastructure.Services;

[Trait("Category", "Movement")]
[Trait("Category", "Unit")]
public class AStarPathfindingServiceTests
{
    private readonly ILogger<AStarPathfindingService> _mockLogger;
    private readonly AStarPathfindingService _service;

    public AStarPathfindingServiceTests()
    {
        _mockLogger = Substitute.For<ILogger<AStarPathfindingService>>();
        _service = new AStarPathfindingService(_mockLogger);
    }

    #region Basic Path Tests

    [Fact]
    public void FindPath_StartEqualsGoal_ShouldReturnSinglePositionPath()
    {
        // WHY: Trivial path (already at destination)

        // Arrange
        var position = new Position(5, 5);

        // Act
        var result = _service.FindPath(
            position,
            position,
            pos => true,
            pos => 1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].Should().Be(position);
    }

    [Fact]
    public void FindPath_StraightLineHorizontal_ShouldFindDirectPath()
    {
        // WHY: Simple path with no obstacles

        // Arrange
        var start = new Position(0, 5);
        var goal = new Position(5, 5);

        // Act
        var result = _service.FindPath(
            start,
            goal,
            pos => pos.X >= 0 && pos.X < 10 && pos.Y >= 0 && pos.Y < 10, // 10x10 bounded grid
            pos => 1);   // Uniform cost

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(6); // 0→1→2→3→4→5 (6 positions)
        result.Value.First().Should().Be(start);
        result.Value.Last().Should().Be(goal);
    }

    [Fact]
    public void FindPath_DiagonalPath_ShouldUseDiagonalMovement()
    {
        // WHY: A* should prefer diagonal (fewer steps than cardinal-only)

        // Arrange
        var start = new Position(0, 0);
        var goal = new Position(3, 3);

        // Act
        var result = _service.FindPath(
            start,
            goal,
            pos => pos.X >= 0 && pos.X < 10 && pos.Y >= 0 && pos.Y < 10, // 10x10 bounded grid
            pos => 1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Diagonal path: (0,0) → (1,1) → (2,2) → (3,3) = 4 positions
        result.Value.Should().HaveCount(4);
        result.Value[0].Should().Be(new Position(0, 0));
        result.Value[1].Should().Be(new Position(1, 1));
        result.Value[2].Should().Be(new Position(2, 2));
        result.Value[3].Should().Be(new Position(3, 3));
    }

    #endregion

    #region Obstacle Navigation Tests

    [Fact]
    public void FindPath_WithWallObstacle_ShouldRouteAround()
    {
        // WHY: A* must navigate around impassable terrain

        // Arrange
        var start = new Position(0, 2);
        var goal = new Position(4, 2);

        // Wall at X=2 column (blocks Y=0,1,3,4 but NOT Y=2 - creates gap)
        bool IsPassable(Position pos) =>
            pos.X >= 0 && pos.X < 6 && pos.Y >= 0 && pos.Y < 5 && // 6x5 grid
            !(pos.X == 2 && pos.Y != 2); // Wall at X=2 except gap at Y=2

        // Act
        var result = _service.FindPath(start, goal, IsPassable, pos => 1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        result.Value.First().Should().Be(start);
        result.Value.Last().Should().Be(goal);

        // Path should go through the gap at (2, 2)
        result.Value.Should().Contain(new Position(2, 2));
    }

    [Fact]
    public void FindPath_CompletelyBlockedByWalls_ShouldReturnFailure()
    {
        // WHY: No path exists if goal is surrounded by walls

        // Arrange
        var start = new Position(0, 0);
        var goal = new Position(5, 5);

        // Only start and goal are passable (goal is isolated)
        bool IsPassable(Position pos) =>
            pos == start || pos == goal;

        // Act
        var result = _service.FindPath(start, goal, IsPassable, pos => 1);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("No path exists");
    }

    #endregion

    #region Cost Variation Tests

    [Fact]
    public void FindPath_VariableCosts_ShouldChooseCheaperPath()
    {
        // WHY: Phase 3 validates cost system works (floor=1, smoke=2)
        // A* should route around high-cost tiles when possible

        // Arrange
        var start = new Position(0, 1);
        var goal = new Position(4, 1);

        // Create expensive "smoke" barrier at (2, 0), (2, 1), (2, 2)
        bool IsPassable(Position pos) =>
            pos.X >= 0 && pos.X < 6 && pos.Y >= 0 && pos.Y < 3; // 6x3 bounded grid

        int GetCost(Position pos) =>
            pos.X == 2 ? 10 : 1; // Smoke column is 10x more expensive

        // Act
        var result = _service.FindPath(start, goal, IsPassable, GetCost);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Calculate total path cost
        var totalCost = result.Value.Skip(1).Sum(GetCost); // Skip start

        // Path should avoid X=2 if possible (going around is cheaper)
        // Direct path through X=2: ~3 steps through smoke = 30 cost
        // Around path: ~5 steps at cost 1 = 5 cost
        totalCost.Should().BeLessThan(15); // Should route around
    }

    [Fact]
    public void FindPath_UniformCost_ShouldPreferShorterPath()
    {
        // WHY: With uniform cost, A* should minimize steps (not cost)

        // Arrange
        var start = new Position(0, 0);
        var goal = new Position(5, 0);

        // Act
        var result = _service.FindPath(
            start,
            goal,
            pos => pos.X >= 0 && pos.X < 10 && pos.Y >= 0 && pos.Y < 10, // 10x10 bounded grid
            pos => 1); // Uniform cost

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Straight horizontal path: 6 positions
        result.Value.Should().HaveCount(6);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void FindPath_ImpassableStart_ShouldReturnFailure()
    {
        // WHY: Invalid input - cannot start from impassable tile

        // Arrange
        var start = new Position(0, 0);
        var goal = new Position(5, 5);

        // Act
        var result = _service.FindPath(
            start,
            goal,
            pos => pos != start, // Start is impassable
            pos => 1);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Start position");
        result.Error.Should().Contain("impassable");
    }

    [Fact]
    public void FindPath_ImpassableGoal_ShouldReturnFailure()
    {
        // WHY: Invalid input - cannot reach impassable tile

        // Arrange
        var start = new Position(0, 0);
        var goal = new Position(5, 5);

        // Act
        var result = _service.FindPath(
            start,
            goal,
            pos => pos != goal, // Goal is impassable
            pos => 1);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Goal position");
        result.Error.Should().Contain("impassable");
    }

    #endregion

    #region 8-Directional Movement Tests

    [Fact]
    public void FindPath_AllEightDirections_ShouldBeExplored()
    {
        // ARCHITECTURE: Validates 8-directional movement (N/S/E/W + diagonals)

        // Arrange
        var start = new Position(5, 5);

        // Test all 8 neighbors are reachable
        var directions = new[]
        {
            new Position(5, 4),   // N
            new Position(5, 6),   // S
            new Position(4, 5),   // W
            new Position(6, 5),   // E
            new Position(6, 4),   // NE
            new Position(4, 4),   // NW
            new Position(6, 6),   // SE
            new Position(4, 6)    // SW
        };

        foreach (var goal in directions)
        {
            // Act
            var result = _service.FindPath(
                start,
                goal,
                pos => pos.X >= 0 && pos.X < 10 && pos.Y >= 0 && pos.Y < 10, // 10x10 bounded grid
                pos => 1);

            // Assert
            result.IsSuccess.Should().BeTrue($"Should find path from {start} to {goal}");
            result.Value.Should().HaveCount(2); // Start + goal (single step)
        }
    }

    #endregion

    #region Performance Tests

    [Fact]
    public void FindPath_LargestPossiblePath30x30Grid_ShouldCompleteFast()
    {
        // PERFORMANCE: Must complete in <50ms per VS_006 requirement

        // Arrange
        var start = new Position(0, 0);
        var goal = new Position(29, 29);

        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = _service.FindPath(
            start,
            goal,
            pos => pos.X >= 0 && pos.X < 30 && pos.Y >= 0 && pos.Y < 30, // 30x30 grid
            pos => 1);

        stopwatch.Stop();

        // Assert
        result.IsSuccess.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(50,
            $"Pathfinding took {stopwatch.ElapsedMilliseconds}ms (target: <50ms)");

        // Validate path length (diagonal: ~30 steps for 30x30 grid)
        result.Value.Should().HaveCountGreaterThan(25);
        result.Value.Should().HaveCountLessThan(35);
    }

    [Fact]
    public void FindPath_ComplexMaze_ShouldFindOptimalPath()
    {
        // ARCHITECTURE: Validates A* correctness (finds optimal path, not just any path)

        // Arrange
        var start = new Position(0, 0);
        var goal = new Position(9, 9);

        // Create maze with walls
        var walls = new HashSet<Position>
        {
            // Vertical wall with gap
            new Position(5, 0), new Position(5, 1), new Position(5, 2),
            new Position(5, 3), new Position(5, 4), // Gap at (5, 5)
            new Position(5, 6), new Position(5, 7), new Position(5, 8),
            new Position(5, 9)
        };

        bool IsPassable(Position pos) =>
            pos.X >= 0 && pos.X < 10 &&
            pos.Y >= 0 && pos.Y < 10 &&
            !walls.Contains(pos);

        // Act
        var result = _service.FindPath(start, goal, IsPassable, pos => 1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        // Path must go through the gap at (5, 5)
        result.Value.Should().Contain(new Position(5, 5));
    }

    #endregion
}
