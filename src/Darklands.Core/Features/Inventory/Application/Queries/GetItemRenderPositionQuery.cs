using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using MediatR;

namespace Darklands.Core.Features.Inventory.Application.Queries;

/// <summary>
/// Query to get item's render position information (grid position + centering offset).
/// Returns data for Presentation to position sprites correctly.
/// </summary>
/// <param name="ContainerId">Actor whose inventory contains the item</param>
/// <param name="ItemId">Item to get render position for</param>
/// <remarks>
/// TD_004 Leak #3: Replaces equipment slot centering logic at SpatialInventoryContainerNode.cs:853-871
///
/// BUSINESS RULES HANDLED BY CORE:
/// - Equipment slot detection (line 855 in Presentation - now in Core)
/// - Centering rule (lines 856-867 - Core decides, Presentation applies)
///
/// Result contains:
/// - GridPosition: Item's grid origin
/// - GridOffset: Centering offset (Zero for regular items, Center for equipment slots)
///
/// Presentation applies: pixelX = (position.X + offset.X) * CellSize
/// </remarks>
public sealed record GetItemRenderPositionQuery(
    ActorId ContainerId,
    ItemId ItemId
) : IRequest<Result<ItemRenderPosition>>;

/// <summary>
/// Response DTO containing item's render positioning data.
/// </summary>
/// <param name="Position">Item's grid origin position</param>
/// <param name="GridOffset">Centering offset in grid units (0.5 = half-cell for centering)</param>
/// <remarks>
/// Presentation formula: pixelPosition = (Position + GridOffset) * CellSize
/// - Regular items: Offset = (0, 0) → top-left aligned
/// - Equipment slots: Offset = (0.5, 0.5) → centered in cell
/// </remarks>
public sealed record ItemRenderPosition(
    GridPosition Position,
    GridOffset GridOffset
);
