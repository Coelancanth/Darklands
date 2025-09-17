using Darklands.Domain.Grid;
using Darklands.Application.Grid.Services;
using Darklands.Application.Infrastructure.Identity;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using System.Linq;

namespace Darklands.Core.Tests.Application.Grid
{
    /// <summary>
    /// Debug test to understand the actual grid state and obstacle placement
    /// </summary>
    [Trait("Category", "Phase3")]
    [Trait("Category", "Application")]
    [Trait("Category", "Grid")]
    [Trait("Category", "Debug")]
    public class GridStateDebugTest
    {
        private readonly ITestOutputHelper _output;
        private readonly InMemoryGridStateService _gridService;

        public GridStateDebugTest(ITestOutputHelper output)
        {
            _output = output;
            _gridService = new InMemoryGridStateService();
        }

        [Fact]
        public void Debug_CheckPathObstacles()
        {
            // Check the actual obstacles on the path from (21,10) to (14,10)
            var obstacles = _gridService.GetObstacles();

            _output.WriteLine($"Total obstacles in grid: {obstacles.Count}");

            // Check each position on the horizontal path
            _output.WriteLine("Checking horizontal path at Y=10:");
            for (int x = 14; x <= 21; x++)
            {
                var pos = new Position(x, 10);
                var isObstacle = obstacles.Contains(pos);
                var isWalkable = _gridService.IsWalkable(pos);

                _output.WriteLine($"  Position ({x}, 10): Obstacle={isObstacle}, Walkable={isWalkable}");

                // The path should be clear
                if (x >= 14 && x <= 21)
                {
                    isObstacle.Should().BeFalse($"Position ({x}, 10) should not be an obstacle");
                    isWalkable.Should().BeTrue($"Position ({x}, 10) should be walkable");
                }
            }

            // Check surrounding tiles for context
            _output.WriteLine("\nChecking surrounding tiles:");
            for (int y = 8; y <= 12; y++)
            {
                string line = $"Y={y:D2}: ";
                for (int x = 12; x <= 23; x++)
                {
                    var pos = new Position(x, y);
                    if (obstacles.Contains(pos))
                    {
                        line += "X ";
                    }
                    else
                    {
                        line += ". ";
                    }
                }
                _output.WriteLine(line);
            }
        }

        [Fact]
        public void Debug_CheckGridDimensions()
        {
            var gridResult = _gridService.GetCurrentGrid();

            gridResult.IsSucc.Should().BeTrue("Grid should be initialized");

            gridResult.Match(
                Succ: grid =>
                {
                    _output.WriteLine($"Grid dimensions: {grid.Width}x{grid.Height}");

                    // Check specific tiles on our path
                    for (int x = 14; x <= 21; x++)
                    {
                        var pos = new Position(x, 10);
                        var tileResult = grid.GetTile(pos);

                        tileResult.Match(
                            Succ: tile =>
                            {
                                _output.WriteLine($"Tile at ({x}, 10): Terrain={tile.TerrainType}, Passable={tile.IsPassable}, Empty={tile.IsEmpty}");
                                return tile;
                            },
                            Fail: error =>
                            {
                                _output.WriteLine($"Failed to get tile at ({x}, 10): {error}");
                                return default;
                            }
                        );
                    }
                },
                Fail: error => throw new Xunit.Sdk.XunitException($"Failed to get grid: {error}")
            );
        }

        [Fact]
        public void Debug_ListAllObstaclesNearPath()
        {
            var obstacles = _gridService.GetObstacles();

            // Find obstacles near the path
            var nearbyObstacles = obstacles
                .Where(o => o.Y >= 8 && o.Y <= 12 && o.X >= 12 && o.X <= 23)
                .OrderBy(o => o.Y)
                .ThenBy(o => o.X);

            _output.WriteLine($"Obstacles near path (21,10) to (14,10):");
            foreach (var obstacle in nearbyObstacles)
            {
                _output.WriteLine($"  Obstacle at {obstacle}");
            }

            // Count obstacles by Y coordinate
            var obstaclesByY = obstacles
                .Where(o => o.X >= 10 && o.X <= 25)
                .GroupBy(o => o.Y)
                .OrderBy(g => g.Key);

            _output.WriteLine("\nObstacle count by Y coordinate (X range 10-25):");
            foreach (var group in obstaclesByY)
            {
                _output.WriteLine($"  Y={group.Key}: {group.Count()} obstacles");
            }
        }
    }
}
