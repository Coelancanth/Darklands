using FluentAssertions;
using Xunit;
using Darklands.Core.Application.Combat.Commands;
using Darklands.Core.Domain.Combat;
using Darklands.Core.Domain.Grid;
using static LanguageExt.Prelude;

namespace Darklands.Core.Tests.Application.Combat.Commands;

/// <summary>
/// Tests for ExecuteAttackCommand - validates command creation and properties.
/// Covers factory methods, validation rules, and record equality.
/// Following TDD+VSA Comprehensive Development Workflow.
/// </summary>
public class ExecuteAttackCommandTests
{
    private readonly ActorId _validAttackerId = ActorId.NewId();
    private readonly ActorId _validTargetId = ActorId.NewId();
    private readonly CombatAction _validCombatAction = CombatAction.Common.SwordSlash;

    [Fact]
    public void Create_ValidParameters_CreatesCommand()
    {
        // Arrange & Act
        var command = ExecuteAttackCommand.Create(_validAttackerId, _validTargetId, _validCombatAction);

        // Assert
        command.AttackerId.Should().Be(_validAttackerId);
        command.TargetId.Should().Be(_validTargetId);
        command.CombatAction.Should().Be(_validCombatAction);
    }

    [Fact]
    public void Create_WithoutCombatAction_UsesDefaultSwordSlash()
    {
        // Arrange & Act
        var command = ExecuteAttackCommand.Create(_validAttackerId, _validTargetId);

        // Assert
        command.AttackerId.Should().Be(_validAttackerId);
        command.TargetId.Should().Be(_validTargetId);
        command.CombatAction.Should().Be(CombatAction.Common.SwordSlash);
    }

    [Fact]
    public void Create_EmptyAttackerId_StillCreatesCommand()
    {
        // Arrange & Act
        var command = ExecuteAttackCommand.Create(ActorId.Empty, _validTargetId, _validCombatAction);

        // Assert
        command.AttackerId.Should().Be(ActorId.Empty);
        command.TargetId.Should().Be(_validTargetId);
        command.CombatAction.Should().Be(_validCombatAction);
    }

    [Fact]
    public void Create_EmptyTargetId_StillCreatesCommand()
    {
        // Arrange & Act  
        var command = ExecuteAttackCommand.Create(_validAttackerId, ActorId.Empty, _validCombatAction);

        // Assert
        command.AttackerId.Should().Be(_validAttackerId);
        command.TargetId.Should().Be(ActorId.Empty);
        command.CombatAction.Should().Be(_validCombatAction);
    }

    [Fact]
    public void Create_RecordEquality_WorksCorrectly()
    {
        // Arrange
        var command1 = ExecuteAttackCommand.Create(_validAttackerId, _validTargetId, _validCombatAction);
        var command2 = ExecuteAttackCommand.Create(_validAttackerId, _validTargetId, _validCombatAction);
        var command3 = ExecuteAttackCommand.Create(ActorId.NewId(), _validTargetId, _validCombatAction);

        // Act & Assert
        command1.Should().Be(command2);
        command1.Should().NotBe(command3);
    }

    [Theory]
    [InlineData("SwordSlash")]
    [InlineData("DaggerStab")]
    [InlineData("AxeChop")]
    public void Create_DifferentCombatActions_CreatesCorrectCommand(string actionName)
    {
        // Arrange
        var combatAction = actionName switch
        {
            "SwordSlash" => CombatAction.Common.SwordSlash,
            "DaggerStab" => CombatAction.Common.DaggerStab,
            "AxeChop" => CombatAction.Common.AxeChop,
            _ => throw new ArgumentException($"Unknown action: {actionName}")
        };

        // Act
        var command = ExecuteAttackCommand.Create(_validAttackerId, _validTargetId, combatAction);

        // Assert
        command.CombatAction.Should().Be(combatAction);
        command.CombatAction.Name.Should().Contain(actionName.Replace("Slash", " Slash").Replace("Stab", " Stab").Replace("Chop", " Chop"));
    }
}