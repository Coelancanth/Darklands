using CSharpFunctionalExtensions;
using Darklands.Core.Application.Repositories;
using Darklands.Core.Features.Equipment.Domain;
using Darklands.Core.Features.Inventory.Application;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.Equipment.Application.Commands;

/// <summary>
/// Handles EquipItemCommand - atomic operation moving item from inventory to equipment slot.
/// Implements rollback on failure to maintain consistency.
/// </summary>
public sealed class EquipItemCommandHandler : IRequestHandler<EquipItemCommand, Result>
{
    private readonly IActorRepository _actors;
    private readonly IInventoryRepository _inventories;
    private readonly ILogger<EquipItemCommandHandler> _logger;

    public EquipItemCommandHandler(
        IActorRepository actors,
        IInventoryRepository inventories,
        ILogger<EquipItemCommandHandler> logger)
    {
        _actors = actors ?? throw new ArgumentNullException(nameof(actors));
        _inventories = inventories ?? throw new ArgumentNullException(nameof(inventories));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(
        EquipItemCommand cmd,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Equipping item {ItemId} to {Slot} for actor {ActorId} (two-handed: {IsTwoHanded})",
            cmd.ItemId,
            cmd.Slot,
            cmd.ActorId,
            cmd.IsTwoHanded);

        // 1. Get actor and validate has EquipmentComponent
        var actorResult = await _actors.GetByIdAsync(cmd.ActorId);
        if (actorResult.IsFailure)
        {
            return Result.Failure($"Actor {cmd.ActorId} not found");
        }

        var actor = actorResult.Value;

        if (!actor.HasComponent<IEquipmentComponent>())
        {
            // Actor doesn't have equipment component - add one
            var equipment = new EquipmentComponent(cmd.ActorId);
            var addResult = actor.AddComponent<IEquipmentComponent>(equipment);
            if (addResult.IsFailure)
            {
                return Result.Failure($"Failed to add equipment component: {addResult.Error}");
            }

            _logger.LogDebug("Added EquipmentComponent to actor {ActorId}", cmd.ActorId);
        }

        var equipmentComp = actor.GetComponent<IEquipmentComponent>().Value;

        // 2. Get inventory and validate item exists
        var inventoryResult = await _inventories.GetByActorIdAsync(cmd.ActorId, cancellationToken);
        if (inventoryResult.IsFailure)
        {
            return Result.Failure($"Failed to get inventory: {inventoryResult.Error}");
        }

        var inventory = inventoryResult.Value;

        if (!inventory.Contains(cmd.ItemId))
        {
            return Result.Failure("ERROR_EQUIPMENT_ITEM_NOT_IN_INVENTORY");
        }

        // 3. Validate equipment slot availability
        var slotCheckResult = equipmentComp.EquipItem(cmd.Slot, cmd.ItemId, cmd.IsTwoHanded);
        if (slotCheckResult.IsFailure)
        {
            return Result.Failure(slotCheckResult.Error);
        }

        // Rollback slot (we just validated, haven't removed from inventory yet)
        var unequipResult = equipmentComp.UnequipItem(cmd.Slot);
        if (unequipResult.IsFailure)
        {
            // This should never happen since we just equipped it
            _logger.LogError(
                "CRITICAL: Failed to rollback test equip for {ItemId} slot {Slot}: {Error}",
                cmd.ItemId,
                cmd.Slot,
                unequipResult.Error);
            return Result.Failure($"Internal error during slot validation: {unequipResult.Error}");
        }

        // 4. Remove item from inventory (ATOMIC POINT - if this succeeds, we MUST equip or restore)
        var removeResult = inventory.RemoveItem(cmd.ItemId);
        if (removeResult.IsFailure)
        {
            return Result.Failure($"Failed to remove item from inventory: {removeResult.Error}");
        }

        // 5. Equip item (if fails, restore to inventory)
        var equipResult = equipmentComp.EquipItem(cmd.Slot, cmd.ItemId, cmd.IsTwoHanded);
        if (equipResult.IsFailure)
        {
            // ROLLBACK: Restore item to inventory
            var restoreResult = inventory.AddItem(cmd.ItemId);
            if (restoreResult.IsFailure)
            {
                // CRITICAL: Item lost! Log error
                _logger.LogError(
                    "CRITICAL: Item {ItemId} lost during rollback for actor {ActorId}. Equip failed: {EquipError}, Restore failed: {RestoreError}",
                    cmd.ItemId,
                    cmd.ActorId,
                    equipResult.Error,
                    restoreResult.Error);
                return Result.Failure($"Item lost during transaction (contact support): {equipResult.Error}");
            }

            return Result.Failure(equipResult.Error);
        }

        // 6. Save inventory (actor changes are persisted via reference)
        var saveInventoryResult = await _inventories.SaveAsync(inventory, cancellationToken);
        if (saveInventoryResult.IsFailure)
        {
            _logger.LogError(
                "Failed to save inventory after equipping {ItemId}: {Error}",
                cmd.ItemId,
                saveInventoryResult.Error);
            return Result.Failure($"Failed to save inventory: {saveInventoryResult.Error}");
        }

        _logger.LogInformation(
            "Equipped item {ItemId} to {Slot} for actor {ActorId}",
            cmd.ItemId,
            cmd.Slot,
            cmd.ActorId);

        return Result.Success();
    }
}
