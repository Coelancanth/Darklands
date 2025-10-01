using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Grid.Domain;
using Darklands.Core.Features.Grid.Infrastructure.Services;
using FluentAssertions;
using Xunit;

namespace Darklands.Core.Tests.Features.Grid.Infrastructure;

/// <summary>
/// Tests for ShadowcastingFOVService - Phase 3 Infrastructure layer.
/// Verifies custom shadowcasting implementation with REAL terrain opacity checks.
/// </summary>
[Trait("Category", "Phase3")]
[Trait("Category", "Unit")]
public sealed class ShadowcastingFOVServiceTests
{
    [Fact]
    public void CalculateFOV_EmptyGrid_AllTilesWithinRadiusVisible()
    {
        // Arrange: 30x30 grid, all Floor (transparent)
        var map = new GridMap();
        var fov = new ShadowcastingFOVService();
        var observer = new Position(15, 15); // Center of grid
        int radius = 8;

        // Act
        var result = fov.CalculateFOV(map, observer, radius);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain(observer, "observer position is always visible");

        // Verify tiles at radius edge are visible (empty grid = no obstacles)
        result.Value.Should().Contain(new Position(15 + 8, 15), "east at radius 8");
        result.Value.Should().Contain(new Position(15 - 8, 15), "west at radius 8");
        result.Value.Should().Contain(new Position(15, 15 + 8), "south at radius 8");
        result.Value.Should().Contain(new Position(15, 15 - 8), "north at radius 8");

        // Verify tiles beyond radius are NOT visible
        result.Value.Should().NotContain(new Position(15 + 9, 15), "beyond radius in cardinal direction");

        // ALGORITHM VERIFICATION: In empty grid, visible tiles ≈ π × radius²
        // For radius=8: ~201 tiles (allowing ±10% for edge discretization)
        result.Value.Count.Should().BeInRange(180, 220,
            "visible count should approximate π × radius² for empty grid");
    }

    [Fact]
    public void CalculateFOV_WallBlocksVision_TilesBehindWallNotVisible()
    {
        // Arrange: Place wall directly east of observer
        var map = new GridMap();
        var observer = new Position(5, 5);
        var wallPos = new Position(10, 5); // 5 tiles east

        map.SetTerrain(wallPos, TerrainType.Wall);
        var fov = new ShadowcastingFOVService();

        // Act
        var result = fov.CalculateFOV(map, observer, radius: 10);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // SHADOWCASTING: Wall itself is visible (can see the obstacle)
        result.Value.Should().Contain(wallPos, "wall itself blocks but is visible");

        // SHADOWCASTING: Tiles directly behind wall should be in shadow
        result.Value.Should().NotContain(new Position(11, 5), "tile immediately behind wall");
        result.Value.Should().NotContain(new Position(15, 5), "tile in shadow cone");

        // Tiles to the side of wall should still be visible
        result.Value.Should().Contain(new Position(10, 4), "tile north of wall, not in shadow");
        result.Value.Should().Contain(new Position(10, 6), "tile south of wall, not in shadow");
    }

    [Fact]
    public void CalculateFOV_SmokeBlocksVision_ButIsPassable()
    {
        // Arrange: Place smoke between observer and target
        var map = new GridMap();
        var observer = new Position(5, 5);
        var smokePos = new Position(10, 10); // Diagonal from observer

        map.SetTerrain(smokePos, TerrainType.Smoke);
        var fov = new ShadowcastingFOVService();

        // Act
        var result = fov.CalculateFOV(map, observer, radius: 10);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // SMOKE MECHANICS: Smoke tile itself is visible
        result.Value.Should().Contain(smokePos, "smoke is opaque but visible itself");

        // SMOKE MECHANICS: Tiles behind smoke are NOT visible (smoke blocks vision)
        result.Value.Should().NotContain(new Position(15, 15), "tile in shadow behind smoke");

        // BUSINESS RULE: Verify smoke is passable (tactical distinction from walls)
        var smokeTerrain = map.GetTerrain(smokePos);
        smokeTerrain.Value.IsPassable().Should().BeTrue(
            "smoke must be passable (key tactical difference from walls)");
        smokeTerrain.Value.IsOpaque().Should().BeTrue(
            "smoke must be opaque (blocks vision like walls)");
    }

    [Fact]
    public void CalculateFOV_InvalidRadius_ShouldReturnFailure()
    {
        // Arrange
        var map = new GridMap();
        var fov = new ShadowcastingFOVService();
        var observer = new Position(15, 15);

        // Act: Radius must be positive
        var resultZero = fov.CalculateFOV(map, observer, radius: 0);
        var resultNegative = fov.CalculateFOV(map, observer, radius: -5);

        // Assert
        resultZero.IsFailure.Should().BeTrue();
        resultZero.Error.Should().Contain("positive", "error message should explain constraint");

        resultNegative.IsFailure.Should().BeTrue();
        resultNegative.Error.Should().Contain("positive");
    }

    [Fact]
    public void CalculateFOV_ObserverOutOfBounds_ShouldReturnFailure()
    {
        // Arrange
        var map = new GridMap(); // 30x30 grid
        var fov = new ShadowcastingFOVService();

        // Act: Observer positions outside grid bounds
        var resultNegative = fov.CalculateFOV(map, new Position(-1, 15), radius: 8);
        var resultTooLarge = fov.CalculateFOV(map, new Position(50, 15), radius: 8);

        // Assert
        resultNegative.IsFailure.Should().BeTrue();
        resultNegative.Error.Should().Contain("outside grid bounds");

        resultTooLarge.IsFailure.Should().BeTrue();
        resultTooLarge.Error.Should().Contain("outside grid bounds");
    }

    [Fact]
    public void CalculateFOV_ObserverAlwaysVisible_RegardlessOfRadius()
    {
        // Arrange
        var map = new GridMap();
        var fov = new ShadowcastingFOVService();
        var observer = new Position(15, 15);

        // Act: Even with minimal radius
        var result = fov.CalculateFOV(map, observer, radius: 1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain(observer, "observer can always see their own position");
    }

    [Fact]
    [Trait("Category", "Performance")]
    public void CalculateFOV_30x30Grid_CompletesInUnder10Milliseconds()
    {
        // PERFORMANCE: FOV recalculates every actor move, must be fast for smooth gameplay
        var map = new GridMap();

        // Add realistic obstacle pattern (walls scattered across grid)
        for (int i = 5; i < 25; i += 3)
        {
            map.SetTerrain(new Position(i, 15), TerrainType.Wall);
            map.SetTerrain(new Position(15, i), TerrainType.Wall);
        }

        var fov = new ShadowcastingFOVService();
        var observer = new Position(15, 15);

        // Warmup run (JIT compilation, cache warming)
        fov.CalculateFOV(map, observer, radius: 8);

        // Act: Measure performance
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = fov.CalculateFOV(map, observer, radius: 8);
        sw.Stop();

        // Assert
        result.IsSuccess.Should().BeTrue();
        sw.ElapsedMilliseconds.Should().BeLessThan(10,
            "FOV calculation must complete in <10ms for real-time gameplay");

        // Typically expect 2-5ms on modern hardware
        // (This validates O(8 × radius²) complexity is acceptable)
    }

    [Fact]
    public void CalculateFOV_AllEightOctants_ProvideFullCircularCoverage()
    {
        // ALGORITHM VERIFICATION: Test that all 8 octants are correctly implemented
        var map = new GridMap();
        var observer = new Position(15, 15); // Center
        var fov = new ShadowcastingFOVService();

        // Act
        var result = fov.CalculateFOV(map, observer, radius: 5);

        // Assert: Verify visibility in all 8 compass directions (octants)
        result.IsSuccess.Should().BeTrue();

        // Cardinal directions (exact octant boundaries)
        result.Value.Should().Contain(new Position(20, 15), "East - Octant 0");
        result.Value.Should().Contain(new Position(15, 20), "South - Octant 7");
        result.Value.Should().Contain(new Position(10, 15), "West - Octant 4");
        result.Value.Should().Contain(new Position(15, 10), "North - Octant 3");

        // Diagonal directions (within radius, distance ≈ 3.5 from center)
        result.Value.Should().Contain(new Position(18, 18), "Southeast - Octant 6/7");
        result.Value.Should().Contain(new Position(12, 18), "Southwest - Octant 5/6");
        result.Value.Should().Contain(new Position(12, 12), "Northwest - Octant 2/3");
        result.Value.Should().Contain(new Position(18, 12), "Northeast - Octant 0/1");
    }
}
