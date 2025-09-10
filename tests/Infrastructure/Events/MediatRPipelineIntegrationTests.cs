using FluentAssertions;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using Darklands.Core.Application.Events;
using Darklands.Core.Infrastructure.DependencyInjection;
using Darklands.Core.Tests.TestUtilities;
using System.Reflection;

namespace Darklands.Core.Tests.Infrastructure.Events;

/// <summary>
/// Integration tests for MediatR pipeline focusing on handler discovery, registration,
/// and the complete flow from domain events to UI event bus.
/// 
/// These tests would have caught issues #1, #2, and #5 from TD_017 incident:
/// - MediatR auto-discovery conflicts (multiple handlers for same event)
/// - Missing handler registrations (events not reaching UI)
/// - Duplicate processing (events handled twice)
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "MediatR")]
[Collection("GameStrapper")]
public class MediatRPipelineIntegrationTests : IDisposable
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

    private IMediator GetMediator() => GetServiceProvider().GetRequiredService<IMediator>();
    private IUIEventBus GetEventBus() => GetServiceProvider().GetRequiredService<IUIEventBus>();

    [Fact]
    public void UIEventForwarder_AutoDiscovery_SingleHandlerPerEventType()
    {
        // Arrange: Get service provider (already configured with MediatR)
        var serviceProvider = GetServiceProvider();

        // Act: Get registered handlers for our test event
        var testEventHandlers = serviceProvider.GetServices<INotificationHandler<TestEvent>>().ToList();

        // Assert: Should have exactly one handler (UIEventForwarder<TestEvent>)
        testEventHandlers.Should().HaveCount(1,
            "TestEvent should have exactly one handler (UIEventForwarder<TestEvent>)");

        var handlerType = testEventHandlers.First().GetType();
        handlerType.IsGenericType.Should().BeTrue();
        handlerType.GetGenericTypeDefinition().Should().Be(typeof(UIEventForwarder<>),
            "handler should be UIEventForwarder<TestEvent>");

        // Verify different event types get different handler instances
        var concurrentEventHandlers = serviceProvider.GetServices<INotificationHandler<ConcurrentTestEvent>>().ToList();
        concurrentEventHandlers.Should().HaveCount(1);

        var concurrentHandlerType = concurrentEventHandlers.First().GetType();
        concurrentHandlerType.GetGenericTypeDefinition().Should().Be(typeof(UIEventForwarder<>));

        // Verify they are different generic specializations
        handlerType.Should().NotBe(concurrentHandlerType,
            "different event types should have different handler specializations");
    }

    [Fact]
    public async Task EndToEndEventFlow_DomainToUI_CompleteIntegration()
    {
        // Arrange: Set up UI subscriber
        var eventBus = GetEventBus();
        var subscriber = new MockSubscriber();
        eventBus.Subscribe<TestEvent>(subscriber, subscriber.HandleTestEvent);

        var mediator = GetMediator();
        var testEvent = new TestEvent(42, "end-to-end test");

        // Act: Publish event through MediatR (simulating domain layer)
        await mediator.Publish(testEvent);

        // Allow time for event processing
        await Task.Delay(50);

        // Assert: Event should reach UI subscriber
        subscriber.EventCount.Should().Be(1,
            "event should flow from MediatR through UIEventForwarder to UIEventBus");

        var receivedEvent = subscriber.GetLastEvent<TestEvent>();
        receivedEvent.Should().NotBeNull();
        receivedEvent!.Id.Should().Be(42);
        receivedEvent.Message.Should().Be("end-to-end test");
    }

    [Fact]
    public async Task MultipleEventTypes_DifferentHandlers_NoInterference()
    {
        // Arrange: Set up subscribers for different event types
        var eventBus = GetEventBus();
        var testEventSubscriber = new MockSubscriber();
        var concurrentEventSubscriber = new MockSubscriber();
        var lifetimeEventSubscriber = new MockSubscriber();

        eventBus.Subscribe<TestEvent>(testEventSubscriber, testEventSubscriber.HandleTestEvent);
        eventBus.Subscribe<ConcurrentTestEvent>(concurrentEventSubscriber, concurrentEventSubscriber.HandleConcurrentTestEvent);
        eventBus.Subscribe<LifetimeTestEvent>(lifetimeEventSubscriber, lifetimeEventSubscriber.HandleLifetimeTestEvent);

        var mediator = GetMediator();

        // Act: Publish different event types
        await mediator.Publish(new TestEvent(1, "test"));
        await mediator.Publish(new ConcurrentTestEvent(2, 1));
        await mediator.Publish(new LifetimeTestEvent(Guid.NewGuid()));
        await mediator.Publish(new TestEvent(2, "another test"));

        await Task.Delay(50);

        // Assert: Each subscriber should receive only their event type
        testEventSubscriber.EventCount.Should().Be(2, "should receive both TestEvents");
        concurrentEventSubscriber.EventCount.Should().Be(1, "should receive only ConcurrentTestEvent");
        lifetimeEventSubscriber.EventCount.Should().Be(1, "should receive only LifetimeTestEvent");

        // Verify event isolation
        testEventSubscriber.GetAllEvents<TestEvent>().Should().HaveCount(2);
        testEventSubscriber.GetAllEvents<ConcurrentTestEvent>().Should().BeEmpty();
        testEventSubscriber.GetAllEvents<LifetimeTestEvent>().Should().BeEmpty();
    }

    [Fact]
    public void HandlerLifetime_TransientInstances_NoAccumulation()
    {
        // Arrange: Get service provider
        var serviceProvider = GetServiceProvider();

        // Act: Resolve handlers multiple times
        var handler1 = serviceProvider.GetService<INotificationHandler<TestEvent>>();
        var handler2 = serviceProvider.GetService<INotificationHandler<TestEvent>>();
        var handler3 = serviceProvider.GetService<INotificationHandler<TestEvent>>();

        // Assert: Each resolution should create new instance
        handler1.Should().NotBeNull();
        handler2.Should().NotBeNull();
        handler3.Should().NotBeNull();

        // Verify they are different instances (transient lifetime)
        ReferenceEquals(handler1, handler2).Should().BeFalse(
            "handlers should be transient - new instance per resolution");
        ReferenceEquals(handler1, handler3).Should().BeFalse(
            "handlers should be transient - new instance per resolution");
        ReferenceEquals(handler2, handler3).Should().BeFalse(
            "handlers should be transient - new instance per resolution");
    }

    [Fact]
    public async Task ConcurrentMediatRPublishing_MultipleEventTypes_NoHandlerCorruption()
    {
        // Arrange: Set up subscribers for different event types
        var eventBus = GetEventBus();
        var testSubscriber = new MockSubscriber();
        var concurrentSubscriber = new MockSubscriber();

        eventBus.Subscribe<TestEvent>(testSubscriber, testSubscriber.HandleTestEvent);
        eventBus.Subscribe<ConcurrentTestEvent>(concurrentSubscriber, concurrentSubscriber.HandleConcurrentTestEvent);

        var mediator = GetMediator();
        const int eventsPerType = 50;

        // Act: Publish events concurrently through MediatR
        var testEventTasks = Enumerable.Range(0, eventsPerType)
            .Select(i => mediator.Publish(new TestEvent(i, $"test-{i}")))
            .ToArray();

        var concurrentEventTasks = Enumerable.Range(0, eventsPerType)
            .Select(i => mediator.Publish(new ConcurrentTestEvent(Thread.CurrentThread.ManagedThreadId, i)))
            .ToArray();

        await Task.WhenAll(testEventTasks.Concat(concurrentEventTasks));
        await Task.Delay(100); // Allow processing to complete

        // Assert: All events should be delivered correctly
        testSubscriber.EventCount.Should().Be(eventsPerType,
            "all TestEvents should be delivered through MediatR pipeline");

        concurrentSubscriber.EventCount.Should().Be(eventsPerType,
            "all ConcurrentTestEvents should be delivered through MediatR pipeline");

        // Verify no cross-contamination
        testSubscriber.GetAllEvents<ConcurrentTestEvent>().Should().BeEmpty(
            "TestEvent subscriber should not receive ConcurrentTestEvents");

        concurrentSubscriber.GetAllEvents<TestEvent>().Should().BeEmpty(
            "ConcurrentTestEvent subscriber should not receive TestEvents");

        // Verify event data integrity
        var receivedTestEvents = testSubscriber.GetAllEvents<TestEvent>();
        receivedTestEvents.Should().OnlyContain(e => e.Message.StartsWith("test-"),
            "TestEvent data should be preserved through pipeline");

        var uniqueIds = receivedTestEvents.Select(e => e.Id).Distinct().Count();
        uniqueIds.Should().Be(eventsPerType, "all TestEvent IDs should be unique");
    }

    [Fact]
    public async Task PipelineWithExceptions_UIEventForwarder_ContinuesOperation()
    {
        // Arrange: Set up subscriber that throws exceptions
        var eventBus = GetEventBus();
        var goodSubscriber = new MockSubscriber();
        var exceptionCount = 0;

        eventBus.Subscribe<TestEvent>(goodSubscriber, goodSubscriber.HandleTestEvent);
        eventBus.Subscribe<TestEvent>(this, (TestEvent evt) =>
        {
            exceptionCount++;
            throw new InvalidOperationException($"Test exception {evt.Id}");
        });

        var mediator = GetMediator();

        // Act: Publish events that will cause exceptions
        await mediator.Publish(new TestEvent(1, "exception test 1"));
        await mediator.Publish(new TestEvent(2, "exception test 2"));
        await mediator.Publish(new TestEvent(3, "exception test 3"));

        await Task.Delay(100);

        // Assert: Good subscriber should still receive events despite exceptions
        goodSubscriber.EventCount.Should().Be(3,
            "good subscriber should receive all events despite other subscriber exceptions");

        exceptionCount.Should().Be(3, "exception handler should be called for each event");

        // Verify system remains functional after exceptions
        await mediator.Publish(new TestEvent(4, "post-exception test"));
        await Task.Delay(50);

        goodSubscriber.EventCount.Should().Be(4,
            "event bus should remain functional after handler exceptions");
    }

    [Fact]
    public void MediatRRegistration_NoConflictingHandlers_CleanDiscovery()
    {
        // Arrange & Act: Inspect MediatR configuration for conflicting registrations
        var serviceProvider = GetServiceProvider();

        // Get all notification handler registrations from the container
        var allHandlerTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch { return Enumerable.Empty<Type>(); }
            })
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => t.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INotificationHandler<>)))
            .ToList();

        // Assert: Verify we have expected handler types and no conflicts
        var uiEventForwarderTypes = allHandlerTypes
            .Where(t => t.IsGenericTypeDefinition &&
                       t.GetGenericTypeDefinition() == typeof(UIEventForwarder<>))
            .ToList();

        uiEventForwarderTypes.Should().HaveCount(1,
            "should have exactly one UIEventForwarder<T> generic type definition");

        // Verify no deprecated handlers exist (like old GameManagerEventRouter)
        var deprecatedHandlers = allHandlerTypes
            .Where(t => t.Name.Contains("GameManagerEventRouter"))
            .ToList();

        deprecatedHandlers.Should().BeEmpty(
            "no deprecated event router handlers should exist - would cause TD_017 issue #1");

        // Verify UIEventForwarder can handle our test events
        var testEventForwarder = serviceProvider.GetService<INotificationHandler<TestEvent>>();
        testEventForwarder.Should().NotBeNull("UIEventForwarder<TestEvent> should be resolvable");
        testEventForwarder.Should().BeOfType<UIEventForwarder<TestEvent>>();
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
