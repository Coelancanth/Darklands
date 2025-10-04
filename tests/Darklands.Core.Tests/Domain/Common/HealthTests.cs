using Darklands.Core.Domain.Common;
using FluentAssertions;
using Xunit;

namespace Darklands.Core.Tests.Domain.Common;

[Trait("Category", "Common")]
[Trait("Category", "Unit")]
public class HealthTests
{
    #region Create Tests

    [Fact]
    public void Create_ValidValues_ShouldReturnSuccess()
    {
        // Act
        var result = Health.Create(current: 50, maximum: 100);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Current.Should().Be(50);
        result.Value.Maximum.Should().Be(100);
        result.Value.Percentage.Should().BeApproximately(0.5f, 0.001f);
        result.Value.IsDepleted.Should().BeFalse();
    }

    [Fact]
    public void Create_ZeroMaximum_ShouldThrowArgumentOutOfRangeException()
    {
        // PROGRAMMER ERROR: No game should ever have zero max health
        // This is a contract violation, not a business rule

        // Act
        var act = () => Health.Create(current: 0, maximum: 0);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("maximum")
            .WithMessage("*must be positive*");
    }

    [Fact]
    public void Create_NegativeMaximum_ShouldThrowArgumentOutOfRangeException()
    {
        // PROGRAMMER ERROR: Contract violation

        // Act
        var act = () => Health.Create(current: 10, maximum: -50);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("maximum");
    }

    [Fact]
    public void Create_NegativeCurrent_ShouldReturnFailure()
    {
        // DOMAIN ERROR: Business validation - health cannot be negative

        // Act
        var result = Health.Create(current: -10, maximum: 100);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("negative");
    }

    [Fact]
    public void Create_CurrentExceedsMaximum_ShouldReturnFailure()
    {
        // DOMAIN ERROR: Business validation - can't have more than max health

        // Act
        var result = Health.Create(current: 150, maximum: 100);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("exceed");
    }

    [Fact]
    public void Create_CurrentEqualsMaximum_ShouldReturnSuccess()
    {
        // Full health is valid

        // Act
        var result = Health.Create(current: 100, maximum: 100);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Percentage.Should().Be(1.0f);
        result.Value.IsDepleted.Should().BeFalse();
    }

    [Fact]
    public void Create_CurrentIsZero_ShouldReturnSuccessAndBeDepleted()
    {
        // Zero health is valid (actor is dead)

        // Act
        var result = Health.Create(current: 0, maximum: 100);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsDepleted.Should().BeTrue();
        result.Value.Percentage.Should().Be(0);
    }

    #endregion

    #region Reduce Tests

    [Fact]
    public void Reduce_PartialDamage_ShouldReduceHealthCorrectly()
    {
        // Arrange
        var health = Health.Create(50, 100).Value;

        // Act
        var result = health.Reduce(20);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Current.Should().Be(30);
        result.Value.Maximum.Should().Be(100);
        result.Value.IsDepleted.Should().BeFalse();
    }

    [Fact]
    public void Reduce_LethalDamage_ShouldClampToZero()
    {
        // WHY: Lethal damage should reduce health to exactly zero, not negative

        // Arrange
        var health = Health.Create(50, 100).Value;

        // Act
        var result = health.Reduce(70); // More damage than current health

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Current.Should().Be(0);
        result.Value.IsDepleted.Should().BeTrue();
    }

    [Fact]
    public void Reduce_ExactlyCurrentHealth_ShouldResultInZero()
    {
        // Arrange
        var health = Health.Create(50, 100).Value;

        // Act
        var result = health.Reduce(50);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Current.Should().Be(0);
        result.Value.IsDepleted.Should().BeTrue();
    }

    [Fact]
    public void Reduce_NegativeAmount_ShouldReturnFailure()
    {
        // DOMAIN ERROR: Negative damage is unclear intent

        // Arrange
        var health = Health.Create(50, 100).Value;

        // Act
        var result = health.Reduce(-10);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("negative");
    }

    [Fact]
    public void Reduce_ZeroDamage_ShouldReturnUnchangedHealth()
    {
        // WHY: Zero damage attacks are valid (status effects without damage)

        // Arrange
        var health = Health.Create(50, 100).Value;

        // Act
        var result = health.Reduce(0);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Current.Should().Be(50);
    }

    [Fact]
    public void Reduce_ShouldReturnNewInstance()
    {
        // IMMUTABILITY: Health is immutable - operations return new instances

        // Arrange
        var original = Health.Create(50, 100).Value;

        // Act
        var reduced = original.Reduce(10).Value;

        // Assert
        reduced.Should().NotBeSameAs(original);
        original.Current.Should().Be(50); // Original unchanged
        reduced.Current.Should().Be(40);
    }

    #endregion

    #region Increase Tests

    [Fact]
    public void Increase_PartialHeal_ShouldIncreaseHealthCorrectly()
    {
        // Arrange
        var health = Health.Create(30, 100).Value;

        // Act
        var result = health.Increase(20);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Current.Should().Be(50);
        result.Value.Maximum.Should().Be(100);
    }

    [Fact]
    public void Increase_OverhealAmount_ShouldClampToMaximum()
    {
        // WHY: Healing beyond maximum should cap at maximum, not exceed

        // Arrange
        var health = Health.Create(80, 100).Value;

        // Act
        var result = health.Increase(30); // Would be 110, clamped to 100

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Current.Should().Be(100);
        result.Value.Percentage.Should().Be(1.0f);
    }

    [Fact]
    public void Increase_ExactlyToMaximum_ShouldReturnMaximum()
    {
        // Arrange
        var health = Health.Create(70, 100).Value;

        // Act
        var result = health.Increase(30);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Current.Should().Be(100);
    }

    [Fact]
    public void Increase_NegativeAmount_ShouldReturnFailure()
    {
        // DOMAIN ERROR: Negative heal is unclear intent

        // Arrange
        var health = Health.Create(50, 100).Value;

        // Act
        var result = health.Increase(-10);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("negative");
    }

    [Fact]
    public void Increase_ZeroAmount_ShouldReturnUnchangedHealth()
    {
        // Arrange
        var health = Health.Create(50, 100).Value;

        // Act
        var result = health.Increase(0);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Current.Should().Be(50);
    }

    [Fact]
    public void Increase_FromZeroHealth_ShouldRevive()
    {
        // WHY: Resurrection mechanics - healing from 0 HP should work

        // Arrange
        var health = Health.Create(0, 100).Value;

        // Act
        var result = health.Increase(50);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Current.Should().Be(50);
        result.Value.IsDepleted.Should().BeFalse();
    }

    [Fact]
    public void Increase_ShouldReturnNewInstance()
    {
        // IMMUTABILITY: Health is immutable

        // Arrange
        var original = Health.Create(50, 100).Value;

        // Act
        var healed = original.Increase(20).Value;

        // Assert
        healed.Should().NotBeSameAs(original);
        original.Current.Should().Be(50); // Original unchanged
        healed.Current.Should().Be(70);
    }

    #endregion

    #region Property Tests

    [Theory]
    [InlineData(0, 100, 0.0f)]
    [InlineData(25, 100, 0.25f)]
    [InlineData(50, 100, 0.5f)]
    [InlineData(75, 100, 0.75f)]
    [InlineData(100, 100, 1.0f)]
    public void Percentage_ShouldCalculateCorrectly(float current, float maximum, float expectedPercentage)
    {
        // Arrange
        var health = Health.Create(current, maximum).Value;

        // Act
        var percentage = health.Percentage;

        // Assert
        percentage.Should().BeApproximately(expectedPercentage, 0.001f);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(0.01f, false)]
    [InlineData(1, false)]
    [InlineData(50, false)]
    [InlineData(100, false)]
    public void IsDepleted_ShouldReturnCorrectValue(float current, bool expectedDepleted)
    {
        // Arrange
        var health = Health.Create(current, 100).Value;

        // Act
        var isDepleted = health.IsDepleted;

        // Assert
        isDepleted.Should().Be(expectedDepleted);
    }

    #endregion
}