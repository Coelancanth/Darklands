using FluentAssertions;
using Xunit;
using Darklands.Domain.Grid;
using System.Collections.Immutable;
using LanguageExt;
using static LanguageExt.Prelude;
using ActorEntity = Darklands.Domain.Actor.Actor;

namespace Darklands.Core.Tests.Domain.Actor;

[Trait("Category", "Phase1")]
[Trait("Layer", "Domain")]
public class ActorMovementTests
{
    [Fact]
    public void HasActivePath_WithNoPath_ReturnsFalse()
    {
        // Arrange
        var actor = CreateTestActor();

        // Act & Assert
        actor.HasActivePath.Should().BeFalse();
        actor.IsMoving.Should().BeFalse();
    }

    [Fact]
    public void HasActivePath_WithPath_ReturnsTrue()
    {
        // Arrange
        var path = ImmutableList.Create(new Position(1, 1), new Position(2, 2));
        var actorResult = CreateTestActor().StartMovement(path);

        // Act
        var actor = actorResult.IfFail(error => throw new Exception($"Should succeed: {error.Message}"));

        // Assert
        actor.HasActivePath.Should().BeTrue();
        actor.IsMoving.Should().BeTrue();
        actor.NextPosition.Should().Be(new Position(1, 1));
    }

    [Fact]
    public void StartMovement_WithValidPath_ReturnsSuccess()
    {
        // Arrange
        var actor = CreateTestActor();
        var path = ImmutableList.Create(new Position(1, 1), new Position(2, 2));

        // Act
        var result = actor.StartMovement(path);

        // Assert
        result.IsSucc.Should().BeTrue();
        var movedActor = result.IfFail(error => throw new Exception($"Should succeed: {error.Message}"));
        movedActor.ActivePath.Should().Equal(path);
        movedActor.CurrentPathStep.Should().Be(0);
        movedActor.NextPosition.Should().Be(new Position(1, 1));
    }

    [Fact]
    public void StartMovement_WithEmptyPath_ReturnsError()
    {
        // Arrange
        var actor = CreateTestActor();
        var emptyPath = ImmutableList<Position>.Empty;

        // Act
        var result = actor.StartMovement(emptyPath);

        // Assert
        result.IsFail.Should().BeTrue();
        result.IfFail(error => error.Message.Should().Contain("empty path"));
    }

    [Fact]
    public void StartMovement_WhenAlreadyMoving_ReturnsError()
    {
        // Arrange
        var path1 = ImmutableList.Create(new Position(1, 1));
        var path2 = ImmutableList.Create(new Position(2, 2));
        var actor = CreateTestActor()
            .StartMovement(path1)
            .IfFail(error => throw new Exception($"Setup should succeed: {error.Message}"));

        // Act
        var result = actor.StartMovement(path2);

        // Assert
        result.IsFail.Should().BeTrue();
        result.IfFail(error => error.Message.Should().Contain("already moving"));
    }

    [Fact]
    public void StartMovement_WithDeadActor_ReturnsError()
    {
        // Arrange
        var deadActor = CreateTestActor().SetToDead();
        var path = ImmutableList.Create(new Position(1, 1));

        // Act
        var result = deadActor.StartMovement(path);

        // Assert
        result.IsFail.Should().BeTrue();
        result.IfFail(error => error.Message.Should().Contain("Dead actors cannot move"));
    }

    [Fact]
    public void AdvanceMovement_WithoutActivePath_ReturnsError()
    {
        // Arrange
        var actor = CreateTestActor();

        // Act
        var result = actor.AdvanceMovement();

        // Assert
        result.IsFail.Should().BeTrue();
        result.IfFail(error => error.Message.Should().Contain("No active movement"));
    }

    [Fact]
    public void AdvanceMovement_WithStepsRemaining_AdvancesToNextStep()
    {
        // Arrange
        var path = ImmutableList.Create(new Position(1, 1), new Position(2, 2), new Position(3, 3));
        var actor = CreateTestActor()
            .StartMovement(path)
            .IfFail(error => throw new Exception($"Setup should succeed: {error.Message}"));

        // Act
        var result = actor.AdvanceMovement();

        // Assert
        result.IsSucc.Should().BeTrue();
        var advancedActor = result.IfFail(error => throw new Exception($"Should succeed: {error.Message}"));
        advancedActor.CurrentPathStep.Should().Be(1);
        advancedActor.NextPosition.Should().Be(new Position(2, 2));
        advancedActor.HasActivePath.Should().BeTrue();
    }

    [Fact]
    public void AdvanceMovement_OnFinalStep_CompletesMovement()
    {
        // Arrange
        var path = ImmutableList.Create(new Position(1, 1));
        var actor = CreateTestActor()
            .StartMovement(path)
            .IfFail(error => throw new Exception($"Setup should succeed: {error.Message}"));

        // Act
        var result = actor.AdvanceMovement();

        // Assert
        result.IsSucc.Should().BeTrue();
        var completedActor = result.IfFail(error => throw new Exception($"Should succeed: {error.Message}"));
        completedActor.HasActivePath.Should().BeFalse();
        completedActor.ActivePath.Should().BeNull();
        completedActor.CurrentPathStep.Should().Be(0);
        completedActor.NextPosition.Should().BeNull();
    }

    [Fact]
    public void AdvanceMovement_WithDeadActor_ReturnsError()
    {
        // Arrange
        var path = ImmutableList.Create(new Position(1, 1));
        var actor = CreateTestActor()
            .StartMovement(path)
            .IfFail(error => throw new Exception($"Setup should succeed: {error.Message}"))
            .SetToDead();

        // Act
        var result = actor.AdvanceMovement();

        // Assert
        result.IsFail.Should().BeTrue();
        result.IfFail(error => error.Message.Should().Contain("Dead actors cannot move"));
    }

    [Fact]
    public void CancelMovement_ClearsMovementState()
    {
        // Arrange
        var path = ImmutableList.Create(new Position(1, 1), new Position(2, 2));
        var actor = CreateTestActor()
            .StartMovement(path)
            .IfFail(error => throw new Exception($"Setup should succeed: {error.Message}"));

        // Act
        var cancelledActor = actor.CancelMovement();

        // Assert
        cancelledActor.HasActivePath.Should().BeFalse();
        cancelledActor.ActivePath.Should().BeNull();
        cancelledActor.CurrentPathStep.Should().Be(0);
    }

    [Fact]
    public void InterruptMovement_ClearsMovementState()
    {
        // Arrange
        var path = ImmutableList.Create(new Position(1, 1), new Position(2, 2));
        var actor = CreateTestActor()
            .StartMovement(path)
            .IfFail(error => throw new Exception($"Setup should succeed: {error.Message}"));

        // Act
        var interruptedActor = actor.InterruptMovement();

        // Assert
        interruptedActor.HasActivePath.Should().BeFalse();
        interruptedActor.ActivePath.Should().BeNull();
        interruptedActor.CurrentPathStep.Should().Be(0);
    }

    [Fact]
    public void RemainingPath_WithActivePath_ReturnsCorrectRemaining()
    {
        // Arrange
        var path = ImmutableList.Create(
            new Position(1, 1),
            new Position(2, 2),
            new Position(3, 3));
        var actor = CreateTestActor()
            .StartMovement(path)
            .IfFail(error => throw new Exception($"Setup should succeed: {error.Message}"))
            .AdvanceMovement()
            .IfFail(error => throw new Exception($"Advance should succeed: {error.Message}"));

        // Act
        var remaining = actor.RemainingPath;

        // Assert
        remaining.Should().HaveCount(2);
        remaining.Should().Equal(new Position(2, 2), new Position(3, 3));
    }

    [Fact]
    public void RemainingPath_WithoutActivePath_ReturnsEmpty()
    {
        // Arrange
        var actor = CreateTestActor();

        // Act
        var remaining = actor.RemainingPath;

        // Assert
        remaining.Should().BeEmpty();
    }

    private static ActorEntity CreateTestActor() =>
        ActorEntity.Presets.CreateWarrior("TestActor")
            .IfFail(error => throw new Exception($"Test actor creation failed: {error.Message}"));
}
