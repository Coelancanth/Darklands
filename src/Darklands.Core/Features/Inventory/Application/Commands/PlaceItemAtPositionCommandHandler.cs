using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Item.Application;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.Inventory.Application.Commands;

/// <summary>
/// Handler for PlaceItemAtPositionCommand.
/// Orchestrates item type validation + inventory placement.
/// </summary>
/// <remarks>
/// CROSS-AGGREGATE ORCHESTRATION:
/// - Validates item type compatibility with container type
/// - Delegates spatial placement to Inventory entity
/// - Type filtering is Application-layer responsibility (not Domain)
///
/// WHY HERE: Domain entities (Inventory, Item) stay decoupled.
/// Handler knows about BOTH and orchestrates their interaction.
/// </remarks>
public sealed class PlaceItemAtPositionCommandHandler
    : IRequestHandler<PlaceItemAtPositionCommand, Result>
{
    private readonly IInventoryRepository _inventories;
    private readonly IItemRepository _items;
    private readonly ILogger<PlaceItemAtPositionCommandHandler> _logger;

    public PlaceItemAtPositionCommandHandler(
        IInventoryRepository inventories,
        IItemRepository items,
        ILogger<PlaceItemAtPositionCommandHandler> logger)
    {
        _inventories = inventories ?? throw new ArgumentNullException(nameof(inventories));
        _items = items ?? throw new ArgumentNullException(nameof(items));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(
        PlaceItemAtPositionCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Placing item {ItemId} at position {Position} in inventory {InventoryId}",
            command.ItemId,
            command.Position,
            command.InventoryId);

        // Get item for type validation
        var itemResult = await _items.GetByIdAsync(command.ItemId, cancellationToken);
        if (itemResult.IsFailure)
            return itemResult;

        var item = itemResult.Value;

        // Get inventory
        // TD_019: Use GetByIdAsync (not GetByActorIdAsync)
        var inventoryResult = await _inventories.GetByIdAsync(command.InventoryId, cancellationToken);
        if (inventoryResult.IsFailure)
            return inventoryResult;

        var inventory = inventoryResult.Value;

        // BUSINESS RULE: Type filtering for specialized containers
        if (inventory.ContainerType == ContainerType.WeaponOnly &&
            item.Type != "weapon")
        {
            return Result.Failure("Container only accepts weapons");
        }

        // PHASE 4: Use Item.Shape for accurate L/T-shape collision
        // EQUIPMENT SLOT OVERRIDE: Weapon/armor slots ignore item shape (always 1×1 rectangle for placement)
        // WHY: Industry standard - equipment slots accept any weapon regardless of backpack Tetris size
        bool isEquipmentSlot = inventory.ContainerType == ContainerType.WeaponOnly;

        ItemShape placementShape;
        if (isEquipmentSlot)
        {
            // Override: Force 1×1 rectangle for equipment slots (ignores L-shapes)
            placementShape = ItemShape.CreateRectangle(1, 1).Value;
        }
        else
        {
            // Use item's actual shape (preserves L/T-shapes)
            placementShape = item.Shape;
        }

        // BR_008 FIX: Support moving existing items (not just adding new ones)
        // WHY: Equipment unequip adds item at (0,0), then needs to move to drop position
        bool itemAlreadyInInventory = inventory.Contains(command.ItemId);

        Result placeResult;
        if (itemAlreadyInInventory)
        {
            // Move existing item to new position
            _logger.LogDebug("Item {ItemId} already in inventory - moving to new position {Position}",
                command.ItemId, command.Position);

            // Remove from old position, add at new position
            var removeResult = inventory.RemoveItem(command.ItemId);
            if (removeResult.IsFailure)
                return removeResult;

            placeResult = inventory.PlaceItemAt(
                command.ItemId,
                command.Position,
                placementShape,
                rotation: command.Rotation); // BR_008 FIX: Use command rotation, not hardcoded 0°
        }
        else
        {
            // Add new item to inventory
            placeResult = inventory.PlaceItemAt(
                command.ItemId,
                command.Position,
                placementShape,
                rotation: command.Rotation); // BR_008 FIX: Use command rotation, not hardcoded 0°
        }

        if (placeResult.IsFailure)
            return placeResult;

        // Persist
        await _inventories.SaveAsync(inventory, cancellationToken);

        _logger.LogInformation(
            "Item {ItemId} placed at {Position} with rotation {Rotation} in actor {ActorId}'s inventory",
            command.ItemId,
            command.Position,
            command.Rotation,
            command.InventoryId);

        return Result.Success();
    }
}
