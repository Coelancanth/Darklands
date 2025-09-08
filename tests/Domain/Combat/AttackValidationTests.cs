using FluentAssertions;
using Xunit;
using Darklands.Core.Domain.Combat;
using Darklands.Core.Domain.Grid;
using static LanguageExt.Prelude;

namespace Darklands.Core.Tests.Domain.Combat;

/// <summary>
/// Tests for attack validation domain logic.
/// Covers adjacency rules, target validation, and attack result modeling.
/// Following TDD+VSA Comprehensive Development Workflow.
/// </summary>
public class AttackValidationTests
{
    private readonly ActorId _attackerId = ActorId.NewId();
    private readonly ActorId _targetId = ActorId.NewId();
    private readonly Position _attackerPosition = new(2, 2);
    private readonly Position _adjacentPosition = new(2, 3); // Adjacent vertically
    private readonly Position _diagonalPosition = new(3, 3); // Adjacent diagonally  
    private readonly Position _distantPosition = new(5, 5); // Not adjacent

    [Fact]
    public void ValidateAttack_AdjacentTarget_ReturnsSuccess()
    {
        // Arrange
        var attackValidation = AttackValidation.Create(
            _attackerId,
            _attackerPosition,
            _targetId,
            _adjacentPosition,
            isTargetAlive: true);

        // Act & Assert
        attackValidation.IsSucc.Should().BeTrue();
    }

    [Fact]
    public void ValidateAttack_DiagonallyAdjacentTarget_ReturnsSuccess()
    {
        // Arrange
        var attackValidation = AttackValidation.Create(
            _attackerId,
            _attackerPosition,
            _targetId,
            _diagonalPosition,
            isTargetAlive: true);

        // Act & Assert
        attackValidation.IsSucc.Should().BeTrue();
    }

    [Theory]
    [InlineData(0, 2)] // Two positions away horizontally
    [InlineData(4, 2)] // Two positions away horizontally  
    [InlineData(2, 0)] // Two positions away vertically
    [InlineData(2, 4)] // Two positions away vertically
    [InlineData(0, 0)] // Two positions away diagonally
    public void ValidateAttack_NonAdjacentTarget_ReturnsFailure(int targetX, int targetY)
    {
        // Arrange
        var targetPosition = new Position(targetX, targetY);
        var attackValidation = AttackValidation.Create(
            _attackerId,
            _attackerPosition,
            _targetId,
            targetPosition,
            isTargetAlive: true);

        // Act & Assert
        attackValidation.IsFail.Should().BeTrue();
        attackValidation.IfFail(error => error.Message.Should().Contain("not adjacent"));
    }

    [Fact]
    public void ValidateAttack_DeadTarget_ReturnsFailure()
    {
        // Arrange
        var attackValidation = AttackValidation.Create(
            _attackerId,
            _attackerPosition,
            _targetId,
            _adjacentPosition,
            isTargetAlive: false);

        // Act & Assert
        attackValidation.IsFail.Should().BeTrue();
        attackValidation.IfFail(error => error.Message.Should().Contain("is dead"));
    }

    [Fact]
    public void ValidateAttack_SelfTarget_ReturnsFailure()
    {
        // Arrange - attacker targeting themselves
        var attackValidation = AttackValidation.Create(
            _attackerId,
            _attackerPosition,
            _attackerId, // Same as attacker
            _attackerPosition,
            isTargetAlive: true);

        // Act & Assert
        attackValidation.IsFail.Should().BeTrue();
        attackValidation.IfFail(error => error.Message.Should().Contain("cannot attack itself"));
    }

    [Fact]
    public void ValidateAttack_EmptyActorIds_ReturnsFailure()
    {
        // Arrange
        var attackValidation = AttackValidation.Create(
            ActorId.Empty,
            _attackerPosition,
            _targetId,
            _adjacentPosition,
            isTargetAlive: true);

        // Act & Assert
        attackValidation.IsFail.Should().BeTrue();
        attackValidation.IfFail(error => error.Message.Should().Contain("Invalid attacker"));
    }
}
