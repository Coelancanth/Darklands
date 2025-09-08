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
    public class DummyActorTests
    {
        private readonly ActorId _validActorId = ActorId.NewId();
        private readonly Health _validHealth = Health.CreateAtFullHealth(50).Match(
            Succ: h => h,
            Fail: _ => throw new InvalidOperationException("Test setup failed")
        );

        [Fact]
        public void Create_ValidParameters_ReturnsSuccess()
        {
            // Act
            var result = DummyActor.Create(_validActorId, _validHealth, "Test Dummy");

            // Assert
            result.IsSucc.Should().BeTrue();
            result.Match(
                Succ: dummy =>
                {
                    dummy.Id.Should().Be(_validActorId);
                    dummy.Health.Should().Be(_validHealth);
                    dummy.Name.Should().Be("Test Dummy");
                    dummy.IsAlive.Should().BeTrue();
                    dummy.IsStatic.Should().BeTrue();
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
            // Act
            var result = DummyActor.Create(_validActorId, _validHealth, invalidName!);

            // Assert
            result.IsFail.Should().BeTrue();
            result.Match(
                Succ: _ => throw new InvalidOperationException("Expected failure"),
                Fail: error => error.Message.Should().Contain("Dummy name cannot be empty")
            );
        }

        [Fact]
        public void Create_EmptyActorId_ReturnsError()
        {
            // Act
            var result = DummyActor.Create(ActorId.Empty, _validHealth, "Test Dummy");

            // Assert
            result.IsFail.Should().BeTrue();
            result.Match(
                Succ: _ => throw new InvalidOperationException("Expected failure"),
                Fail: error => error.Message.Should().Contain("Dummy ID cannot be empty")
            );
        }

        [Fact]
        public void Create_NameWithWhitespace_TrimsCorrectly()
        {
            // Act
            var result = DummyActor.Create(_validActorId, _validHealth, "  Test Dummy  ");

            // Assert
            result.IsSucc.Should().BeTrue();
            result.Match(
                Succ: dummy => dummy.Name.Should().Be("Test Dummy"),
                Fail: _ => throw new InvalidOperationException("Expected success")
            );
        }

        [Fact]
        public void CreateAtFullHealth_ValidParameters_ReturnsDummyAtFullHealth()
        {
            // Act
            var result = DummyActor.CreateAtFullHealth(_validActorId, 75, "Combat Dummy");

            // Assert
            result.IsSucc.Should().BeTrue();
            result.Match(
                Succ: dummy =>
                {
                    dummy.Health.Current.Should().Be(75);
                    dummy.Health.Maximum.Should().Be(75);
                    dummy.Health.IsFullHealth.Should().BeTrue();
                    dummy.IsAlive.Should().BeTrue();
                    dummy.IsStatic.Should().BeTrue();
                    dummy.Name.Should().Be("Combat Dummy");
                },
                Fail: _ => throw new InvalidOperationException("Expected success")
            );
        }

        [Fact]
        public void CreateAtFullHealth_InvalidMaxHealth_ReturnsError()
        {
            // Act
            var result = DummyActor.CreateAtFullHealth(_validActorId, -10, "Invalid Dummy");

            // Assert
            result.IsFail.Should().BeTrue();
            result.Match(
                Succ: _ => throw new InvalidOperationException("Expected failure"),
                Fail: error => error.Message.Should().Contain("Maximum health must be greater than 0")
            );
        }

        [Fact]
        public void IsStatic_AllDummyActors_ReturnsTrue()
        {
            // Arrange
            var dummy = DummyActor.CreateAtFullHealth(_validActorId, 50, "Static Dummy").Match(
                Succ: d => d,
                Fail: _ => throw new InvalidOperationException("Test setup failed")
            );

            // Act & Assert
            dummy.IsStatic.Should().BeTrue("All dummy actors should be static targets");
        }

        [Fact]
        public void TakeDamage_ValidDamage_UpdatesHealthCorrectly()
        {
            // Arrange
            var dummy = DummyActor.CreateAtFullHealth(_validActorId, 100, "Test Dummy").Match(
                Succ: d => d,
                Fail: _ => throw new InvalidOperationException("Test setup failed")
            );

            // Act
            var result = dummy.TakeDamage(30);

            // Assert
            result.IsSucc.Should().BeTrue();
            result.Match(
                Succ: damagedDummy =>
                {
                    damagedDummy.Health.Current.Should().Be(70);
                    damagedDummy.Health.Maximum.Should().Be(100);
                    damagedDummy.IsAlive.Should().BeTrue();
                    damagedDummy.IsStatic.Should().BeTrue();
                    damagedDummy.Id.Should().Be(_validActorId); // Other properties unchanged
                },
                Fail: _ => throw new InvalidOperationException("Expected success")
            );
        }

        [Fact]
        public void TakeDamage_LethalDamage_SetsDummyToDead()
        {
            // Arrange
            var dummy = DummyActor.CreateAtFullHealth(_validActorId, 50, "Test Dummy").Match(
                Succ: d => d,
                Fail: _ => throw new InvalidOperationException("Test setup failed")
            );

            // Act
            var result = dummy.TakeDamage(60); // More than current health

            // Assert
            result.IsSucc.Should().BeTrue();
            result.Match(
                Succ: deadDummy =>
                {
                    deadDummy.Health.Current.Should().Be(0);
                    deadDummy.Health.IsDead.Should().BeTrue();
                    deadDummy.IsAlive.Should().BeFalse();
                    deadDummy.IsStatic.Should().BeTrue(); // Still static even when dead
                },
                Fail: _ => throw new InvalidOperationException("Expected success")
            );
        }

        [Fact]
        public void TakeDamage_InvalidDamage_ReturnsError()
        {
            // Arrange
            var dummy = DummyActor.CreateAtFullHealth(_validActorId, 100, "Test Dummy").Match(
                Succ: d => d,
                Fail: _ => throw new InvalidOperationException("Test setup failed")
            );

            // Act
            var result = dummy.TakeDamage(-10);

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
            var dummy = DummyActor.Create(_validActorId, damagedHealth, "Test Dummy").Match(
                Succ: d => d,
                Fail: _ => throw new InvalidOperationException("Test setup failed")
            );

            // Act
            var result = dummy.Heal(25);

            // Assert
            result.IsSucc.Should().BeTrue();
            result.Match(
                Succ: healedDummy =>
                {
                    healedDummy.Health.Current.Should().Be(55);
                    healedDummy.Health.Maximum.Should().Be(100);
                    healedDummy.IsAlive.Should().BeTrue();
                    healedDummy.IsStatic.Should().BeTrue();
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
            var dummy = DummyActor.Create(_validActorId, damagedHealth, "Test Dummy").Match(
                Succ: d => d,
                Fail: _ => throw new InvalidOperationException("Test setup failed")
            );

            // Act
            var result = dummy.Heal(50); // More than needed

            // Assert
            result.IsSucc.Should().BeTrue();
            result.Match(
                Succ: healedDummy =>
                {
                    healedDummy.Health.Current.Should().Be(100);
                    healedDummy.Health.IsFullHealth.Should().BeTrue();
                    healedDummy.IsStatic.Should().BeTrue();
                },
                Fail: _ => throw new InvalidOperationException("Expected success")
            );
        }

        [Fact]
        public void RestoreToFullHealth_DamagedDummy_RestoresToFull()
        {
            // Arrange
            var damagedHealth = Health.Create(10, 100).Match(
                Succ: h => h,
                Fail: _ => throw new InvalidOperationException("Test setup failed")
            );
            var dummy = DummyActor.Create(_validActorId, damagedHealth, "Test Dummy").Match(
                Succ: d => d,
                Fail: _ => throw new InvalidOperationException("Test setup failed")
            );

            // Act
            var result = dummy.RestoreToFullHealth();

            // Assert
            result.Health.Current.Should().Be(100);
            result.Health.Maximum.Should().Be(100);
            result.Health.IsFullHealth.Should().BeTrue();
            result.IsAlive.Should().BeTrue();
            result.IsStatic.Should().BeTrue();
        }

        [Fact]
        public void SetToDead_LivingDummy_SetsToDead()
        {
            // Arrange
            var dummy = DummyActor.CreateAtFullHealth(_validActorId, 100, "Test Dummy").Match(
                Succ: d => d,
                Fail: _ => throw new InvalidOperationException("Test setup failed")
            );

            // Act
            var result = dummy.SetToDead();

            // Assert
            result.Health.Current.Should().Be(0);
            result.Health.IsDead.Should().BeTrue();
            result.IsAlive.Should().BeFalse();
            result.IsStatic.Should().BeTrue(); // Still static even when dead
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
            var dummy = DummyActor.Create(_validActorId, health, "Test Dummy").Match(
                Succ: d => d,
                Fail: _ => throw new InvalidOperationException("Test setup failed")
            );

            // Act & Assert
            dummy.IsAlive.Should().Be(expectedAlive);
            dummy.IsStatic.Should().BeTrue(); // Always static regardless of health
        }

        [Fact]
        public void Presets_CreateCombatDummy_ReturnsCorrectDummy()
        {
            // Act
            var result = DummyActor.Presets.CreateCombatDummy("Custom Combat Target");

            // Assert
            result.IsSucc.Should().BeTrue();
            result.Match(
                Succ: dummy =>
                {
                    dummy.Health.Maximum.Should().Be(50);
                    dummy.Health.IsFullHealth.Should().BeTrue();
                    dummy.Name.Should().Be("Custom Combat Target");
                    dummy.IsAlive.Should().BeTrue();
                    dummy.IsStatic.Should().BeTrue();
                },
                Fail: _ => throw new InvalidOperationException("Expected success")
            );
        }

        [Fact]
        public void Presets_CreateTrainingDummy_ReturnsCorrectDummy()
        {
            // Act
            var result = DummyActor.Presets.CreateTrainingDummy();

            // Assert
            result.IsSucc.Should().BeTrue();
            result.Match(
                Succ: dummy =>
                {
                    dummy.Health.Maximum.Should().Be(100);
                    dummy.Health.IsFullHealth.Should().BeTrue();
                    dummy.Name.Should().Be("Training Dummy");
                    dummy.IsStatic.Should().BeTrue();
                },
                Fail: _ => throw new InvalidOperationException("Expected success")
            );
        }

        [Fact]
        public void Presets_CreateWeakDummy_ReturnsCorrectDummy()
        {
            // Act
            var result = DummyActor.Presets.CreateWeakDummy();

            // Assert
            result.IsSucc.Should().BeTrue();
            result.Match(
                Succ: dummy =>
                {
                    dummy.Health.Maximum.Should().Be(25);
                    dummy.Health.IsFullHealth.Should().BeTrue();
                    dummy.Name.Should().Be("Weak Dummy");
                    dummy.IsStatic.Should().BeTrue();
                },
                Fail: _ => throw new InvalidOperationException("Expected success")
            );
        }

        [Fact]
        public void ToString_ValidDummy_ReturnsCorrectFormat()
        {
            // Arrange
            var dummy = DummyActor.CreateAtFullHealth(_validActorId, 50, "Target Dummy").Match(
                Succ: d => d,
                Fail: _ => throw new InvalidOperationException("Test setup failed")
            );

            // Act
            var result = dummy.ToString();

            // Assert
            result.Should().Be("Target Dummy (Health(50/50)) [Static]");
        }

        [Fact]
        public void ToString_DamagedDummy_ReturnsCorrectFormat()
        {
            // Arrange
            var damagedHealth = Health.Create(15, 50).Match(
                Succ: h => h,
                Fail: _ => throw new InvalidOperationException("Test setup failed")
            );
            var dummy = DummyActor.Create(_validActorId, damagedHealth, "Damaged Dummy").Match(
                Succ: d => d,
                Fail: _ => throw new InvalidOperationException("Test setup failed")
            );

            // Act
            var result = dummy.ToString();

            // Assert
            result.Should().Be("Damaged Dummy (Health(15/50)) [Static]");
        }
    }
}
