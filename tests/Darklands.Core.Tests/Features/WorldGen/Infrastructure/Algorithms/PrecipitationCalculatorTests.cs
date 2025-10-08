using Darklands.Core.Features.WorldGen.Infrastructure.Algorithms;
using FluentAssertions;
using Xunit;

namespace Darklands.Core.Tests.Features.WorldGen.Infrastructure.Algorithms;

/// <summary>
/// Tests for PrecipitationCalculator (VS_026 Stage 3 - Base Precipitation).
/// Validates WorldEngine algorithm: noise + temperature gamma curve + renormalization.
/// </summary>
public class PrecipitationCalculatorTests
{
    // ═══════════════════════════════════════════════════════════════════════
    // Gamma Curve Edge Cases (Physics Validation)
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Calculate_ArcticTemperature_ShouldApplyMinimumPrecipitation()
    {
        // WHY: Arctic regions (t=0) should still get precipitation (20% of base)
        // Physics: Even cold air holds SOME moisture (snow in Arctic)

        // Arrange: Uniform temperature map (all Arctic, t=0)
        const int size = 32;
        var temperatureMap = new float[size, size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                temperatureMap[y, x] = 0.0f;  // Arctic

        // Act
        var result = PrecipitationCalculator.Calculate(temperatureMap, size, size, seed: 42);

        // Assert: Temperature shaping should apply curve=0.2 (20% of base noise)
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float baseNoise = result.NoiseOnlyMap[y, x];
                float tempShaped = result.TemperatureShapedMap[y, x];

                // Gamma curve at t=0: curve = 0² × 0.8 + 0.2 = 0.2
                // Expected: tempShaped ≈ baseNoise × 0.2
                tempShaped.Should().BeApproximately(baseNoise * 0.2f, 0.001f,
                    because: "Arctic (t=0) should get 20% of base precipitation (gamma curve minimum bonus)");
            }
        }
    }

    [Fact]
    public void Calculate_TropicalTemperature_ShouldApplyFullPrecipitation()
    {
        // WHY: Tropical regions (t=1) should get full precipitation (100% of base)
        // Physics: Hot air holds maximum moisture (monsoons in tropics)

        // Arrange: Uniform temperature map (all tropical, t=1)
        const int size = 32;
        var temperatureMap = new float[size, size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                temperatureMap[y, x] = 1.0f;  // Tropical

        // Act
        var result = PrecipitationCalculator.Calculate(temperatureMap, size, size, seed: 42);

        // Assert: Temperature shaping should apply curve=1.0 (100% of base noise)
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float baseNoise = result.NoiseOnlyMap[y, x];
                float tempShaped = result.TemperatureShapedMap[y, x];

                // Gamma curve at t=1: curve = 1² × 0.8 + 0.2 = 1.0
                // Expected: tempShaped ≈ baseNoise × 1.0
                tempShaped.Should().BeApproximately(baseNoise * 1.0f, 0.001f,
                    because: "Tropical (t=1) should get 100% of base precipitation (gamma curve maximum)");
            }
        }
    }

    [Fact]
    public void Calculate_TemperateTemperature_ShouldApplyMidRangePrecipitation()
    {
        // WHY: Mid-latitude regions (t=0.5) should get moderate precipitation
        // Physics: Quadratic curve → t=0.5 gives curve ≈ 0.4

        // Arrange: Uniform temperature map (all temperate, t=0.5)
        const int size = 32;
        var temperatureMap = new float[size, size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                temperatureMap[y, x] = 0.5f;  // Temperate

        // Act
        var result = PrecipitationCalculator.Calculate(temperatureMap, size, size, seed: 42);

        // Assert: Temperature shaping should apply curve ≈ 0.4
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float baseNoise = result.NoiseOnlyMap[y, x];
                float tempShaped = result.TemperatureShapedMap[y, x];

                // Gamma curve at t=0.5: curve = 0.5² × 0.8 + 0.2 = 0.25 × 0.8 + 0.2 = 0.4
                float expectedCurve = 0.4f;
                tempShaped.Should().BeApproximately(baseNoise * expectedCurve, 0.001f,
                    because: "Temperate (t=0.5) should get 40% of base precipitation (gamma curve mid-point)");
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Renormalization Validation
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Calculate_AfterRenormalization_ShouldFillFullRange()
    {
        // WHY: Renormalization should restore [0,1] dynamic range
        // Temperature shaping compresses values → renorm stretches back

        // Arrange: Gradient temperature (cold poles, hot equator)
        const int size = 64;
        var temperatureMap = new float[size, size];
        for (int y = 0; y < size; y++)
        {
            float t = (float)y / (size - 1);  // [0,1] gradient
            for (int x = 0; x < size; x++)
                temperatureMap[y, x] = t;
        }

        // Act
        var result = PrecipitationCalculator.Calculate(temperatureMap, size, size, seed: 42);

        // Assert: Final map should have values close to 0.0 and 1.0
        float min = float.MaxValue;
        float max = float.MinValue;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float value = result.FinalMap[y, x];
                if (value < min) min = value;
                if (value > max) max = value;
            }
        }

        min.Should().BeLessThan(0.1f, because: "Renormalization should produce values near 0");
        max.Should().BeGreaterThan(0.9f, because: "Renormalization should produce values near 1");
    }

    [Fact]
    public void Calculate_FinalMapRange_ShouldBe01()
    {
        // WHY: Final map must be in [0,1] for visualization

        // Arrange
        const int size = 64;
        var temperatureMap = CreateRandomTemperatureMap(size, seed: 42);

        // Act
        var result = PrecipitationCalculator.Calculate(temperatureMap, size, size, seed: 42);

        // Assert: All values in [0, 1]
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float value = result.FinalMap[y, x];
                value.Should().BeInRange(0.0f, 1.0f,
                    because: "Final precipitation values must be normalized to [0,1]");
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Temperature Correlation Validation
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Calculate_HotVsCold_ShouldProduceHigherPrecipitationInHotRegions()
    {
        // WHY: Validate temperature correlation - hot → wet, cold → dry

        // Arrange: Split temperature map (cold top half, hot bottom half)
        const int size = 64;
        var temperatureMap = new float[size, size];
        for (int y = 0; y < size; y++)
        {
            float t = y < size / 2 ? 0.1f : 0.9f;  // Cold top, hot bottom
            for (int x = 0; x < size; x++)
                temperatureMap[y, x] = t;
        }

        // Act
        var result = PrecipitationCalculator.Calculate(temperatureMap, size, size, seed: 42);

        // Assert: Average precipitation should be higher in hot regions (bottom half)
        float coldAvg = 0f, hotAvg = 0f;
        int coldCount = 0, hotCount = 0;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float precip = result.TemperatureShapedMap[y, x];
                if (y < size / 2)
                {
                    coldAvg += precip;
                    coldCount++;
                }
                else
                {
                    hotAvg += precip;
                    hotCount++;
                }
            }
        }

        coldAvg /= coldCount;
        hotAvg /= hotCount;

        hotAvg.Should().BeGreaterThan(coldAvg,
            because: "Hot regions (high evaporation) should have higher precipitation than cold regions");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Threshold Validation
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Calculate_Thresholds_ShouldBeOrdered()
    {
        // WHY: Quantile thresholds must be in ascending order

        // Arrange
        const int size = 64;
        var temperatureMap = CreateRandomTemperatureMap(size, seed: 42);

        // Act
        var result = PrecipitationCalculator.Calculate(temperatureMap, size, size, seed: 42);

        // Assert: Low < Medium < High
        result.Thresholds.LowThreshold.Should().BeLessThan(result.Thresholds.MediumThreshold,
            because: "30th percentile < 70th percentile");
        result.Thresholds.MediumThreshold.Should().BeLessThan(result.Thresholds.HighThreshold,
            because: "70th percentile < 95th percentile");
    }

    [Fact]
    public void Calculate_Thresholds_ShouldBeInRange01()
    {
        // WHY: Thresholds calculated from [0,1] normalized map

        // Arrange
        const int size = 64;
        var temperatureMap = CreateRandomTemperatureMap(size, seed: 42);

        // Act
        var result = PrecipitationCalculator.Calculate(temperatureMap, size, size, seed: 42);

        // Assert: All thresholds in [0, 1]
        result.Thresholds.LowThreshold.Should().BeInRange(0.0f, 1.0f);
        result.Thresholds.MediumThreshold.Should().BeInRange(0.0f, 1.0f);
        result.Thresholds.HighThreshold.Should().BeInRange(0.0f, 1.0f);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Noise Determinism
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Calculate_SameSeed_ShouldProduceSameOutput()
    {
        // WHY: Deterministic generation for testing/debugging

        // Arrange
        const int size = 32;
        const int seed = 123;
        var temperatureMap = CreateRandomTemperatureMap(size, seed: 42);

        // Act: Generate twice with same seed
        var result1 = PrecipitationCalculator.Calculate(temperatureMap, size, size, seed);
        var result2 = PrecipitationCalculator.Calculate(temperatureMap, size, size, seed);

        // Assert: All maps should be identical
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                result1.NoiseOnlyMap[y, x].Should().Be(result2.NoiseOnlyMap[y, x],
                    because: "Same seed should produce identical noise");
                result1.FinalMap[y, x].Should().Be(result2.FinalMap[y, x],
                    because: "Same seed should produce identical final precipitation");
            }
        }
    }

    [Fact]
    public void Calculate_DifferentSeeds_ShouldProduceDifferentOutput()
    {
        // WHY: Different seeds should generate varied precipitation patterns

        // Arrange
        const int size = 32;
        var temperatureMap = CreateRandomTemperatureMap(size, seed: 42);

        // Act: Generate with different seeds
        var result1 = PrecipitationCalculator.Calculate(temperatureMap, size, size, seed: 100);
        var result2 = PrecipitationCalculator.Calculate(temperatureMap, size, size, seed: 200);

        // Assert: Maps should differ (sample a few cells)
        bool foundDifference = false;
        for (int y = 0; y < size && !foundDifference; y++)
        {
            for (int x = 0; x < size && !foundDifference; x++)
            {
                if (result1.NoiseOnlyMap[y, x] != result2.NoiseOnlyMap[y, x])
                {
                    foundDifference = true;
                }
            }
        }

        foundDifference.Should().BeTrue(because: "Different seeds should produce different noise patterns");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Helper Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a random temperature map for testing (values in [0,1]).
    /// </summary>
    private static float[,] CreateRandomTemperatureMap(int size, int seed)
    {
        var rng = new System.Random(seed);
        var map = new float[size, size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                map[y, x] = (float)rng.NextDouble();  // [0,1]
            }
        }

        return map;
    }
}
