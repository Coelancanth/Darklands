using Xunit;
using FluentAssertions;
using FsCheck;
using FsCheck.Fluent;
using Darklands.Core.Domain.Grid;
using Darklands.Core.Domain.Vision;
using Darklands.Core.Domain.Common;
using Darklands.Core.Tests.TestUtilities;
using System.Collections.Immutable;
using System.Linq;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Darklands.Core.Tests.Domain.Vision;

/// <summary>
/// Property-based tests for ShadowcastingFOV determinism verification.
/// Ensures identical results across different runs with same inputs (ADR-004 compliance).
/// </summary>
[Trait("Category", "Domain")]
[Trait("Category", "Phase1")]
[Trait("Category", "PropertyBased")]
[Trait("Feature", "Vision")]
public class ShadowcastingFOVDeterminismTests
{
    private readonly IStableIdGenerator _idGenerator = TestIdGenerator.Instance;

    /// <summary>
    /// Property: FOV calculation is deterministic - same inputs always produce identical results
    /// </summary>
    [Fact]
    public void CalculateFOV_WithSameInputs_ProducesIdenticalResults()
    {
        var generator =
            from gridSize in Gen.Choose(10, 30)
            from originX in Gen.Choose(2, gridSize - 3)
            from originY in Gen.Choose(2, gridSize - 3)
            from range in Gen.Choose(1, 10)
            select new
            {
                GridSize = gridSize,
                Origin = new Position(originX, originY),
                Range = range
            };

        Prop.ForAll(generator.ToArbitrary(),
            test =>
            {
                // Arrange
                var grid = CreateEmptyGrid(test.GridSize, test.GridSize);

                // Act - Calculate FOV multiple times with identical inputs
                var result1 = ShadowcastingFOV.CalculateFOV(test.Origin, test.Range, grid);
                var result2 = ShadowcastingFOV.CalculateFOV(test.Origin, test.Range, grid);
                var result3 = ShadowcastingFOV.CalculateFOV(test.Origin, test.Range, grid);

                // Assert
                result1.IsSucc.Should().BeTrue("First FOV calculation should succeed");
                result2.IsSucc.Should().BeTrue("Second FOV calculation should succeed");
                result3.IsSucc.Should().BeTrue("Third FOV calculation should succeed");

                var visible1 = result1.IfFail(ImmutableHashSet<Position>.Empty);
                var visible2 = result2.IfFail(ImmutableHashSet<Position>.Empty);
                var visible3 = result3.IfFail(ImmutableHashSet<Position>.Empty);

                visible1.Should().BeEquivalentTo(visible2,
                    "Multiple FOV calculations with identical inputs must produce identical results");
                visible1.Should().BeEquivalentTo(visible3,
                    "Multiple FOV calculations with identical inputs must produce identical results");
            }
        ).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property: FOV with walls is deterministic across multiple runs
    /// </summary>
    [Fact]
    public void CalculateFOV_WithWalls_ProducesIdenticalResults()
    {
        var generator =
            from gridSize in Gen.Choose(15, 25)
            from originX in Gen.Choose(3, gridSize - 4)
            from originY in Gen.Choose(3, gridSize - 4)
            from range in Gen.Choose(3, 8)
            from wallX in Gen.Choose(0, gridSize - 1)
            from wallY in Gen.Choose(0, gridSize - 1)
            select new
            {
                GridSize = gridSize,
                Origin = new Position(originX, originY),
                Range = range,
                Wall = new Position(wallX, wallY)
            };

        Prop.ForAll(generator.ToArbitrary(),
            test =>
            {
                // Arrange
                var grid = CreateEmptyGrid(test.GridSize, test.GridSize);
                if (test.Wall != test.Origin)
                {
                    grid = PlaceWall(grid, test.Wall);
                }

                // Act - Calculate FOV multiple times
                var result1 = ShadowcastingFOV.CalculateFOV(test.Origin, test.Range, grid);
                var result2 = ShadowcastingFOV.CalculateFOV(test.Origin, test.Range, grid);

                // Assert
                result1.IsSucc.Should().BeTrue("FOV calculation with walls should succeed");
                result2.IsSucc.Should().BeTrue("FOV calculation with walls should succeed");

                var visible1 = result1.IfFail(ImmutableHashSet<Position>.Empty);
                var visible2 = result2.IfFail(ImmutableHashSet<Position>.Empty);

                visible1.Should().BeEquivalentTo(visible2,
                    "FOV with walls must be deterministic across multiple calculations");
            }
        ).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property: FOV results are consistent when grid is recreated identically
    /// </summary>
    [Fact]
    public void CalculateFOV_WithRecreatedGrid_ProducesIdenticalResults()
    {
        var generator =
            from gridSize in Gen.Choose(12, 20)
            from originX in Gen.Choose(2, gridSize - 3)
            from originY in Gen.Choose(2, gridSize - 3)
            from range in Gen.Choose(2, 6)
            select new
            {
                GridSize = gridSize,
                Origin = new Position(originX, originY),
                Range = range
            };

        Prop.ForAll(generator.ToArbitrary(),
            test =>
            {
                // Arrange - Create two identical grids independently
                var grid1 = CreateEmptyGrid(test.GridSize, test.GridSize);
                var grid2 = CreateEmptyGrid(test.GridSize, test.GridSize);

                // Act
                var result1 = ShadowcastingFOV.CalculateFOV(test.Origin, test.Range, grid1);
                var result2 = ShadowcastingFOV.CalculateFOV(test.Origin, test.Range, grid2);

                // Assert
                result1.IsSucc.Should().BeTrue("FOV on first grid should succeed");
                result2.IsSucc.Should().BeTrue("FOV on second grid should succeed");

                var visible1 = result1.IfFail(ImmutableHashSet<Position>.Empty);
                var visible2 = result2.IfFail(ImmutableHashSet<Position>.Empty);

                visible1.Should().BeEquivalentTo(visible2,
                    "Identical grids must produce identical FOV results");
            }
        ).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property: FOV calculation order independence - changing iteration order doesn't affect results
    /// </summary>
    [Fact]
    public void CalculateFOV_OrderIndependence_ProducesConsistentResults()
    {
        var generator =
            from gridSize in Gen.Choose(10, 18)
            from originX in Gen.Choose(3, gridSize - 4)
            from originY in Gen.Choose(3, gridSize - 4)
            from range in Gen.Choose(2, 7)
            select new
            {
                GridSize = gridSize,
                Origin = new Position(originX, originY),
                Range = range
            };

        Prop.ForAll(generator.ToArbitrary(),
            test =>
            {
                // Arrange
                var grid = CreateEmptyGrid(test.GridSize, test.GridSize);

                // Act - Multiple calculations to test internal consistency
                var results = Enumerable.Range(0, 10)
                    .Select(_ => ShadowcastingFOV.CalculateFOV(test.Origin, test.Range, grid))
                    .ToList();

                // Assert
                results.Should().AllSatisfy(r => r.IsSucc.Should().BeTrue("All FOV calculations should succeed"));

                var visibleSets = results
                    .Select(r => r.IfFail(ImmutableHashSet<Position>.Empty))
                    .ToList();

                // All results should be identical
                for (int i = 1; i < visibleSets.Count; i++)
                {
                    visibleSets[i].Should().BeEquivalentTo(visibleSets[0],
                        $"FOV calculation {i} should match first calculation (deterministic)");
                }
            }
        ).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property: FOV results have reasonable bounds based on range
    /// </summary>
    [Fact]
    public void CalculateFOV_ResultBounds_AreReasonable()
    {
        var generator =
            from gridSize in Gen.Choose(20, 40)
            from originX in Gen.Choose(10, gridSize - 11)
            from originY in Gen.Choose(10, gridSize - 11)
            from range in Gen.Choose(1, 15)
            select new
            {
                GridSize = gridSize,
                Origin = new Position(originX, originY),
                Range = range
            };

        Prop.ForAll(generator.ToArbitrary(),
            test =>
            {
                // Arrange
                var grid = CreateEmptyGrid(test.GridSize, test.GridSize);

                // Act
                var result = ShadowcastingFOV.CalculateFOV(test.Origin, test.Range, grid);

                // Assert
                result.IsSucc.Should().BeTrue("FOV calculation should succeed");
                var visiblePositions = result.IfFail(ImmutableHashSet<Position>.Empty);

                // All visible positions should be within grid bounds
                visiblePositions.Should().AllSatisfy(pos =>
                {
                    pos.X.Should().BeInRange(0, test.GridSize - 1);
                    pos.Y.Should().BeInRange(0, test.GridSize - 1);
                });

                // Origin should always be visible
                visiblePositions.Should().Contain(test.Origin, "Origin position should always be visible");

                // No position should be further than range (allowing for slight rounding)
                visiblePositions.Should().AllSatisfy(pos =>
                {
                    var dx = pos.X - test.Origin.X;
                    var dy = pos.Y - test.Origin.Y;
                    var distance = System.Math.Sqrt(dx * dx + dy * dy);
                    distance.Should().BeLessThanOrEqualTo(test.Range + 0.01,
                        $"Position {pos} (distance {distance:F3}) should be within range {test.Range}");
                });

                // Result size should be reasonable for the range
                var maxExpected = (test.Range * 2 + 1) * (test.Range * 2 + 1); // Square area
                visiblePositions.Count.Should().BeLessThanOrEqualTo(maxExpected,
                    "FOV result should not exceed theoretical maximum area");
            }
        ).QuickCheckThrowOnFailure();
    }

    // Helper methods
    private Darklands.Core.Domain.Grid.Grid CreateEmptyGrid(int width, int height)
    {
        return Darklands.Core.Domain.Grid.Grid.Create(_idGenerator, width, height, TerrainType.Open)
            .IfFail(_ => throw new System.InvalidOperationException("Failed to create grid"));
    }

    private Darklands.Core.Domain.Grid.Grid PlaceWall(Darklands.Core.Domain.Grid.Grid grid, Position position)
    {
        return grid.SetTerrain(position, TerrainType.Wall)
            .IfFail(grid);
    }
}
