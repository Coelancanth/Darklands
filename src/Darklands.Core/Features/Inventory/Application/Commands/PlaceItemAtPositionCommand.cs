using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using MediatR;
using InventoryId = Darklands.Core.Features.Inventory.Domain.InventoryId;

namespace Darklands.Core.Features.Inventory.Application.Commands;

/// <summary>
/// Command to place an item at a specific grid position in an inventory.
/// Supports both adding new items AND moving existing items to new positions.
/// Pure DTO - no behavior, just data for the handler.
/// </summary>
/// <param name="InventoryId">Inventory to place the item into</param>
/// <param name="ItemId">Item to place</param>
/// <param name="Position">Grid position to place the item at</param>
/// <param name="Rotation">Rotation to apply (defaults to 0° if not specified)</param>
/// <remarks>
/// VS_032 Phase 4 FIX (BR_008): Added Rotation parameter to preserve rotation from drag operations.
/// Handler now supports moving existing items (unequip → move to drop position).
/// TD_019: Changed from ActorId to InventoryId parameter (breaking change).
/// </remarks>
public sealed record PlaceItemAtPositionCommand(
    InventoryId InventoryId,
    ItemId ItemId,
    GridPosition Position,
    Rotation Rotation = default
) : IRequest<Result>;
