using Xunit;
using FluentAssertions;
using Darklands.Domain.Grid;
using Darklands.Core.Tests.TestUtilities;

namespace Darklands.Core.Tests.Domain.Grid;

/// <summary>
/// Basic tests to verify Grid domain implementation compiles and works.
/// </summary>
[Trait("Category", "Domain")]
[Trait("Feature", "Grid")]
public class BasicGridTests
{
    [Fact]
    public void Position_Creation_Works()
    {
        // Act
        var position = new Position(5, 10);

        // Assert
        position.X.Should().Be(5);
        position.Y.Should().Be(10);
    }

    [Fact]
    public void Position_ManhattanDistance_CalculatesCorrectly()
    {
        // Arrange
        var pos1 = new Position(0, 0);
        var pos2 = new Position(3, 4);

        // Act
        var distance = pos1.ManhattanDistanceTo(pos2);

        // Assert
        distance.Should().Be(7);
    }

    [Fact]
    public void ActorId_Creation_Works()
    {
        // Act
        var actorId = ActorId.NewId(TestIdGenerator.Instance);

        // Assert
        actorId.IsEmpty.Should().BeFalse();
        actorId.Value.Should().NotBe(System.Guid.Empty);
    }

    [Fact]
    public void Tile_CreateEmpty_Works()
    {
        // Arrange
        var position = new Position(2, 3);

        // Act
        var tile = Tile.CreateEmpty(position, TerrainType.Forest);

        // Assert
        tile.Position.Should().Be(position);
        tile.TerrainType.Should().Be(TerrainType.Forest);
        tile.IsEmpty.Should().BeTrue();
        tile.IsOccupied.Should().BeFalse();
        tile.IsPassable.Should().BeTrue(); // Forest is passable but empty
    }

    [Fact]
    public void Tile_WithOccupant_Works()
    {
        // Arrange
        var position = new Position(1, 1);
        var actorId = ActorId.NewId(TestIdGenerator.Instance);
        var tile = Tile.CreateEmpty(position, TerrainType.Open);

        // Act
        var occupiedTile = tile.WithOccupant(actorId);

        // Assert
        occupiedTile.IsOccupied.Should().BeTrue();
        occupiedTile.IsEmpty.Should().BeFalse();
        occupiedTile.IsPassable.Should().BeFalse(); // Not passable when occupied
    }

    [Fact]
    public void Movement_Creation_Works()
    {
        // Arrange
        var from = new Position(1, 1);
        var to = new Position(2, 2);

        // Act
        var movement = new Movement(from, to);

        // Assert
        movement.From.Should().Be(from);
        movement.To.Should().Be(to);
        movement.ManhattanDistance.Should().Be(2);
        movement.IsDiagonal.Should().BeTrue();
    }

    [Fact]
    public void TerrainType_Enum_HasExpectedValues()
    {
        // Assert
        System.Enum.GetValues<TerrainType>().Should().Contain(new[]
        {
            TerrainType.Open,
            TerrainType.Forest,
            TerrainType.Water,
            TerrainType.Wall
        });
    }
}
