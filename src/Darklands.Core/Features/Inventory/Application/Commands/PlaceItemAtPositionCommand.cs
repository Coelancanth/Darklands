using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using MediatR;

namespace Darklands.Core.Features.Inventory.Application.Commands;

/// <summary>
/// Command to place an item at a specific grid position in an inventory.
/// Pure DTO - no behavior, just data for the handler.
/// </summary>
/// <param name="ActorId">Actor whose inventory will receive the item</param>
/// <param name="ItemId">Item to place</param>
/// <param name="Position">Grid position to place the item at</param>
public sealed record PlaceItemAtPositionCommand(
    ActorId ActorId,
    ItemId ItemId,
    GridPosition Position
) : IRequest<Result>;
