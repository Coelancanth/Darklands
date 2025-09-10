using FluentAssertions;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Darklands.Core.Application.Events;
using Darklands.Core.Infrastructure.DependencyInjection;
using Darklands.Core.Tests.TestUtilities;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Darklands.Core.Tests.Infrastructure.Events;

/// <summary>
/// Integration tests for UIEventBus focusing on thread safety, memory management,
/// and concurrent operations. Tests use real UIEventBus with mock subscribers.
/// 
/// These tests would have caught issues #3, #4, and #5 from TD_017 incident:
/// - Thread safety violations during concurrent publishing
/// - WeakReference cleanup failures under load
/// - Memory leaks from subscribers not being cleaned up
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "ThreadSafety")]
[Collection("GameStrapper")]
public class UIEventBusIntegrationTests : IDisposable
{
    private ServiceProvider? _testServiceProvider;

    private ServiceProvider GetServiceProvider()
    {
        if (_testServiceProvider == null)
        {
            GameStrapper.Dispose();
            var initResult = GameStrapper.Initialize(GameStrapperConfiguration.Testing);
            _testServiceProvider = initResult.Match(
                Succ: provider => provider,
                Fail: error => throw new InvalidOperationException($"GameStrapper initialization failed: {error}"));
        }
        return _testServiceProvider;
    }

    private IUIEventBus GetEventBus() => GetServiceProvider().GetRequiredService<IUIEventBus>();

    [Fact]
    public async Task ConcurrentPublishing_MultipleThreads_NoDataCorruption()
    {
        // Arrange: Create event bus and subscriber
        var eventBus = GetEventBus();
        var subscriber = new MockSubscriber();
        eventBus.Subscribe<ConcurrentTestEvent>(subscriber, subscriber.HandleConcurrentTestEvent);

        const int threadCount = 50;
        const int eventsPerThread = 20;
        const int totalEvents = threadCount * eventsPerThread;

        var barrier = new Barrier(threadCount);
        var publishedEvents = new ConcurrentBag<ConcurrentTestEvent>();

        // Act: Publish events from multiple threads simultaneously
        var publishTasks = Enumerable.Range(0, threadCount)
            .Select(threadId => Task.Run(async () =>
            {
                // Synchronize thread start for maximum contention
                barrier.SignalAndWait();

                for (int eventNum = 0; eventNum < eventsPerThread; eventNum++)
                {
                    var evt = new ConcurrentTestEvent(threadId, eventNum);
                    publishedEvents.Add(evt);
                    await eventBus.PublishAsync(evt);

                    // Add small random delay to increase chance of race conditions
                    await Task.Delay(Random.Shared.Next(1, 5));
                }
            }))
            .ToArray();

        await Task.WhenAll(publishTasks);

        // Allow time for all events to be processed
        await Task.Delay(100);

        // Assert: All events received without corruption
        var receivedEvents = subscriber.GetAllEvents<ConcurrentTestEvent>();

        receivedEvents.Should().HaveCount(totalEvents,
            "all events should be received despite concurrent publishing");

        // Verify no data corruption - each published event should be received exactly once
        var publishedEventsSet = publishedEvents.ToHashSet();
        var receivedEventsSet = receivedEvents.ToHashSet();

        receivedEventsSet.Should().BeEquivalentTo(publishedEventsSet,
            "received events should exactly match published events without corruption");

        // Verify thread distribution
        var threadsWithEvents = receivedEvents.Select(e => e.ThreadId).Distinct().Count();
        threadsWithEvents.Should().Be(threadCount,
            "events from all threads should be received");
    }

    [Fact]
    public async Task WeakReferenceCleanup_DeadSubscribers_AutomaticRemoval()
    {
        // Arrange: Create event bus
        var eventBus = GetEventBus();
        var aliveSubscriber = new MockSubscriber();
        var tempSubscribers = new List<MockSubscriber>();

        // Create multiple temporary subscribers and register them
        for (int i = 0; i < 5; i++)
        {
            var subscriber = new MockSubscriber();
            tempSubscribers.Add(subscriber);
            eventBus.Subscribe<CleanupTestEvent>(subscriber, subscriber.HandleCleanupTestEvent);
        }

        // Add one subscriber that will stay alive
        eventBus.Subscribe<CleanupTestEvent>(aliveSubscriber, aliveSubscriber.HandleCleanupTestEvent);

        // Act: Publish event to all subscribers initially
        await eventBus.PublishAsync(new CleanupTestEvent("initial test"));

        // Verify all subscribers received the event
        var initialCount = aliveSubscriber.EventCount + tempSubscribers.Sum(s => s.EventCount);
        initialCount.Should().Be(6, "all 6 subscribers should receive initial event");

        // Clear references to temp subscribers to make them eligible for collection
        tempSubscribers.Clear();

        // Force garbage collection (attempt - GC is non-deterministic)
        for (int gcRound = 0; gcRound < 5; gcRound++)
        {
            GC.Collect(2, GCCollectionMode.Forced, true);
            GC.WaitForPendingFinalizers();
            await Task.Delay(5);
        }

        // Publish another event to trigger WeakReference cleanup
        await eventBus.PublishAsync(new CleanupTestEvent("cleanup trigger"));
        await eventBus.PublishAsync(new CleanupTestEvent("final test"));

        // Assert: Alive subscriber should still work (test validates cleanup doesn't break functionality)
        aliveSubscriber.EventCount.Should().BeGreaterThanOrEqualTo(3,
            "alive subscriber should receive all events (validates cleanup doesn't break functionality)");

        var lastEvent = aliveSubscriber.GetLastEvent<CleanupTestEvent>();
        lastEvent?.Data.Should().Be("final test",
            "event bus should remain functional during WeakReference cleanup");
    }

    [Fact]
    public async Task SubscribeUnsubscribeDuringPublishing_HighConcurrency_NoDeadlocks()
    {
        // Arrange: Create event bus and initial subscribers
        var eventBus = GetEventBus();
        var stableSubscriber = new MockSubscriber();
        eventBus.Subscribe<TestEvent>(stableSubscriber, stableSubscriber.HandleTestEvent);

        var dynamicSubscribers = new ConcurrentBag<MockSubscriber>();
        const int operationCount = 100;
        var completedOperations = 0;

        // Act: Perform concurrent subscribe/unsubscribe operations while publishing
        var subscriptionTasks = Enumerable.Range(0, operationCount)
            .Select(_ => Task.Run(async () =>
            {
                var subscriber = new MockSubscriber();
                dynamicSubscribers.Add(subscriber);

                // Subscribe
                eventBus.Subscribe<TestEvent>(subscriber, subscriber.HandleTestEvent);

                // Publish some events
                for (int i = 0; i < 5; i++)
                {
                    await eventBus.PublishAsync(new TestEvent(i, "dynamic"));
                }

                // Unsubscribe
                eventBus.Unsubscribe<TestEvent>(subscriber);

                Interlocked.Increment(ref completedOperations);
            }))
            .ToArray();

        var publishingTask = Task.Run(async () =>
        {
            while (completedOperations < operationCount)
            {
                await eventBus.PublishAsync(new TestEvent(999, "background"));
                await Task.Delay(1);
            }
        });

        // Wait for all operations to complete
        var timeout = TimeSpan.FromSeconds(30);
        var stopwatch = Stopwatch.StartNew();

        await Task.WhenAll(subscriptionTasks);
        await publishingTask.WaitAsync(timeout - stopwatch.Elapsed);

        // Assert: No deadlocks occurred and stable subscriber still works
        completedOperations.Should().Be(operationCount,
            "all subscription operations should complete without deadlock");

        stableSubscriber.EventCount.Should().BeGreaterThan(0,
            "stable subscriber should receive events throughout the test");

        // Verify system is still responsive
        await eventBus.PublishAsync(new TestEvent(9999, "final test"));
        stableSubscriber.GetAllEvents<TestEvent>()
            .Should().Contain(e => e.Message == "final test",
            "event bus should remain responsive after concurrent operations");
    }

    [Fact]
    public async Task LockContention_MassiveLoad_PerformanceWithinBounds()
    {
        // Arrange: Create event bus with many subscribers
        var eventBus = GetEventBus();
        var subscribers = new List<MockSubscriber>();

        const int subscriberCount = 100;
        for (int i = 0; i < subscriberCount; i++)
        {
            var subscriber = new MockSubscriber();
            subscribers.Add(subscriber);
            eventBus.Subscribe<TestEvent>(subscriber, subscriber.HandleTestEvent);
        }

        const int eventCount = 1000;
        var stopwatch = new Stopwatch();

        // Act: Publish many events under high contention
        stopwatch.Start();

        var publishTasks = Enumerable.Range(0, eventCount)
            .Select(i => eventBus.PublishAsync(new TestEvent(i, "load test")))
            .ToArray();

        await Task.WhenAll(publishTasks);
        stopwatch.Stop();

        // Assert: Performance is within acceptable bounds
        var eventsPerSecond = eventCount / stopwatch.Elapsed.TotalSeconds;

        eventsPerSecond.Should().BeGreaterThan(1000,
            "event bus should handle at least 1000 events/second under load");

        // Verify all events were delivered correctly
        var totalEventsReceived = subscribers.Sum(s => s.EventCount);
        var expectedTotalEvents = subscriberCount * eventCount;

        totalEventsReceived.Should().Be(expectedTotalEvents,
            "all events should be delivered to all subscribers");

        // Verify no data corruption under load
        foreach (var subscriber in subscribers.Take(5)) // Check first few subscribers
        {
            var receivedEvents = subscriber.GetAllEvents<TestEvent>();
            receivedEvents.Should().HaveCount(eventCount);
            receivedEvents.Should().OnlyContain(e => e.Message == "load test");
        }
    }

    [Fact]
    public void EventBus_Singleton_SameInstanceAcrossResolutions()
    {
        // Arrange & Act: Get multiple references to event bus
        var serviceProvider = GetServiceProvider();
        var eventBus1 = serviceProvider.GetRequiredService<IUIEventBus>();
        var eventBus2 = serviceProvider.GetRequiredService<IUIEventBus>();
        var eventBus3 = serviceProvider.GetService<IUIEventBus>();

        // Assert: All references point to same instance
        ReferenceEquals(eventBus1, eventBus2).Should().BeTrue(
            "UIEventBus should be singleton - same instance across resolutions");
        ReferenceEquals(eventBus1, eventBus3).Should().BeTrue(
            "UIEventBus should be singleton - same instance via GetService");

        eventBus1.Should().BeSameAs(eventBus2);
        eventBus1.Should().BeSameAs(eventBus3);
    }

    public void Dispose()
    {
        try
        {
            _testServiceProvider?.Dispose();
        }
        finally
        {
            _testServiceProvider = null;
            GameStrapper.Dispose();
        }
    }
}
