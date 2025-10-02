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
/// </remarks>
public sealed class InMemoryInventoryRepository : IInventoryRepository
{
    private readonly Dictionary<ActorId, InventoryEntity> _inventoriesByActor = new();
    private readonly ILogger<InMemoryInventoryRepository> _logger;
    private const int DefaultCapacity = 20;

    public InMemoryInventoryRepository(ILogger<InMemoryInventoryRepository> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<Result<InventoryEntity>> GetByActorIdAsync(
        ActorId actorId,
        CancellationToken cancellationToken = default)
    {
        if (!_inventoriesByActor.TryGetValue(actorId, out var inventory))
        {
            // DESIGN DECISION: Auto-create inventory with default capacity
            // WHY: Every player-controlled actor needs an inventory. Explicit creation adds boilerplate.
            // ALTERNATIVE: Require explicit CreateInventoryCommand (more ceremony, no clear benefit for MVP)

            _logger.LogDebug(
                "Auto-creating inventory for actor {ActorId} with capacity {Capacity}",
                actorId,
                DefaultCapacity);

            inventory = InventoryEntity.Create(InventoryId.NewId(), DefaultCapacity).Value;
            _inventoriesByActor[actorId] = inventory;
        }

        return Task.FromResult(Result.Success(inventory));
    }

    public Task<Result> SaveAsync(
        InventoryEntity inventory,
        CancellationToken cancellationToken = default)
    {
        // In-memory: Update dictionary to handle entity replacement
        // Find the actor that owns this inventory and update it
        var actorId = _inventoriesByActor.FirstOrDefault(kvp => kvp.Value.Id == inventory.Id).Key;
        if (actorId != default)
        {
            _inventoriesByActor[actorId] = inventory;
        }

        // Future implementations: Write to SQLite/JSON here
        return Task.FromResult(Result.Success());
    }

    public Task<Result> DeleteAsync(
        InventoryId inventoryId,
        CancellationToken cancellationToken = default)
    {
        var toRemove = _inventoriesByActor.FirstOrDefault(kvp => kvp.Value.Id == inventoryId);
        if (toRemove.Key != default)
        {
            _inventoriesByActor.Remove(toRemove.Key);
            _logger.LogDebug("Deleted inventory {InventoryId}", inventoryId);
        }

        return Task.FromResult(Result.Success());
    }
}
