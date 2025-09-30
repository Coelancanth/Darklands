using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Health.Application;
using Darklands.Core.Features.Health.Application.Commands;
using Darklands.Core.Features.Health.Application.Events;
using Darklands.Core.Features.Health.Domain;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using HealthValue = Darklands.Core.Domain.Common.Health;

namespace Darklands.Core.Tests.Features.Health.Application;

[Trait("Category", "Phase2")]
[Trait("Category", "Handlers")]
public class TakeDamageCommandHandlerTests
{
    private readonly IHealthComponentRegistry _mockRegistry;
    private readonly IMediator _mockMediator;
    private readonly ILogger<TakeDamageCommandHandler> _mockLogger;
    private readonly TakeDamageCommandHandler _handler;

    public TakeDamageCommandHandlerTests()
    {
        _mockRegistry = Substitute.For<IHealthComponentRegistry>();
        _mockMediator = Substitute.For<IMediator>();
        _mockLogger = Substitute.For<ILogger<TakeDamageCommandHandler>>();

        _handler = new TakeDamageCommandHandler(
            _mockRegistry,
            _mockMediator,
            _mockLogger);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullRegistry_ShouldThrowArgumentNullException()
    {
        // PROGRAMMER ERROR: Dependencies must not be null

        // Act
        var act = () => new TakeDamageCommandHandler(
            null!,
            _mockMediator,
            _mockLogger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("registry");
    }

    [Fact]
    public void Constructor_NullMediator_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new TakeDamageCommandHandler(
            _mockRegistry,
            null!,
            _mockLogger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("mediator");
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new TakeDamageCommandHandler(
            _mockRegistry,
            _mockMediator,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region Handle - Success Cases

    [Fact]
    public async Task Handle_ValidDamage_ShouldReduceHealthAndReturnResult()
    {
        // Arrange
        var actorId = ActorId.NewId();
        var health = HealthValue.Create(100, 100).Value;
        var component = new HealthComponent(actorId, health);

        _mockRegistry.GetComponent(actorId).Returns(Maybe<IHealthComponent>.From(component));

        var command = new TakeDamageCommand(actorId, 30);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ActorId.Should().Be(actorId);
        result.Value.OldHealth.Should().Be(100);
        result.Value.NewHealth.Should().Be(70);
        result.Value.DamageApplied.Should().Be(30);
        result.Value.IsDead.Should().BeFalse();
        result.Value.IsCritical.Should().BeFalse();

        // Verify component state changed
        component.CurrentHealth.Current.Should().Be(70);
    }

    [Fact]
    public async Task Handle_LethalDamage_ShouldSetIsDeadTrue()
    {
        // WHY: When damage kills actor, result should indicate death

        // Arrange
        var actorId = ActorId.NewId();
        var health = HealthValue.Create(30, 100).Value;
        var component = new HealthComponent(actorId, health);

        _mockRegistry.GetComponent(actorId).Returns(Maybe<IHealthComponent>.From(component));

        var command = new TakeDamageCommand(actorId, 50);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.NewHealth.Should().Be(0);
        result.Value.IsDead.Should().BeTrue();
        component.IsAlive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_DamageToCriticalThreshold_ShouldSetIsCriticalTrue()
    {
        // WHY: IsCritical pre-computed for subscribers (they shouldn't recalculate)

        // Arrange
        var actorId = ActorId.NewId();
        var health = HealthValue.Create(100, 100).Value;
        var component = new HealthComponent(actorId, health);

        _mockRegistry.GetComponent(actorId).Returns(Maybe<IHealthComponent>.From(component));

        var command = new TakeDamageCommand(actorId, 80); // 100 -> 20 = 20%

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.NewHealth.Should().Be(20);
        result.Value.IsCritical.Should().BeTrue();
        result.Value.IsDead.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ValidDamage_ShouldPublishHealthChangedEvent()
    {
        // WHY: ADR-004 Rule 1 - Command orchestrates, event notifies

        // Arrange
        var actorId = ActorId.NewId();
        var health = HealthValue.Create(50, 100).Value;
        var component = new HealthComponent(actorId, health);

        _mockRegistry.GetComponent(actorId).Returns(Maybe<IHealthComponent>.From(component));

        var command = new TakeDamageCommand(actorId, 20);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _mockMediator.Received(1).Publish(
            Arg.Is<HealthChangedEvent>(e =>
                e.ActorId == actorId &&
                e.OldHealth == 50 &&
                e.NewHealth == 30 &&
                !e.IsDead &&
                !e.IsCritical),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Handle - Failure Cases

    [Fact]
    public async Task Handle_ActorNotFound_ShouldReturnFailure()
    {
        // DOMAIN ERROR: Actor doesn't exist in registry

        // Arrange
        var actorId = ActorId.NewId();
        _mockRegistry.GetComponent(actorId).Returns(Maybe<IHealthComponent>.None);

        var command = new TakeDamageCommand(actorId, 10);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_NegativeDamage_ShouldReturnFailure()
    {
        // DOMAIN ERROR: Negative damage violates domain rules

        // Arrange
        var actorId = ActorId.NewId();
        var health = HealthValue.Create(50, 100).Value;
        var component = new HealthComponent(actorId, health);

        _mockRegistry.GetComponent(actorId).Returns(Maybe<IHealthComponent>.From(component));

        var command = new TakeDamageCommand(actorId, -10);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("negative");

        // Verify component state unchanged on failure
        component.CurrentHealth.Current.Should().Be(50);
    }

    [Fact]
    public async Task Handle_FailedDamageApplication_ShouldNotPublishEvent()
    {
        // WHY: Events should only be published on successful operations

        // Arrange
        var actorId = ActorId.NewId();
        var health = HealthValue.Create(50, 100).Value;
        var component = new HealthComponent(actorId, health);

        _mockRegistry.GetComponent(actorId).Returns(Maybe<IHealthComponent>.From(component));

        var command = new TakeDamageCommand(actorId, -10); // Invalid damage

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _mockMediator.DidNotReceive().Publish(
            Arg.Any<HealthChangedEvent>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Railway Composition Tests

    [Fact]
    public async Task Handle_MultipleSequentialCommands_ShouldAccumulateDamage()
    {
        // WHY: Verify railway composition maintains state correctly

        // Arrange
        var actorId = ActorId.NewId();
        var health = HealthValue.Create(100, 100).Value;
        var component = new HealthComponent(actorId, health);

        _mockRegistry.GetComponent(actorId).Returns(Maybe<IHealthComponent>.From(component));

        // Act
        var result1 = await _handler.Handle(new TakeDamageCommand(actorId, 25), CancellationToken.None);
        var result2 = await _handler.Handle(new TakeDamageCommand(actorId, 25), CancellationToken.None);
        var result3 = await _handler.Handle(new TakeDamageCommand(actorId, 25), CancellationToken.None);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result3.IsSuccess.Should().BeTrue();

        result1.Value.NewHealth.Should().Be(75);
        result2.Value.NewHealth.Should().Be(50);
        result3.Value.NewHealth.Should().Be(25);

        component.CurrentHealth.Current.Should().Be(25);

        // Should have published 3 events
        await _mockMediator.Received(3).Publish(
            Arg.Any<HealthChangedEvent>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Logging Tests

    [Fact]
    public async Task Handle_ValidDamage_ShouldLogInformation()
    {
        // Arrange
        var actorId = ActorId.NewId();
        var health = HealthValue.Create(50, 100).Value;
        var component = new HealthComponent(actorId, health);

        _mockRegistry.GetComponent(actorId).Returns(Maybe<IHealthComponent>.From(component));

        var command = new TakeDamageCommand(actorId, 10);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        // Verify logger was called (NSubstitute limitation: can't verify exact message)
        _mockLogger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    #endregion
}