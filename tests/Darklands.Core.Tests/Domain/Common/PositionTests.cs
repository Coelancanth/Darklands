using Darklands.Core.Domain.Common;
using FluentAssertions;
using Xunit;

namespace Darklands.Core.Tests.Domain.Common;

[Trait("Category", "Common")]
[Trait("Category", "Unit")]
public class PositionTests
{
    [Fact]
    public void Constructor_ValidCoordinates_ShouldCreatePosition()
    {
        // Act
        var position = new Position(X: 5, Y: 10);

        // Assert
        position.X.Should().Be(5);
        position.Y.Should().Be(10);
    }

    [Fact]
    public void Constructor_ZeroCoordinates_ShouldBeValid()
    {
        // WHY: (0, 0) is a valid grid coordinate (top-left corner)

        // Act
        var position = new Position(X: 0, Y: 0);

        // Assert
        position.X.Should().Be(0);
        position.Y.Should().Be(0);
    }

    [Fact]
    public void Constructor_NegativeCoordinates_ShouldBeAllowed()
    {
        // WHY: Position is context-free; GridMap enforces bounds validation

        // Act
        var position = new Position(X: -5, Y: -10);

        // Assert
        position.X.Should().Be(-5);
        position.Y.Should().Be(-10);
    }

    [Fact]
    public void Equality_TwoPositionsWithSameCoordinates_ShouldBeEqual()
    {
        // VALUE SEMANTICS: Record struct provides value-based equality

        // Arrange
        var pos1 = new Position(X: 3, Y: 7);
        var pos2 = new Position(X: 3, Y: 7);

        // Act & Assert
        pos1.Should().Be(pos2);
        (pos1 == pos2).Should().BeTrue();
        pos1.GetHashCode().Should().Be(pos2.GetHashCode());
    }

    [Fact]
    public void Equality_TwoPositionsWithDifferentCoordinates_ShouldNotBeEqual()
    {
        // Arrange
        var pos1 = new Position(X: 3, Y: 7);
        var pos2 = new Position(X: 3, Y: 8);

        // Act & Assert
        pos1.Should().NotBe(pos2);
        (pos1 != pos2).Should().BeTrue();
    }

    [Fact]
    public void ToString_ShouldReturnReadableFormat()
    {
        // WHY: Useful for debugging test failures

        // Arrange
        var position = new Position(X: 12, Y: 25);

        // Act
        var result = position.ToString();

        // Assert
        result.Should().Contain("12");
        result.Should().Contain("25");
    }
}
