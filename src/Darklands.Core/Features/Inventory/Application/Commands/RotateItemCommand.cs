using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using MediatR;
using InventoryId = Darklands.Core.Features.Inventory.Domain.InventoryId;

namespace Darklands.Core.Features.Inventory.Application.Commands;

/// <summary>
/// Command to rotate an item in an inventory (Phase 3).
/// Validates that rotated item still fits without collisions.
/// </summary>
/// <param name="InventoryId">Inventory containing the item</param>
/// <param name="ItemId">Item to rotate</param>
/// <param name="NewRotation">New rotation state (0°, 90°, 180°, 270°)</param>
/// <remarks>
/// TD_019: Changed from ActorId to InventoryId parameter (breaking change).
/// </remarks>
public sealed record RotateItemCommand(
    InventoryId InventoryId,
    ItemId ItemId,
    Rotation NewRotation
) : IRequest<Result>;
