using MediatR;

namespace Darklands.Core.Domain.Events;

/// <summary>
/// Simple test event for validating the event bus implementation.
/// Will be used in Phase 2/3 tests to verify MediatR → GodotEventBus flow.
/// </summary>
public record TestEvent(string Message) : INotification;
