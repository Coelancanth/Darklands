using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using MediatR;

namespace Darklands.Core.Features.Inventory.Application.Queries;

/// <summary>
/// Query to calculate which grid cells should be highlighted during drag-drop.
/// Returns absolute cell positions (origin + shape offsets) accounting for rotation and equipment slot override.
/// </summary>
/// <param name="ContainerId">Actor whose inventory is the target container</param>
/// <param name="ItemId">Item being dragged (to get shape)</param>
/// <param name="Position">Origin position where item would be placed</param>
/// <param name="Rotation">Current drag rotation state</param>
/// <remarks>
/// BUSINESS RULES:
/// - Regular containers: Show item's actual rotated shape (L-shapes, rectangles)
/// - Equipment slots: Override to 1×1 highlight regardless of item shape (industry standard)
///
/// TD_004: Replaces Presentation logic at SpatialInventoryContainerNode.cs:1057-1075
/// Single Source of Truth for highlight calculation - Presentation just renders results.
/// </remarks>
public sealed record CalculateHighlightCellsQuery(
    ActorId ContainerId,
    ItemId ItemId,
    GridPosition Position,
    Rotation Rotation
) : IRequest<Result<List<GridPosition>>>;
