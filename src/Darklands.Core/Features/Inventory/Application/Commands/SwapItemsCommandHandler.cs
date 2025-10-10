using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Item.Application;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.Inventory.Application.Commands;

/// <summary>
/// Handler for SwapItemsCommand.
/// Implements atomic swap with rollback safety (transactional swap operation).
/// </summary>
/// <remarks>
/// TD_004 Leak #5: Eliminates 78 lines of swap logic from Presentation.
///
/// SWAP ALGORITHM (4 steps with 3 rollback paths):
/// 1. Remove source item (rollback: abort if fails)
/// 2. Remove target item (rollback: restore source)
/// 3. Place source at target (rollback: restore both)
/// 4. Place target at source (rollback: restore both)
///
/// BUSINESS RULE: Only equipment slots (WeaponOnly) support swap
/// Regular containers just move items.
/// </remarks>
public sealed class SwapItemsCommandHandler : IRequestHandler<SwapItemsCommand, Result>
{
    private readonly IInventoryRepository _inventories;
    private readonly IItemRepository _items;
    private readonly ILogger<SwapItemsCommandHandler> _logger;

    public SwapItemsCommandHandler(
        IInventoryRepository inventories,
        IItemRepository items,
        ILogger<SwapItemsCommandHandler> logger)
    {
        _inventories = inventories ?? throw new ArgumentNullException(nameof(inventories));
        _items = items ?? throw new ArgumentNullException(nameof(items));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(SwapItemsCommand cmd, CancellationToken cancellationToken)
    {
        // Get inventories
        var sourceInvResult = await _inventories.GetByIdAsync(cmd.SourceInventoryId, cancellationToken);
        if (sourceInvResult.IsFailure)
            return Result.Failure(sourceInvResult.Error);

        var targetInvResult = await _inventories.GetByIdAsync(cmd.TargetInventoryId, cancellationToken);
        if (targetInvResult.IsFailure)
            return Result.Failure(targetInvResult.Error);

        var sourceInventory = sourceInvResult.Value;
        var targetInventory = targetInvResult.Value;

        // Get source item for shape
        var sourceItemResult = await _items.GetByIdAsync(cmd.SourceItemId, cancellationToken);
        if (sourceItemResult.IsFailure)
            return Result.Failure(sourceItemResult.Error);

        var sourceItem = sourceItemResult.Value;

        // Check if this is a SWAP or MOVE
        bool isSwap = cmd.TargetItemId.HasValue;

        if (isSwap)
        {
            return await ExecuteSwap(
                sourceInventory, cmd.SourceItemId, sourceItem.Shape, cmd.SourcePosition,
                targetInventory, cmd.TargetItemId!.Value, cmd.TargetPosition,
                cmd.Rotation, cancellationToken);
        }
        else
        {
            return await ExecuteMove(
                sourceInventory, cmd.SourceItemId, sourceItem.Shape, cmd.SourcePosition,
                targetInventory, cmd.TargetPosition,
                cmd.Rotation, cancellationToken);
        }
    }

    /// <summary>
    /// Executes atomic swap with full rollback safety.
    /// </summary>
    private async Task<Result> ExecuteSwap(
        Domain.Inventory sourceInventory,
        ItemId sourceItemId,
        ItemShape sourceItemShape,
        GridPosition sourcePos,
        Domain.Inventory targetInventory,
        ItemId targetItemId,
        GridPosition targetPos,
        Rotation rotation,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("SWAP: {SourceItem} @ ({SX},{SY}) ↔ {TargetItem} @ ({TX},{TY}) with rotation {Rotation}",
            sourceItemId, sourcePos.X, sourcePos.Y, targetItemId, targetPos.X, targetPos.Y, rotation);

        // Check if same-container swap (to avoid double-save bug)
        bool isSameContainer = sourceInventory == targetInventory;

        // Get target item ORIGINAL shape from repository (not from inventory storage!)
        // WHY: Equipment slots store items as 1×1, but we need the actual L/T-shape when swapping back to inventory
        var targetItemResult = await _items.GetByIdAsync(targetItemId, cancellationToken);
        if (targetItemResult.IsFailure)
            return Result.Failure(targetItemResult.Error);

        var targetItem = targetItemResult.Value;
        var targetItemShape = targetItem.Shape; // ORIGINAL shape from catalog, not 1×1 override

        var targetItemRotation = targetInventory.GetItemRotation(targetItemId);
        if (targetItemRotation.IsFailure)
            return Result.Failure(targetItemRotation.Error);

        // STEP 1: Remove source item
        var removeSourceResult = sourceInventory.RemoveItem(sourceItemId);
        if (removeSourceResult.IsFailure)
        {
            _logger.LogError("SWAP ABORTED: Failed to remove source item: {Error}", removeSourceResult.Error);
            return Result.Failure(removeSourceResult.Error);
        }

        _logger.LogDebug("SWAP STEP 1: Source item removed");

        // STEP 2: Remove target item (ROLLBACK PATH 1: restore source if fails)
        var removeTargetResult = targetInventory.RemoveItem(targetItemId);
        if (removeTargetResult.IsFailure)
        {
            _logger.LogError("SWAP ABORTED: Failed to remove target item: {Error}", removeTargetResult.Error);
            // Rollback: Restore source
            sourceInventory.PlaceItemAt(sourceItemId, sourcePos, sourceItemShape, Rotation.Degrees0);
            await _inventories.SaveAsync(sourceInventory, cancellationToken);
            return Result.Failure(removeTargetResult.Error);
        }

        _logger.LogDebug("SWAP STEP 2: Target item removed");

        // Apply equipment slot override for source item placement
        // WHY: Equipment slots (1×1) accept any weapon regardless of backpack Tetris size
        bool isTargetEquipmentSlot = targetInventory.ContainerType == ContainerType.WeaponOnly;
        var sourcePlacementShape = isTargetEquipmentSlot
            ? ItemShape.CreateRectangle(1, 1).Value  // Override: Equipment slots force 1×1
            : sourceItemShape;                        // Use actual L/T-shape
        var sourcePlacementRotation = isTargetEquipmentSlot
            ? Rotation.Degrees0                       // Equipment slots ignore rotation
            : rotation;

        // STEP 3: Place source at target (ROLLBACK PATH 2: restore both if fails)
        var placeSourceResult = targetInventory.PlaceItemAt(sourceItemId, targetPos, sourcePlacementShape, sourcePlacementRotation);
        if (placeSourceResult.IsFailure)
        {
            _logger.LogError("SWAP FAILED: Could not place source at target: {Error}", placeSourceResult.Error);
            // Rollback: Restore both items
            sourceInventory.PlaceItemAt(sourceItemId, sourcePos, sourceItemShape, Rotation.Degrees0);
            targetInventory.PlaceItemAt(targetItemId, targetPos, targetItemShape, targetItemRotation.Value);

            // Save inventories (avoid double-save if same container)
            if (isSameContainer)
                await _inventories.SaveAsync(sourceInventory, cancellationToken);
            else
            {
                await _inventories.SaveAsync(sourceInventory, cancellationToken);
                await _inventories.SaveAsync(targetInventory, cancellationToken);
            }

            return Result.Failure(placeSourceResult.Error);
        }

        _logger.LogDebug("SWAP STEP 3: Source placed at target");

        // Apply equipment slot override for target item placement
        bool isSourceEquipmentSlot = sourceInventory.ContainerType == ContainerType.WeaponOnly;
        var targetPlacementShape = isSourceEquipmentSlot
            ? ItemShape.CreateRectangle(1, 1).Value  // Override: Equipment slots force 1×1
            : targetItemShape;                        // Use actual L/T-shape
        var targetPlacementRotation = isSourceEquipmentSlot
            ? Rotation.Degrees0                       // Equipment slots ignore rotation
            : targetItemRotation.Value;

        // STEP 4: Place target at source (ROLLBACK PATH 3: restore both if fails)
        var placeTargetResult = sourceInventory.PlaceItemAt(targetItemId, sourcePos, targetPlacementShape, targetPlacementRotation);
        if (placeTargetResult.IsFailure)
        {
            _logger.LogError("SWAP FAILED: Could not place target at source: {Error}", placeTargetResult.Error);
            // Rollback: Remove source from wrong place, restore both
            targetInventory.RemoveItem(sourceItemId);
            sourceInventory.PlaceItemAt(sourceItemId, sourcePos, sourceItemShape, Rotation.Degrees0);
            targetInventory.PlaceItemAt(targetItemId, targetPos, targetItemShape, targetItemRotation.Value);

            // Save inventories (avoid double-save if same container)
            if (isSameContainer)
                await _inventories.SaveAsync(sourceInventory, cancellationToken);
            else
            {
                await _inventories.SaveAsync(sourceInventory, cancellationToken);
                await _inventories.SaveAsync(targetInventory, cancellationToken);
            }

            return Result.Failure(placeTargetResult.Error);
        }

        _logger.LogInformation("SWAP COMPLETED: {SourceItem} ↔ {TargetItem}", sourceItemId, targetItemId);

        // Persist inventories (check if same container to avoid double-save)
        if (isSameContainer)
        {
            // Same-container swap: Save once
            await _inventories.SaveAsync(sourceInventory, cancellationToken);
        }
        else
        {
            // Cross-container swap: Save both
            await _inventories.SaveAsync(sourceInventory, cancellationToken);
            await _inventories.SaveAsync(targetInventory, cancellationToken);
        }

        return Result.Success();
    }

    /// <summary>
    /// Executes simple move operation (no swap).
    /// </summary>
    private async Task<Result> ExecuteMove(
        Domain.Inventory sourceInventory,
        ItemId itemId,
        ItemShape itemShape,
        GridPosition sourcePos,
        Domain.Inventory targetInventory,
        GridPosition targetPos,
        Rotation rotation,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("MOVE: {ItemId} from ({SX},{SY}) to ({TX},{TY}) with rotation {Rotation}",
            itemId, sourcePos.X, sourcePos.Y, targetPos.X, targetPos.Y, rotation);

        // Remove from source
        var removeResult = sourceInventory.RemoveItem(itemId);
        if (removeResult.IsFailure)
            return Result.Failure(removeResult.Error);

        // Place at target
        var placeResult = targetInventory.PlaceItemAt(itemId, targetPos, itemShape, rotation);
        if (placeResult.IsFailure)
        {
            // Rollback: Restore to source
            sourceInventory.PlaceItemAt(itemId, sourcePos, itemShape, Rotation.Degrees0);
            await _inventories.SaveAsync(sourceInventory, cancellationToken);
            return Result.Failure(placeResult.Error);
        }

        // Persist inventories (check if same container to avoid double-save)
        bool isSameContainer = sourceInventory == targetInventory;

        if (isSameContainer)
        {
            // Same-container move: Save once
            await _inventories.SaveAsync(sourceInventory, cancellationToken);
        }
        else
        {
            // Cross-container move: Save both
            await _inventories.SaveAsync(sourceInventory, cancellationToken);
            await _inventories.SaveAsync(targetInventory, cancellationToken);
        }

        _logger.LogInformation("MOVE COMPLETED: {ItemId} moved successfully", itemId);
        return Result.Success();
    }
}
