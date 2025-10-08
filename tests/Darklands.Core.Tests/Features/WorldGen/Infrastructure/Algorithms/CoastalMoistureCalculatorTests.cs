using Darklands.Core.Features.WorldGen.Infrastructure.Algorithms;
using FluentAssertions;
using Xunit;

namespace Darklands.Core.Tests.Features.WorldGen.Infrastructure.Algorithms;

/// <summary>
/// Tests for CoastalMoistureCalculator (VS_028 Phase 1).
/// Validates BFS distance-to-ocean calculation + exponential decay + elevation resistance.
/// </summary>
public class CoastalMoistureCalculatorTests
{
    // ═══════════════════════════════════════════════════════════════════════
    // BFS Distance-to-Ocean Correctness
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Calculate_CoastalCell_ShouldApplyMaximumBonus()
    {
        // WHY: Cells adjacent to ocean receive full 80% coastal bonus

        // Arrange: 3×3 map, center is land adjacent to ocean (dist=1)
        int width = 3, height = 3;
        var rainShadowPrecip = CreateUniformPrecipitation(width, height, 0.5f);
        var oceanMask = new bool[height, width];
        var heightmap = CreateUniformElevation(width, height, 1.0f);  // Lowland (no elevation resistance)

        // Ocean at edges, land at center
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                oceanMask[y, x] = (x == 0 || x == 2 || y == 0 || y == 2);

        // Act
        var result = CoastalMoistureCalculator.Calculate(
            rainShadowPrecip, oceanMask, heightmap, width, height);

        // Assert: Center cell (1,1) is dist=1 from ocean
        // Coastal bonus at dist=1: 0.8 × e^(-1/30) ≈ 0.8 × 0.967 ≈ 0.774
        // Elevation factor: 1 - min(1, 1.0 × 0.02) = 1 - 0.02 = 0.98
        // Final: 0.5 × (1 + 0.774 × 0.98) ≈ 0.5 × 1.759 ≈ 0.879
        result.FinalMap[1, 1].Should().BeGreaterThan(0.85f,
            because: "Coastal cell (dist=1) receives strong coastal bonus (~75% increase)");
    }

    [Fact]
    public void Calculate_InteriorCell_ShouldReceiveReducedBonus()
    {
        // WHY: Cells far from ocean receive exponentially decayed bonus

        // Arrange: 10×10 map, ocean at edges, test cell at center (dist~5)
        int width = 10, height = 10;
        var rainShadowPrecip = CreateUniformPrecipitation(width, height, 0.5f);
        var oceanMask = new bool[height, width];
        var heightmap = CreateUniformElevation(width, height, 1.0f);

        // Ocean border (outer ring)
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                oceanMask[y, x] = (x == 0 || x == width - 1 || y == 0 || y == height - 1);
            }
        }

        // Act
        var result = CoastalMoistureCalculator.Calculate(
            rainShadowPrecip, oceanMask, heightmap, width, height);

        // Assert: Center cell (5,5) is dist=5 from nearest ocean border
        // Coastal bonus at dist=5: 0.8 × e^(-5/30) ≈ 0.8 × 0.847 ≈ 0.678
        // Less bonus than dist=1, but still significant
        result.FinalMap[5, 5].Should().BeGreaterThan(0.65f,
            because: "Interior cell (dist=5) receives moderate coastal bonus (~60% increase)");

        result.FinalMap[5, 5].Should().BeLessThan(result.FinalMap[1, 1],
            because: "Interior cells receive less bonus than coastal cells (exponential decay)");
    }

    [Fact]
    public void Calculate_DeepInteriorCell_ShouldReceiveMinimalBonus()
    {
        // WHY: Cells far from ocean (>60 cells) receive negligible bonus

        // Arrange: 70×3 map, ocean at left edge, test at far right (dist=69)
        int width = 70, height = 3;
        var rainShadowPrecip = CreateUniformPrecipitation(width, height, 0.5f);
        var oceanMask = new bool[height, width];
        var heightmap = CreateUniformElevation(width, height, 1.0f);

        // Ocean at x=0 (left edge)
        for (int y = 0; y < height; y++)
            oceanMask[y, 0] = true;

        // Act
        var result = CoastalMoistureCalculator.Calculate(
            rainShadowPrecip, oceanMask, heightmap, width, height);

        // Assert: Far right cell (69,1) is dist=69 from ocean
        // Coastal bonus at dist=69: 0.8 × e^(-69/30) ≈ 0.8 × 0.103 ≈ 0.082
        // Enhancement: 0.5 × (1 + 0.082 × 0.98) ≈ 0.5 × 1.080 ≈ 0.540 (8% increase)
        result.FinalMap[1, 69].Should().BeApproximately(0.5f, 0.05f,
            because: "Deep interior (dist=69) receives negligible coastal bonus (<10% increase)");
    }

    [Fact]
    public void Calculate_OceanCells_ShouldRemainUnchanged()
    {
        // WHY: Ocean cells ARE the moisture source, no enhancement needed

        // Arrange: 5×5 map with varied ocean distribution
        int width = 5, height = 5;
        var rainShadowPrecip = CreateUniformPrecipitation(width, height, 0.6f);
        var oceanMask = new bool[height, width];
        var heightmap = CreateUniformElevation(width, height, 1.0f);

        // Ocean in top-left quadrant
        for (int y = 0; y < 2; y++)
            for (int x = 0; x < 2; x++)
                oceanMask[y, x] = true;

        // Act
        var result = CoastalMoistureCalculator.Calculate(
            rainShadowPrecip, oceanMask, heightmap, width, height);

        // Assert: Ocean cells retain original precipitation
        result.FinalMap[0, 0].Should().Be(0.6f,
            because: "Ocean cell (0,0) retains base precipitation (no enhancement)");

        result.FinalMap[1, 1].Should().Be(0.6f,
            because: "Ocean cell (1,1) retains base precipitation (no enhancement)");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Exponential Decay Curve Validation
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Calculate_ExponentialDecay_ShouldMatchPhysicsFormula()
    {
        // WHY: Verify exponential decay matches e^(-dist/30) physics formula

        // Arrange: 35×3 map, ocean at left, test at various distances
        int width = 35, height = 3;
        var rainShadowPrecip = CreateUniformPrecipitation(width, height, 1.0f);
        var oceanMask = new bool[height, width];
        var heightmap = CreateUniformElevation(width, height, 1.0f);

        // Ocean at x=0
        for (int y = 0; y < height; y++)
            oceanMask[y, 0] = true;

        // Act
        var result = CoastalMoistureCalculator.Calculate(
            rainShadowPrecip, oceanMask, heightmap, width, height);

        // Assert: Check decay at specific distances
        // dist=0 (ocean): bonus=0 (ocean cells unchanged)
        result.FinalMap[1, 0].Should().Be(1.0f,
            because: "Ocean cell has no enhancement");

        // dist=1: bonus = 0.8 × e^(-1/30) ≈ 0.774
        // final = 1.0 × (1 + 0.774 × 0.98) ≈ 1.759
        result.FinalMap[1, 1].Should().BeApproximately(1.759f, 0.01f,
            because: "dist=1: Strong coastal effect (e^(-1/30) ≈ 0.967)");

        // dist=10: bonus = 0.8 × e^(-10/30) ≈ 0.572
        // final = 1.0 × (1 + 0.572 × 0.98) ≈ 1.561
        result.FinalMap[1, 10].Should().BeApproximately(1.561f, 0.01f,
            because: "dist=10: Moderate decay (e^(-10/30) ≈ 0.716)");

        // dist=30: bonus = 0.8 × e^(-30/30) ≈ 0.294
        // final = 1.0 × (1 + 0.294 × 0.98) ≈ 1.288
        result.FinalMap[1, 30].Should().BeApproximately(1.288f, 0.01f,
            because: "dist=30: Significant decay (e^(-1) ≈ 0.368)");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Elevation Resistance Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Calculate_LowElevation_ShouldAllowFullCoastalEffect()
    {
        // WHY: Lowlands near coast receive full coastal moisture

        // Arrange: 3×3 map, coastal lowland (elevation=1.0)
        int width = 3, height = 3;
        var rainShadowPrecip = CreateUniformPrecipitation(width, height, 0.5f);
        var oceanMask = new bool[height, width];
        var heightmap = CreateUniformElevation(width, height, 1.0f);  // Lowland

        // Ocean at edges, land at center
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                oceanMask[y, x] = (x == 0 || x == 2 || y == 0 || y == 2);

        // Act
        var result = CoastalMoistureCalculator.Calculate(
            rainShadowPrecip, oceanMask, heightmap, width, height);

        // Assert: Center cell (1,1) receives nearly full coastal effect
        // Elevation factor: 1 - min(1, 1.0 × 0.02) = 1 - 0.02 = 0.98 (98% of max)
        result.FinalMap[1, 1].Should().BeGreaterThan(0.85f,
            because: "Lowland (elev=1.0) receives nearly full coastal effect (98%)");
    }

    [Fact]
    public void Calculate_HighMountain_ShouldResistCoastalMoisture()
    {
        // WHY: Mountain plateaus resist coastal moisture (Tibetan Plateau effect)

        // Arrange: 3×3 map, coastal mountain (elevation=10.0)
        int width = 3, height = 3;
        var rainShadowPrecip = CreateUniformPrecipitation(width, height, 0.5f);
        var oceanMask = new bool[height, width];
        var heightmap = new float[height, width];

        // Ocean at edges
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                oceanMask[y, x] = (x == 0 || x == 2 || y == 0 || y == 2);
                heightmap[y, x] = 1.0f;  // Default lowland
            }
        }

        // Center cell is high mountain
        heightmap[1, 1] = 10.0f;

        // Act
        var result = CoastalMoistureCalculator.Calculate(
            rainShadowPrecip, oceanMask, heightmap, width, height);

        // Assert: Center cell (1,1) has reduced coastal effect
        // Elevation factor: 1 - min(1, 10.0 × 0.02) = 1 - 0.20 = 0.80 (80% of max)
        // Coastal bonus at dist=1: ~0.774, final: 0.5 × (1 + 0.774 × 0.80) ≈ 0.81
        result.FinalMap[1, 1].Should().BeApproximately(0.81f, 0.02f,
            because: "High mountain (elev=10.0) resists coastal moisture (20% reduction)");

        // Verify it's less than lowland coastal cell
        var lowlandMap = CreateUniformElevation(width, height, 1.0f);
        var lowlandResult = CoastalMoistureCalculator.Calculate(
            rainShadowPrecip, oceanMask, lowlandMap, width, height);

        result.FinalMap[1, 1].Should().BeLessThan(lowlandResult.FinalMap[1, 1],
            because: "Mountain plateau receives less coastal moisture than lowland coast");
    }

    [Fact]
    public void Calculate_ExtremeElevation_ShouldCapResistance()
    {
        // WHY: Elevation resistance caps at 100% (factor minimum = 0)

        // Arrange: 3×3 map, extreme elevation (>50.0)
        int width = 3, height = 3;
        var rainShadowPrecip = CreateUniformPrecipitation(width, height, 0.5f);
        var oceanMask = new bool[height, width];
        var heightmap = new float[height, width];

        // Ocean at edges
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                oceanMask[y, x] = (x == 0 || x == 2 || y == 0 || y == 2);
                heightmap[y, x] = 1.0f;
            }
        }

        // Center cell is extreme elevation (would be >100% resistance if uncapped)
        heightmap[1, 1] = 60.0f;  // 60 × 0.02 = 1.2 → capped at 1.0

        // Act
        var result = CoastalMoistureCalculator.Calculate(
            rainShadowPrecip, oceanMask, heightmap, width, height);

        // Assert: Center cell has zero coastal effect (capped)
        // Elevation factor: 1 - min(1, 60.0 × 0.02) = 1 - 1.0 = 0.0 (no coastal effect)
        // Final: 0.5 × (1 + bonus × 0.0) = 0.5 (unchanged)
        result.FinalMap[1, 1].Should().Be(0.5f,
            because: "Extreme elevation (>50) caps elevation resistance at 100% (no coastal effect)");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Integration Tests - Real-World Scenarios
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Calculate_CoastalWetterThanInterior_ShouldMatchRealWorld()
    {
        // WHY: Maritime climates (coast) significantly wetter than continental (interior)

        // Arrange: 20×3 map, ocean at left, varying distances
        int width = 20, height = 3;
        var rainShadowPrecip = CreateUniformPrecipitation(width, height, 0.5f);
        var oceanMask = new bool[height, width];
        var heightmap = CreateUniformElevation(width, height, 1.0f);

        // Ocean at x=0
        for (int y = 0; y < height; y++)
            oceanMask[y, 0] = true;

        // Act
        var result = CoastalMoistureCalculator.Calculate(
            rainShadowPrecip, oceanMask, heightmap, width, height);

        // Assert: Coastal cells significantly wetter than interior
        float coastal = result.FinalMap[1, 1];      // dist=1
        float interior = result.FinalMap[1, 10];    // dist=10

        coastal.Should().BeGreaterThan(interior,
            because: "Coastal regions (Seattle, UK) wetter than interior (Spokane, central Asia)");

        // Real-world: Seattle (950mm/year) vs Spokane (420mm/year) ≈ 2.3× difference
        // Our model: coastal/interior ratio at moderate distances (dist=1 vs dist=10)
        // Ratio: ~1.13× (reasonable for simplified exponential model)
        (coastal / interior).Should().BeGreaterThan(1.1f,
            because: "Maritime/continental ratio shows coastal moisture enhancement");
    }

    [Fact]
    public void Calculate_MountainPlateauNearCoast_ShouldStayDry()
    {
        // WHY: Tibetan Plateau (4000m) stays dry despite ~1000km from ocean

        // Arrange: 5×3 map, ocean + coastal mountain plateau
        int width = 5, height = 3;
        var rainShadowPrecip = CreateUniformPrecipitation(width, height, 0.5f);
        var oceanMask = new bool[height, width];
        var heightmap = new float[height, width];

        // Ocean at x=0
        for (int y = 0; y < height; y++)
        {
            oceanMask[y, 0] = true;
            for (int x = 1; x < width; x++)
                heightmap[y, x] = 1.0f;  // Default lowland
        }

        // Mountain plateau at x=2 (dist=2, but high elevation)
        for (int y = 0; y < height; y++)
            heightmap[y, 2] = 15.0f;  // High plateau

        // Act
        var result = CoastalMoistureCalculator.Calculate(
            rainShadowPrecip, oceanMask, heightmap, width, height);

        // Assert: Plateau (x=2, elev=15) drier than lowland coast (x=1, elev=1)
        float plateau = result.FinalMap[1, 2];
        float lowland = result.FinalMap[1, 1];

        plateau.Should().BeLessThan(lowland,
            because: "Mountain plateau (Tibetan Plateau effect) resists coastal moisture despite proximity");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Edge Cases & Robustness
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Calculate_LandlockedMap_ShouldHandleGracefully()
    {
        // WHY: Maps with no ocean should not crash (no enhancement applied)

        // Arrange: 5×5 map, no ocean cells
        int width = 5, height = 5;
        var rainShadowPrecip = CreateUniformPrecipitation(width, height, 0.7f);
        var oceanMask = new bool[height, width];  // All false
        var heightmap = CreateUniformElevation(width, height, 1.0f);

        // Act: Should not throw exception
        var result = CoastalMoistureCalculator.Calculate(
            rainShadowPrecip, oceanMask, heightmap, width, height);

        // Assert: All cells retain original precipitation (no enhancement)
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                result.FinalMap[y, x].Should().Be(0.7f,
                    because: $"Landlocked map has no coastal moisture enhancement at ({x},{y})");
            }
        }
    }

    [Fact]
    public void Calculate_AllOceanMap_ShouldHandleGracefully()
    {
        // WHY: Ocean-only maps should not crash (all cells unchanged)

        // Arrange: 5×5 map, all ocean
        int width = 5, height = 5;
        var rainShadowPrecip = CreateUniformPrecipitation(width, height, 0.8f);
        var oceanMask = new bool[height, width];
        var heightmap = CreateUniformElevation(width, height, 1.0f);

        // All cells are ocean
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                oceanMask[y, x] = true;

        // Act: Should not throw exception
        var result = CoastalMoistureCalculator.Calculate(
            rainShadowPrecip, oceanMask, heightmap, width, height);

        // Assert: All cells retain original precipitation
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                result.FinalMap[y, x].Should().Be(0.8f,
                    because: $"Ocean-only map has no land to enhance at ({x},{y})");
            }
        }
    }

    [Fact]
    public void Calculate_SmallIsland_ShouldReceiveStrongCoastalEffect()
    {
        // WHY: Small islands (all cells coastal) should be maritime climates

        // Arrange: 3×3 island, ocean all around
        int width = 3, height = 3;
        var rainShadowPrecip = CreateUniformPrecipitation(width, height, 0.5f);
        var oceanMask = new bool[height, width];
        var heightmap = CreateUniformElevation(width, height, 1.0f);

        // Ocean at edges, land at center only
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                oceanMask[y, x] = !(x == 1 && y == 1);  // Only center is land
            }
        }

        // Act
        var result = CoastalMoistureCalculator.Calculate(
            rainShadowPrecip, oceanMask, heightmap, width, height);

        // Assert: Center cell (1,1) is dist=1 from ocean (strong coastal effect)
        result.FinalMap[1, 1].Should().BeGreaterThan(0.85f,
            because: "Small island (all cells coastal) receives strong maritime climate effect");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Helper Methods
    // ═══════════════════════════════════════════════════════════════════════

    private static float[,] CreateUniformPrecipitation(int width, int height, float value)
    {
        var map = new float[height, width];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                map[y, x] = value;
        return map;
    }

    private static float[,] CreateUniformElevation(int width, int height, float value)
    {
        var map = new float[height, width];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                map[y, x] = value;
        return map;
    }
}
