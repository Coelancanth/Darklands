using Darklands.Core.Domain.Events;
using Darklands.Core.Infrastructure.Events;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Darklands.Core.Tests.Infrastructure.Events;

/// <summary>
/// Integration tests for complete event flow: MediatR → UIEventForwarder → GodotEventBus.
/// Category: Phase2 for VS_004 implementation gates.
///
/// CRITICAL: Validates that MediatR.Publish() correctly routes to GodotEventBus.
/// User requested verification that MediatR autoscan creates correct instances.
/// </summary>
[Trait("Category", "Phase2")]
public class EventBusIntegrationTests
{
    [Fact]
    public async Task MediatR_ShouldInvokeCorrectUIEventForwarderInstance()
    {
        // CRITICAL TEST - User's specific concern about MediatR type resolution
        // WHY: Verifies MediatR creates UIEventForwarder<TestEvent> (not wrong type)
        // and forwards EXACT event instance to GodotEventBus

        // Arrange - Full MediatR + DI setup (simulates production config)
        var services = new ServiceCollection();

        // Register MediatR core (scan for IMediator, but NOT handlers)
        // WHY: Open generic registration below handles UIEventForwarder
        // If we scan for handlers, MediatR registers UIEventForwarder twice → events published twice!
        // We scan typeof(IMediator).Assembly for core MediatR types only
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(IMediator).Assembly));

        // Register EventBus infrastructure (as GameStrapper will do)
        var mockEventBus = Substitute.For<IGodotEventBus>();
        services.AddSingleton<IGodotEventBus>(mockEventBus);
        services.AddLogging();

        // Register UIEventForwarder with open generics (the pattern from backlog)
        services.AddTransient(typeof(INotificationHandler<>), typeof(UIEventForwarder<>));

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act - Publish event through MediatR
        var testEvent = new TestEvent("Verify Instance Forwarding");
        await mediator.Publish(testEvent);

        // Assert - Verify EXACT event instance was forwarded to GodotEventBus
        await mockEventBus.Received(1).PublishAsync(
            Arg.Is<TestEvent>(e => ReferenceEquals(e, testEvent)));
    }

    [Fact]
    public async Task MediatR_ShouldHandleMultipleEventTypes_Independently()
    {
        // CRITICAL TEST - Verifies MediatR doesn't mix up event types
        // WHY: Ensures TestEvent goes to UIEventForwarder<TestEvent>,
        // and AnotherEvent goes to UIEventForwarder<AnotherEvent>

        // Arrange - Full setup with multiple event types
        var services = new ServiceCollection();

        // Register MediatR (scan MediatR assembly for core types, not our handlers)
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(IMediator).Assembly));

        var mockEventBus = Substitute.For<IGodotEventBus>();
        services.AddSingleton<IGodotEventBus>(mockEventBus);
        services.AddLogging();
        services.AddTransient(typeof(INotificationHandler<>), typeof(UIEventForwarder<>));

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act - Publish different event types
        var testEvent = new TestEvent("First event");
        var anotherEvent = new AnotherTestEvent(42);

        await mediator.Publish(testEvent);
        await mediator.Publish(anotherEvent);

        // Assert - Each event type forwarded with correct type
        await mockEventBus.Received(1).PublishAsync(
            Arg.Is<TestEvent>(e => ReferenceEquals(e, testEvent)));

        await mockEventBus.Received(1).PublishAsync(
            Arg.Is<AnotherTestEvent>(e => ReferenceEquals(e, anotherEvent)));

        // Verify exactly 2 calls (no duplicates, no missing)
        await mockEventBus.Received(2).PublishAsync(Arg.Any<INotification>());
    }

    /// <summary>
    /// Second test event type for multi-type testing.
    /// </summary>
    private record AnotherTestEvent(int Value) : INotification;
}

// NOTE: Complete end-to-end test (MediatR → UIEventForwarder → GodotEventBus → Godot nodes)
// is performed manually in Phase 3 using a Godot test scene. This requires Godot runtime
// for CallDeferred execution and node lifecycle management.
//
// Manual test coverage (Phase 3):
// - Full event flow from MediatR.Publish to Godot node handler
// - CallDeferred marshals to main thread correctly
// - Multiple subscribers receive same event
// - Unsubscribe prevents future event reception
// - EventAwareNode._ExitTree() cleanup works