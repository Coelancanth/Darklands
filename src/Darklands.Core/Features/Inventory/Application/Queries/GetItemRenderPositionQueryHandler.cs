using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
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
    private readonly ILogger<GetItemRenderPositionQueryHandler> _logger;

    public GetItemRenderPositionQueryHandler(
        IInventoryRepository inventories,
        ILogger<GetItemRenderPositionQueryHandler> logger)
    {
        _inventories = inventories ?? throw new ArgumentNullException(nameof(inventories));
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

        // BUSINESS RULE: Equipment slots center items
        // Detection: WeaponOnly container with 1×1 grid (Diablo 2 pattern)
        bool isEquipmentSlot = inventory.ContainerType == ContainerType.WeaponOnly &&
                               inventory.GridWidth == 1 &&
                               inventory.GridHeight == 1;

        var gridOffset = isEquipmentSlot ? GridOffset.Center : GridOffset.Zero;

        _logger.LogDebug("Item {ItemId} at ({X},{Y}) - Equipment slot: {IsEquipment}, Offset: ({OffsetX},{OffsetY})",
            query.ItemId, position.X, position.Y, isEquipmentSlot, gridOffset.X, gridOffset.Y);

        return Result.Success(new ItemRenderPosition(position, gridOffset));
    }
}
