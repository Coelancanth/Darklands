using Xunit;
using FluentAssertions;
using Darklands.Core.Application.Actor.Commands;
using Darklands.Core.Domain.Grid;

namespace Darklands.Core.Tests.Application.Actor.Commands
{
    [Trait("Category", "Phase1")]
    public class DamageActorCommandTests
    {
        private readonly ActorId _validActorId = ActorId.NewId();

        [Fact]
        public void Create_ValidParameters_CreatesCommand()
        {
            // Act
            var command = DamageActorCommand.Create(_validActorId, 25, "Sword Attack");

            // Assert
            command.ActorId.Should().Be(_validActorId);
            command.Damage.Should().Be(25);
            command.Source.Should().Be("Sword Attack");
        }

        [Fact]
        public void Create_WithoutSource_CreatesCommandWithNullSource()
        {
            // Act
            var command = DamageActorCommand.Create(_validActorId, 15);

            // Assert
            command.ActorId.Should().Be(_validActorId);
            command.Damage.Should().Be(15);
            command.Source.Should().BeNull();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(9999)]
        public void Create_ValidDamageAmounts_CreatesCommand(int damage)
        {
            // Act
            var command = DamageActorCommand.Create(_validActorId, damage);

            // Assert
            command.Damage.Should().Be(damage);
        }

        [Fact]
        public void Create_RecordEquality_WorksCorrectly()
        {
            // Arrange
            var command1 = DamageActorCommand.Create(_validActorId, 25, "Test");
            var command2 = DamageActorCommand.Create(_validActorId, 25, "Test");
            var command3 = DamageActorCommand.Create(_validActorId, 30, "Test");

            // Assert
            command1.Should().Be(command2); // Same values = equal
            command1.Should().NotBe(command3); // Different damage = not equal
        }

        [Fact]
        public void Create_EmptyActorId_StillCreatesCommand()
        {
            // Note: Command creation doesn't validate - that's handler's responsibility
            // This test documents the current behavior
            
            // Act
            var command = DamageActorCommand.Create(ActorId.Empty, 25);

            // Assert
            command.ActorId.Should().Be(ActorId.Empty);
            command.Damage.Should().Be(25);
        }

        [Fact]
        public void Create_NegativeDamage_StillCreatesCommand()
        {
            // Note: Command creation doesn't validate - that's handler's responsibility
            // This test documents the current behavior
            
            // Act
            var command = DamageActorCommand.Create(_validActorId, -10);

            // Assert
            command.ActorId.Should().Be(_validActorId);
            command.Damage.Should().Be(-10);
        }
    }
}