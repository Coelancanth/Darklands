using Darklands.Core.Features.WorldGen.Infrastructure.Algorithms;
using FluentAssertions;
using Xunit;

namespace Darklands.Core.Tests.Features.WorldGen.Infrastructure.Algorithms;

/// <summary>
/// Tests for FlowDirectionCalculator (VS_029 Phase 1 - Step 1b).
/// Validates steepest descent flow direction computation (8-connected).
/// </summary>
public class FlowDirectionCalculatorTests
{
    // ═══════════════════════════════════════════════════════════════════════
    // Basic Flow Direction Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Calculate_SimpleSlope_ShouldFlowDownhill()
    {
        // WHY: Cells should flow towards steepest downhill neighbor

        // Arrange: 3×3 heightmap with clear East slope (center column lower)
        var heightmap = new float[,]
        {
            { 3.0f, 2.5f, 2.0f },
            { 3.0f, 1.5f, 0.5f },  // Center clearly lowest to the East
            { 3.0f, 2.5f, 2.0f }
        };
        var oceanMask = new bool[3, 3];  // No ocean

        // Act
        var flowDirections = FlowDirectionCalculator.Calculate(heightmap, oceanMask);

        // Assert: Center cell (1,1) should flow East (dir=2) to (2,1)
        // Drop to E: 1.5 - 0.5 = 1.0 (steepest!)
        flowDirections[1, 1].Should().Be(2, because: "Cell flows East to lowest neighbor (clear steepest)");

        // Left-center should also flow East
        flowDirections[1, 0].Should().Be(2, because: "Left column flows East downhill");
    }

    [Fact]
    public void Calculate_DiagonalSlope_ShouldFlowDiagonally()
    {
        // WHY: Cells should flow diagonally if that's steepest descent

        // Arrange: 3×3 heightmap with diagonal slope (NW high → SE low)
        var heightmap = new float[,]
        {
            { 5.0f, 4.0f, 3.0f },
            { 4.0f, 3.0f, 2.0f },
            { 3.0f, 2.0f, 1.0f }
        };
        var oceanMask = new bool[3, 3];

        // Act
        var flowDirections = FlowDirectionCalculator.Calculate(heightmap, oceanMask);

        // Assert: Center cell (1,1) should flow South-East (dir=3) to (2,2)
        flowDirections[1, 1].Should().Be(3, because: "Cell flows SE to lowest neighbor");

        // Top-left cell should flow SE (steepest diagonal)
        flowDirections[0, 0].Should().Be(3, because: "NW corner flows SE downhill");
    }

    [Fact]
    public void Calculate_OceanCell_ShouldBeSink()
    {
        // WHY: Ocean cells are terminal sinks (no downstream flow)

        // Arrange: 3×3 map with ocean at center
        var heightmap = CreateUniformHeightmap(3, 3, 2.0f);
        var oceanMask = new bool[,]
        {
            { false, false, false },
            { false, true,  false },  // Center is ocean
            { false, false, false }
        };

        // Act
        var flowDirections = FlowDirectionCalculator.Calculate(heightmap, oceanMask);

        // Assert: Ocean cell should be sink (-1)
        flowDirections[1, 1].Should().Be(-1, because: "Ocean cells are terminal sinks");
    }

    [Fact]
    public void Calculate_LocalMinimum_ShouldBeSink()
    {
        // WHY: Cells with no lower neighbors are sinks (pits after pit filling)

        // Arrange: 3×3 pit (center lower than all neighbors)
        var heightmap = new float[,]
        {
            { 3.0f, 3.0f, 3.0f },
            { 3.0f, 1.0f, 3.0f },  // Center is pit
            { 3.0f, 3.0f, 3.0f }
        };
        var oceanMask = new bool[3, 3];

        // Act
        var flowDirections = FlowDirectionCalculator.Calculate(heightmap, oceanMask);

        // Assert: Pit center should be sink (-1) - no lower neighbor
        flowDirections[1, 1].Should().Be(-1, because: "Local minimum has no lower neighbor (sink)");

        // Surrounding cells should flow towards center (pit)
        flowDirections[0, 0].Should().Be(3, because: "NW cell flows SE towards pit");
        flowDirections[1, 0].Should().Be(2, because: "W cell flows E towards pit");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Direction Encoding Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Calculate_AllEightDirections_ShouldEncodeCorrectly()
    {
        // WHY: Verify all 8 compass directions encoded properly (0-7)

        // Arrange: 3×3 heightmap with center higher than all neighbors
        // Each neighbor is progressively lower in different direction
        var heightmap = new float[,]
        {
            { 7.0f, 0.0f, 1.0f },  // N=0 (top-center lowest)
            { 6.0f, 9.0f, 2.0f },  // Center highest
            { 5.0f, 4.0f, 3.0f }
        };
        var oceanMask = new bool[3, 3];

        // Act
        var flowDirections = FlowDirectionCalculator.Calculate(heightmap, oceanMask);

        // Assert: Center should flow North (dir=0) to (1,0) - steepest drop
        flowDirections[1, 1].Should().Be(0, because: "Center flows North to (1,0) with drop of 9.0");
    }

    [Fact]
    public void GetDirectionOffset_AllDirections_ShouldReturnCorrectOffsets()
    {
        // WHY: Verify direction encoding maps to correct (dx, dy) offsets

        // Assert: Check all 8 cardinal/diagonal directions
        FlowDirectionCalculator.GetDirectionOffset(0).Should().Be((0, -1), because: "0 = North");
        FlowDirectionCalculator.GetDirectionOffset(1).Should().Be((1, -1), because: "1 = North-East");
        FlowDirectionCalculator.GetDirectionOffset(2).Should().Be((1, 0), because: "2 = East");
        FlowDirectionCalculator.GetDirectionOffset(3).Should().Be((1, 1), because: "3 = South-East");
        FlowDirectionCalculator.GetDirectionOffset(4).Should().Be((0, 1), because: "4 = South");
        FlowDirectionCalculator.GetDirectionOffset(5).Should().Be((-1, 1), because: "5 = South-West");
        FlowDirectionCalculator.GetDirectionOffset(6).Should().Be((-1, 0), because: "6 = West");
        FlowDirectionCalculator.GetDirectionOffset(7).Should().Be((-1, -1), because: "7 = North-West");

        // Sink has no offset
        FlowDirectionCalculator.GetDirectionOffset(-1).Should().Be((0, 0), because: "Sink has no offset");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Edge Cases
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Calculate_FlatTerrain_ShouldAllBeSinks()
    {
        // WHY: Flat regions have no flow direction (all cells are sinks)

        // Arrange: 5×5 uniform elevation
        var heightmap = CreateUniformHeightmap(5, 5, 2.0f);
        var oceanMask = new bool[5, 5];

        // Act
        var flowDirections = FlowDirectionCalculator.Calculate(heightmap, oceanMask);

        // Assert: All cells should be sinks (no elevation gradient)
        for (int y = 0; y < 5; y++)
        {
            for (int x = 0; x < 5; x++)
            {
                flowDirections[y, x].Should().Be(-1,
                    because: $"Flat terrain cell ({x},{y}) has no lower neighbor");
            }
        }
    }

    [Fact]
    public void Calculate_BorderCells_ShouldHandleBoundsCorrectly()
    {
        // WHY: Border cells should only check valid neighbors (no out-of-bounds)

        // Arrange: 3×3 slope towards bottom-right corner
        var heightmap = new float[,]
        {
            { 5.0f, 4.0f, 3.0f },
            { 4.0f, 3.0f, 2.0f },
            { 3.0f, 2.0f, 1.0f }
        };
        var oceanMask = new bool[3, 3];

        // Act
        var flowDirections = FlowDirectionCalculator.Calculate(heightmap, oceanMask);

        // Assert: Bottom-right corner should be sink (no lower neighbor exists)
        flowDirections[2, 2].Should().Be(-1, because: "Bottom-right corner is lowest (sink)");

        // Top-left should flow SE towards lowest point
        flowDirections[0, 0].Should().Be(3, because: "Top-left flows SE downhill");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Helper Methods
    // ═══════════════════════════════════════════════════════════════════════

    private static float[,] CreateUniformHeightmap(int width, int height, float value)
    {
        var heightmap = new float[height, width];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                heightmap[y, x] = value;
        return heightmap;
    }
}
