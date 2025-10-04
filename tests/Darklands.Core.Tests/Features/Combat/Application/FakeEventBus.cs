using Darklands.Core.Infrastructure.Events;
using MediatR;

namespace Darklands.Core.Tests.Features.Combat.Application;

/// <summary>
/// Fake event bus for testing event publishing in handlers.
/// Captures published events for verification.
/// </summary>
public class FakeEventBus : IGodotEventBus
{
    private readonly List<INotification> _publishedEvents = new();

    public void Subscribe<TEvent>(object subscriber, Action<TEvent> handler) where TEvent : INotification
    {
        // Not needed for handler tests
        throw new NotImplementedException("Fake does not support Subscribe");
    }

    public void Unsubscribe<TEvent>(object subscriber) where TEvent : INotification
    {
        // Not needed for handler tests
        throw new NotImplementedException("Fake does not support Unsubscribe");
    }

    public void UnsubscribeAll(object subscriber)
    {
        // Not needed for handler tests
        throw new NotImplementedException("Fake does not support UnsubscribeAll");
    }

    public Task PublishAsync<TEvent>(TEvent eventData) where TEvent : INotification
    {
        _publishedEvents.Add(eventData);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Test helper: Get all published events of a specific type.
    /// </summary>
    public IReadOnlyList<T> GetPublishedEvents<T>() where T : INotification
    {
        return _publishedEvents.OfType<T>().ToList();
    }

    /// <summary>
    /// Test helper: Clear all published events.
    /// </summary>
    public void Clear()
    {
        _publishedEvents.Clear();
    }
}
