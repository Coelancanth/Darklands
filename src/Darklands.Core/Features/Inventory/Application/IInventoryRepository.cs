using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using InventoryEntity = Darklands.Core.Features.Inventory.Domain.Inventory;
using InventoryId = Darklands.Core.Features.Inventory.Domain.InventoryId;

namespace Darklands.Core.Features.Inventory.Application;

/// <summary>
/// Repository contract for managing inventory persistence.
/// Defined in Application layer (Dependency Inversion Principle).
/// Implemented in Infrastructure layer.
/// </summary>
/// <remarks>
/// ASYNC DESIGN: Methods are async even though in-memory implementation is synchronous.
/// This future-proofs for SQLite/JSON persistence without changing consumers.
///
/// AUTO-CREATION: GetByActorIdAsync auto-creates inventory if it doesn't exist.
/// This eliminates boilerplate for MVP (all player-controlled actors need inventories).
/// </remarks>
public interface IInventoryRepository
{
    /// <summary>
    /// Retrieves an actor's inventory. Auto-creates if doesn't exist.
    /// </summary>
    /// <param name="actorId">Actor whose inventory to retrieve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing Inventory or error message</returns>
    Task<Result<InventoryEntity>> GetByActorIdAsync(
        ActorId actorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists inventory changes.
    /// </summary>
    /// <param name="inventory">Inventory to save</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success or failure result</returns>
    Task<Result> SaveAsync(
        InventoryEntity inventory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an inventory (e.g., when actor dies or inventory is dropped).
    /// </summary>
    /// <param name="inventoryId">Inventory to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success or failure result</returns>
    Task<Result> DeleteAsync(
        InventoryId inventoryId,
        CancellationToken cancellationToken = default);
}
