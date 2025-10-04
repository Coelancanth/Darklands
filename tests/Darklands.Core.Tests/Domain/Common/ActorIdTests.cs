using Darklands.Core.Domain.Common;
using FluentAssertions;
using Xunit;

namespace Darklands.Core.Tests.Domain.Common;

[Trait("Category", "Common")]
[Trait("Category", "Unit")]
public class ActorIdTests
{
    [Fact]
    public void NewId_ShouldCreateUniqueIds()
    {
        // Act
        var id1 = ActorId.NewId();
        var id2 = ActorId.NewId();

        // Assert
        id1.Should().NotBe(id2);
        id1.Value.Should().NotBe(Guid.Empty);
        id2.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void From_ValidGuidString_ShouldParseCorrectly()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var guidString = guid.ToString();

        // Act
        var actorId = ActorId.From(guidString);

        // Assert
        actorId.Value.Should().Be(guid);
    }

    [Fact]
    public void From_InvalidString_ShouldThrowFormatException()
    {
        // PROGRAMMER ERROR: Invalid format is a bug in calling code

        // Act
        var act = () => ActorId.From("not-a-guid");

        // Assert
        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void ToString_ShouldReturnGuidString()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var actorId = new ActorId(guid);

        // Act
        var result = actorId.ToString();

        // Assert
        result.Should().Be(guid.ToString());
    }

    [Fact]
    public void Equality_TwoActorIdsWithSameGuid_ShouldBeEqual()
    {
        // VALUE SEMANTICS: Two ActorIds with same Guid should be equal

        // Arrange
        var guid = Guid.NewGuid();
        var id1 = new ActorId(guid);
        var id2 = new ActorId(guid);

        // Act & Assert
        id1.Should().Be(id2);
        (id1 == id2).Should().BeTrue();
        id1.GetHashCode().Should().Be(id2.GetHashCode());
    }

    [Fact]
    public void Equality_TwoActorIdsWithDifferentGuids_ShouldNotBeEqual()
    {
        // Arrange
        var id1 = ActorId.NewId();
        var id2 = ActorId.NewId();

        // Act & Assert
        id1.Should().NotBe(id2);
        (id1 != id2).Should().BeTrue();
    }
}