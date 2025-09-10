using MediatR;

namespace Darklands.Core.Tests.Infrastructure.Events;

/// <summary>
/// Test events for integration testing the MediatR→UIEventBus pipeline.
/// These events simulate real domain events without requiring domain dependencies.
/// </summary>
public sealed record TestEvent(int Id, string Message = "Test") : INotification;

public sealed record ConcurrentTestEvent(int ThreadId, int EventNumber) : INotification;

public sealed record LifetimeTestEvent(Guid SubscriberId) : INotification;

public sealed record CleanupTestEvent(string Data) : INotification;

/// <summary>
/// Mock subscriber that can be used for testing event delivery and cleanup.
/// Tracks received events for verification and supports weak reference testing.
/// </summary>
public sealed class MockSubscriber
{
    private readonly List<object> _receivedEvents = new();
    private readonly object _lock = new();
    public string Id { get; } = Guid.NewGuid().ToString();

    public IReadOnlyList<object> ReceivedEvents
    {
        get
        {
            lock (_lock)
            {
                return _receivedEvents.ToList();
            }
        }
    }

    public int EventCount => ReceivedEvents.Count;

    public void HandleTestEvent(TestEvent evt)
    {
        lock (_lock)
        {
            _receivedEvents.Add(evt);
        }
    }

    public void HandleConcurrentTestEvent(ConcurrentTestEvent evt)
    {
        lock (_lock)
        {
            _receivedEvents.Add(evt);
        }
    }

    public void HandleLifetimeTestEvent(LifetimeTestEvent evt)
    {
        lock (_lock)
        {
            _receivedEvents.Add(evt);
        }
    }

    public void HandleCleanupTestEvent(CleanupTestEvent evt)
    {
        lock (_lock)
        {
            _receivedEvents.Add(evt);
        }
    }

    public T? GetLastEvent<T>() where T : class
    {
        lock (_lock)
        {
            return _receivedEvents.OfType<T>().LastOrDefault();
        }
    }

    public List<T> GetAllEvents<T>() where T : class
    {
        lock (_lock)
        {
            return _receivedEvents.OfType<T>().ToList();
        }
    }
}
