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
/// <param name="ShouldCenterInSlot">True if item should be centered (equipment slots)</param>
/// <param name="EffectiveWidth">Item width after rotation (for centering calculation)</param>
/// <param name="EffectiveHeight">Item height after rotation (for centering calculation)</param>
/// <remarks>
/// PIXEL-PERFECT CENTERING (Option B):
/// Core provides: Rule (ShouldCenter) + Sprite dimensions (EffectiveWidth/Height)
/// Presentation applies: pixelX = ShouldCenter ? (CellSize - spriteWidth)/2 : position.X * CellSize
///
/// This maintains clean separation:
/// - Core: Business rule "equipment slots center items" + sprite dimensions
/// - Presentation: Pixel math for centering
/// </remarks>
public sealed record ItemRenderPosition(
    GridPosition Position,
    bool ShouldCenterInSlot,
    int EffectiveWidth,
    int EffectiveHeight
);
