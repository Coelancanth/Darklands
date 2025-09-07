using Xunit;
using FluentAssertions;
using LanguageExt;
using LanguageExt.Common;
using Darklands.Core.Domain.Actor;
using Darklands.Core.Domain.Grid;
using static LanguageExt.Prelude;

namespace Darklands.Core.Tests.Domain.Actor
{
    [Trait("Category", "Phase1")]
    public class ActorTests
    {
        private readonly ActorId _validActorId = ActorId.NewId();
        private readonly Health _validHealth = Health.CreateAtFullHealth(100).Match(
            Succ: h => h,
            Fail: _ => throw new InvalidOperationException("Test setup failed")
        );

        [Fact]
        public void Create_ValidParameters_ReturnsSuccess()
        {
            // Act - Position now managed separately by GridStateService
            var result = Darklands.Core.Domain.Actor.Actor.Create(_validActorId, _validHealth, "Test Actor");

            // Assert
            result.IsSucc.Should().BeTrue();
            result.Match(
                Succ: actor =>
                {
                    actor.Id.Should().Be(_validActorId);
                    actor.Health.Should().Be(_validHealth);
                    actor.Name.Should().Be("Test Actor");
                    actor.IsAlive.Should().BeTrue();
                },
                Fail: _ => throw new InvalidOperationException("Expected success")
            );
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData((string?)null)]
        public void Create_InvalidName_ReturnsError(string? invalidName)
        {
            // Act - Position removed from Actor.Create signature
            var result = Darklands.Core.Domain.Actor.Actor.Create(_validActorId, _validHealth, invalidName!);

            // Assert
            result.IsFail.Should().BeTrue();
            result.Match(
                Succ: _ => throw new InvalidOperationException("Expected failure"),
                Fail: error => error.Message.Should().Contain("Actor name cannot be empty")
            );
        }

        [Fact]
        public void Create_EmptyActorId_ReturnsError()
        {
            // Act
            var result = Darklands.Core.Domain.Actor.Actor.Create(ActorId.Empty, _validHealth, "Test Actor");

            // Assert
            result.IsFail.Should().BeTrue();
            result.Match(
                Succ: _ => throw new InvalidOperationException("Expected failure"),
                Fail: error => error.Message.Should().Contain("Actor ID cannot be empty")
            );
        }

        [Fact]
        public void Create_NameWithWhitespace_TrimsCorrectly()
        {
            // Act
            var result = Darklands.Core.Domain.Actor.Actor.Create(_validActorId, _validHealth, "  Test Actor  ");

            // Assert
            result.IsSucc.Should().BeTrue();
            result.Match(
                Succ: actor => actor.Name.Should().Be("Test Actor"),
                Fail: _ => throw new InvalidOperationException("Expected success")
            );
        }

        [Fact]
        public void CreateAtFullHealth_ValidParameters_ReturnsActorAtFullHealth()
        {
            // Act
            var result = Darklands.Core.Domain.Actor.Actor.CreateAtFullHealth(_validActorId, 80, "Rogue");

            // Assert
            result.IsSucc.Should().BeTrue();
            result.Match(
                Succ: actor =>
                {
                    actor.Health.Current.Should().Be(80);
                    actor.Health.Maximum.Should().Be(80);
                    actor.Health.IsFullHealth.Should().BeTrue();
                    actor.IsAlive.Should().BeTrue();
                    actor.Name.Should().Be("Rogue");
                },
                Fail: _ => throw new InvalidOperationException("Expected success")
            );
        }

        [Fact]
        public void CreateAtFullHealth_InvalidMaxHealth_ReturnsError()
        {
            // Act
            var result = Darklands.Core.Domain.Actor.Actor.CreateAtFullHealth(_validActorId, -10, "Invalid Actor");

            // Assert
            result.IsFail.Should().BeTrue();
            result.Match(
                Succ: _ => throw new InvalidOperationException("Expected failure"),
                Fail: error => error.Message.Should().Contain("Maximum health must be greater than 0")
            );
        }

        // MoveTo test removed - Position now managed by GridStateService, not Actor domain model

        [Fact]
        public void TakeDamage_ValidDamage_UpdatesHealthCorrectly()
        {
            // Arrange
            var actor = Darklands.Core.Domain.Actor.Actor.CreateAtFullHealth(_validActorId, 100, "Test Actor").Match(
                Succ: a => a,
                Fail: _ => throw new InvalidOperationException("Test setup failed")
            );

            // Act
            var result = actor.TakeDamage(30);

            // Assert
            result.IsSucc.Should().BeTrue();
            result.Match(
                Succ: damagedActor =>
                {
                    damagedActor.Health.Current.Should().Be(70);
                    damagedActor.Health.Maximum.Should().Be(100);
                    damagedActor.IsAlive.Should().BeTrue();
                    damagedActor.Id.Should().Be(_validActorId); // Other properties unchanged
                },
                Fail: _ => throw new InvalidOperationException("Expected success")
            );
        }

        [Fact]
        public void TakeDamage_LethalDamage_SetsActorToDead()
        {
            // Arrange
            var actor = Darklands.Core.Domain.Actor.Actor.CreateAtFullHealth(_validActorId, 50, "Test Actor").Match(
                Succ: a => a,
                Fail: _ => throw new InvalidOperationException("Test setup failed")
            );

            // Act
            var result = actor.TakeDamage(60); // More than current health

            // Assert
            result.IsSucc.Should().BeTrue();
            result.Match(
                Succ: deadActor =>
                {
                    deadActor.Health.Current.Should().Be(0);
                    deadActor.Health.IsDead.Should().BeTrue();
                    deadActor.IsAlive.Should().BeFalse();
                },
                Fail: _ => throw new InvalidOperationException("Expected success")
            );
        }

        [Fact]
        public void TakeDamage_InvalidDamage_ReturnsError()
        {
            // Arrange
            var actor = Darklands.Core.Domain.Actor.Actor.CreateAtFullHealth(_validActorId, 100, "Test Actor").Match(
                Succ: a => a,
                Fail: _ => throw new InvalidOperationException("Test setup failed")
            );

            // Act
            var result = actor.TakeDamage(-10);

            // Assert
            result.IsFail.Should().BeTrue();
            result.Match(
                Succ: _ => throw new InvalidOperationException("Expected failure"),
                Fail: error => error.Message.Should().Contain("Damage amount cannot be negative")
            );
        }

        [Fact]
        public void Heal_ValidHealing_UpdatesHealthCorrectly()
        {
            // Arrange
            var damagedHealth = Health.Create(30, 100).Match(
                Succ: h => h,
                Fail: _ => throw new InvalidOperationException("Test setup failed")
            );
            var actor = Darklands.Core.Domain.Actor.Actor.Create(_validActorId, damagedHealth, "Test Actor").Match(
                Succ: a => a,
                Fail: _ => throw new InvalidOperationException("Test setup failed")
            );

            // Act
            var result = actor.Heal(25);

            // Assert
            result.IsSucc.Should().BeTrue();
            result.Match(
                Succ: healedActor =>
                {
                    healedActor.Health.Current.Should().Be(55);
                    healedActor.Health.Maximum.Should().Be(100);
                    healedActor.IsAlive.Should().BeTrue();
                },
                Fail: _ => throw new InvalidOperationException("Expected success")
            );
        }

        [Fact]
        public void Heal_Overheal_CapsAtMaximum()
        {
            // Arrange
            var damagedHealth = Health.Create(80, 100).Match(
                Succ: h => h,
                Fail: _ => throw new InvalidOperationException("Test setup failed")
            );
            var actor = Darklands.Core.Domain.Actor.Actor.Create(_validActorId, damagedHealth, "Test Actor").Match(
                Succ: a => a,
                Fail: _ => throw new InvalidOperationException("Test setup failed")
            );

            // Act
            var result = actor.Heal(50); // More than needed

            // Assert
            result.IsSucc.Should().BeTrue();
            result.Match(
                Succ: healedActor =>
                {
                    healedActor.Health.Current.Should().Be(100);
                    healedActor.Health.IsFullHealth.Should().BeTrue();
                },
                Fail: _ => throw new InvalidOperationException("Expected success")
            );
        }

        [Fact]
        public void RestoreToFullHealth_DamagedActor_RestoresToFull()
        {
            // Arrange
            var damagedHealth = Health.Create(10, 100).Match(
                Succ: h => h,
                Fail: _ => throw new InvalidOperationException("Test setup failed")
            );
            var actor = Darklands.Core.Domain.Actor.Actor.Create(_validActorId, damagedHealth, "Test Actor").Match(
                Succ: a => a,
                Fail: _ => throw new InvalidOperationException("Test setup failed")
            );

            // Act
            var result = actor.RestoreToFullHealth();

            // Assert
            result.Health.Current.Should().Be(100);
            result.Health.Maximum.Should().Be(100);
            result.Health.IsFullHealth.Should().BeTrue();
            result.IsAlive.Should().BeTrue();
        }

        [Fact]
        public void SetToDead_LivingActor_SetsToDead()
        {
            // Arrange
            var actor = Darklands.Core.Domain.Actor.Actor.CreateAtFullHealth(_validActorId, 100, "Test Actor").Match(
                Succ: a => a,
                Fail: _ => throw new InvalidOperationException("Test setup failed")
            );

            // Act
            var result = actor.SetToDead();

            // Assert
            result.Health.Current.Should().Be(0);
            result.Health.IsDead.Should().BeTrue();
            result.IsAlive.Should().BeFalse();
        }

        [Theory]
        [InlineData(true, 100)]   // Full health = alive
        [InlineData(true, 1)]     // Any health > 0 = alive
        [InlineData(false, 0)]    // Zero health = dead
        public void IsAlive_VariousHealthStates_ReturnsCorrectStatus(bool expectedAlive, int currentHealth)
        {
            // Arrange
            var health = Health.Create(currentHealth, 100).Match(
                Succ: h => h,
                Fail: _ => throw new InvalidOperationException("Test setup failed")
            );
            var actor = Darklands.Core.Domain.Actor.Actor.Create(_validActorId, health, "Test Actor").Match(
                Succ: a => a,
                Fail: _ => throw new InvalidOperationException("Test setup failed")
            );

            // Act & Assert
            actor.IsAlive.Should().Be(expectedAlive);
        }

        [Fact]
        public void Presets_CreateWarrior_ReturnsCorrectWarrior()
        {
            // Act
            var result = Darklands.Core.Domain.Actor.Actor.Presets.CreateWarrior("Custom Warrior");

            // Assert
            result.IsSucc.Should().BeTrue();
            result.Match(
                Succ: warrior =>
                {
                    warrior.Health.Maximum.Should().Be(100);
                    warrior.Health.IsFullHealth.Should().BeTrue();
                    warrior.Name.Should().Be("Custom Warrior");
                    warrior.IsAlive.Should().BeTrue();
                },
                Fail: _ => throw new InvalidOperationException("Expected success")
            );
        }

        [Fact]
        public void Presets_CreateMage_ReturnsCorrectMage()
        {
            // Act
            var result = Darklands.Core.Domain.Actor.Actor.Presets.CreateMage();

            // Assert
            result.IsSucc.Should().BeTrue();
            result.Match(
                Succ: mage =>
                {
                    mage.Health.Maximum.Should().Be(60);
                    mage.Health.IsFullHealth.Should().BeTrue();
                    mage.Name.Should().Be("Mage");
                },
                Fail: _ => throw new InvalidOperationException("Expected success")
            );
        }

        [Fact]
        public void ToString_ValidActor_ReturnsCorrectFormat()
        {
            // Arrange
            var actor = Darklands.Core.Domain.Actor.Actor.CreateAtFullHealth(_validActorId, 100, "Hero").Match(
                Succ: a => a,
                Fail: _ => throw new InvalidOperationException("Test setup failed")
            );

            // Act
            var result = actor.ToString();

            // Assert
            result.Should().Be("Hero (Health(100/100))");
        }
    }
}
