using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using MediatR;
using InventoryId = Darklands.Core.Features.Inventory.Domain.InventoryId;

namespace Darklands.Core.Features.Inventory.Application.Commands;

/// <summary>
/// Command to move an item from one inventory to another (or within the same inventory to a new position).
/// Pure DTO - no behavior, just data for the handler.
/// </summary>
/// <param name="SourceInventoryId">Inventory currently containing the item</param>
/// <param name="TargetInventoryId">Inventory that will receive the item</param>
/// <param name="ItemId">Item to move</param>
/// <param name="TargetPosition">Grid position in target inventory</param>
/// <param name="Rotation">Rotation state for the item at target position (Phase 3, optional, defaults to 0°)</param>
/// <remarks>
/// TD_019: Changed from ActorId to InventoryId parameters (breaking change).
/// This enables cross-actor transfers (e.g., Enemy Loot → Player Equipment).
/// </remarks>
public sealed record MoveItemBetweenContainersCommand(
    InventoryId SourceInventoryId,
    InventoryId TargetInventoryId,
    ItemId ItemId,
    GridPosition TargetPosition,
    Rotation Rotation = default
) : IRequest<Result>;
