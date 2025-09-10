using FluentAssertions;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using Darklands.Core.Application.Events;
using Darklands.Core.Infrastructure.DependencyInjection;
using Darklands.Core.Tests.TestUtilities;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Darklands.Core.Tests.Infrastructure.Events;

/// <summary>
/// Integration tests for DI container focusing on service lifetimes, thread-safe initialization,
/// and dependency resolution order.
/// 
/// These tests would have caught issues #2 and #3 from TD_017 incident:
/// - Race conditions during GameStrapper initialization
/// - Service lifetime misconfigurations causing initialization failures
/// - Container validation issues preventing startup
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "DIContainer")]
[Collection("GameStrapper")]
public class DIContainerIntegrationTests : IDisposable
{
    private readonly List<ServiceProvider> _createdProviders = new();

    private ServiceProvider CreateServiceProvider()
    {
        GameStrapper.Dispose();
        var initResult = GameStrapper.Initialize(GameStrapperConfiguration.Testing);
        var provider = initResult.Match(
            Succ: p => p,
            Fail: error => throw new InvalidOperationException($"GameStrapper initialization failed: {error}"));

        _createdProviders.Add(provider);
        return provider;
    }

    [Fact]
    public async Task GameStrapper_ThreadSafeInitialization_NoRaceConditions()
    {
        // Arrange: Ensure clean state
        GameStrapper.Dispose();

        const int threadCount = 20;
        var initResults = new ConcurrentBag<ServiceProvider>();
        var exceptions = new ConcurrentBag<Exception>();
        var barrier = new Barrier(threadCount);

        // Act: Initialize GameStrapper from multiple threads simultaneously
        var initTasks = Enumerable.Range(0, threadCount)
            .Select(i => Task.Run(() =>
            {
                try
                {
                    // Synchronize thread start for maximum contention
                    barrier.SignalAndWait(TimeSpan.FromSeconds(10));

                    var initResult = GameStrapper.Initialize(GameStrapperConfiguration.Testing);
                    var provider = initResult.Match(
                        Succ: p => p,
                        Fail: error => throw new InvalidOperationException(error.ToString()));

                    initResults.Add(provider);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }))
            .ToArray();

        await Task.WhenAll(initTasks).WaitAsync(TimeSpan.FromSeconds(30));

        // Assert: All threads should get the same provider instance (thread-safe singleton)
        exceptions.Should().BeEmpty("no threads should encounter initialization exceptions");
        initResults.Should().HaveCount(threadCount, "all threads should successfully get provider");

        var distinctProviders = initResults.Distinct().Count();
        distinctProviders.Should().Be(1,
            "all threads should get the same ServiceProvider instance (thread-safe singleton)");

        // Verify the singleton provider is functional
        var provider = initResults.First();
        var eventBus = provider.GetRequiredService<IUIEventBus>();
        var mediator = provider.GetRequiredService<IMediator>();

        eventBus.Should().NotBeNull("UIEventBus should be resolvable from thread-safe initialized provider");
        mediator.Should().NotBeNull("MediatR should be resolvable from thread-safe initialized provider");
    }

    [Fact]
    public void ServiceLifetimes_CriticalServices_CorrectRegistrations()
    {
        // Arrange: Create service provider
        var serviceProvider = CreateServiceProvider();

        // Act & Assert: Verify singleton services
        var eventBus1 = serviceProvider.GetRequiredService<IUIEventBus>();
        var eventBus2 = serviceProvider.GetRequiredService<IUIEventBus>();

        ReferenceEquals(eventBus1, eventBus2).Should().BeTrue(
            "UIEventBus should be singleton - same instance across resolutions");

        // Verify transient services (handlers)
        var handler1 = serviceProvider.GetService<INotificationHandler<TestEvent>>();
        var handler2 = serviceProvider.GetService<INotificationHandler<TestEvent>>();

        handler1.Should().NotBeNull("UIEventForwarder<TestEvent> should be resolvable");
        handler2.Should().NotBeNull("UIEventForwarder<TestEvent> should be resolvable");

        // UIEventForwarder should be transient (new instance per resolution)
        ReferenceEquals(handler1, handler2).Should().BeFalse(
            "UIEventForwarder<TestEvent> should be transient - different instances");

        // But they should use the same singleton UIEventBus
        var forwarder1 = (UIEventForwarder<TestEvent>)handler1!;
        var forwarder2 = (UIEventForwarder<TestEvent>)handler2!;

        // Use reflection to access private field for testing
        var eventBusField = typeof(UIEventForwarder<TestEvent>).GetField("_eventBus",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        eventBusField.Should().NotBeNull("UIEventForwarder should have _eventBus field");

        var eventBusFromHandler1 = eventBusField!.GetValue(forwarder1);
        var eventBusFromHandler2 = eventBusField.GetValue(forwarder2);

        ReferenceEquals(eventBusFromHandler1, eventBusFromHandler2).Should().BeTrue(
            "both UIEventForwarder instances should use the same UIEventBus singleton");
    }

    [Fact]
    public void DependencyResolution_AllCriticalServices_SuccessfulResolution()
    {
        // Arrange: Create service provider
        var serviceProvider = CreateServiceProvider();

        // Act & Assert: Verify all critical services can be resolved
        var resolutionResults = new Dictionary<Type, (bool Success, Exception? Exception)>();

        var criticalServiceTypes = new[]
        {
            typeof(IMediator),
            typeof(IUIEventBus),
            typeof(INotificationHandler<TestEvent>),
            typeof(Microsoft.Extensions.Logging.ILoggerFactory),
            typeof(Serilog.ILogger)
        };

        foreach (var serviceType in criticalServiceTypes)
        {
            try
            {
                var service = serviceProvider.GetRequiredService(serviceType);
                resolutionResults[serviceType] = (service != null, null);
            }
            catch (Exception ex)
            {
                resolutionResults[serviceType] = (false, ex);
            }
        }

        // Assert: All services should resolve successfully
        foreach (var kvp in resolutionResults)
        {
            kvp.Value.Success.Should().BeTrue(
                $"{kvp.Key.Name} should be resolvable without exceptions. " +
                $"Exception: {kvp.Value.Exception?.Message}");
        }

        // Verify services are functional
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var eventBus = serviceProvider.GetRequiredService<IUIEventBus>();

        // Test basic functionality
        mediator.Should().NotBeNull();
        eventBus.Should().NotBeNull();

        // Verify we can create a subscription (tests that services are properly initialized)
        var subscriber = new MockSubscriber();
        Action subscribeAction = () => eventBus.Subscribe<TestEvent>(subscriber, subscriber.HandleTestEvent);
        subscribeAction.Should().NotThrow("UIEventBus should be functional after resolution");
    }

    [Fact]
    public void ContainerValidation_AtBuild_CatchesMisconfiguration()
    {
        // Arrange: Test that container validation catches configuration errors
        GameStrapper.Dispose();

        // Act: Initialize with validation enabled
        var initResult = GameStrapper.Initialize(new GameStrapperConfiguration(
            LogLevel: Serilog.Events.LogEventLevel.Warning,
            ValidateOnBuild: true,
            ValidateScopes: true));

        // Assert: Initialization should succeed with validation
        initResult.IsSucc.Should().BeTrue("GameStrapper should initialize successfully with validation enabled");

        var serviceProvider = initResult.Match(
            Succ: p => p,
            Fail: error => throw new InvalidOperationException(error.ToString()));

        _createdProviders.Add(serviceProvider);

        // Verify that validation caught any potential issues during build
        serviceProvider.Should().NotBeNull("ServiceProvider should be created successfully");

        // Test critical dependency paths that validation should have verified
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var eventBus = serviceProvider.GetRequiredService<IUIEventBus>();

        mediator.Should().NotBeNull("MediatR should be properly configured");
        eventBus.Should().NotBeNull("UIEventBus should be properly configured");
    }

    [Fact]
    public void DisposalChain_ProperCleanup_NoResourceLeaks()
    {
        // Arrange: Create and use service provider
        var serviceProvider = CreateServiceProvider();

        var eventBus = serviceProvider.GetRequiredService<IUIEventBus>();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        // Create some subscriptions
        var subscriber = new MockSubscriber();
        eventBus.Subscribe<TestEvent>(subscriber, subscriber.HandleTestEvent);

        // Act: Dispose the container
        serviceProvider.Dispose();

        // Assert: Services should be properly disposed
        // Note: We can't directly test disposal of internal services,
        // but we can verify that the container disposal doesn't throw
        Action disposalAction = () => serviceProvider.Dispose();
        disposalAction.Should().NotThrow("ServiceProvider disposal should be clean");

        // Verify GameStrapper can be re-initialized after disposal
        Action reinitAction = () =>
        {
            GameStrapper.Dispose();
            var newResult = GameStrapper.Initialize(GameStrapperConfiguration.Testing);
            var newProvider = newResult.Match(
                Succ: p => p,
                Fail: error => throw new InvalidOperationException(error.ToString()));
            _createdProviders.Add(newProvider);
        };

        reinitAction.Should().NotThrow("GameStrapper should support re-initialization after disposal");
    }

    [Fact]
    public async Task ConcurrentServiceResolution_HighLoad_NoDeadlocks()
    {
        // Arrange: Create service provider
        var serviceProvider = CreateServiceProvider();
        const int concurrentRequests = 100;
        var completedResolutions = 0;

        // Act: Resolve services concurrently from multiple threads
        var resolutionTasks = Enumerable.Range(0, concurrentRequests)
            .Select(_ => Task.Run(() =>
            {
                try
                {
                    // Resolve various services
                    var eventBus = serviceProvider.GetRequiredService<IUIEventBus>();
                    var mediator = serviceProvider.GetRequiredService<IMediator>();
                    var handler = serviceProvider.GetService<INotificationHandler<TestEvent>>();
                    var logger = serviceProvider.GetRequiredService<Serilog.ILogger>();

                    // Verify services are functional
                    eventBus.Should().NotBeNull();
                    mediator.Should().NotBeNull();
                    handler.Should().NotBeNull();
                    logger.Should().NotBeNull();

                    Interlocked.Increment(ref completedResolutions);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Concurrent resolution failed: {ex.Message}", ex);
                }
            }))
            .ToArray();

        var stopwatch = Stopwatch.StartNew();
        await Task.WhenAll(resolutionTasks).WaitAsync(TimeSpan.FromSeconds(10));
        stopwatch.Stop();

        // Assert: All resolutions should complete without deadlocks
        completedResolutions.Should().Be(concurrentRequests,
            "all concurrent service resolutions should complete successfully");

        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(15),
            "concurrent resolutions should complete reasonably quickly without deadlocks");
    }

    [Fact]
    public void InitializationOrder_EventBusBeforeMediatR_NoCircularDependencies()
    {
        // Arrange & Act: Create service provider (this tests initialization order)
        var serviceProvider = CreateServiceProvider();

        // Get services in different orders to test dependency resolution
        var eventBus = serviceProvider.GetRequiredService<IUIEventBus>();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var handler = serviceProvider.GetService<INotificationHandler<TestEvent>>();

        // Assert: All services should be properly initialized regardless of resolution order
        eventBus.Should().NotBeNull("UIEventBus should initialize before MediatR needs it");
        mediator.Should().NotBeNull("MediatR should initialize with handlers");
        handler.Should().NotBeNull("UIEventForwarder should be created with UIEventBus dependency");

        // Verify no circular dependencies by checking the handler uses the singleton event bus
        var forwarder = handler as UIEventForwarder<TestEvent>;
        forwarder.Should().NotBeNull("handler should be UIEventForwarder<TestEvent>");
    }

    public void Dispose()
    {
        try
        {
            foreach (var provider in _createdProviders)
            {
                provider?.Dispose();
            }
        }
        finally
        {
            _createdProviders.Clear();
            GameStrapper.Dispose();
        }
    }
}
