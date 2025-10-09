using CSharpFunctionalExtensions;
using Darklands.Core.Application.Repositories;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Equipment.Domain;
using Darklands.Core.Features.Inventory.Application;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.Equipment.Application.Commands;

/// <summary>
/// Handles SwapEquipmentCommand - atomic operation swapping equipped item with inventory item.
/// Implements complex multi-step rollback on failure to maintain consistency.
/// </summary>
public sealed class SwapEquipmentCommandHandler : IRequestHandler<SwapEquipmentCommand, Result<ItemId>>
{
    private readonly IActorRepository _actors;
    private readonly IInventoryRepository _inventories;
    private readonly ILogger<SwapEquipmentCommandHandler> _logger;

    public SwapEquipmentCommandHandler(
        IActorRepository actors,
        IInventoryRepository inventories,
        ILogger<SwapEquipmentCommandHandler> logger)
    {
        _actors = actors ?? throw new ArgumentNullException(nameof(actors));
        _inventories = inventories ?? throw new ArgumentNullException(nameof(inventories));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<ItemId>> Handle(
        SwapEquipmentCommand cmd,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Swapping equipment at {Slot} for actor {ActorId}: New item {NewItemId} (two-handed: {IsTwoHanded})",
            cmd.Slot,
            cmd.ActorId,
            cmd.NewItemId,
            cmd.IsTwoHanded);

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

        // 2. Validate slot is occupied (swap requires existing item)
        if (!equipmentComp.IsSlotOccupied(cmd.Slot))
        {
            return Result.Failure<ItemId>("ERROR_EQUIPMENT_SLOT_EMPTY_CANNOT_SWAP");
        }

        // 3. Get inventory and validate new item exists
        var inventoryResult = await _inventories.GetByActorIdAsync(cmd.ActorId, cancellationToken);
        if (inventoryResult.IsFailure)
        {
            return Result.Failure<ItemId>($"Failed to get inventory: {inventoryResult.Error}");
        }

        var inventory = inventoryResult.Value;

        if (!inventory.Contains(cmd.NewItemId))
        {
            return Result.Failure<ItemId>("ERROR_EQUIPMENT_ITEM_NOT_IN_INVENTORY");
        }

        // 4. Check if old item is two-handed BEFORE unequipping (needed for rollback)
        var oldItemIsTwoHanded = equipmentComp.IsSlotOccupied(EquipmentSlot.MainHand) &&
                                  equipmentComp.IsSlotOccupied(EquipmentSlot.OffHand) &&
                                  equipmentComp.GetEquippedItem(EquipmentSlot.MainHand).IsSuccess &&
                                  equipmentComp.GetEquippedItem(EquipmentSlot.OffHand).IsSuccess &&
                                  equipmentComp.GetEquippedItem(EquipmentSlot.MainHand).Value ==
                                  equipmentComp.GetEquippedItem(EquipmentSlot.OffHand).Value;

        // 5. Unequip old item (ATOMIC STEP 1)
        var unequipResult = equipmentComp.UnequipItem(cmd.Slot);
        if (unequipResult.IsFailure)
        {
            return Result.Failure<ItemId>(unequipResult.Error);
        }

        var oldItemId = unequipResult.Value;

        // 6. Add old item to inventory (ATOMIC STEP 2 - if fails, restore to equipment)
        var addOldResult = inventory.AddItem(oldItemId);
        if (addOldResult.IsFailure)
        {
            // ROLLBACK STEP 1: Restore old item to equipment
            var restoreOldResult = equipmentComp.EquipItem(cmd.Slot, oldItemId, oldItemIsTwoHanded);
            if (restoreOldResult.IsFailure)
            {
                _logger.LogError(
                    "CRITICAL: Item {OldItemId} lost during swap rollback (step 1) for actor {ActorId}. Add to inventory failed: {Error1}, Restore to equipment failed: {Error2}",
                    oldItemId,
                    cmd.ActorId,
                    addOldResult.Error,
                    restoreOldResult.Error);
                return Result.Failure<ItemId>($"Item lost during transaction (contact support): {addOldResult.Error}");
            }

            return Result.Failure<ItemId>(addOldResult.Error);
        }

        // 7. Remove new item from inventory (ATOMIC STEP 3 - if fails, rollback steps 1 & 2)
        var removeNewResult = inventory.RemoveItem(cmd.NewItemId);
        if (removeNewResult.IsFailure)
        {
            // ROLLBACK STEPS 2 & 1: Remove old from inventory, restore old to equipment
            var removeOldResult = inventory.RemoveItem(oldItemId);
            if (removeOldResult.IsFailure)
            {
                _logger.LogError(
                    "CRITICAL: Item {OldItemId} lost during swap rollback (step 2) for actor {ActorId}. Remove new failed: {Error1}, Remove old from inventory failed: {Error2}",
                    oldItemId,
                    cmd.ActorId,
                    removeNewResult.Error,
                    removeOldResult.Error);
                return Result.Failure<ItemId>($"Item lost during transaction (contact support): {removeNewResult.Error}");
            }

            var restoreOldResult = equipmentComp.EquipItem(cmd.Slot, oldItemId, oldItemIsTwoHanded);
            if (restoreOldResult.IsFailure)
            {
                _logger.LogError(
                    "CRITICAL: Item {OldItemId} lost during swap rollback (step 2b) for actor {ActorId}. Remove new failed: {Error1}, Restore old to equipment failed: {Error2}",
                    oldItemId,
                    cmd.ActorId,
                    removeNewResult.Error,
                    restoreOldResult.Error);
                return Result.Failure<ItemId>($"Item lost during transaction (contact support): {removeNewResult.Error}");
            }

            return Result.Failure<ItemId>(removeNewResult.Error);
        }

        // 8. Equip new item (ATOMIC STEP 4 - if fails, rollback ALL steps)
        var equipNewResult = equipmentComp.EquipItem(cmd.Slot, cmd.NewItemId, cmd.IsTwoHanded);
        if (equipNewResult.IsFailure)
        {
            // ROLLBACK ALL: Restore new to inventory, remove old from inventory, restore old to equipment
            var restoreNewResult = inventory.AddItem(cmd.NewItemId);
            if (restoreNewResult.IsFailure)
            {
                _logger.LogError(
                    "CRITICAL: Item {NewItemId} lost during swap rollback (step 3a) for actor {ActorId}. Equip new failed: {Error1}, Restore new to inventory failed: {Error2}",
                    cmd.NewItemId,
                    cmd.ActorId,
                    equipNewResult.Error,
                    restoreNewResult.Error);
            }

            var removeOldResult = inventory.RemoveItem(oldItemId);
            if (removeOldResult.IsFailure)
            {
                _logger.LogError(
                    "CRITICAL: Item {OldItemId} lost during swap rollback (step 3b) for actor {ActorId}. Equip new failed: {Error1}, Remove old from inventory failed: {Error2}",
                    oldItemId,
                    cmd.ActorId,
                    equipNewResult.Error,
                    removeOldResult.Error);
                return Result.Failure<ItemId>($"Item lost during transaction (contact support): {equipNewResult.Error}");
            }

            var restoreOldResult = equipmentComp.EquipItem(cmd.Slot, oldItemId, oldItemIsTwoHanded);
            if (restoreOldResult.IsFailure)
            {
                _logger.LogError(
                    "CRITICAL: Item {OldItemId} lost during swap rollback (step 3c) for actor {ActorId}. Equip new failed: {Error1}, Restore old to equipment failed: {Error2}",
                    oldItemId,
                    cmd.ActorId,
                    equipNewResult.Error,
                    restoreOldResult.Error);
                return Result.Failure<ItemId>($"Item lost during transaction (contact support): {equipNewResult.Error}");
            }

            return Result.Failure<ItemId>(equipNewResult.Error);
        }

        // 9. Save inventory (actor changes are persisted via reference)
        var saveInventoryResult = await _inventories.SaveAsync(inventory, cancellationToken);
        if (saveInventoryResult.IsFailure)
        {
            _logger.LogError(
                "Failed to save inventory after swapping equipment: {Error}",
                saveInventoryResult.Error);
            return Result.Failure<ItemId>($"Failed to save inventory: {saveInventoryResult.Error}");
        }

        _logger.LogInformation(
            "Swapped equipment at {Slot} for actor {ActorId}: {OldItemId} -> {NewItemId}",
            cmd.Slot,
            cmd.ActorId,
            oldItemId,
            cmd.NewItemId);

        return Result.Success(oldItemId); // Return old item ID
    }
}
