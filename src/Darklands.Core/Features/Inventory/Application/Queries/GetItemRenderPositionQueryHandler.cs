using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Item.Application;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.Inventory.Application.Queries;

/// <summary>
/// Handler for GetItemRenderPositionQuery.
/// Determines item positioning rules (equipment slot centering, etc.).
/// </summary>
/// <remarks>
/// TD_004 Leak #3: Eliminates equipment slot centering logic from Presentation.
///
/// BUSINESS RULE: Equipment slots center items
/// - Detection: WeaponOnly container with 1×1 grid
/// - Result: GridOffset.Center (0.5, 0.5) for pixel centering
/// - Presentation applies: pixelX = (position.X + 0.5) * CellSize - centers item in cell
///
/// REPLACES: Lines 853-871 in SpatialInventoryContainerNode.cs
/// </remarks>
public sealed class GetItemRenderPositionQueryHandler
    : IRequestHandler<GetItemRenderPositionQuery, Result<ItemRenderPosition>>
{
    private readonly IInventoryRepository _inventories;
    private readonly IItemRepository _items;
    private readonly ILogger<GetItemRenderPositionQueryHandler> _logger;

    public GetItemRenderPositionQueryHandler(
        IInventoryRepository inventories,
        IItemRepository items,
        ILogger<GetItemRenderPositionQueryHandler> logger)
    {
        _inventories = inventories ?? throw new ArgumentNullException(nameof(inventories));
        _items = items ?? throw new ArgumentNullException(nameof(items));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<ItemRenderPosition>> Handle(
        GetItemRenderPositionQuery query,
        CancellationToken cancellationToken)
    {
        // Get inventory
        var inventoryResult = await _inventories.GetByActorIdAsync(query.ContainerId, cancellationToken);
        if (inventoryResult.IsFailure)
            return Result.Failure<ItemRenderPosition>(inventoryResult.Error);

        var inventory = inventoryResult.Value;

        // Get item's position
        var positionResult = inventory.GetItemPosition(query.ItemId);
        if (positionResult.IsFailure)
            return Result.Failure<ItemRenderPosition>($"Item {query.ItemId} not found in inventory");

        var position = positionResult.Value;

        // Get item for dimensions
        var itemResult = await _items.GetByIdAsync(query.ItemId, cancellationToken);
        if (itemResult.IsFailure)
            return Result.Failure<ItemRenderPosition>(itemResult.Error);

        var item = itemResult.Value;

        // Get rotation from inventory
        var rotationResult = inventory.GetItemRotation(query.ItemId);
        if (rotationResult.IsFailure)
            return Result.Failure<ItemRenderPosition>(rotationResult.Error);

        var rotation = rotationResult.Value;

        // Calculate effective dimensions (after rotation)
        int effectiveWidth, effectiveHeight;
        int rotationDegrees = (int)rotation;

        if (rotationDegrees == 90 || rotationDegrees == 270)
        {
            // 90° or 270° rotation: swap dimensions
            effectiveWidth = item.InventoryHeight;
            effectiveHeight = item.InventoryWidth;
        }
        else
        {
            // 0° or 180° rotation: keep dimensions
            effectiveWidth = item.InventoryWidth;
            effectiveHeight = item.InventoryHeight;
        }

        // BUSINESS RULE: Equipment slots center items
        // Detection: WeaponOnly container with 1×1 grid (Diablo 2 pattern)
        bool shouldCenterInSlot = inventory.ContainerType == ContainerType.WeaponOnly &&
                                  inventory.GridWidth == 1 &&
                                  inventory.GridHeight == 1;

        _logger.LogDebug("Item {ItemId} at ({X},{Y}) - Equipment slot: {ShouldCenter}, EffectiveDims: {W}×{H}",
            query.ItemId, position.X, position.Y, shouldCenterInSlot, effectiveWidth, effectiveHeight);

        return Result.Success(new ItemRenderPosition(position, shouldCenterInSlot, effectiveWidth, effectiveHeight));
    }
}
