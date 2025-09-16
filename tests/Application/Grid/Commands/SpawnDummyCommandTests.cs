using Xunit;
using FluentAssertions;
using Darklands.Application.Grid.Commands;
using Darklands.Domain.Grid;

namespace Darklands.Core.Tests.Application.Grid.Commands
{
    [Trait("Category", "Phase2")]
    public class SpawnDummyCommandTests
    {
        private readonly Position _validPosition = new Position(5, 5);

        [Fact]
        public void Create_ValidParameters_CreatesCommand()
        {
            // Act
            var result = SpawnDummyCommand.Create(_validPosition, 100, "Test Dummy");

            // Assert
            result.Position.Should().Be(_validPosition);
            result.MaxHealth.Should().Be(100);
            result.Name.Should().Be("Test Dummy");
        }

        [Theory]
        [InlineData(1)]
        [InlineData(50)]
        [InlineData(100)]
        [InlineData(500)]
        public void Create_ValidMaxHealth_CreatesCommand(int maxHealth)
        {
            // Act
            var result = SpawnDummyCommand.Create(_validPosition, maxHealth, "Test Dummy");

            // Assert
            result.MaxHealth.Should().Be(maxHealth);
            result.Position.Should().Be(_validPosition);
            result.Name.Should().Be("Test Dummy");
        }

        [Theory]
        [InlineData("Combat Dummy")]
        [InlineData("Training Target")]
        [InlineData("Test Dummy 123")]
        [InlineData("  Dummy with spaces  ")]
        public void Create_ValidNames_CreatesCommand(string name)
        {
            // Act
            var result = SpawnDummyCommand.Create(_validPosition, 50, name);

            // Assert
            result.Name.Should().Be(name);
            result.Position.Should().Be(_validPosition);
            result.MaxHealth.Should().Be(50);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-10)]
        [InlineData(-100)]
        public void Create_InvalidMaxHealth_StillCreatesCommand(int invalidMaxHealth)
        {
            // Act - Command creation doesn't validate, domain model does
            var result = SpawnDummyCommand.Create(_validPosition, invalidMaxHealth, "Test Dummy");

            // Assert - Command should be created, validation happens in domain/handler
            result.MaxHealth.Should().Be(invalidMaxHealth);
            result.Position.Should().Be(_validPosition);
            result.Name.Should().Be("Test Dummy");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Create_InvalidName_StillCreatesCommand(string? invalidName)
        {
            // Act - Command creation doesn't validate, domain model does
            var result = SpawnDummyCommand.Create(_validPosition, 50, invalidName!);

            // Assert - Command should be created, validation happens in domain/handler
            result.Name.Should().Be(invalidName);
            result.Position.Should().Be(_validPosition);
            result.MaxHealth.Should().Be(50);
        }

        [Fact]
        public void CreateCombatDummy_ValidPosition_CreatesCorrectCommand()
        {
            // Act
            var result = SpawnDummyCommand.CreateCombatDummy(_validPosition);

            // Assert
            result.Position.Should().Be(_validPosition);
            result.MaxHealth.Should().Be(50);
            result.Name.Should().Be("Combat Dummy");
        }

        [Fact]
        public void CreateTrainingDummy_ValidPosition_CreatesCorrectCommand()
        {
            // Act
            var result = SpawnDummyCommand.CreateTrainingDummy(_validPosition);

            // Assert
            result.Position.Should().Be(_validPosition);
            result.MaxHealth.Should().Be(100);
            result.Name.Should().Be("Training Dummy");
        }

        [Fact]
        public void RecordEquality_SameValues_AreEqual()
        {
            // Arrange
            var command1 = SpawnDummyCommand.Create(_validPosition, 75, "Test Dummy");
            var command2 = SpawnDummyCommand.Create(_validPosition, 75, "Test Dummy");

            // Act & Assert
            command1.Should().Be(command2);
            command1.GetHashCode().Should().Be(command2.GetHashCode());
        }

        [Fact]
        public void RecordEquality_DifferentValues_AreNotEqual()
        {
            // Arrange
            var command1 = SpawnDummyCommand.Create(_validPosition, 50, "Dummy A");
            var command2 = SpawnDummyCommand.Create(_validPosition, 75, "Dummy B");

            // Act & Assert
            command1.Should().NotBe(command2);
        }

        [Fact]
        public void ToString_ValidCommand_ReturnsCorrectFormat()
        {
            // Arrange
            var command = SpawnDummyCommand.Create(_validPosition, 50, "Test Dummy");

            // Act
            var result = command.ToString();

            // Assert
            result.Should().Contain("SpawnDummyCommand");
            result.Should().Contain(_validPosition.ToString());
            result.Should().Contain("50");
            result.Should().Contain("Test Dummy");
        }
    }
}
