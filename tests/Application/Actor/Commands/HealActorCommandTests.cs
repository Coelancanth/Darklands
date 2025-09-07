using Xunit;
using FluentAssertions;
using Darklands.Core.Application.Actor.Commands;
using Darklands.Core.Domain.Grid;

namespace Darklands.Core.Tests.Application.Actor.Commands
{
    [Trait("Category", "Phase1")]
    public class HealActorCommandTests
    {
        private readonly ActorId _validActorId = ActorId.NewId();

        [Fact]
        public void Create_ValidParameters_CreatesCommand()
        {
            // Act
            var command = HealActorCommand.Create(_validActorId, 30, "Healing Potion");

            // Assert
            command.ActorId.Should().Be(_validActorId);
            command.HealAmount.Should().Be(30);
            command.Source.Should().Be("Healing Potion");
        }

        [Fact]
        public void Create_WithoutSource_CreatesCommandWithNullSource()
        {
            // Act
            var command = HealActorCommand.Create(_validActorId, 20);

            // Assert
            command.ActorId.Should().Be(_validActorId);
            command.HealAmount.Should().Be(20);
            command.Source.Should().BeNull();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(50)]
        [InlineData(9999)]
        public void Create_ValidHealAmounts_CreatesCommand(int healAmount)
        {
            // Act
            var command = HealActorCommand.Create(_validActorId, healAmount);

            // Assert
            command.HealAmount.Should().Be(healAmount);
        }

        [Fact]
        public void Create_RecordEquality_WorksCorrectly()
        {
            // Arrange
            var command1 = HealActorCommand.Create(_validActorId, 40, "Spell");
            var command2 = HealActorCommand.Create(_validActorId, 40, "Spell");
            var command3 = HealActorCommand.Create(_validActorId, 50, "Spell");

            // Assert
            command1.Should().Be(command2); // Same values = equal
            command1.Should().NotBe(command3); // Different heal amount = not equal
        }

        [Fact]
        public void Create_EmptyActorId_StillCreatesCommand()
        {
            // Note: Command creation doesn't validate - that's handler's responsibility
            // This test documents the current behavior

            // Act
            var command = HealActorCommand.Create(ActorId.Empty, 25);

            // Assert
            command.ActorId.Should().Be(ActorId.Empty);
            command.HealAmount.Should().Be(25);
        }

        [Fact]
        public void Create_NegativeHealAmount_StillCreatesCommand()
        {
            // Note: Command creation doesn't validate - that's handler's responsibility
            // This test documents the current behavior

            // Act
            var command = HealActorCommand.Create(_validActorId, -15);

            // Assert
            command.ActorId.Should().Be(_validActorId);
            command.HealAmount.Should().Be(-15);
        }
    }
}
