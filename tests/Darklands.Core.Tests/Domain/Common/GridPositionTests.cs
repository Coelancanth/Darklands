using Darklands.Core.Domain.Common;
using FluentAssertions;
using Xunit;

namespace Darklands.Core.Tests.Domain.Common;

[Trait("Category", "Common")]
[Trait("Category", "Unit")]
public class GridPositionTests
{
    [Fact]
    public void Create_WithValidCoordinates_ShouldSucceed()
    {
        // Act
        var position = new GridPosition(5, 10);

        // Assert
        position.X.Should().Be(5);
        position.Y.Should().Be(10);
    }

    [Fact]
    public void Equality_WithSameCoordinates_ShouldBeEqual()
    {
        // ARCHITECTURE: Value semantics via record struct

        // Arrange
        var pos1 = new GridPosition(3, 7);
        var pos2 = new GridPosition(3, 7);

        // Assert
        pos1.Should().Be(pos2);
        (pos1 == pos2).Should().BeTrue();
    }

    [Fact]
    public void Equality_WithDifferentCoordinates_ShouldNotBeEqual()
    {
        // Arrange
        var pos1 = new GridPosition(3, 7);
        var pos2 = new GridPosition(3, 8);

        // Assert
        pos1.Should().NotBe(pos2);
        (pos1 != pos2).Should().BeTrue();
    }

    [Fact]
    public void GridPosition_CanBeUsedAsDictionaryKey()
    {
        // ARCHITECTURE: Value semantics enable Dictionary<GridPosition, T>

        // Arrange
        var dict = new Dictionary<GridPosition, string>
        {
            [new GridPosition(0, 0)] = "TopLeft",
            [new GridPosition(5, 5)] = "Center"
        };

        // Assert
        dict[new GridPosition(0, 0)].Should().Be("TopLeft");
        dict[new GridPosition(5, 5)].Should().Be("Center");
    }
}
