using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Item.Application;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.Inventory.Application.Commands;

/// <summary>
/// Handler for MoveItemBetweenContainersCommand.
/// Orchestrates cross-container item movement (remove from source, add to target).
/// </summary>
/// <remarks>
/// TRANSACTIONAL SEMANTICS:
/// - Removes from source first
/// - If target placement fails, item is lost (acceptable for in-memory MVP)
/// - Future: Wrap in transaction for persistent storage
///
/// SUPPORTS:
/// - Inter-container movement (backpack A → backpack B)
/// - Intra-container repositioning (same inventory, new position)
/// </remarks>
public sealed class MoveItemBetweenContainersCommandHandler
    : IRequestHandler<MoveItemBetweenContainersCommand, Result>
{
    private readonly IInventoryRepository _inventories;
    private readonly IItemRepository _items;
    private readonly ILogger<MoveItemBetweenContainersCommandHandler> _logger;

    public MoveItemBetweenContainersCommandHandler(
        IInventoryRepository inventories,
        IItemRepository items,
        ILogger<MoveItemBetweenContainersCommandHandler> logger)
    {
        _inventories = inventories ?? throw new ArgumentNullException(nameof(inventories));
        _items = items ?? throw new ArgumentNullException(nameof(items));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(
        MoveItemBetweenContainersCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Moving item {ItemId} from actor {SourceActorId} to actor {TargetActorId} at position {TargetPosition}",
            command.ItemId,
            command.SourceActorId,
            command.TargetActorId,
            command.TargetPosition);

        // Get item for type validation
        var itemResult = await _items.GetByIdAsync(command.ItemId, cancellationToken);
        if (itemResult.IsFailure)
            return itemResult;

        var item = itemResult.Value;

        // Get source inventory
        var sourceResult = await _inventories.GetByActorIdAsync(command.SourceActorId, cancellationToken);
        if (sourceResult.IsFailure)
            return sourceResult;

        var sourceInventory = sourceResult.Value;

        // Get target inventory
        var targetResult = await _inventories.GetByActorIdAsync(command.TargetActorId, cancellationToken);
        if (targetResult.IsFailure)
            return targetResult;

        var targetInventory = targetResult.Value;

        // BUSINESS RULE: Type filtering for specialized containers
        // WHY: Validate BEFORE removing from source to prevent data loss
        if (targetInventory.ContainerType == ContainerType.WeaponOnly &&
            item.Type != "weapon")
        {
            _logger.LogWarning(
                "Item {ItemId} type '{Type}' rejected by weapon-only container",
                command.ItemId,
                item.Type);
            return Result.Failure("Target container only accepts weapons");
        }

        // Check if this is intra-container repositioning (same inventory)
        bool isSameContainer = command.SourceActorId == command.TargetActorId;

        if (isSameContainer)
        {
            // INTRA-CONTAINER MOVE: Must preserve original position for rollback
            // WHY: If new placement fails, we need to restore item at original position

            // PHASE 3: Capture original position AND rotation before removing (for rollback)
            var originalPositionResult = sourceInventory.GetItemPosition(command.ItemId);
            if (originalPositionResult.IsFailure)
            {
                _logger.LogError("Item not found in source inventory: {Error}", originalPositionResult.Error);
                return Result.Failure(originalPositionResult.Error);
            }

            var originalPosition = originalPositionResult.Value;

            // Capture original rotation BEFORE RemoveItem (which clears rotation dictionary)
            var originalRotationResult = sourceInventory.GetItemRotation(command.ItemId);
            var originalRotation = originalRotationResult.IsSuccess
                ? originalRotationResult.Value
                : Rotation.Degrees0;

            var removeResult = sourceInventory.RemoveItem(command.ItemId);
            if (removeResult.IsFailure)
            {
                _logger.LogError("Failed to remove item for repositioning: {Error}", removeResult.Error);
                return removeResult;
            }

            // PHASE 4: Use item.Shape for L/T-shape preservation
            var placeResult = sourceInventory.PlaceItemAt(
                command.ItemId,
                command.TargetPosition,
                item.Shape,
                command.Rotation); // PHASE 3: Apply rotation from command

            if (placeResult.IsFailure)
            {
                // ROLLBACK: Restore item at original position with original rotation
                _logger.LogWarning("Failed to place item at new position: {Error}, rolling back to original position ({X},{Y}) with rotation {Rotation}",
                    placeResult.Error, originalPosition.X, originalPosition.Y, originalRotation);

                var rollbackResult = sourceInventory.PlaceItemAt(
                    command.ItemId,
                    originalPosition,
                    item.Shape,
                    originalRotation); // Restore original rotation on rollback

                if (rollbackResult.IsFailure)
                {
                    _logger.LogError("CRITICAL: Rollback failed! Item {ItemId} is lost: {Error}",
                        command.ItemId, rollbackResult.Error);
                }
                else
                {
                    // Rollback succeeded - save and return original error
                    await _inventories.SaveAsync(sourceInventory, cancellationToken);
                }

                return placeResult; // Return original placement error
            }

            await _inventories.SaveAsync(sourceInventory, cancellationToken);
        }
        else
        {
            // CROSS-CONTAINER MOVE: Remove from source, place in target
            var removeResult = sourceInventory.RemoveItem(command.ItemId);
            if (removeResult.IsFailure)
                return removeResult;

            // PHASE 4: Use Item.Shape for accurate L/T-shape collision
            // EQUIPMENT SLOT OVERRIDE: Weapon/armor slots ignore item shape (always 1×1 rectangle for placement)
            // WHY: Industry standard - equipment slots accept any weapon regardless of backpack Tetris size
            bool isEquipmentSlot = targetInventory.ContainerType == ContainerType.WeaponOnly;

            ItemShape placementShape;
            Rotation placementRotation;

            if (isEquipmentSlot)
            {
                // Override: Force 1×1 rectangle for equipment slots (ignores L-shapes)
                placementShape = ItemShape.CreateRectangle(1, 1).Value;
                placementRotation = Rotation.Degrees0; // Equipment slots show standard orientation
            }
            else
            {
                // Use item's actual shape (preserves L/T-shapes)
                placementShape = item.Shape;
                placementRotation = command.Rotation;
            }

            var placeResult = targetInventory.PlaceItemAt(
                command.ItemId,
                command.TargetPosition,
                placementShape,
                placementRotation); // PHASE 4: Pass shape + rotation

            if (placeResult.IsFailure)
                return placeResult;

            // Persist both inventories
            await _inventories.SaveAsync(sourceInventory, cancellationToken);
            await _inventories.SaveAsync(targetInventory, cancellationToken);
        }

        _logger.LogInformation(
            "Item {ItemId} moved from actor {SourceActorId} to actor {TargetActorId} at position {TargetPosition}",
            command.ItemId,
            command.SourceActorId,
            command.TargetActorId,
            command.TargetPosition);

        return Result.Success();
    }
}
