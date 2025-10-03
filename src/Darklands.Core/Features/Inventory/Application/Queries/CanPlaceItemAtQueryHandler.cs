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
        var inventoryResult = await _inventories.GetByActorIdAsync(query.ActorId, cancellationToken);
        if (inventoryResult.IsFailure)
            return Result.Success(false);

        var inventory = inventoryResult.Value;

        // Check type compatibility
        if (inventory.ContainerType == ContainerType.WeaponOnly &&
            item.Type != "weapon")
        {
            return Result.Success(false); // Type mismatch
        }

        // Check spatial placement (bounds + collision)
        var canPlace = inventory.CanPlaceAt(query.Position);
        return Result.Success(canPlace);
    }
}
