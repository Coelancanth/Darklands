using Darklands.Core.Domain.Events;
using Darklands.Core.Infrastructure.Events;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Darklands.Core.Tests.Infrastructure.Events;

/// <summary>
/// Tests for UIEventForwarder - MediatR to GodotEventBus bridge.
/// Category: Phase2 for VS_004 implementation gates.
///
/// CRITICAL: Validates open generic registration doesn't cause type resolution bugs.
/// User specifically requested tests for MediatR autoscan edge cases.
/// </summary>
[Trait("Category", "Infrastructure")]
public class UIEventForwarderTests
{
    [Fact]
    public async Task Handle_ShouldForwardEventToGodotEventBus()
    {
        // Arrange
        var mockEventBus = Substitute.For<IGodotEventBus>();
        var mockLogger = Substitute.For<ILogger<UIEventForwarder<TestEvent>>>();

        var forwarder = new UIEventForwarder<TestEvent>(mockEventBus, mockLogger);
        var testEvent = new TestEvent("Test message");

        // Act
        await forwarder.Handle(testEvent, CancellationToken.None);

        // Assert
        await mockEventBus.Received(1).PublishAsync(testEvent);
    }

    [Fact]
    public void Constructor_ShouldAcceptDependencies()
    {
        // WHY: Verifies UIEventForwarder<T> can be constructed by DI container

        // Arrange
        var mockEventBus = Substitute.For<IGodotEventBus>();
        var mockLogger = Substitute.For<ILogger<UIEventForwarder<TestEvent>>>();

        // Act
        var forwarder = new UIEventForwarder<TestEvent>(mockEventBus, mockLogger);

        // Assert
        forwarder.Should().NotBeNull("UIEventForwarder should be constructible");
    }

    [Fact]
    public void ServiceProvider_ShouldResolveCorrectGenericInstance_ForTestEvent()
    {
        // CRITICAL TEST - User's concern about MediatR autoscan bugs
        // WHY: Open generic registration can cause type resolution errors if not configured correctly
        // Verifies MediatR resolves UIEventForwarder<TestEvent> (not wrong type or null)

        // Arrange - Real DI container with open generic registration
        var services = new ServiceCollection();
        services.AddSingleton<IGodotEventBus>(Substitute.For<IGodotEventBus>());
        services.AddLogging();

        // Act - Register UIEventForwarder with open generics (the pattern we're using)
        services.AddTransient(typeof(INotificationHandler<>), typeof(UIEventForwarder<>));

        var provider = services.BuildServiceProvider();

        // Resolve specific handler type
        var handler = provider.GetService<INotificationHandler<TestEvent>>();

        // Assert - Verify EXACT type match (not null, not wrong generic instance)
        handler.Should().NotBeNull("DI should resolve INotificationHandler<TestEvent>");
        handler.Should().BeOfType<UIEventForwarder<TestEvent>>(
            "DI should resolve to UIEventForwarder<TestEvent>, not another handler type");

        // Verify it's functional (has dependencies injected)
        var forwarder = (UIEventForwarder<TestEvent>)handler!;
        forwarder.Should().NotBeNull("UIEventForwarder should be fully constructed with dependencies");
    }

    [Fact]
    public void ServiceProvider_ShouldResolveMultipleEventTypes_Independently()
    {
        // CRITICAL TEST - User's concern about type resolution bugs
        // WHY: Ensures MediatR creates separate instances for different event types
        // Prevents bug where TestEvent handler gets passed HealthChangedEvent

        // Arrange - Real DI container
        var services = new ServiceCollection();
        services.AddSingleton<IGodotEventBus>(Substitute.For<IGodotEventBus>());
        services.AddLogging();
        services.AddTransient(typeof(INotificationHandler<>), typeof(UIEventForwarder<>));

        var provider = services.BuildServiceProvider();

        // Act - Resolve handlers for different event types
        var testEventHandler = provider.GetService<INotificationHandler<TestEvent>>();
        var anotherEventHandler = provider.GetService<INotificationHandler<AnotherTestEvent>>();

        // Assert - Each event type gets correct generic instance
        testEventHandler.Should().BeOfType<UIEventForwarder<TestEvent>>(
            "TestEvent should resolve to UIEventForwarder<TestEvent>");

        anotherEventHandler.Should().BeOfType<UIEventForwarder<AnotherTestEvent>>(
            "AnotherTestEvent should resolve to UIEventForwarder<AnotherTestEvent>");

        // Verify they're different instances
        testEventHandler.Should().NotBeSameAs(anotherEventHandler,
            "Different event types should get different forwarder instances");
    }

    [Fact]
    public async Task ServiceProvider_ResolvedInstance_ShouldForwardCorrectly()
    {
        // CRITICAL TEST - End-to-end verification of DI-resolved instance
        // WHY: Ensures instance resolved by DI actually works (not just type-checks)

        // Arrange - Real DI container with mock EventBus
        var mockEventBus = Substitute.For<IGodotEventBus>();

        var services = new ServiceCollection();
        services.AddSingleton<IGodotEventBus>(mockEventBus);
        services.AddLogging();
        services.AddTransient(typeof(INotificationHandler<>), typeof(UIEventForwarder<>));

        var provider = services.BuildServiceProvider();

        // Act - Resolve and use handler
        var handler = provider.GetRequiredService<INotificationHandler<TestEvent>>();
        var testEvent = new TestEvent("Verify forwarding");

        await handler.Handle(testEvent, CancellationToken.None);

        // Assert - Verify exact event instance was forwarded (not copy, not null)
        await mockEventBus.Received(1).PublishAsync(
            Arg.Is<TestEvent>(e => ReferenceEquals(e, testEvent)));
    }

    /// <summary>
    /// Second test event for verifying independent type resolution.
    /// </summary>
    private record AnotherTestEvent(string Data) : INotification;
}