using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using MediatR;
using InventoryId = Darklands.Core.Features.Inventory.Domain.InventoryId;

namespace Darklands.Core.Features.Inventory.Application.Commands;

/// <summary>
/// Command to remove an item from an inventory.
/// Pure DTO - no behavior, just data for the handler.
/// </summary>
/// <param name="InventoryId">Inventory to remove item from</param>
/// <param name="ItemId">Item to remove</param>
/// <remarks>
/// TD_019: Changed from ActorId to InventoryId parameter (breaking change).
/// </remarks>
public sealed record RemoveItemCommand(
    InventoryId InventoryId,
    ItemId ItemId
) : IRequest<Result>;
