using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Item.Application;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.Inventory.Application.Queries;

/// <summary>
/// Handler for CalculateHighlightCellsQuery.
/// Calculates absolute cell positions for drag-drop highlights (rotation + equipment slot override).
/// </summary>
/// <remarks>
/// SINGLE SOURCE OF TRUTH for highlight calculation logic.
/// Replaces Presentation logic (TD_004 - Leak #1).
///
/// ALGORITHM:
/// 1. Get item's base shape
/// 2. Apply rotation transform (shape.RotateClockwise)
/// 3. Check equipment slot override (force 1×1 if WeaponOnly container)
/// 4. Convert relative offsets to absolute positions (origin + offset)
/// </remarks>
public sealed class CalculateHighlightCellsQueryHandler
    : IRequestHandler<CalculateHighlightCellsQuery, Result<List<GridPosition>>>
{
    private readonly IInventoryRepository _inventories;
    private readonly IItemRepository _items;
    private readonly ILogger<CalculateHighlightCellsQueryHandler> _logger;

    public CalculateHighlightCellsQueryHandler(
        IInventoryRepository inventories,
        IItemRepository items,
        ILogger<CalculateHighlightCellsQueryHandler> logger)
    {
        _inventories = inventories ?? throw new ArgumentNullException(nameof(inventories));
        _items = items ?? throw new ArgumentNullException(nameof(items));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<List<GridPosition>>> Handle(
        CalculateHighlightCellsQuery query,
        CancellationToken cancellationToken)
    {
        // Get item for shape data
        var itemResult = await _items.GetByIdAsync(query.ItemId, cancellationToken);
        if (itemResult.IsFailure)
            return Result.Failure<List<GridPosition>>(itemResult.Error);

        var item = itemResult.Value;

        // Get inventory to check for equipment slot override
        var inventoryResult = await _inventories.GetByIdAsync(query.InventoryId, cancellationToken);
        if (inventoryResult.IsFailure)
            return Result.Failure<List<GridPosition>>(inventoryResult.Error);

        var inventory = inventoryResult.Value;

        // Determine highlight shape: equipment slot override or rotated item shape
        ItemShape highlightShape;

        // BUSINESS RULE: Equipment slots show 1×1 highlight regardless of item's actual shape
        bool isEquipmentSlot = inventory.ContainerType == ContainerType.WeaponOnly;

        if (isEquipmentSlot)
        {
            // Override: Force 1×1 rectangle (Diablo 2 pattern - visual feedback = single cell)
            highlightShape = ItemShape.CreateRectangle(1, 1).Value;
            _logger.LogDebug("Equipment slot detected: Overriding highlight to 1×1 (item shape ignored)");
        }
        else
        {
            // Regular container: Use item's rotated shape (preserves L/T-shapes)
            highlightShape = ApplyRotation(item.Shape, query.Rotation);
        }

        // Convert relative shape offsets to absolute grid positions
        var absoluteCells = highlightShape.OccupiedCells
            .Select(offset => new GridPosition(
                query.Position.X + offset.X,
                query.Position.Y + offset.Y))
            .ToList();

        _logger.LogDebug("Calculated {CellCount} highlight cells at origin ({X},{Y}) with rotation {Rotation}",
            absoluteCells.Count, query.Position.X, query.Position.Y, query.Rotation);

        return Result.Success(absoluteCells);
    }

    /// <summary>
    /// Applies rotation to shape by rotating clockwise N times (90° increments).
    /// </summary>
    private ItemShape ApplyRotation(ItemShape baseShape, Rotation rotation)
    {
        var rotated = baseShape;
        int rotations = (int)rotation / 90; // Degrees0=0, Degrees90=1, Degrees180=2, Degrees270=3

        for (int i = 0; i < rotations; i++)
        {
            var rotateResult = rotated.RotateClockwise();
            if (rotateResult.IsSuccess)
            {
                rotated = rotateResult.Value;
            }
            else
            {
                // Should never fail for valid shapes, but log if it does
                _logger.LogWarning("Rotation failed: {Error}", rotateResult.Error);
                break;
            }
        }

        return rotated;
    }
}
