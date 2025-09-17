using Darklands.Domain.Grid;
using Darklands.Domain.Pathfinding;
using FluentAssertions;
using Xunit;
using System.Collections.Immutable;
using Xunit.Abstractions;

namespace Darklands.Core.Tests.Domain.Pathfinding
{
    /// <summary>
    /// Debug test to reproduce and diagnose the runtime pathfinding failure
    /// Specifically tests the (21,10) → (14,10) path that's failing in production
    /// </summary>
    [Trait("Category", "Phase1")]
    [Trait("Category", "Domain")]
    [Trait("Category", "Pathfinding")]
    [Trait("Category", "Debug")]
    public class AStarRuntimeDebugTest
    {
        private readonly ITestOutputHelper _output;
        private readonly AStarAlgorithm _pathfinding;

        public AStarRuntimeDebugTest(ITestOutputHelper output)
        {
            _output = output;
            _pathfinding = new AStarAlgorithm();
        }

        [Fact]
        public void Debug_SimpleFOV_TwoTilesEast()
        {
            // This is the exact case that's failing in runtime:
            // Moving from (21,10) to (14,10) - straight horizontal path
            var start = new Position(21, 10);
            var end = new Position(14, 10);

            // Create empty obstacles first
            var obstacles = ImmutableHashSet<Position>.Empty;

            System.Console.WriteLine($"[TEST] Testing path from {start} to {end}");
            System.Console.WriteLine($"[TEST] Initial obstacles: {obstacles.Count}");

            // First test with no obstacles at all
            var result = _pathfinding.FindPath(start, end, obstacles);

            result.IsSome.Should().BeTrue("Should find path with no obstacles");

            result.Match(
                Some: path =>
                {
                    _output.WriteLine($"[TEST] Path found with {path.Count} positions");
                    foreach (var pos in path)
                    {
                        _output.WriteLine($"  -> {pos}");
                    }

                    // Verify it's a straight horizontal path
                    path.Count.Should().Be(8, "Should be 8 positions for horizontal movement");
                    path[0].Should().Be(start, "Should start at start position");
                    path[path.Count - 1].Should().Be(end, "Should end at target position");

                    // All positions should have Y=10
                    foreach (var pos in path)
                    {
                        pos.Y.Should().Be(10, $"All positions should be on Y=10, but got {pos}");
                    }
                },
                None: () => throw new Xunit.Sdk.XunitException("No path found when there should be one")
            );
        }

        [Fact]
        public void Debug_WithSimulatedGridObstacles()
        {
            // Test with obstacles that simulate the actual grid
            var start = new Position(21, 10);
            var end = new Position(14, 10);

            // Add perimeter walls like in the actual grid
            var obstacles = ImmutableHashSet<Position>.Empty;

            // Add top and bottom walls (but not at our Y=10 level)
            for (int x = 0; x < 30; x++)
            {
                obstacles = obstacles.Add(new Position(x, 0));      // Top border
                obstacles = obstacles.Add(new Position(x, 19));     // Bottom border
            }

            // Add side walls
            for (int y = 0; y < 20; y++)
            {
                obstacles = obstacles.Add(new Position(0, y));      // Left border
                obstacles = obstacles.Add(new Position(29, y));     // Right border
            }

            // Add the horizontal wall at Y=6 (shouldn't block Y=10)
            for (int x = 3; x <= 12; x++)
            {
                obstacles = obstacles.Add(new Position(x, 6));
            }
            for (int x = 14; x <= 26; x++)
            {
                obstacles = obstacles.Add(new Position(x, 6));
            }

            _output.WriteLine($"[TEST] Testing with grid-like obstacles: {obstacles.Count} total");

            var result = _pathfinding.FindPath(start, end, obstacles);

            result.IsSome.Should().BeTrue("Should find path even with walls");

            result.Match(
                Some: path =>
                {
                    _output.WriteLine($"[TEST] Path found with {path.Count} positions");
                    path[0].Should().Be(start);
                    path[path.Count - 1].Should().Be(end);
                },
                None: () => throw new Xunit.Sdk.XunitException("Failed to find path with simulated grid obstacles")
            );
        }

        [Fact]
        public void Debug_BlockedEndpoint()
        {
            // Test what happens when the endpoint is blocked
            var start = new Position(21, 10);
            var end = new Position(14, 10);

            // Block the endpoint
            var obstacles = ImmutableHashSet<Position>.Empty.Add(end);

            _output.WriteLine($"[TEST] Testing with blocked endpoint");

            var result = _pathfinding.FindPath(start, end, obstacles);

            result.IsNone.Should().BeTrue("Should not find path when endpoint is blocked");
            _output.WriteLine($"[TEST] Correctly returned no path for blocked endpoint");
        }

        [Fact]
        public void Debug_ObstaclesInPath()
        {
            // Test with obstacles directly in the path
            var start = new Position(21, 10);
            var end = new Position(14, 10);

            // Add some obstacles in the direct path
            var obstacles = ImmutableHashSet<Position>.Empty
                .Add(new Position(18, 10))  // Block middle of path
                .Add(new Position(17, 10));  // Block adjacent position

            _output.WriteLine($"[TEST] Testing with obstacles in direct path");

            var result = _pathfinding.FindPath(start, end, obstacles);

            // Should still find a path by going around
            result.IsSome.Should().BeTrue("Should find alternate path around obstacles");

            result.Match(
                Some: path =>
                {
                    _output.WriteLine($"[TEST] Found alternate path with {path.Count} positions");
                    foreach (var pos in path)
                    {
                        _output.WriteLine($"  -> {pos}");
                    }

                    // Path should go around the obstacles
                    path.Should().NotContain(new Position(18, 10), "Path should not include blocked position");
                    path.Should().NotContain(new Position(17, 10), "Path should not include blocked position");
                },
                None: () => throw new Xunit.Sdk.XunitException("Should have found alternate path")
            );
        }
    }
}
