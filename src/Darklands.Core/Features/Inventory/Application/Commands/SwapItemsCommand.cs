using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using MediatR;

namespace Darklands.Core.Features.Inventory.Application.Commands;

/// <summary>
/// Command to swap two items between positions (equipment slots) or move a single item.
/// Handles swap vs move decision internally based on container type.
/// </summary>
/// <param name="SourceContainerId">Container holding the source item</param>
/// <param name="SourceItemId">Item being moved/swapped</param>
/// <param name="SourcePosition">Current position of source item</param>
/// <param name="TargetContainerId">Container receiving the item</param>
/// <param name="TargetItemId">Item at target position (may be null for move)</param>
/// <param name="TargetPosition">Destination position</param>
/// <param name="Rotation">Rotation to apply to source item</param>
/// <remarks>
/// TD_004 Leak #5: Replaces Presentation swap logic at SpatialInventoryContainerNode.cs:476-491, 1122-1202
///
/// BUSINESS RULES HANDLED BY CORE:
/// - Swap vs Move decision (equipment slot detection)
/// - Atomic swap with rollback on failure
/// - Transaction safety (all-or-nothing)
///
/// ALGORITHM:
/// 1. Check if target position occupied + equipment slot → SWAP
/// 2. Otherwise → MOVE
/// 3. Execute with full rollback if any step fails
///
/// REPLACES: 78 lines of Presentation logic
/// </remarks>
public sealed record SwapItemsCommand(
    ActorId SourceContainerId,
    ItemId SourceItemId,
    GridPosition SourcePosition,
    ActorId TargetContainerId,
    ItemId? TargetItemId,
    GridPosition TargetPosition,
    Rotation Rotation
) : IRequest<Result>;
