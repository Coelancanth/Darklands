using CSharpFunctionalExtensions;
using Darklands.Core.Application.Repositories;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Equipment.Domain;
using Darklands.Core.Features.Inventory.Application;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.Equipment.Application.Commands;

/// <summary>
/// Handles UnequipItemCommand - atomic operation moving item from equipment slot to inventory.
/// Implements rollback on failure to maintain consistency.
/// </summary>
public sealed class UnequipItemCommandHandler : IRequestHandler<UnequipItemCommand, Result<ItemId>>
{
    private readonly IActorRepository _actors;
    private readonly IInventoryRepository _inventories;
    private readonly ILogger<UnequipItemCommandHandler> _logger;

    public UnequipItemCommandHandler(
        IActorRepository actors,
        IInventoryRepository inventories,
        ILogger<UnequipItemCommandHandler> logger)
    {
        _actors = actors ?? throw new ArgumentNullException(nameof(actors));
        _inventories = inventories ?? throw new ArgumentNullException(nameof(inventories));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<ItemId>> Handle(
        UnequipItemCommand cmd,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Unequipping item from {Slot} for actor {ActorId}",
            cmd.Slot,
            cmd.ActorId);

        // 1. Get actor and validate has EquipmentComponent
        var actorResult = await _actors.GetByIdAsync(cmd.ActorId);
        if (actorResult.IsFailure)
        {
            return Result.Failure<ItemId>($"Actor {cmd.ActorId} not found");
        }

        var actor = actorResult.Value;

        if (!actor.HasComponent<IEquipmentComponent>())
        {
            return Result.Failure<ItemId>("ERROR_EQUIPMENT_NO_EQUIPMENT_COMPONENT");
        }

        var equipmentComp = actor.GetComponent<IEquipmentComponent>().Value;

        // 2. Get inventory
        var inventoryResult = await _inventories.GetByActorIdAsync(cmd.ActorId, cancellationToken);
        if (inventoryResult.IsFailure)
        {
            return Result.Failure<ItemId>($"Failed to get inventory: {inventoryResult.Error}");
        }

        var inventory = inventoryResult.Value;

        // 3. Check if item is two-handed BEFORE unequipping (needed for rollback)
        var isTwoHanded = equipmentComp.IsSlotOccupied(EquipmentSlot.MainHand) &&
                          equipmentComp.IsSlotOccupied(EquipmentSlot.OffHand) &&
                          equipmentComp.GetEquippedItem(EquipmentSlot.MainHand).IsSuccess &&
                          equipmentComp.GetEquippedItem(EquipmentSlot.OffHand).IsSuccess &&
                          equipmentComp.GetEquippedItem(EquipmentSlot.MainHand).Value ==
                          equipmentComp.GetEquippedItem(EquipmentSlot.OffHand).Value;

        // 4. Remove item from equipment slot (ATOMIC POINT - if succeeds, MUST add to inventory or restore)
        var unequipResult = equipmentComp.UnequipItem(cmd.Slot);
        if (unequipResult.IsFailure)
        {
            return Result.Failure<ItemId>(unequipResult.Error);
        }

        var itemId = unequipResult.Value;

        // 5. Add item to inventory (if fails, restore to equipment slot)
        var addResult = inventory.AddItem(itemId);
        if (addResult.IsFailure)
        {
            // ROLLBACK: Restore item to equipment slot
            var restoreResult = equipmentComp.EquipItem(cmd.Slot, itemId, isTwoHanded);
            if (restoreResult.IsFailure)
            {
                // CRITICAL: Item lost! Log error
                _logger.LogError(
                    "CRITICAL: Item {ItemId} lost during rollback for actor {ActorId}. Inventory add failed: {AddError}, Equipment restore failed: {RestoreError}",
                    itemId,
                    cmd.ActorId,
                    addResult.Error,
                    restoreResult.Error);
                return Result.Failure<ItemId>($"Item lost during transaction (contact support): {addResult.Error}");
            }

            return Result.Failure<ItemId>(addResult.Error);
        }

        // 6. Save inventory (actor changes are persisted via reference)
        var saveInventoryResult = await _inventories.SaveAsync(inventory, cancellationToken);
        if (saveInventoryResult.IsFailure)
        {
            _logger.LogError(
                "Failed to save inventory after unequipping {ItemId}: {Error}",
                itemId,
                saveInventoryResult.Error);
            return Result.Failure<ItemId>($"Failed to save inventory: {saveInventoryResult.Error}");
        }

        _logger.LogInformation(
            "Unequipped item {ItemId} from {Slot} for actor {ActorId}",
            itemId,
            cmd.Slot,
            cmd.ActorId);

        return Result.Success(itemId);
    }
}
