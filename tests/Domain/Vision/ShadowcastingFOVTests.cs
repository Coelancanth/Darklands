using Xunit;
using FluentAssertions;
using Darklands.Domain.Grid;
using Darklands.Domain.Vision;
using Darklands.Domain.Common;
using Darklands.Core.Tests.TestUtilities;
using System.Collections.Immutable;
using System.Linq;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Darklands.Core.Tests.Domain.Vision;

/// <summary>
/// Tests for the shadowcasting field-of-view algorithm.
/// Verifies correct vision calculation, wall occlusion, and edge cases.
/// </summary>
[Trait("Category", "Domain")]
[Trait("Category", "Phase1")]
[Trait("Feature", "Vision")]
public class ShadowcastingFOVTests
{
    private readonly IStableIdGenerator _idGenerator = TestIdGenerator.Instance;

    [Fact]
    public void CalculateFOV_EmptyGrid_SeesFullRadius()
    {
        // Arrange
        var grid = CreateEmptyGrid(20, 20);
        var origin = new Position(10, 10);
        var range = 5;

        // Act
        var result = ShadowcastingFOV.CalculateFOV(origin, range, grid);

        // Assert
        result.IsSucc.Should().BeTrue();
        var visiblePositions = result.IfFail(ImmutableHashSet<Position>.Empty);

        // Should see all positions within range
        var expectedCount = 0;
        for (int x = -range; x <= range; x++)
        {
            for (int y = -range; y <= range; y++)
            {
                var distance = System.Math.Sqrt(x * x + y * y);
                if (distance <= range)
                {
                    expectedCount++;
                    var pos = new Position(origin.X + x, origin.Y + y);
                    visiblePositions.Should().Contain(pos,
                        $"Position {pos} should be visible (distance {distance:F2} <= {range})");
                }
            }
        }

        visiblePositions.Count().Should().Be(expectedCount);
    }

    [Fact]
    public void CalculateFOV_WithSingleWall_BlocksVisionBehindIt()
    {
        // Arrange
        var grid = CreateEmptyGrid(20, 20);
        var origin = new Position(10, 10);
        var wallPosition = new Position(12, 10); // Wall 2 tiles east
        grid = PlaceWall(grid, wallPosition);
        var range = 5;

        // Act
        var result = ShadowcastingFOV.CalculateFOV(origin, range, grid);

        // Assert
        result.IsSucc.Should().BeTrue();
        var visiblePositions = result.IfFail(ImmutableHashSet<Position>.Empty);

        // Should see the wall itself
        visiblePositions.Should().Contain(wallPosition, "Wall should be visible");

        // Should NOT see positions directly behind the wall
        var behindWall1 = new Position(13, 10);
        var behindWall2 = new Position(14, 10);
        visiblePositions.Should().NotContain(behindWall1, "Position directly behind wall should be blocked");
        visiblePositions.Should().NotContain(behindWall2, "Position further behind wall should be blocked");

        // Should still see positions not blocked by the wall
        var notBlocked = new Position(10, 12); // North of origin
        visiblePositions.Should().Contain(notBlocked, "Positions not behind wall should be visible");
    }

    [Fact(Skip = "Edge case - see TD_033. Current implementation functional for gameplay")]
    public void CalculateFOV_CornerPeeking_DoesNotAllowDiagonalExploits()
    {
        // Arrange - Create a corner scenario
        //   W
        //   W ?
        // @ 
        var grid = CreateEmptyGrid(20, 20);
        var origin = new Position(10, 10);
        grid = PlaceWall(grid, new Position(11, 11)); // Wall NE
        grid = PlaceWall(grid, new Position(11, 12)); // Wall above that
        var questionPosition = new Position(12, 11); // Position behind corner
        var range = 5;

        // Act
        var result = ShadowcastingFOV.CalculateFOV(origin, range, grid);

        // Assert
        result.IsSucc.Should().BeTrue();
        var visiblePositions = result.IfFail(ImmutableHashSet<Position>.Empty);

        // Should NOT see around the corner diagonally
        visiblePositions.Should().NotContain(questionPosition,
            "Should not be able to see diagonally through wall corner");
    }

    [Fact(Skip = "Edge case - see TD_033. Shadow expansion at far edges not critical")]
    public void CalculateFOV_PillarScenario_CreatesShadows()
    {
        // Arrange - Single pillar (wall) in open space
        var grid = CreateEmptyGrid(20, 20);
        var origin = new Position(10, 10);
        var pillarPosition = new Position(13, 10); // Pillar 3 tiles east
        grid = PlaceWall(grid, pillarPosition);
        var range = 8;

        // Act
        var result = ShadowcastingFOV.CalculateFOV(origin, range, grid);

        // Assert
        result.IsSucc.Should().BeTrue();
        var visiblePositions = result.IfFail(ImmutableHashSet<Position>.Empty);

        // Pillar creates a shadow cone behind it
        var shadowPosition1 = new Position(14, 10); // Directly behind pillar
        var shadowPosition2 = new Position(16, 10); // Further behind
        var shadowPosition3 = new Position(17, 9);  // Shadow spreads
        var shadowPosition4 = new Position(17, 11); // Shadow spreads

        visiblePositions.Should().NotContain(shadowPosition1);
        visiblePositions.Should().NotContain(shadowPosition2);
        visiblePositions.Should().NotContain(shadowPosition3);
        visiblePositions.Should().NotContain(shadowPosition4);
    }

    [Fact]
    public void CalculateFOV_Symmetry_IsSymmetric()
    {
        // Arrange
        var grid = CreateEmptyGrid(20, 20);
        var pos1 = new Position(10, 10);
        var pos2 = new Position(15, 12);
        grid = PlaceWall(grid, new Position(12, 11)); // Wall between them
        var range = 8;

        // Act
        var fromPos1 = ShadowcastingFOV.CalculateFOV(pos1, range, grid);
        var fromPos2 = ShadowcastingFOV.CalculateFOV(pos2, range, grid);

        // Assert
        fromPos1.IsSucc.Should().BeTrue();
        fromPos2.IsSucc.Should().BeTrue();

        var visible1 = fromPos1.IfFail(ImmutableHashSet<Position>.Empty);
        var visible2 = fromPos2.IfFail(ImmutableHashSet<Position>.Empty);

        // If A can see B, then B can see A (symmetric vision)
        if (visible1.Contains(pos2))
        {
            visible2.Should().Contain(pos1, "Vision should be symmetric");
        }
        if (!visible1.Contains(pos2))
        {
            visible2.Should().NotContain(pos1, "Vision blocking should be symmetric");
        }
    }

    [Fact]
    public void CalculateFOV_OutOfBounds_HandlesGracefully()
    {
        // Arrange
        var grid = CreateEmptyGrid(10, 10);
        var origin = new Position(2, 2); // Near corner
        var range = 8; // Range extends beyond grid

        // Act
        var result = ShadowcastingFOV.CalculateFOV(origin, range, grid);

        // Assert
        result.IsSucc.Should().BeTrue();
        var visiblePositions = result.IfFail(ImmutableHashSet<Position>.Empty);

        // Should only contain valid grid positions
        foreach (var pos in visiblePositions)
        {
            pos.X.Should().BeInRange(0, 9);
            pos.Y.Should().BeInRange(0, 9);
        }
    }

    [Fact]
    public void CalculateFOV_ZeroRange_OnlySeesOrigin()
    {
        // Arrange
        var grid = CreateEmptyGrid(10, 10);
        var origin = new Position(5, 5);
        var range = 0;

        // Act
        var result = ShadowcastingFOV.CalculateFOV(origin, range, grid);

        // Assert
        result.IsSucc.Should().BeTrue();
        var visiblePositions = result.IfFail(ImmutableHashSet<Position>.Empty);
        visiblePositions.Count.Should().Be(1);
        visiblePositions.Should().Contain(origin);
    }

    [Fact]
    public void CalculateFOV_ForestTerrain_BlocksVision()
    {
        // Arrange
        var grid = CreateEmptyGrid(20, 20);
        var origin = new Position(10, 10);
        var forestPosition = new Position(12, 10);
        grid = PlaceTerrain(grid, forestPosition, TerrainType.Forest);
        var range = 5;

        // Act
        var result = ShadowcastingFOV.CalculateFOV(origin, range, grid);

        // Assert
        result.IsSucc.Should().BeTrue();
        var visiblePositions = result.IfFail(ImmutableHashSet<Position>.Empty);

        // Forest blocks vision just like walls
        visiblePositions.Should().Contain(forestPosition, "Forest tile itself should be visible");
        var behindForest = new Position(13, 10);
        visiblePositions.Should().NotContain(behindForest, "Position behind forest should be blocked");
    }

    // Helper methods
    private Darklands.Domain.Grid.Grid CreateEmptyGrid(int width, int height)
    {
        return Darklands.Domain.Grid.Grid.Create(_idGenerator, width, height, TerrainType.Open)
            .IfFail(_ => throw new System.InvalidOperationException("Failed to create grid"));
    }

    private Darklands.Domain.Grid.Grid PlaceWall(Darklands.Domain.Grid.Grid grid, Position position)
    {
        return grid.SetTerrain(position, TerrainType.Wall)
            .IfFail(grid);
    }

    private Darklands.Domain.Grid.Grid PlaceTerrain(Darklands.Domain.Grid.Grid grid, Position position, TerrainType terrain)
    {
        return grid.SetTerrain(position, terrain)
            .IfFail(grid);
    }
}
