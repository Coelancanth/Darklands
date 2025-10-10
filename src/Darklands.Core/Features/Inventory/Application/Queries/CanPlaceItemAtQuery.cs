using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using MediatR;
using InventoryId = Darklands.Core.Features.Inventory.Domain.InventoryId;

namespace Darklands.Core.Features.Inventory.Application.Queries;

/// <summary>
/// Query to check if an item can be placed at a specific position with rotation.
/// Returns boolean result (used for UI validation before drag-drop).
/// </summary>
/// <param name="InventoryId">Inventory to check</param>
/// <param name="ItemId">Item to validate placement for</param>
/// <param name="Position">Grid position to check</param>
/// <param name="Rotation">Rotation state to apply (Phase 3+)</param>
/// <remarks>
/// TD_019: Changed from ActorId to InventoryId parameter (breaking change).
/// </remarks>
public sealed record CanPlaceItemAtQuery(
    InventoryId InventoryId,
    ItemId ItemId,
    GridPosition Position,
    Rotation Rotation = default
) : IRequest<Result<bool>>;
