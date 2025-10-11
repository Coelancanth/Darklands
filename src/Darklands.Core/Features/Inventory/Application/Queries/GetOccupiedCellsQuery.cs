using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using MediatR;
using InventoryId = Darklands.Core.Features.Inventory.Domain.InventoryId;

namespace Darklands.Core.Features.Inventory.Application.Queries;

/// <summary>
/// Query to get the absolute grid cells occupied by an item in inventory.
/// Returns positions that can be directly used for rendering without recalculation.
/// </summary>
/// <param name="InventoryId">Inventory containing the item</param>
/// <param name="ItemId">Item to get occupied cells for</param>
/// <remarks>
/// TD_004 Leak #2: Replaces Presentation logic at SpatialInventoryContainerNode.cs:640-683
///
/// BUSINESS RULES HANDLED BY CORE:
/// - Shape rotation (lines 643-647 in Presentation - now in Core)
/// - Occupied cell calculation (lines 649-653 - now in Core)
/// - Rectangle fallback for legacy items (lines 665-674 - now in Core)
///
/// Presentation queries this instead of caching _itemShapes and recalculating.
/// Result: Absolute GridPosition list ready for rendering.
/// TD_019: Changed ContainerId from ActorId to InventoryId (breaking change).
/// </remarks>
public sealed record GetOccupiedCellsQuery(
    InventoryId InventoryId,
    ItemId ItemId
) : IRequest<Result<List<GridPosition>>>;
