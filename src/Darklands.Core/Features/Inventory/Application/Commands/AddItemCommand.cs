using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using MediatR;
using InventoryId = Darklands.Core.Features.Inventory.Domain.InventoryId;

namespace Darklands.Core.Features.Inventory.Application.Commands;

/// <summary>
/// Command to add an item to an inventory.
/// Pure DTO - no behavior, just data for the handler.
/// </summary>
/// <param name="InventoryId">Inventory to add item to</param>
/// <param name="ItemId">Item to add</param>
/// <remarks>
/// TD_019: Changed from ActorId to InventoryId parameter (breaking change).
/// </remarks>
public sealed record AddItemCommand(
    InventoryId InventoryId,
    ItemId ItemId
) : IRequest<Result>;
