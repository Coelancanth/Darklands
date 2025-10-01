using Darklands.Core.Features.Grid.Domain;
using FluentAssertions;
using Xunit;

namespace Darklands.Core.Tests.Features.Grid.Domain;

[Trait("Category", "Phase1")]
[Trait("Category", "Unit")]
public class TerrainTypeTests
{
    #region IsPassable Tests

    [Fact]
    public void IsPassable_Floor_ShouldBeTrue()
    {
        // WHY: Floor allows movement (standard walkable terrain)

        // Act
        var result = TerrainType.Floor.IsPassable();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsPassable_Wall_ShouldBeFalse()
    {
        // WHY: Walls block movement (impassable obstacle)

        // Act
        var result = TerrainType.Wall.IsPassable();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsPassable_Smoke_ShouldBeTrue()
    {
        // WHY: Smoke is passable (can walk through) but opaque (blocks vision)
        // TACTICAL DEPTH: Enables hide/ambush mechanics

        // Act
        var result = TerrainType.Smoke.IsPassable();

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region IsOpaque Tests

    [Fact]
    public void IsOpaque_Floor_ShouldBeFalse()
    {
        // WHY: Floor is transparent (allows vision)

        // Act
        var result = TerrainType.Floor.IsOpaque();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsOpaque_Wall_ShouldBeTrue()
    {
        // WHY: Walls block vision (opaque obstacle)

        // Act
        var result = TerrainType.Wall.IsOpaque();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsOpaque_Smoke_ShouldBeTrue()
    {
        // WHY: Smoke blocks vision but allows movement
        // TACTICAL DEPTH: Hide in smoke to break line-of-sight

        // Act
        var result = TerrainType.Smoke.IsOpaque();

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void AllTerrainTypes_ShouldHaveConsistentBehavior()
    {
        // ARCHITECTURE: Verify all terrain types have defined behavior (no default fallthrough)

        // Arrange
        var terrainTypes = new[] { TerrainType.Floor, TerrainType.Wall, TerrainType.Smoke };

        // Act & Assert - Should not throw
        foreach (var terrain in terrainTypes)
        {
            terrain.IsPassable();  // Should not throw
            terrain.IsOpaque();    // Should not throw
        }
    }

    #endregion
}
