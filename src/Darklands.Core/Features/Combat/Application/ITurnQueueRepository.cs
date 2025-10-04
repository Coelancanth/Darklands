using CSharpFunctionalExtensions;
using Darklands.Core.Features.Combat.Domain;

namespace Darklands.Core.Features.Combat.Application;

/// <summary>
/// Repository contract for managing turn queue persistence.
/// Defined in Application layer (Dependency Inversion Principle).
/// Implemented in Infrastructure layer.
/// </summary>
/// <remarks>
/// SINGLETON PATTERN: Only ONE turn queue exists per game session.
/// Unlike inventory (one per actor), turn queue is global combat state.
///
/// ASYNC DESIGN: Methods are async for consistency with other repositories,
/// even though in-memory implementation is synchronous.
/// </remarks>
public interface ITurnQueueRepository
{
    /// <summary>
    /// Retrieves the global turn queue.
    /// Auto-creates with player if doesn't exist.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing TurnQueue or error message</returns>
    Task<Result<TurnQueue>> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists turn queue changes.
    /// </summary>
    /// <param name="turnQueue">TurnQueue to save</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success or failure result</returns>
    Task<Result> SaveAsync(TurnQueue turnQueue, CancellationToken cancellationToken = default);
}
