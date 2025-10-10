using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Item.Application;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.Inventory.Application.Queries;

/// <summary>
/// Handler for CanPlaceItemAtQuery.
/// Validates if item can be placed at position (bounds, collision, type filtering).
/// </summary>
/// <remarks>
/// UI VALIDATION: Used by drag-drop to show green (valid) or red (invalid) highlight.
/// BUSINESS RULES CHECKED:
/// - Position within bounds
/// - Position not occupied
/// - Item type compatible with container type
/// </remarks>
public sealed class CanPlaceItemAtQueryHandler
    : IRequestHandler<CanPlaceItemAtQuery, Result<bool>>
{
    private readonly IInventoryRepository _inventories;
    private readonly IItemRepository _items;
    private readonly ILogger<CanPlaceItemAtQueryHandler> _logger;

    public CanPlaceItemAtQueryHandler(
        IInventoryRepository inventories,
        IItemRepository items,
        ILogger<CanPlaceItemAtQueryHandler> logger)
    {
        _inventories = inventories ?? throw new ArgumentNullException(nameof(inventories));
        _items = items ?? throw new ArgumentNullException(nameof(items));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<bool>> Handle(
        CanPlaceItemAtQuery query,
        CancellationToken cancellationToken)
    {
        // Get item for type validation
        var itemResult = await _items.GetByIdAsync(query.ItemId, cancellationToken);
        if (itemResult.IsFailure)
            return Result.Success(false); // Item doesn't exist → can't place

        var item = itemResult.Value;

        // Get inventory
        // TD_019: Use GetByIdAsync (not GetByActorIdAsync)
        var inventoryResult = await _inventories.GetByIdAsync(query.InventoryId, cancellationToken);
        if (inventoryResult.IsFailure)
            return Result.Success(false);

        var inventory = inventoryResult.Value;

        // Check type compatibility
        if (inventory.ContainerType == ContainerType.WeaponOnly &&
            item.Type != "weapon")
        {
            return Result.Success(false); // Type mismatch
        }

        // PHASE 4: Validate full item footprint with L-shape support
        // Equipment slots override shape to 1×1 (industry standard - weapon slots ignore backpack dimensions)
        bool isEquipmentSlot = inventory.ContainerType == ContainerType.WeaponOnly;

        ItemShape placementShape;
        Rotation placementRotation;

        if (isEquipmentSlot)
        {
            // Override: Force 1×1 rectangle for equipment slots
            placementShape = ItemShape.CreateRectangle(1, 1).Value;
            placementRotation = Rotation.Degrees0;
        }
        else
        {
            // Use item's actual shape (preserves L/T-shapes)
            placementShape = item.Shape;
            placementRotation = query.Rotation;
        }

        // Simulate placement to check if it would succeed
        // WHY: PlaceItemAt has all collision logic (bounds + OccupiedCells L-shape support)
        // WORKAROUND: Remove item temporarily if it exists, test placement, restore if needed
        bool itemWasInInventory = inventory.Contains(query.ItemId);
        GridPosition? originalPosition = null;
        Rotation? originalRotation = null;
        ItemShape? originalShape = null;

        if (itemWasInInventory)
        {
            // Capture original state for restoration
            var posResult = inventory.GetItemPosition(query.ItemId);
            var rotResult = inventory.GetItemRotation(query.ItemId);

            if (posResult.IsSuccess)
                originalPosition = posResult.Value;
            if (rotResult.IsSuccess)
                originalRotation = rotResult.Value;

            // Get original shape from ItemShapes dictionary
            if (inventory.ItemShapes.TryGetValue(query.ItemId, out var shape))
                originalShape = shape;

            // Temporarily remove for collision testing
            inventory.RemoveItem(query.ItemId);
        }

        // Test if placement would succeed
        var testResult = inventory.PlaceItemAt(query.ItemId, query.Position, placementShape, placementRotation);
        bool canPlace = testResult.IsSuccess;

        // Restore inventory to original state
        inventory.RemoveItem(query.ItemId); // Remove test placement

        if (itemWasInInventory && originalPosition.HasValue && originalRotation.HasValue && originalShape != null)
        {
            // Restore item to original position
            inventory.PlaceItemAt(query.ItemId, originalPosition.Value, originalShape, originalRotation.Value);
        }

        return Result.Success(canPlace);
    }
}
