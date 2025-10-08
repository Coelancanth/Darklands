using Darklands.Core.Features.WorldGen.Infrastructure.Algorithms;
using FluentAssertions;
using Xunit;

namespace Darklands.Core.Tests.Features.WorldGen.Infrastructure.Algorithms;

/// <summary>
/// Tests for RainShadowCalculator (VS_027 Phase 1).
/// Validates latitude-based orographic precipitation blocking.
/// </summary>
/// <remarks>
/// CRITICAL: Gradient blending creates calm belts (zero wind) at 30° and 60° latitude boundaries.
/// Tests use latitudes clearly INSIDE pure wind bands:
/// - **Trade Winds**: height=20, y=11 → 14°N (pure Trade Winds, windX=-1)
/// - **Westerlies**: height=5, y=3 → 45°N (pure Westerlies, windX=+1)
/// - **Polar Easterlies**: height=20, y=18 → 80.5°N (pure Polar Easterlies, windX=-1)
/// </remarks>
public class RainShadowCalculatorTests
{
    // ═══════════════════════════════════════════════════════════════════════
    // Basic Functionality - Single Mountain Blocking
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Calculate_SingleMountainUpwind_ShouldReduceLeesidePrecipitation()
    {
        // WHY: Rain shadow basics - single upwind mountain blocks 5% precipitation

        // Arrange: 5×5 map with mountain at x=1, test at Westerlies latitude (y=3 = 45°N)
        // Westerlies (eastward wind) → upwind is WEST (left side)
        int width = 5, height = 5;  // y=3: 3/4 = 0.75 → 45°N (pure Westerlies)
        float seaLevel = 1.0f, maxElevation = 10.0f;
        float threshold = (maxElevation - seaLevel) * 0.05f;  // 0.45

        var basePrecip = CreateUniformPrecipitation(width, height, 1.0f);
        var elevation = new float[height, width];

        // Fill all cells with sea level
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                elevation[y, x] = seaLevel;

        // Mountain at Westerlies row (y=3), column x=1
        elevation[3, 1] = seaLevel + threshold + 0.5f;

        // Act
        var result = RainShadowCalculator.Calculate(
            basePrecip, elevation, seaLevel, maxElevation, width, height);

        // Assert: x=3 is 2 cells downwind of mountain at x=1 → 5% reduction
        result.WithRainShadowMap[3, 3].Should().BeApproximately(0.95f, 0.001f,
            because: "Single upwind mountain (x=1) blocks 5% precipitation at leeward position (x=3)");

        // Sanity check: x=0 (upwind of mountain) should be unchanged
        result.WithRainShadowMap[3, 0].Should().Be(1.0f,
            because: "Upwind side of mountain has no rain shadow (no blocking)");
    }

    [Fact]
    public void Calculate_NoUpwindMountains_ShouldKeepOriginalPrecipitation()
    {
        // WHY: Flat terrain has no rain shadow effect

        // Arrange: Flat terrain (all cells at sea level)
        int width = 5, height = 3;
        float seaLevel = 1.0f, maxElevation = 2.0f;

        var basePrecip = CreateUniformPrecipitation(width, height, 0.8f);
        var elevation = CreateUniformElevation(width, height, seaLevel);

        // Act
        var result = RainShadowCalculator.Calculate(
            basePrecip, elevation, seaLevel, maxElevation, width, height);

        // Assert: All cells should retain original precipitation (no mountains to block)
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                result.WithRainShadowMap[y, x].Should().Be(0.8f,
                    because: $"Flat terrain at ({x},{y}) has no rain shadow effect");
            }
        }
    }

    [Fact]
    public void Calculate_ElevationBelowThreshold_ShouldNotBlock()
    {
        // WHY: Hills below threshold don't create rain shadow (only significant mountains)

        // Arrange: 5×5 map with small hill (below 5% threshold) at Westerlies latitude
        int width = 5, height = 5;
        float seaLevel = 1.0f, maxElevation = 10.0f;
        float threshold = (maxElevation - seaLevel) * 0.05f;  // 0.45

        var basePrecip = CreateUniformPrecipitation(width, height, 1.0f);
        var elevation = new float[height, width];

        // Fill all cells with sea level
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                elevation[y, x] = seaLevel;

        // Hill at Westerlies row (y=3 = 45°N), column x=1 (below threshold)
        elevation[3, 1] = seaLevel + threshold - 0.1f;

        // Act
        var result = RainShadowCalculator.Calculate(
            basePrecip, elevation, seaLevel, maxElevation, width, height);

        // Assert: No blocking (hill too small)
        result.WithRainShadowMap[3, 3].Should().Be(1.0f,
            because: "Hill at x=1 is below elevation threshold (0.35 < 0.45), doesn't block precipitation");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Multiple Mountains - Accumulative Blocking
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Calculate_MultipleUpwindMountains_ShouldAccumulateBlocking()
    {
        // WHY: Multiple mountain ranges stack blocking (Himalayas → Gobi effect)

        // Arrange: 5×10 map, 3 upwind mountains at Westerlies latitude (15% total blocking)
        int width = 10, height = 5;
        float seaLevel = 1.0f, maxElevation = 10.0f;
        float threshold = (maxElevation - seaLevel) * 0.05f;

        var basePrecip = CreateUniformPrecipitation(width, height, 1.0f);
        var elevation = new float[height, width];

        // Fill all cells with sea level
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                elevation[y, x] = seaLevel;

        // 3 mountains at Westerlies row (y=3 = 45°N), columns x=0, x=2, x=4
        elevation[3, 0] = seaLevel + threshold + 1;
        elevation[3, 2] = seaLevel + threshold + 1;
        elevation[3, 4] = seaLevel + threshold + 1;

        // Act
        var result = RainShadowCalculator.Calculate(
            basePrecip, elevation, seaLevel, maxElevation, width, height);

        // Assert: 3 mountains × 5% = 15% reduction → 85% rainfall remains at x=8
        result.WithRainShadowMap[3, 8].Should().BeApproximately(0.85f, 0.001f,
            because: "3 upwind mountains accumulate 15% blocking (5% each)");
    }

    [Fact]
    public void Calculate_MaximumBlocking_ShouldCapAt80Percent()
    {
        // WHY: Even worst deserts get occasional rainfall (cap at 80% reduction, min 20% rainfall)

        // Arrange: 5×25 map, 20 upwind mountains at Westerlies (would be 100% blocking if uncapped)
        // maxUpwindDistance=20, so test at x=20 to see all 20 mountains (x=0-19)
        int width = 25, height = 5;
        float seaLevel = 1.0f, maxElevation = 10.0f;
        float threshold = (maxElevation - seaLevel) * 0.05f;

        var basePrecip = CreateUniformPrecipitation(width, height, 1.0f);
        var elevation = new float[height, width];

        // Fill all cells with sea level initially
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                elevation[y, x] = seaLevel;

        // Fill first 20 cells of Westerlies row (y=3) with mountains (all above threshold)
        // Mountains at x=0-19 (20 total), test at x=20 (can see all 20 upwind)
        for (int x = 0; x < 20; x++)
        {
            elevation[3, x] = seaLevel + threshold + 1;
        }

        // Act
        var result = RainShadowCalculator.Calculate(
            basePrecip, elevation, seaLevel, maxElevation, width, height);

        // Assert: Capped at 80% reduction → 20% rainfall minimum
        // Test at x=20: Looking back westward, sees all 20 mountains at x=0-19
        // 20 mountains × 5% = 100% blocking → capped at 80% → 20% rainfall remains
        result.WithRainShadowMap[3, 20].Should().BeApproximately(0.20f, 0.001f,
            because: "Maximum blocking is capped at 80% reduction (20% minimum rainfall even in extreme deserts)");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Latitude-Based Wind Direction Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Calculate_TradeWindsLatitude_ShouldBlockFromEast()
    {
        // WHY: Trade Winds (westward) → deserts WEST of mountains (Sahara pattern)

        // Arrange: Mountain at x=3, test leeward (west) at x=1
        // At 14°N: Trade Winds blow westward (wind X=-1)
        // Upwind is EAST → mountain at x=3 blocks positions west of it (x=0,1,2)
        int width = 5, height = 20;  // y=11: 11/19 = 0.579 → 14°N (pure Trade Winds)
        float seaLevel = 1.0f, maxElevation = 10.0f;
        float threshold = (maxElevation - seaLevel) * 0.05f;

        var basePrecip = CreateUniformPrecipitation(width, height, 1.0f);
        var elevation = new float[height, width];

        // Set all to sea level
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                elevation[y, x] = seaLevel;

        // Mountain at Trade Winds row (y=11 = 14°N), column x=3
        elevation[11, 3] = seaLevel + threshold + 1;

        // Act
        var result = RainShadowCalculator.Calculate(
            basePrecip, elevation, seaLevel, maxElevation, width, height);

        // Assert: Leeward (west) side has rain shadow
        result.WithRainShadowMap[11, 1].Should().BeApproximately(0.95f, 0.001f,
            because: "Trade Winds (westward): Mountain at x=3 blocks precipitation at leeward x=1 (5% reduction)");

        // Windward (east) side unchanged
        result.WithRainShadowMap[11, 4].Should().Be(1.0f,
            because: "Windward side (east of mountain) has no rain shadow");
    }

    [Fact]
    public void Calculate_WesterliesLatitude_ShouldBlockFromWest()
    {
        // WHY: Westerlies (eastward) → deserts EAST of mountains (Gobi pattern)

        // Arrange: Mountain at x=1, test leeward (east) at x=3
        // At 45°N: Westerlies blow eastward (wind X=+1)
        // Upwind is WEST → mountain at x=1 blocks positions east of it (x=2,3,4)
        int width = 5, height = 5;  // y=3: 3/4 = 0.75 → 45°N (pure Westerlies)
        float seaLevel = 1.0f, maxElevation = 10.0f;
        float threshold = (maxElevation - seaLevel) * 0.05f;

        var basePrecip = CreateUniformPrecipitation(width, height, 1.0f);
        var elevation = new float[height, width];

        // Set all to sea level
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                elevation[y, x] = seaLevel;

        // Mountain at Westerlies row (y=3), column x=1
        elevation[3, 1] = seaLevel + threshold + 1;

        // Act
        var result = RainShadowCalculator.Calculate(
            basePrecip, elevation, seaLevel, maxElevation, width, height);

        // Assert: Leeward (east) side has rain shadow
        result.WithRainShadowMap[3, 3].Should().BeApproximately(0.95f, 0.001f,
            because: "Westerlies (eastward): Mountain at x=1 blocks precipitation at leeward x=3 (5% reduction)");

        // Windward (west) side unchanged
        result.WithRainShadowMap[3, 0].Should().Be(1.0f,
            because: "Windward side (west of mountain) has no rain shadow");
    }

    [Fact]
    public void Calculate_PolarEasterliesLatitude_ShouldBlockFromEast()
    {
        // WHY: Polar Easterlies (westward) → deserts WEST of mountains (Arctic pattern)

        // Arrange: Mountain at x=3, test leeward (west) at x=1
        // At 80.5°N: Polar Easterlies blow westward (wind X=-1)
        // Upwind is EAST → mountain at x=3 blocks positions west of it
        int width = 5, height = 20;  // y=18: 18/19 = 0.947 → 80.5°N (pure Polar Easterlies)
        float seaLevel = 1.0f, maxElevation = 10.0f;
        float threshold = (maxElevation - seaLevel) * 0.05f;

        var basePrecip = CreateUniformPrecipitation(width, height, 1.0f);
        var elevation = new float[height, width];

        // Set all to sea level
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                elevation[y, x] = seaLevel;

        // Mountain at Polar row (y=18 = 80.5°N), column x=3
        elevation[18, 3] = seaLevel + threshold + 1;

        // Act
        var result = RainShadowCalculator.Calculate(
            basePrecip, elevation, seaLevel, maxElevation, width, height);

        // Assert: Leeward (west) side has rain shadow
        result.WithRainShadowMap[18, 1].Should().BeApproximately(0.95f, 0.001f,
            because: "Polar Easterlies (westward): Mountain at x=3 blocks precipitation at leeward x=1");

        // Windward (east) side unchanged
        result.WithRainShadowMap[18, 4].Should().Be(1.0f,
            because: "Windward side (east of mountain) has no rain shadow");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Edge Cases & Boundary Conditions
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Calculate_MountainAtMapBoundary_ShouldHandleGracefully()
    {
        // WHY: Mountains at map edges shouldn't cause index out of bounds

        // Arrange: 5×5 map, mountain at western edge (x=0) at Westerlies latitude
        int width = 5, height = 5;
        float seaLevel = 1.0f, maxElevation = 10.0f;
        float threshold = (maxElevation - seaLevel) * 0.05f;

        var basePrecip = CreateUniformPrecipitation(width, height, 1.0f);
        var elevation = new float[height, width];

        // Fill all cells with sea level
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                elevation[y, x] = seaLevel;

        // Mountain at Westerlies row (y=3), western edge (x=0)
        elevation[3, 0] = seaLevel + threshold + 1;

        // Act: Should not throw exception
        var result = RainShadowCalculator.Calculate(
            basePrecip, elevation, seaLevel, maxElevation, width, height);

        // Assert: Algorithm completes without error
        result.WithRainShadowMap.Should().NotBeNull();

        // Mountain at edge blocks cells to the east (Westerlies eastward wind)
        result.WithRainShadowMap[3, 2].Should().BeApproximately(0.95f, 0.001f,
            because: "Mountain at x=0 blocks eastward positions (Westerlies pattern)");
    }

    [Fact]
    public void Calculate_VariedPrecipitationInput_ShouldApplyProportionalReduction()
    {
        // WHY: Rain shadow reduces precipitation proportionally (not by fixed amount)

        // Arrange: 5×5 map, varied base precipitation, test at Westerlies latitude
        int width = 5, height = 5;
        float seaLevel = 1.0f, maxElevation = 10.0f;
        float threshold = (maxElevation - seaLevel) * 0.05f;

        var basePrecip = new float[height, width];
        // Row 0-2: Low base precipitation (0.5)
        for (int y = 0; y < 3; y++)
            for (int x = 0; x < width; x++)
                basePrecip[y, x] = 0.5f;
        // Row 3 (Westerlies): High base precipitation (1.0)
        for (int x = 0; x < width; x++)
            basePrecip[3, x] = 1.0f;
        // Row 4: Medium base precipitation (0.7)
        for (int x = 0; x < width; x++)
            basePrecip[4, x] = 0.7f;

        var elevation = new float[height, width];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                elevation[y, x] = seaLevel;

        // Mountains at x=1 for all rows
        for (int y = 0; y < height; y++)
            elevation[y, 1] = seaLevel + threshold + 1;

        // Act
        var result = RainShadowCalculator.Calculate(
            basePrecip, elevation, seaLevel, maxElevation, width, height);

        // Assert: 5% reduction applies proportionally to base values (test Westerlies row only)
        result.WithRainShadowMap[3, 3].Should().BeApproximately(1.0f * 0.95f, 0.001f,
            because: "5% reduction on 1.0 base precipitation = 0.95 (Westerlies eastward wind)");
    }

    [Fact]
    public void Calculate_DynamicThreshold_ShouldAdaptToWorldTerrain()
    {
        // WHY: Flat vs mountainous worlds need different blocking thresholds

        // Arrange: 5×5 maps (Westerlies latitude), different max elevations
        int width = 5, height = 5;
        float seaLevel = 1.0f;

        var basePrecip = CreateUniformPrecipitation(width, height, 1.0f);

        // Flat world: maxElevation = 3.0 → threshold = (3-1)*0.05 = 0.1
        float flatMaxElev = 3.0f;
        float flatThreshold = (flatMaxElev - seaLevel) * 0.05f;  // 0.1
        var flatElevation = new float[height, width];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                flatElevation[y, x] = seaLevel;
        flatElevation[3, 1] = seaLevel + flatThreshold + 0.05f;  // Mountain at Westerlies row

        // Mountainous world: maxElevation = 15.0 → threshold = (15-1)*0.05 = 0.7
        float mountainousMaxElev = 15.0f;
        float mountainousThreshold = (mountainousMaxElev - seaLevel) * 0.05f;  // 0.7
        var mountainousElevation = new float[height, width];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                mountainousElevation[y, x] = seaLevel;
        mountainousElevation[3, 1] = seaLevel + 0.5f;  // Hill at Westerlies row (below threshold)

        // Act
        var flatResult = RainShadowCalculator.Calculate(
            basePrecip, flatElevation, seaLevel, flatMaxElev, width, height);

        var mountainousResult = RainShadowCalculator.Calculate(
            basePrecip, mountainousElevation, seaLevel, mountainousMaxElev, width, height);

        // Assert: Flat world blocks (0.15 > 0.1 threshold)
        flatResult.WithRainShadowMap[3, 3].Should().BeApproximately(0.95f, 0.001f,
            because: "In flat world, 0.15 elevation delta exceeds 0.1 threshold → blocks");

        // Mountainous world doesn't block (0.5 < 0.7 threshold)
        mountainousResult.WithRainShadowMap[3, 3].Should().Be(1.0f,
            because: "In mountainous world, 0.5 elevation delta is below 0.7 threshold → no blocking");
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
