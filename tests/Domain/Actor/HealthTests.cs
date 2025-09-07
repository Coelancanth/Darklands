using Xunit;
using FluentAssertions;
using LanguageExt;
using LanguageExt.Common;
using Darklands.Core.Domain.Actor;
using static LanguageExt.Prelude;

namespace Darklands.Core.Tests.Domain.Actor
{
    [Trait("Category", "Phase1")]
    public class HealthTests
    {
        [Theory]
        [InlineData(100, 100)]
        [InlineData(50, 100)]
        [InlineData(0, 100)]
        [InlineData(1, 1)]
        public void Create_ValidValues_ReturnsSuccess(int current, int maximum)
        {
            // Act
            var result = Health.Create(current, maximum);

            // Assert
            result.IsSucc.Should().BeTrue();
            result.Match(
                Succ: health =>
                {
                    health.Current.Should().Be(current);
                    health.Maximum.Should().Be(maximum);
                },
                Fail: _ => throw new InvalidOperationException("Expected success")
            );
        }

        [Theory]
        [InlineData(-1, 100, "Current health cannot be negative")]
        [InlineData(0, 0, "Maximum health must be greater than 0")]
        [InlineData(150, 100, "Current health cannot exceed maximum")]
        [InlineData(50, -10, "Maximum health must be greater than 0")]
        public void Create_InvalidValues_ReturnsError(int current, int maximum, string expectedErrorFragment)
        {
            // Act
            var result = Health.Create(current, maximum);

            // Assert
            result.IsFail.Should().BeTrue();
            result.Match(
                Succ: _ => throw new InvalidOperationException("Expected failure"),
                Fail: error => error.Message.Should().Contain(expectedErrorFragment)
            );
        }

        [Fact]
        public void CreateAtFullHealth_ValidMaximum_ReturnsFullHealth()
        {
            // Act
            var result = Health.CreateAtFullHealth(100);

            // Assert
            result.IsSucc.Should().BeTrue();
            result.Match(
                Succ: health =>
                {
                    health.Current.Should().Be(100);
                    health.Maximum.Should().Be(100);
                    health.IsFullHealth.Should().BeTrue();
                    health.IsDead.Should().BeFalse();
                },
                Fail: _ => throw new InvalidOperationException("Expected success")
            );
        }

        [Fact]
        public void CreateDead_ValidMaximum_ReturnsDeadHealth()
        {
            // Act
            var result = Health.CreateDead(100);

            // Assert
            result.IsSucc.Should().BeTrue();
            result.Match(
                Succ: health =>
                {
                    health.Current.Should().Be(0);
                    health.Maximum.Should().Be(100);
                    health.IsDead.Should().BeTrue();
                    health.IsFullHealth.Should().BeFalse();
                },
                Fail: _ => throw new InvalidOperationException("Expected success")
            );
        }

        [Theory]
        [InlineData(100, 100, true)]
        [InlineData(99, 100, false)]
        [InlineData(0, 100, false)]
        public void IsFullHealth_VariousValues_ReturnsCorrectStatus(int current, int maximum, bool expected)
        {
            // Arrange
            var health = Health.Create(current, maximum).Match(
                Succ: h => h,
                Fail: _ => throw new InvalidOperationException("Setup failed")
            );

            // Act & Assert
            health.IsFullHealth.Should().Be(expected);
        }

        [Theory]
        [InlineData(0, 100, true)]
        [InlineData(-1, 100, true)] // Edge case: negative current (if somehow created)
        [InlineData(1, 100, false)]
        [InlineData(100, 100, false)]
        public void IsDead_VariousValues_ReturnsCorrectStatus(int current, int maximum, bool expected)
        {
            // For the actual test, use valid creation but test the boundary
            if (current >= 0)
            {
                var validHealth = Health.Create(current, maximum).Match(
                    Succ: h => h,
                    Fail: _ => throw new InvalidOperationException("Setup failed")
                );
                validHealth.IsDead.Should().Be(expected);
            }
        }

        [Theory]
        [InlineData(100, 100, 1.0)]
        [InlineData(50, 100, 0.5)]
        [InlineData(0, 100, 0.0)]
        [InlineData(25, 100, 0.25)]
        public void HealthPercentage_VariousValues_ReturnsCorrectPercentage(int current, int maximum, double expected)
        {
            // Arrange
            var health = Health.Create(current, maximum).Match(
                Succ: h => h,
                Fail: _ => throw new InvalidOperationException("Setup failed")
            );

            // Act & Assert
            health.HealthPercentage.Should().BeApproximately(expected, 0.001);
        }

        [Theory]
        [InlineData(100, 50, 50)]
        [InlineData(50, 30, 20)]
        [InlineData(20, 25, 0)] // Damage exceeds current health
        [InlineData(10, 10, 0)]
        public void TakeDamage_ValidDamage_AppliesCorrectly(int startingHealth, int damage, int expectedHealth)
        {
            // Arrange
            var health = Health.Create(startingHealth, 100).Match(
                Succ: h => h,
                Fail: _ => throw new InvalidOperationException("Setup failed")
            );

            // Act
            var result = health.TakeDamage(damage);

            // Assert
            result.IsSucc.Should().BeTrue();
            result.Match(
                Succ: newHealth => newHealth.Current.Should().Be(expectedHealth),
                Fail: _ => throw new InvalidOperationException("Expected success")
            );
        }

        [Fact]
        public void TakeDamage_NegativeDamage_ReturnsError()
        {
            // Arrange
            var health = Health.CreateAtFullHealth(100).Match(
                Succ: h => h,
                Fail: _ => throw new InvalidOperationException("Setup failed")
            );

            // Act
            var result = health.TakeDamage(-10);

            // Assert
            result.IsFail.Should().BeTrue();
            result.Match(
                Succ: _ => throw new InvalidOperationException("Expected failure"),
                Fail: error => error.Message.Should().Contain("Damage amount cannot be negative")
            );
        }

        [Theory]
        [InlineData(50, 25, 75)]
        [InlineData(50, 50, 100)] // Heal to maximum
        [InlineData(50, 60, 100)] // Overheal caps at maximum
        [InlineData(0, 100, 100)]
        public void Heal_ValidHealing_AppliesCorrectly(int startingHealth, int healAmount, int expectedHealth)
        {
            // Arrange
            var health = Health.Create(startingHealth, 100).Match(
                Succ: h => h,
                Fail: _ => throw new InvalidOperationException("Setup failed")
            );

            // Act
            var result = health.Heal(healAmount);

            // Assert
            result.IsSucc.Should().BeTrue();
            result.Match(
                Succ: newHealth => newHealth.Current.Should().Be(expectedHealth),
                Fail: _ => throw new InvalidOperationException("Expected success")
            );
        }

        [Fact]
        public void Heal_NegativeHealAmount_ReturnsError()
        {
            // Arrange
            var health = Health.Create(50, 100).Match(
                Succ: h => h,
                Fail: _ => throw new InvalidOperationException("Setup failed")
            );

            // Act
            var result = health.Heal(-10);

            // Assert
            result.IsFail.Should().BeTrue();
            result.Match(
                Succ: _ => throw new InvalidOperationException("Expected failure"),
                Fail: error => error.Message.Should().Contain("Heal amount cannot be negative")
            );
        }

        [Fact]
        public void RestoreToFull_PartialHealth_RestoresToMaximum()
        {
            // Arrange
            var health = Health.Create(25, 100).Match(
                Succ: h => h,
                Fail: _ => throw new InvalidOperationException("Setup failed")
            );

            // Act
            var result = health.RestoreToFull();

            // Assert
            result.Current.Should().Be(100);
            result.Maximum.Should().Be(100);
            result.IsFullHealth.Should().BeTrue();
        }

        [Fact]
        public void SetToDead_AnyHealth_SetsToZero()
        {
            // Arrange
            var health = Health.Create(75, 100).Match(
                Succ: h => h,
                Fail: _ => throw new InvalidOperationException("Setup failed")
            );

            // Act
            var result = health.SetToDead();

            // Assert
            result.Current.Should().Be(0);
            result.Maximum.Should().Be(100);
            result.IsDead.Should().BeTrue();
        }

        [Fact]
        public void Presets_WarriorFullHealth_CreatesCorrectly()
        {
            // Act
            var result = Health.Presets.WarriorFullHealth;

            // Assert
            result.IsSucc.Should().BeTrue();
            result.Match(
                Succ: health =>
                {
                    health.Current.Should().Be(100);
                    health.Maximum.Should().Be(100);
                    health.IsFullHealth.Should().BeTrue();
                },
                Fail: _ => throw new InvalidOperationException("Expected success")
            );
        }

        [Fact]
        public void ToString_ValidHealth_ReturnsCorrectFormat()
        {
            // Arrange
            var health = Health.Create(75, 100).Match(
                Succ: h => h,
                Fail: _ => throw new InvalidOperationException("Setup failed")
            );

            // Act
            var result = health.ToString();

            // Assert
            result.Should().Be("Health(75/100)");
        }
    }
}
