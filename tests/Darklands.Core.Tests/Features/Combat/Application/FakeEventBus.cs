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

    public void Publish<T>(T domainEvent) where T : INotification
    {
        _publishedEvents.Add(domainEvent);
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
