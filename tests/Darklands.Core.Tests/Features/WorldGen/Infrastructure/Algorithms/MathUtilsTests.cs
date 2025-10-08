using System;
using System.Linq;
using Darklands.Core.Features.WorldGen.Infrastructure.Algorithms;
using FluentAssertions;
using Xunit;

namespace Darklands.Core.Tests.Features.WorldGen.Infrastructure.Algorithms;

/// <summary>
/// Tests for MathUtils mathematical helper functions.
/// VS_025: Validates Interp() and SampleGaussian() for temperature simulation.
/// </summary>
public class MathUtilsTests
{
    // ═══════════════════════════════════════════════════════════════════════
    // Interp() Tests - Piecewise Linear Interpolation
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Interp_InteriorPoint_ShouldLinearlyInterpolate()
    {
        // Arrange: Simple linear mapping [0, 1] → [0, 10]
        float[] xp = { 0.0f, 1.0f };
        float[] fp = { 0.0f, 10.0f };

        // Act: Midpoint should map to middle
        float result = MathUtils.Interp(0.5f, xp, fp);

        // Assert
        result.Should().BeApproximately(5.0f, 0.001f);
    }

    [Fact]
    public void Interp_MultipleSegments_ShouldInterpolateCorrectly()
    {
        // Arrange: Piecewise function (like latitude temperature)
        float[] xp = { -0.5f, 0.0f, 0.5f };  // Cold pole, equator, cold pole
        float[] fp = { 0.0f, 1.0f, 0.0f };    // Temperature factor

        // Act & Assert: Test multiple points
        MathUtils.Interp(-0.5f, xp, fp).Should().BeApproximately(0.0f, 0.001f);  // Pole
        MathUtils.Interp(0.0f, xp, fp).Should().BeApproximately(1.0f, 0.001f);   // Equator
        MathUtils.Interp(0.5f, xp, fp).Should().BeApproximately(0.0f, 0.001f);   // Other pole
        MathUtils.Interp(-0.25f, xp, fp).Should().BeApproximately(0.5f, 0.001f); // Halfway to equator
        MathUtils.Interp(0.25f, xp, fp).Should().BeApproximately(0.5f, 0.001f);  // Halfway from equator
    }

    [Fact]
    public void Interp_BelowRange_ShouldUseLeftBoundary()
    {
        // Arrange
        float[] xp = { 0.0f, 1.0f };
        float[] fp = { 10.0f, 20.0f };

        // Act: Below xp[0]
        float result = MathUtils.Interp(-0.5f, xp, fp);

        // Assert: Should return fp[0] by default
        result.Should().BeApproximately(10.0f, 0.001f);
    }

    [Fact]
    public void Interp_AboveRange_ShouldUseRightBoundary()
    {
        // Arrange
        float[] xp = { 0.0f, 1.0f };
        float[] fp = { 10.0f, 20.0f };

        // Act: Above xp[last]
        float result = MathUtils.Interp(2.0f, xp, fp);

        // Assert: Should return fp[last] by default
        result.Should().BeApproximately(20.0f, 0.001f);
    }

    [Fact]
    public void Interp_CustomLeftParameter_ShouldUseCustomValue()
    {
        // Arrange
        float[] xp = { 0.0f, 1.0f };
        float[] fp = { 10.0f, 20.0f };

        // Act: Below range with custom 'left' parameter
        float result = MathUtils.Interp(-0.5f, xp, fp, left: 0.0f);

        // Assert: Should use custom left value
        result.Should().BeApproximately(0.0f, 0.001f);
    }

    [Fact]
    public void Interp_CustomRightParameter_ShouldUseCustomValue()
    {
        // Arrange
        float[] xp = { 0.0f, 1.0f };
        float[] fp = { 10.0f, 20.0f };

        // Act: Above range with custom 'right' parameter
        float result = MathUtils.Interp(2.0f, xp, fp, right: 0.0f);

        // Assert: Should use custom right value
        result.Should().BeApproximately(0.0f, 0.001f);
    }

    [Fact]
    public void Interp_MismatchedArrays_ShouldThrow()
    {
        // Arrange
        float[] xp = { 0.0f, 1.0f };
        float[] fp = { 0.0f, 10.0f, 20.0f };  // Mismatched length!

        // Act & Assert
        Action act = () => MathUtils.Interp(0.5f, xp, fp);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*same length*");
    }

    [Fact]
    public void Interp_EmptyArrays_ShouldThrow()
    {
        // Arrange
        float[] xp = Array.Empty<float>();
        float[] fp = Array.Empty<float>();

        // Act & Assert
        Action act = () => MathUtils.Interp(0.5f, xp, fp);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*must not be empty*");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SampleGaussian() Tests - Box-Muller Transform
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void SampleGaussian_DeterministicSeed_ShouldBeReproducible()
    {
        // Arrange: Same seed should produce same results
        var rng1 = new Random(42);
        var rng2 = new Random(42);

        // Act
        float sample1 = MathUtils.SampleGaussian(rng1, mean: 1.0f, hwhm: 0.12f);
        float sample2 = MathUtils.SampleGaussian(rng2, mean: 1.0f, hwhm: 0.12f);

        // Assert: Should be identical (deterministic)
        sample1.Should().Be(sample2);
    }

    [Fact]
    public void SampleGaussian_MeanZero_ShouldCenterAroundZero()
    {
        // Arrange
        var rng = new Random(42);
        int sampleCount = 10000;

        // Act: Generate many samples
        var samples = Enumerable.Range(0, sampleCount)
            .Select(_ => MathUtils.SampleGaussian(rng, mean: 0.0f, hwhm: 0.1f))
            .ToList();

        // Assert: Mean should be close to 0 (law of large numbers)
        float observedMean = samples.Average();
        observedMean.Should().BeApproximately(0.0f, 0.02f);  // Within 2% tolerance
    }

    [Fact]
    public void SampleGaussian_MeanOne_ShouldCenterAroundOne()
    {
        // Arrange
        var rng = new Random(42);
        int sampleCount = 10000;

        // Act: Generate many samples
        var samples = Enumerable.Range(0, sampleCount)
            .Select(_ => MathUtils.SampleGaussian(rng, mean: 1.0f, hwhm: 0.1f))
            .ToList();

        // Assert: Mean should be close to 1.0
        float observedMean = samples.Average();
        observedMean.Should().BeApproximately(1.0f, 0.02f);
    }

    [Fact]
    public void SampleGaussian_HWHM_ShouldMatchExpectedSpread()
    {
        // Arrange: WorldEngine uses HWHM = 0.12 for distance-to-sun
        // Most samples should be within ~1.82 * HWHM of mean (see WorldEngine comments)
        var rng = new Random(42);
        int sampleCount = 10000;
        float mean = 1.0f;
        float hwhm = 0.12f;

        // Act
        var samples = Enumerable.Range(0, sampleCount)
            .Select(_ => MathUtils.SampleGaussian(rng, mean, hwhm))
            .ToList();

        // Assert: ~68% of samples should be within ±1 HWHM of mean (rough approximation)
        float lower = mean - hwhm;
        float upper = mean + hwhm;
        int withinOneHWHM = samples.Count(s => s >= lower && s <= upper);
        float percentage = withinOneHWHM / (float)sampleCount;

        percentage.Should().BeGreaterThan(0.5f);  // At least 50% within ±1 HWHM
        percentage.Should().BeLessThan(0.8f);     // Not all samples (some spread)
    }

    [Fact]
    public void SampleGaussian_RealisticDistanceToSun_ShouldProduceReasonableRange()
    {
        // Arrange: WorldEngine example - distance-to-sun variation
        var rng = new Random(42);
        int sampleCount = 1000;

        // Act: Sample with WorldEngine parameters
        var samples = Enumerable.Range(0, sampleCount)
            .Select(_ =>
            {
                float d = MathUtils.SampleGaussian(rng, mean: 1.0f, hwhm: 0.12f);
                return Math.Max(0.1f, d);  // Clamp like WorldEngine
            })
            .ToList();

        // Assert: Most samples should be in reasonable range
        // WorldEngine comment: "most likely outcomes between 0.78 and 1.22"
        // But outliers exist (it's a Gaussian!), so use wider bounds for min/max
        float min = samples.Min();
        float max = samples.Max();

        min.Should().BeGreaterThan(0.5f);   // Very rare to get <0.5 (but possible)
        max.Should().BeLessThan(1.5f);      // Very rare to get >1.5 (but possible)
        samples.Average().Should().BeApproximately(1.0f, 0.05f);  // Mean ~1.0 (most important!)
    }

    [Fact]
    public void SampleGaussian_DifferentSeeds_ShouldProduceDifferentResults()
    {
        // Arrange
        var rng1 = new Random(42);
        var rng2 = new Random(123);

        // Act
        float sample1 = MathUtils.SampleGaussian(rng1, mean: 1.0f, hwhm: 0.12f);
        float sample2 = MathUtils.SampleGaussian(rng2, mean: 1.0f, hwhm: 0.12f);

        // Assert: Different seeds should (almost always) produce different values
        sample1.Should().NotBe(sample2);
    }
}
