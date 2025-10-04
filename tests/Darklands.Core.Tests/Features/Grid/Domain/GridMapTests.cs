using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Grid.Application;
using Darklands.Core.Features.Grid.Domain;
using Darklands.Core.Features.Grid.Infrastructure.Repositories;
using FluentAssertions;
using Xunit;

namespace Darklands.Core.Tests.Features.Grid.Domain;

[Trait("Category", "Grid")]
[Trait("Category", "Unit")]
public class GridMapTests
{
    private readonly ITerrainRepository _terrainRepo;
    private readonly TerrainDefinition _floorTerrain;
    private readonly TerrainDefinition _wallTerrain;
    private readonly TerrainDefinition _smokeTerrain;

    public GridMapTests()
    {
        _terrainRepo = new StubTerrainRepository();
        _floorTerrain = _terrainRepo.GetByName("floor").Value;
        _wallTerrain = _terrainRepo.GetByName("wall").Value;
        _smokeTerrain = _terrainRepo.GetByName("smoke").Value;
    }

    #region Construction Tests

    [Fact]
    public void Constructor_ShouldInitializeWithAllDefaultTerrain()
    {
        // WHY: Default map should be fully walkable (all floor terrain)

        // Arrange
        var defaultTerrain = _terrainRepo.GetDefault().Value;

        // Act
        var map = new GridMap(defaultTerrain);

        // Assert
        for (int x = 0; x < GridMap.Width; x++)
        {
            for (int y = 0; y < GridMap.Height; y++)
            {
                var position = new Position(x, y);
                var result = map.GetTerrain(position);

                result.IsSuccess.Should().BeTrue();
                result.Value.Should().Be(_floorTerrain);
                result.Value.Name.Should().Be("floor");
            }
        }
    }

    [Fact]
    public void Constructor_ShouldCreate30x30Grid()
    {
        // ARCHITECTURE: Validate grid dimensions match specification

        // Assert
        GridMap.Width.Should().Be(30);
        GridMap.Height.Should().Be(30);
    }

    #endregion

    #region IsValidPosition Tests

    [Theory]
    [InlineData(0, 0, true)]      // Top-left corner
    [InlineData(29, 29, true)]    // Bottom-right corner
    [InlineData(15, 15, true)]    // Center
    [InlineData(0, 29, true)]     // Bottom-left corner
    [InlineData(29, 0, true)]     // Top-right corner
    public void IsValidPosition_InsideBounds_ShouldReturnTrue(int x, int y, bool expected)
    {
        // Arrange
        var map = new GridMap(_floorTerrain);
        var position = new Position(x, y);

        // Act
        var result = map.IsValidPosition(position);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(-1, 0, false)]    // X negative
    [InlineData(0, -1, false)]    // Y negative
    [InlineData(-1, -1, false)]   // Both negative
    [InlineData(30, 0, false)]    // X beyond bounds
    [InlineData(0, 30, false)]    // Y beyond bounds
    [InlineData(30, 30, false)]   // Both beyond bounds
    [InlineData(100, 100, false)] // Far outside bounds
    public void IsValidPosition_OutsideBounds_ShouldReturnFalse(int x, int y, bool expected)
    {
        // Arrange
        var map = new GridMap(_floorTerrain);
        var position = new Position(x, y);

        // Act
        var result = map.IsValidPosition(position);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region GetTerrain Tests

    [Fact]
    public void GetTerrain_ValidPosition_ShouldReturnSuccess()
    {
        // Arrange
        var map = new GridMap(_floorTerrain);
        var position = new Position(10, 10);

        // Act
        var result = map.GetTerrain(position);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(_floorTerrain);
        result.Value.Name.Should().Be("floor");
    }

    [Fact]
    public void GetTerrain_OutOfBounds_ShouldReturnFailure()
    {
        // DOMAIN ERROR: Out-of-bounds access is a business rule violation

        // Arrange
        var map = new GridMap(_floorTerrain);
        var position = new Position(-1, 5);

        // Act
        var result = map.GetTerrain(position);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("outside grid bounds");
        result.Error.Should().Contain("-1");
        result.Error.Should().Contain("5");
    }

    #endregion

    #region SetTerrain Tests

    [Fact]
    public void SetTerrain_ValidPosition_ShouldUpdateTerrain()
    {
        // Arrange
        var map = new GridMap(_floorTerrain);
        var position = new Position(5, 5);

        // Act
        var setResult = map.SetTerrain(position, _wallTerrain);
        var getResult = map.GetTerrain(position);

        // Assert
        setResult.IsSuccess.Should().BeTrue();
        getResult.IsSuccess.Should().BeTrue();
        getResult.Value.Should().Be(_wallTerrain);
        getResult.Value.Name.Should().Be("wall");
    }

    [Fact]
    public void SetTerrain_OutOfBounds_ShouldReturnFailure()
    {
        // DOMAIN ERROR: Cannot modify terrain outside grid bounds

        // Arrange
        var map = new GridMap(_floorTerrain);
        var position = new Position(30, 30);

        // Act
        var result = map.SetTerrain(position, _wallTerrain);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("outside grid bounds");
        result.Error.Should().Contain("30");
    }

    [Fact]
    public void SetTerrain_SmokeType_ShouldUpdateCorrectly()
    {
        // WHY: Validate Smoke terrain type (tactical depth)

        // Arrange
        var map = new GridMap(_floorTerrain);
        var position = new Position(10, 10);

        // Act
        var setResult = map.SetTerrain(position, _smokeTerrain);
        var getResult = map.GetTerrain(position);

        // Assert
        setResult.IsSuccess.Should().BeTrue();
        getResult.Value.Should().Be(_smokeTerrain);
        getResult.Value.Name.Should().Be("smoke");
    }

    #endregion

    #region IsPassable Tests

    [Fact]
    public void IsPassable_FloorTerrain_ShouldReturnTrue()
    {
        // Arrange
        var map = new GridMap(_floorTerrain);
        var position = new Position(5, 5);
        map.SetTerrain(position, _floorTerrain);

        // Act
        var result = map.IsPassable(position);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public void IsPassable_WallTerrain_ShouldReturnFalse()
    {
        // Arrange
        var map = new GridMap(_floorTerrain);
        var position = new Position(5, 5);
        map.SetTerrain(position, _wallTerrain);

        // Act
        var result = map.IsPassable(position);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public void IsPassable_SmokeTerrain_ShouldReturnTrue()
    {
        // WHY: Smoke is passable (can walk through) but opaque (blocks vision)

        // Arrange
        var map = new GridMap(_floorTerrain);
        var position = new Position(5, 5);
        map.SetTerrain(position, _smokeTerrain);

        // Act
        var result = map.IsPassable(position);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public void IsPassable_OutOfBounds_ShouldReturnFailure()
    {
        // RAILWAY-ORIENTED: Failure propagates from GetTerrain

        // Arrange
        var map = new GridMap(_floorTerrain);
        var position = new Position(-1, -1);

        // Act
        var result = map.IsPassable(position);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("outside grid bounds");
    }

    #endregion

    #region IsOpaque Tests

    [Fact]
    public void IsOpaque_FloorTerrain_ShouldReturnFalse()
    {
        // Arrange
        var map = new GridMap(_floorTerrain);
        var position = new Position(5, 5);
        map.SetTerrain(position, _floorTerrain);

        // Act
        var result = map.IsOpaque(position);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public void IsOpaque_WallTerrain_ShouldReturnTrue()
    {
        // Arrange
        var map = new GridMap(_floorTerrain);
        var position = new Position(5, 5);
        map.SetTerrain(position, _wallTerrain);

        // Act
        var result = map.IsOpaque(position);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public void IsOpaque_SmokeTerrain_ShouldReturnTrue()
    {
        // WHY: Smoke blocks vision (opaque) but allows movement (passable)

        // Arrange
        var map = new GridMap(_floorTerrain);
        var position = new Position(5, 5);
        map.SetTerrain(position, _smokeTerrain);

        // Act
        var result = map.IsOpaque(position);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public void IsOpaque_OutOfBounds_ShouldReturnFailure()
    {
        // RAILWAY-ORIENTED: Failure propagates from GetTerrain

        // Arrange
        var map = new GridMap(_floorTerrain);
        var position = new Position(50, 50);

        // Act
        var result = map.IsOpaque(position);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("outside grid bounds");
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void GridMap_MultipleTerrainTypes_ShouldMaintainIndependence()
    {
        // EDGE CASE: Setting terrain at one position shouldn't affect others

        // Arrange
        var map = new GridMap(_floorTerrain);
        var wallPos = new Position(5, 5);
        var smokePos = new Position(10, 10);
        var floorPos = new Position(15, 15);

        // Act
        map.SetTerrain(wallPos, _wallTerrain);
        map.SetTerrain(smokePos, _smokeTerrain);
        // floorPos stays default (floor terrain)

        // Assert
        map.GetTerrain(wallPos).Value.Should().Be(_wallTerrain);
        map.GetTerrain(smokePos).Value.Should().Be(_smokeTerrain);
        map.GetTerrain(floorPos).Value.Should().Be(_floorTerrain);

        map.IsPassable(wallPos).Value.Should().BeFalse();
        map.IsPassable(smokePos).Value.Should().BeTrue();
        map.IsPassable(floorPos).Value.Should().BeTrue();

        map.IsOpaque(wallPos).Value.Should().BeTrue();
        map.IsOpaque(smokePos).Value.Should().BeTrue();
        map.IsOpaque(floorPos).Value.Should().BeFalse();
    }

    #endregion
}
