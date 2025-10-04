using Darklands.Core.Domain.Events;
using Darklands.Core.Infrastructure.Events;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Darklands.Core.Tests.Infrastructure.Events;

/// <summary>
/// Tests for IGodotEventBus interface contract.
/// Category: Phase2 for VS_004 implementation gates.
///
/// NOTE: GodotEventBus implementation (with Godot.CallDeferred) is tested manually in Phase 3.
/// These tests verify the interface contract using a mock implementation.
/// </summary>
[Trait("Category", "Infrastructure")]
public class GodotEventBusTests
{
    [Fact]
    public void IGodotEventBus_ShouldHaveSubscribeMethod()
    {
        // WHY: Verifies interface contract for subscription

        // Arrange
        var mockEventBus = Substitute.For<IGodotEventBus>();
        var subscriber = new object();

        // Act
        mockEventBus.Subscribe<TestEvent>(subscriber, evt => { });

        // Assert
        mockEventBus.Received(1).Subscribe<TestEvent>(subscriber, Arg.Any<Action<TestEvent>>());
    }

    [Fact]
    public void IGodotEventBus_ShouldHaveUnsubscribeMethod()
    {
        // WHY: Verifies interface contract for unsubscription

        // Arrange
        var mockEventBus = Substitute.For<IGodotEventBus>();
        var subscriber = new object();

        // Act
        mockEventBus.Unsubscribe<TestEvent>(subscriber);

        // Assert
        mockEventBus.Received(1).Unsubscribe<TestEvent>(subscriber);
    }

    [Fact]
    public void IGodotEventBus_ShouldHaveUnsubscribeAllMethod()
    {
        // WHY: EventAwareNode._ExitTree() requires UnsubscribeAll for cleanup

        // Arrange
        var mockEventBus = Substitute.For<IGodotEventBus>();
        var subscriber = new object();

        // Act
        mockEventBus.UnsubscribeAll(subscriber);

        // Assert
        mockEventBus.Received(1).UnsubscribeAll(subscriber);
    }

    [Fact]
    public async Task IGodotEventBus_ShouldHavePublishAsyncMethod()
    {
        // WHY: UIEventForwarder requires PublishAsync to forward events

        // Arrange
        var mockEventBus = Substitute.For<IGodotEventBus>();
        var testEvent = new TestEvent("Test");

        // Act
        await mockEventBus.PublishAsync(testEvent);

        // Assert
        await mockEventBus.Received(1).PublishAsync(testEvent);
    }
}

/// <summary>
/// NOTE: Full GodotEventBus implementation tests (Subscribe/Unsubscribe mechanics,
/// CallDeferred marshalling, error isolation) are performed manually in Phase 3
/// using a Godot scene. These require Godot runtime which isn't available in unit tests.
///
/// Manual test coverage (Phase 3):
/// - Subscribe registers handler correctly
/// - PublishAsync invokes all subscribers on main thread
/// - Unsubscribe prevents handler invocation
/// - UnsubscribeAll cleans up all subscriptions
/// - CallDeferred marshals to Godot main thread
/// - Error in one subscriber doesn't break others
/// </summary>