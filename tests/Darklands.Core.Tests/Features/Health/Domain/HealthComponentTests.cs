using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Health.Domain;
using FluentAssertions;
using Xunit;
using HealthValue = Darklands.Core.Domain.Common.Health;

namespace Darklands.Core.Tests.Features.Health.Domain;

[Trait("Category", "Health")]
[Trait("Category", "Unit")]
public class HealthComponentTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_ValidParameters_ShouldCreateComponent()
    {
        // Arrange
        var actorId = ActorId.NewId();
        var health = HealthValue.Create(50, 100).Value;

        // Act
        var component = new HealthComponent(actorId, health);

        // Assert
        component.OwnerId.Should().Be(actorId);
        component.CurrentHealth.Should().Be(health);
        component.IsAlive.Should().BeTrue();
    }

    [Fact]
    public void Constructor_EmptyActorId_ShouldThrowArgumentException()
    {
        // PROGRAMMER ERROR: Component must belong to a valid actor

        // Arrange
        var emptyId = new ActorId(Guid.Empty);
        var health = HealthValue.Create(50, 100).Value;

        // Act
        var act = () => new HealthComponent(emptyId, health);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("ownerId");
    }

    [Fact]
    public void Constructor_NullHealth_ShouldThrowArgumentNullException()
    {
        // PROGRAMMER ERROR: Component must have initial health

        // Arrange
        var actorId = ActorId.NewId();

        // Act
        var act = () => new HealthComponent(actorId, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("initialHealth");
    }

    #endregion

    #region TakeDamage Tests

    [Fact]
    public void TakeDamage_ValidAmount_ShouldReduceHealthAndReturnNewValue()
    {
        // Arrange
        var health = HealthValue.Create(50, 100).Value;
        var component = new HealthComponent(ActorId.NewId(), health);

        // Act
        var result = component.TakeDamage(20);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Current.Should().Be(30);
        component.CurrentHealth.Current.Should().Be(30); // State mutated
        component.IsAlive.Should().BeTrue();
    }

    [Fact]
    public void TakeDamage_LethalDamage_ShouldSetIsAliveToFalse()
    {
        // WHY: When health reaches zero, actor should be marked as dead

        // Arrange
        var health = HealthValue.Create(30, 100).Value;
        var component = new HealthComponent(ActorId.NewId(), health);

        // Act
        var result = component.TakeDamage(40); // More than current health

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsDepleted.Should().BeTrue();
        component.CurrentHealth.IsDepleted.Should().BeTrue();
        component.IsAlive.Should().BeFalse();
    }

    [Fact]
    public void TakeDamage_NegativeAmount_ShouldReturnFailureAndNotMutateState()
    {
        // DOMAIN ERROR: Negative damage is invalid

        // Arrange
        var health = HealthValue.Create(50, 100).Value;
        var component = new HealthComponent(ActorId.NewId(), health);

        // Act
        var result = component.TakeDamage(-10);

        // Assert
        result.IsFailure.Should().BeTrue();
        component.CurrentHealth.Current.Should().Be(50); // State unchanged on failure
    }

    [Fact]
    public void TakeDamage_MultipleSequentialDamages_ShouldAccumulateCorrectly()
    {
        // WHY: Verify railway composition works for multiple operations

        // Arrange
        var health = HealthValue.Create(100, 100).Value;
        var component = new HealthComponent(ActorId.NewId(), health);

        // Act
        var result1 = component.TakeDamage(25);
        var result2 = component.TakeDamage(25);
        var result3 = component.TakeDamage(25);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result3.IsSuccess.Should().BeTrue();
        component.CurrentHealth.Current.Should().Be(25);
        component.IsAlive.Should().BeTrue();
    }

    [Fact]
    public void TakeDamage_OnDeadActor_ShouldStillSucceed()
    {
        // WHY: Overkill damage is valid (e.g., for damage calculations)

        // Arrange
        var health = HealthValue.Create(0, 100).Value;
        var component = new HealthComponent(ActorId.NewId(), health);

        // Act
        var result = component.TakeDamage(10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Current.Should().Be(0); // Clamped at zero
        component.IsAlive.Should().BeFalse();
    }

    #endregion

    #region Heal Tests

    [Fact]
    public void Heal_ValidAmount_ShouldIncreaseHealthAndReturnNewValue()
    {
        // Arrange
        var health = HealthValue.Create(30, 100).Value;
        var component = new HealthComponent(ActorId.NewId(), health);

        // Act
        var result = component.Heal(20);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Current.Should().Be(50);
        component.CurrentHealth.Current.Should().Be(50); // State mutated
        component.IsAlive.Should().BeTrue();
    }

    [Fact]
    public void Heal_OverhealAmount_ShouldClampToMaximum()
    {
        // Arrange
        var health = HealthValue.Create(80, 100).Value;
        var component = new HealthComponent(ActorId.NewId(), health);

        // Act
        var result = component.Heal(30); // Would be 110, clamped to 100

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Current.Should().Be(100);
        component.CurrentHealth.Current.Should().Be(100);
    }

    [Fact]
    public void Heal_NegativeAmount_ShouldReturnFailureAndNotMutateState()
    {
        // DOMAIN ERROR: Negative heal is invalid

        // Arrange
        var health = HealthValue.Create(50, 100).Value;
        var component = new HealthComponent(ActorId.NewId(), health);

        // Act
        var result = component.Heal(-10);

        // Assert
        result.IsFailure.Should().BeTrue();
        component.CurrentHealth.Current.Should().Be(50); // State unchanged on failure
    }

    [Fact]
    public void Heal_FromZeroHealth_ShouldReviveActor()
    {
        // WHY: Resurrection mechanics - healing from death should work

        // Arrange
        var health = HealthValue.Create(0, 100).Value;
        var component = new HealthComponent(ActorId.NewId(), health);

        // Act
        var result = component.Heal(50);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Current.Should().Be(50);
        component.CurrentHealth.Current.Should().Be(50);
        component.IsAlive.Should().BeTrue();
    }

    [Fact]
    public void Heal_MultipleSequentialHeals_ShouldAccumulateCorrectly()
    {
        // Arrange
        var health = HealthValue.Create(10, 100).Value;
        var component = new HealthComponent(ActorId.NewId(), health);

        // Act
        var result1 = component.Heal(20);
        var result2 = component.Heal(30);
        var result3 = component.Heal(15);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result3.IsSuccess.Should().BeTrue();
        component.CurrentHealth.Current.Should().Be(75);
    }

    #endregion

    #region IsAlive Tests

    [Fact]
    public void IsAlive_WithPositiveHealth_ShouldReturnTrue()
    {
        // Arrange
        var health = HealthValue.Create(50, 100).Value;
        var component = new HealthComponent(ActorId.NewId(), health);

        // Act & Assert
        component.IsAlive.Should().BeTrue();
    }

    [Fact]
    public void IsAlive_WithZeroHealth_ShouldReturnFalse()
    {
        // Arrange
        var health = HealthValue.Create(0, 100).Value;
        var component = new HealthComponent(ActorId.NewId(), health);

        // Act & Assert
        component.IsAlive.Should().BeFalse();
    }

    [Fact]
    public void IsAlive_AfterLethalDamage_ShouldReturnFalse()
    {
        // Arrange
        var health = HealthValue.Create(10, 100).Value;
        var component = new HealthComponent(ActorId.NewId(), health);

        // Act
        component.TakeDamage(15);

        // Assert
        component.IsAlive.Should().BeFalse();
    }

    [Fact]
    public void IsAlive_AfterRevive_ShouldReturnTrue()
    {
        // Arrange
        var health = HealthValue.Create(0, 100).Value;
        var component = new HealthComponent(ActorId.NewId(), health);

        // Act
        component.Heal(50);

        // Assert
        component.IsAlive.Should().BeTrue();
    }

    #endregion

    #region Railway Composition Tests

    [Fact]
    public void TakeDamageAndHeal_ShouldMaintainCorrectState()
    {
        // WHY: Verify .Tap() pattern works correctly for state mutation

        // Arrange
        var health = HealthValue.Create(100, 100).Value;
        var component = new HealthComponent(ActorId.NewId(), health);

        // Act
        component.TakeDamage(40);  // 100 -> 60
        component.TakeDamage(20);  // 60 -> 40
        component.Heal(30);        // 40 -> 70

        // Assert
        component.CurrentHealth.Current.Should().Be(70);
        component.IsAlive.Should().BeTrue();
    }

    #endregion
}