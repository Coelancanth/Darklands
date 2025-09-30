using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Health.Domain;

namespace Darklands.Core.Features.Health.Application;

/// <summary>
/// Registry for managing health components by actor ID.
/// Application-layer abstraction for component lookup.
/// </summary>
public interface IHealthComponentRegistry
{
    /// <summary>
    /// Retrieves a health component for the specified actor.
    /// </summary>
    /// <param name="actorId">The actor ID to look up</param>
    /// <returns>Maybe containing the component if found</returns>
    Maybe<IHealthComponent> GetComponent(ActorId actorId);

    /// <summary>
    /// Registers a health component for an actor.
    /// </summary>
    /// <param name="component">The component to register</param>
    /// <returns>Result indicating success or failure</returns>
    Result RegisterComponent(IHealthComponent component);

    /// <summary>
    /// Removes a health component for an actor.
    /// </summary>
    /// <param name="actorId">The actor ID to remove</param>
    /// <returns>Result indicating success or failure</returns>
    Result UnregisterComponent(ActorId actorId);

    /// <summary>
    /// Executes an operation on a health component while holding the registry lock.
    /// Prevents race conditions when multiple commands modify the same component concurrently.
    /// BR_001 FIX: Thread-safe component mutation.
    /// </summary>
    /// <typeparam name="T">The result type of the operation</typeparam>
    /// <param name="actorId">The actor ID to look up</param>
    /// <param name="operation">The operation to execute on the component (inside lock)</param>
    /// <returns>Result of the operation, or failure if actor not found</returns>
    Result<T> WithComponentLock<T>(ActorId actorId, Func<IHealthComponent, Result<T>> operation);
}
