using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Health.Application;
using Darklands.Core.Features.Health.Domain;

namespace Darklands.Core.Features.Health.Infrastructure;

/// <summary>
/// Thread-safe in-memory registry for health components.
/// Uses Dictionary for O(1) lookups.
/// </summary>
public sealed class HealthComponentRegistry : IHealthComponentRegistry
{
    private readonly Dictionary<ActorId, IHealthComponent> _components = new();
    private readonly object _lock = new();

    /// <summary>
    /// Retrieves a health component for the specified actor.
    /// Thread-safe with lock.
    /// </summary>
    public Maybe<IHealthComponent> GetComponent(ActorId actorId)
    {
        lock (_lock)
        {
            return _components.TryGetValue(actorId, out var component)
                ? Maybe<IHealthComponent>.From(component)
                : Maybe<IHealthComponent>.None;
        }
    }

    /// <summary>
    /// Registers a health component for an actor.
    /// Overwrites existing component if present.
    /// </summary>
    public Result RegisterComponent(IHealthComponent component)
    {
        if (component == null)
            throw new ArgumentNullException(nameof(component));

        lock (_lock)
        {
            _components[component.OwnerId] = component;
            return Result.Success();
        }
    }

    /// <summary>
    /// Removes a health component for an actor.
    /// Returns success even if actor not found (idempotent).
    /// </summary>
    public Result UnregisterComponent(ActorId actorId)
    {
        lock (_lock)
        {
            _components.Remove(actorId);
            return Result.Success();
        }
    }

    /// <summary>
    /// Executes an operation on a health component while holding the registry lock.
    /// Prevents race conditions when multiple commands modify the same component concurrently.
    /// BR_001 FIX: Thread-safe component mutation.
    /// </summary>
    public Result<T> WithComponentLock<T>(ActorId actorId, Func<IHealthComponent, Result<T>> operation)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        lock (_lock)
        {
            // Lookup component (still inside lock)
            if (!_components.TryGetValue(actorId, out var component))
            {
                return Result.Failure<T>($"Actor {actorId} not found");
            }

            // Execute operation (component mutation happens inside lock)
            return operation(component);
        }
    }

    /// <summary>
    /// Gets the count of registered components (for testing/debugging).
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _components.Count;
            }
        }
    }
}
