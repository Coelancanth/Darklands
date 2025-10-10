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
/// TD_019: Inventory-First Architecture
/// - Primary key: InventoryId (use GetByIdAsync)
/// - Ownership: Optional ActorId (use GetByOwnerAsync)
/// - World containers: OwnerId = null (loot, chests)
/// </remarks>
public interface IInventoryRepository
{
    /// <summary>
    /// Retrieves inventory by ID.
    /// </summary>
    /// <param name="inventoryId">Inventory identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing Inventory or error message</returns>
    Task<Result<InventoryEntity>> GetByIdAsync(
        InventoryId inventoryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all inventories owned by an actor.
    /// </summary>
    /// <param name="ownerId">Actor who owns the inventories</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing list of owned inventories (may be empty)</returns>
    Task<Result<IReadOnlyList<InventoryEntity>>> GetByOwnerAsync(
        ActorId ownerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an actor's primary inventory. Auto-creates if doesn't exist.
    /// </summary>
    /// <param name="actorId">Actor whose inventory to retrieve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing Inventory or error message</returns>
    /// <remarks>
    /// DEPRECATED: Use GetByOwnerAsync for explicit multi-inventory support.
    /// This method returns the FIRST owned inventory or auto-creates one.
    /// Preserved for backward compatibility with existing command handlers.
    /// </remarks>
    [Obsolete("Use GetByIdAsync (with explicit InventoryId) or GetByOwnerAsync (for actor-owned inventories). This method will be removed in a future version.")]
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
