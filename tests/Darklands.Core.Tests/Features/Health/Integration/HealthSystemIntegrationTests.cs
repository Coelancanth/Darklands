using CSharpFunctionalExtensions;
using Darklands.Core.Application.Infrastructure;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Health.Application;
using Darklands.Core.Features.Health.Application.Commands;
using Darklands.Core.Features.Health.Application.Events;
using Darklands.Core.Features.Health.Domain;
using Darklands.Core.Features.Health.Infrastructure;
using Darklands.Core.Infrastructure.Events;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using HealthValue = Darklands.Core.Domain.Common.Health;

namespace Darklands.Core.Tests.Features.Health.Integration;

/// <summary>
/// Integration tests for the complete health system.
/// Tests the REAL pipeline: Command → Handler → Registry → Event (no mocks).
/// </summary>
[Trait("Category", "Phase3")]
[Trait("Category", "Integration")]
public class HealthSystemIntegrationTests : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMediator _mediator;
    private readonly IHealthComponentRegistry _registry;
    private readonly List<HealthChangedEvent> _publishedEvents;

    public HealthSystemIntegrationTests()
    {
        // Reset GameStrapper before each test
        GameStrapper.Reset();

        // Build real DI container with all services
        var services = new ServiceCollection();

        // Core services
        services.AddSingleton<IHealthComponentRegistry, HealthComponentRegistry>();

        // Mock IGodotEventBus (required by UIEventForwarder)
        services.AddSingleton<IGodotEventBus, MockGodotEventBus>();

        // MediatR with assembly scanning (will find UIEventForwarder + TakeDamageCommandHandler)
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(TakeDamageCommand).Assembly);
        });

        // Logging (use NullLogger for tests - no output needed)
        services.AddLogging();

        // Event capture for testing
        _publishedEvents = new List<HealthChangedEvent>();
        services.AddSingleton<INotificationHandler<HealthChangedEvent>>(
            new TestEventCapture(_publishedEvents));

        _serviceProvider = services.BuildServiceProvider();
        _mediator = _serviceProvider.GetRequiredService<IMediator>();
        _registry = _serviceProvider.GetRequiredService<IHealthComponentRegistry>();
    }

    public void Dispose()
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        GameStrapper.Reset();
    }

    #region End-to-End Tests

    [Fact]
    public async Task TakeDamageCommand_EndToEnd_ShouldReduceHealthAndPublishEvent()
    {
        // WHY: Validate complete pipeline with real DI, MediatR, and event publishing

        // Arrange
        var actorId = ActorId.NewId();
        var health = HealthValue.Create(100, 100).Value;
        var component = new HealthComponent(actorId, health);

        _registry.RegisterComponent(component);

        var command = new TakeDamageCommand(actorId, 30);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.NewHealth.Should().Be(70);
        component.CurrentHealth.Current.Should().Be(70);

        // Verify event was published
        _publishedEvents.Should().HaveCount(1);
        var evt = _publishedEvents[0];
        evt.ActorId.Should().Be(actorId);
        evt.OldHealth.Should().Be(100);
        evt.NewHealth.Should().Be(70);
        evt.IsDead.Should().BeFalse();
        evt.IsCritical.Should().BeFalse();
    }

    [Fact]
    public async Task TakeDamageCommand_LethalDamage_ShouldKillActorAndPublishDeathEvent()
    {
        // Arrange
        var actorId = ActorId.NewId();
        var health = HealthValue.Create(50, 100).Value;
        var component = new HealthComponent(actorId, health);

        _registry.RegisterComponent(component);

        var command = new TakeDamageCommand(actorId, 60);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsDead.Should().BeTrue();
        component.IsAlive.Should().BeFalse();

        _publishedEvents.Should().HaveCount(1);
        _publishedEvents[0].IsDead.Should().BeTrue();
        _publishedEvents[0].NewHealth.Should().Be(0);
    }

    [Fact]
    public async Task TakeDamageCommand_MultipleSequential_ShouldAccumulateDamageAndPublishMultipleEvents()
    {
        // WHY: Verify state persistence across multiple commands

        // Arrange
        var actorId = ActorId.NewId();
        var health = HealthValue.Create(100, 100).Value;
        var component = new HealthComponent(actorId, health);

        _registry.RegisterComponent(component);

        // Act
        await _mediator.Send(new TakeDamageCommand(actorId, 20));
        await _mediator.Send(new TakeDamageCommand(actorId, 30));
        await _mediator.Send(new TakeDamageCommand(actorId, 15));

        // Assert
        component.CurrentHealth.Current.Should().Be(35);

        _publishedEvents.Should().HaveCount(3);
        _publishedEvents[0].NewHealth.Should().Be(80);
        _publishedEvents[1].NewHealth.Should().Be(50);
        _publishedEvents[2].NewHealth.Should().Be(35);
    }

    [Fact]
    public async Task TakeDamageCommand_ActorNotRegistered_ShouldReturnFailureAndNotPublishEvent()
    {
        // DOMAIN ERROR: Actor doesn't exist

        // Arrange
        var nonExistentActorId = ActorId.NewId();
        var command = new TakeDamageCommand(nonExistentActorId, 10);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");

        // No events should be published on failure
        _publishedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task TakeDamageCommand_NegativeDamage_ShouldReturnFailureAndNotPublishEvent()
    {
        // DOMAIN ERROR: Negative damage

        // Arrange
        var actorId = ActorId.NewId();
        var health = HealthValue.Create(50, 100).Value;
        var component = new HealthComponent(actorId, health);

        _registry.RegisterComponent(component);

        var command = new TakeDamageCommand(actorId, -10);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("negative");

        // Component state should be unchanged
        component.CurrentHealth.Current.Should().Be(50);

        // No events published on failure
        _publishedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task TakeDamageCommand_CriticalHealthThreshold_ShouldSetIsCriticalTrue()
    {
        // Arrange
        var actorId = ActorId.NewId();
        var health = HealthValue.Create(100, 100).Value;
        var component = new HealthComponent(actorId, health);

        _registry.RegisterComponent(component);

        var command = new TakeDamageCommand(actorId, 85); // 100 -> 15 = 15% (critical)

        // Act
        var result = await _mediator.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsCritical.Should().BeTrue();

        _publishedEvents.Should().HaveCount(1);
        _publishedEvents[0].IsCritical.Should().BeTrue();
    }

    #endregion

    #region Registry Tests

    [Fact]
    public void Registry_RegisterAndRetrieve_ShouldSucceed()
    {
        // Arrange
        var actorId = ActorId.NewId();
        var health = HealthValue.Create(50, 100).Value;
        var component = new HealthComponent(actorId, health);

        // Act
        var registerResult = _registry.RegisterComponent(component);
        var getResult = _registry.GetComponent(actorId);

        // Assert
        registerResult.IsSuccess.Should().BeTrue();
        getResult.HasValue.Should().BeTrue();
        getResult.Value.Should().Be(component);
    }

    [Fact]
    public void Registry_GetNonExistent_ShouldReturnNone()
    {
        // Arrange
        var nonExistentId = ActorId.NewId();

        // Act
        var result = _registry.GetComponent(nonExistentId);

        // Assert
        result.HasNoValue.Should().BeTrue();
    }

    [Fact]
    public void Registry_Unregister_ShouldRemoveComponent()
    {
        // Arrange
        var actorId = ActorId.NewId();
        var health = HealthValue.Create(50, 100).Value;
        var component = new HealthComponent(actorId, health);

        _registry.RegisterComponent(component);

        // Act
        var unregisterResult = _registry.UnregisterComponent(actorId);
        var getResult = _registry.GetComponent(actorId);

        // Assert
        unregisterResult.IsSuccess.Should().BeTrue();
        getResult.HasNoValue.Should().BeTrue();
    }

    [Fact]
    public void Registry_UnregisterNonExistent_ShouldSucceed()
    {
        // WHY: Unregister is idempotent

        // Arrange
        var nonExistentId = ActorId.NewId();

        // Act
        var result = _registry.UnregisterComponent(nonExistentId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    /// <summary>
    /// Test helper to capture published events.
    /// </summary>
    private class TestEventCapture : INotificationHandler<HealthChangedEvent>
    {
        private readonly List<HealthChangedEvent> _events;

        public TestEventCapture(List<HealthChangedEvent> events)
        {
            _events = events;
        }

        public Task Handle(HealthChangedEvent notification, CancellationToken cancellationToken)
        {
            _events.Add(notification);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Mock implementation of IGodotEventBus for testing.
    /// UIEventForwarder requires this, but we capture events via TestEventCapture instead.
    /// </summary>
    private class MockGodotEventBus : IGodotEventBus
    {
        public void Subscribe<TEvent>(object subscriber, Action<TEvent> handler) where TEvent : INotification
        {
            // No-op for tests
        }

        public void Unsubscribe<TEvent>(object subscriber) where TEvent : INotification
        {
            // No-op for tests
        }

        public void UnsubscribeAll(object subscriber)
        {
            // No-op for tests
        }

        public Task PublishAsync<TEvent>(TEvent notification) where TEvent : INotification
        {
            // No-op for tests - events captured by TestEventCapture instead
            return Task.CompletedTask;
        }
    }
}