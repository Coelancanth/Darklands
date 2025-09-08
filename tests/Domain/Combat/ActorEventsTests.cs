using Xunit;
using FluentAssertions;
using MediatR;
using Darklands.Core.Domain.Combat;
using Darklands.Core.Domain.Grid;
using Darklands.Core.Domain.Actor;

namespace Darklands.Core.Tests.Domain.Combat;

/// <summary>
/// Tests for ActorDiedEvent and ActorDamagedEvent domain events.
/// These events replace the static callback anti-pattern in ExecuteAttackCommandHandler.
/// </summary>
public class ActorEventsTests
{
    [Fact]
    public void ActorDiedEvent_Create_ProducesValidEvent()
    {
        // Arrange
        var actorId = ActorId.NewId();
        var position = new Position(5, 7);

        // Act
        var eventObj = ActorDiedEvent.Create(actorId, position);

        // Assert
        eventObj.Should().BeAssignableTo<INotification>();
        eventObj.ActorId.Should().Be(actorId);
        eventObj.Position.Should().Be(position);
    }

    [Fact]
    public void ActorDiedEvent_ToString_ReturnsFormattedString()
    {
        // Arrange
        var actorId = ActorId.NewId();
        var position = new Position(3, 4);
        var eventObj = ActorDiedEvent.Create(actorId, position);

        // Act
        var result = eventObj.ToString();

        // Assert
        result.Should().Contain("ActorDiedEvent");
        result.Should().Contain(actorId.ToString());
        result.Should().Contain("3");
        result.Should().Contain("4");
    }

    [Fact]
    public void ActorDamagedEvent_Create_ProducesValidEvent()
    {
        // Arrange
        var actorId = ActorId.NewId();
        var oldHealth = Health.Create(100, 100).Match(h => h, _ => throw new Exception("Should not fail"));
        var newHealth = Health.Create(75, 100).Match(h => h, _ => throw new Exception("Should not fail"));

        // Act
        var eventObj = ActorDamagedEvent.Create(actorId, oldHealth, newHealth);

        // Assert
        eventObj.Should().BeAssignableTo<INotification>();
        eventObj.ActorId.Should().Be(actorId);
        eventObj.OldHealth.Should().Be(oldHealth);
        eventObj.NewHealth.Should().Be(newHealth);
    }

    [Fact]
    public void ActorDamagedEvent_DamageAmount_CalculatesCorrectly()
    {
        // Arrange
        var actorId = ActorId.NewId();
        var oldHealth = Health.Create(80, 100).Match(h => h, _ => throw new Exception("Should not fail"));
        var newHealth = Health.Create(55, 100).Match(h => h, _ => throw new Exception("Should not fail"));

        // Act
        var eventObj = ActorDamagedEvent.Create(actorId, oldHealth, newHealth);

        // Assert
        eventObj.DamageAmount.Should().Be(25); // 80 - 55 = 25 damage
    }

    [Fact]
    public void ActorDamagedEvent_ToString_ReturnsFormattedString()
    {
        // Arrange
        var actorId = ActorId.NewId();
        var oldHealth = Health.Create(60, 100).Match(h => h, _ => throw new Exception("Should not fail"));
        var newHealth = Health.Create(45, 100).Match(h => h, _ => throw new Exception("Should not fail"));
        var eventObj = ActorDamagedEvent.Create(actorId, oldHealth, newHealth);

        // Act
        var result = eventObj.ToString();

        // Assert
        result.Should().Contain("ActorDamagedEvent");
        result.Should().Contain(actorId.ToString());
        result.Should().Contain("60");
        result.Should().Contain("45");
        result.Should().Contain("15"); // Damage amount
    }

    [Fact]
    public void ActorEvents_RecordEquality_WorksCorrectly()
    {
        // Arrange
        var actorId = ActorId.NewId();
        var position = new Position(2, 3);
        var health = Health.Create(50, 100).Match(h => h, _ => throw new Exception("Should not fail"));

        var event1 = ActorDiedEvent.Create(actorId, position);
        var event2 = ActorDiedEvent.Create(actorId, position);
        var event3 = ActorDamagedEvent.Create(actorId, health, health);
        var event4 = ActorDamagedEvent.Create(actorId, health, health);

        // Act & Assert
        event1.Should().Be(event2);
        event3.Should().Be(event4);
        event1.Should().NotBe((object)event3); // Different event types
    }
}
