using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Inventory.Application;
using Microsoft.Extensions.Logging;
using InventoryEntity = Darklands.Core.Features.Inventory.Domain.Inventory;
using InventoryId = Darklands.Core.Features.Inventory.Domain.InventoryId;

namespace Darklands.Core.Features.Inventory.Infrastructure;

/// <summary>
/// In-memory inventory repository for MVP.
/// </summary>
/// <remarks>
/// THREAD SAFETY: Not thread-safe by design (single-player game, Godot main thread only).
/// PERSISTENCE: State lost on application restart (acceptable for MVP).
/// FUTURE: Replace with SQLite/JSON persistence without changing interface.
///
/// TD_019: Inventory-First Architecture
/// - Primary storage: Dictionary<InventoryId, Inventory> (key changed from ActorId)
/// - Supports world containers (OwnerId = null)
/// - Supports multi-inventory actors (query by owner returns list)
/// </remarks>
public sealed class InMemoryInventoryRepository : IInventoryRepository
{
    private readonly Dictionary<InventoryId, InventoryEntity> _inventories = new(); // TD_019: Changed key from ActorId
    private readonly ILogger<InMemoryInventoryRepository> _logger;
    private const int DefaultCapacity = 20;

    public InMemoryInventoryRepository(ILogger<InMemoryInventoryRepository> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<Result<InventoryEntity>> GetByIdAsync(
        InventoryId inventoryId,
        CancellationToken cancellationToken = default)
    {
        if (!_inventories.TryGetValue(inventoryId, out var inventory))
        {
            return Task.FromResult(Result.Failure<InventoryEntity>("ERROR_INVENTORY_NOT_FOUND"));
        }

        return Task.FromResult(Result.Success(inventory));
    }

    public Task<Result<IReadOnlyList<InventoryEntity>>> GetByOwnerAsync(
        ActorId ownerId,
        CancellationToken cancellationToken = default)
    {
        var owned = _inventories.Values
            .Where(inv => inv.OwnerId == ownerId)
            .ToList()
            .AsReadOnly();

        return Task.FromResult(Result.Success<IReadOnlyList<InventoryEntity>>(owned));
    }

    // TD_019 Phase 5: Removed obsolete GetByActorIdAsync method
    // All callers now use GetByIdAsync or GetByOwnerAsync explicitly

    public Task<Result> SaveAsync(
        InventoryEntity inventory,
        CancellationToken cancellationToken = default)
    {
        // TD_019: Store by InventoryId (primary key)
        _inventories[inventory.Id] = inventory;
        return Task.FromResult(Result.Success());
    }

    /// <summary>
    /// Registers an inventory (test/manual setup only).
    /// </summary>
    /// <remarks>
    /// WHY: Test scenes need to create inventories with specific dimensions.
    /// TD_019: Updated to support world containers (no owner required).
    /// </remarks>
    public void RegisterInventory(InventoryEntity inventory)
    {
        _inventories[inventory.Id] = inventory;
        _logger.LogDebug(
            "Registered inventory {InventoryId} (Owner: {OwnerId})",
            inventory.Id,
            inventory.OwnerId?.ToString() ?? "none");
    }

    public Task<Result> DeleteAsync(
        InventoryId inventoryId,
        CancellationToken cancellationToken = default)
    {
        // TD_019: Delete by InventoryId (primary key)
        if (_inventories.Remove(inventoryId))
        {
            _logger.LogDebug("Deleted inventory {InventoryId}", inventoryId);
        }

        return Task.FromResult(Result.Success());
    }
}
