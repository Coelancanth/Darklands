using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using MediatR;

namespace Darklands.Core.Features.Inventory.Application.Commands;

/// <summary>
/// Command to remove an item from an actor's inventory.
/// Pure DTO - no behavior, just data for the handler.
/// </summary>
/// <param name="ActorId">Actor whose inventory will lose the item</param>
/// <param name="ItemId">Item to remove</param>
public sealed record RemoveItemCommand(
    ActorId ActorId,
    ItemId ItemId
) : IRequest<Result>;
