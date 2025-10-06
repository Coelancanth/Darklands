using Darklands.Core.Features.WorldGen.Infrastructure.Algorithms;
using FluentAssertions;
using Xunit;

namespace Darklands.Core.Tests.Features.WorldGen.Infrastructure.Algorithms;

[Trait("Category", "WorldGen")]
public class ElevationPostProcessorTests
{
    [Fact]
    public void PlaceOceansAtBorders_WhenCalled_ShouldLowerBorderElevation()
    {
        // ARRANGE
        var heightmap = new float[5, 5];

        // Fill with uniform elevation
        for (int y = 0; y < 5; y++)
            for (int x = 0; x < 5; x++)
                heightmap[y, x] = 1.0f;

        // ACT
        ElevationPostProcessor.PlaceOceansAtBorders(heightmap, borderReduction: 0.5f);

        // ASSERT
        // All border cells should be reduced to 0.5
        heightmap[0, 0].Should().Be(0.5f, "Top-left corner should be lowered");
        heightmap[0, 4].Should().Be(0.5f, "Top-right corner should be lowered");
        heightmap[4, 0].Should().Be(0.5f, "Bottom-left corner should be lowered");
        heightmap[4, 4].Should().Be(0.5f, "Bottom-right corner should be lowered");

        heightmap[0, 2].Should().Be(0.5f, "Top edge should be lowered");
        heightmap[4, 2].Should().Be(0.5f, "Bottom edge should be lowered");
        heightmap[2, 0].Should().Be(0.5f, "Left edge should be lowered");
        heightmap[2, 4].Should().Be(0.5f, "Right edge should be lowered");

        // Center should remain unchanged
        heightmap[2, 2].Should().Be(1.0f, "Center should not be affected");
        heightmap[1, 1].Should().Be(1.0f, "Interior cells should not be affected");
    }

    [Fact]
    public void FillOcean_WhenBorderBelowSeaLevel_ShouldMarkOcean()
    {
        // ARRANGE: Simple 5x5 map with water border and land center
        var heightmap = new float[5, 5]
        {
            { 0.3f, 0.3f, 0.3f, 0.3f, 0.3f }, // Water border
            { 0.3f, 0.8f, 0.8f, 0.8f, 0.3f },
            { 0.3f, 0.8f, 0.9f, 0.8f, 0.3f }, // Land center
            { 0.3f, 0.8f, 0.8f, 0.8f, 0.3f },
            { 0.3f, 0.3f, 0.3f, 0.3f, 0.3f }  // Water border
        };

        float seaLevel = 0.5f;

        // ACT
        var oceanMask = ElevationPostProcessor.FillOcean(heightmap, seaLevel);

        // ASSERT
        // All border cells should be ocean (< sea level)
        oceanMask[0, 0].Should().BeTrue("Top-left border should be ocean");
        oceanMask[0, 4].Should().BeTrue("Top-right border should be ocean");
        oceanMask[4, 0].Should().BeTrue("Bottom-left border should be ocean");
        oceanMask[4, 4].Should().BeTrue("Bottom-right border should be ocean");

        // Border edges should be ocean
        for (int i = 0; i < 5; i++)
        {
            oceanMask[0, i].Should().BeTrue($"Top border [{0},{i}] should be ocean");
            oceanMask[4, i].Should().BeTrue($"Bottom border [{4},{i}] should be ocean");
            oceanMask[i, 0].Should().BeTrue($"Left border [{i},{0}] should be ocean");
            oceanMask[i, 4].Should().BeTrue($"Right border [{i},{4}] should be ocean");
        }

        // Land center should not be ocean (above sea level)
        oceanMask[1, 1].Should().BeFalse("Interior land should not be ocean");
        oceanMask[2, 2].Should().BeFalse("Center land should not be ocean");
        oceanMask[3, 3].Should().BeFalse("Interior land should not be ocean");
    }

    [Fact]
    public void FillOcean_WhenLandlockedLakeBelowSeaLevel_ShouldNotMarkAsOcean()
    {
        // WHY: Lakes (below sea level but not connected to borders) should NOT be ocean.
        // Ocean is defined as water connected to map edges.

        // ARRANGE: Land border with landlocked lake in center
        var heightmap = new float[5, 5]
        {
            { 0.8f, 0.8f, 0.8f, 0.8f, 0.8f }, // Land border
            { 0.8f, 0.9f, 0.9f, 0.9f, 0.8f },
            { 0.8f, 0.9f, 0.3f, 0.9f, 0.8f }, // Lake in center
            { 0.8f, 0.9f, 0.9f, 0.9f, 0.8f },
            { 0.8f, 0.8f, 0.8f, 0.8f, 0.8f }  // Land border
        };

        float seaLevel = 0.5f;

        // ACT
        var oceanMask = ElevationPostProcessor.FillOcean(heightmap, seaLevel);

        // ASSERT
        // Border should not be ocean (above sea level)
        oceanMask[0, 0].Should().BeFalse("Land border should not be ocean");

        // Landlocked lake should NOT be marked as ocean (not connected to border)
        oceanMask[2, 2].Should().BeFalse("Landlocked lake should not be ocean");
    }

    [Fact]
    public void AddNoise_WithSameSeed_ShouldProduceDeterministicResults()
    {
        // WHY: Reproducibility is critical for seed-based world generation.

        // ARRANGE
        var heightmap1 = CreateUniformHeightmap(10, 10, 0.5f);
        var heightmap2 = CreateUniformHeightmap(10, 10, 0.5f);

        int seed = 42;

        // ACT
        ElevationPostProcessor.AddNoise(heightmap1, seed, scale: 0.1f, amplitude: 0.2f);
        ElevationPostProcessor.AddNoise(heightmap2, seed, scale: 0.1f, amplitude: 0.2f);

        // ASSERT
        for (int y = 0; y < 10; y++)
        {
            for (int x = 0; x < 10; x++)
            {
                heightmap1[y, x].Should().BeApproximately(
                    heightmap2[y, x], 0.0001f,
                    $"Heightmap[{y},{x}] should be identical with same seed");
            }
        }
    }

    [Fact]
    public void AddNoise_WithDifferentSeeds_ShouldProduceDifferentResults()
    {
        // ARRANGE
        var heightmap1 = CreateUniformHeightmap(10, 10, 0.5f);
        var heightmap2 = CreateUniformHeightmap(10, 10, 0.5f);

        // ACT
        ElevationPostProcessor.AddNoise(heightmap1, seed: 42, scale: 0.1f, amplitude: 0.2f);
        ElevationPostProcessor.AddNoise(heightmap2, seed: 999, scale: 0.1f, amplitude: 0.2f);

        // ASSERT
        bool hasDifference = false;

        for (int y = 0; y < 10; y++)
        {
            for (int x = 0; x < 10; x++)
            {
                if (Math.Abs(heightmap1[y, x] - heightmap2[y, x]) > 0.01f)
                {
                    hasDifference = true;
                    break;
                }
            }
        }

        hasDifference.Should().BeTrue("Different seeds should produce different noise patterns");
    }

    [Fact]
    public void AddNoise_WhenApplied_ShouldModifyElevation()
    {
        // ARRANGE
        var heightmap = CreateUniformHeightmap(10, 10, 0.5f);
        var originalValue = heightmap[5, 5];

        // ACT
        ElevationPostProcessor.AddNoise(heightmap, seed: 123, scale: 0.1f, amplitude: 0.2f);

        // ASSERT
        bool hasChange = false;

        for (int y = 0; y < 10; y++)
        {
            for (int x = 0; x < 10; x++)
            {
                // Noise should change at least some cells
                if (Math.Abs(heightmap[y, x] - 0.5f) > 0.01f)
                {
                    hasChange = true;
                    break;
                }

                // Elevation should remain in valid range [0, 1]
                heightmap[y, x].Should().BeInRange(0f, 1f, "Elevation should be clamped to valid range");
            }
        }

        hasChange.Should().BeTrue("Noise should modify at least some elevation values");
    }

    // HELPER: Create uniform heightmap for testing
    private static float[,] CreateUniformHeightmap(int width, int height, float value)
    {
        var heightmap = new float[height, width];

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                heightmap[y, x] = value;

        return heightmap;
    }
}
