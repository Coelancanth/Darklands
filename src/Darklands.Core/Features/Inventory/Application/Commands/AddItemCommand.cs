using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using MediatR;

namespace Darklands.Core.Features.Inventory.Application.Commands;

/// <summary>
/// Command to add an item to an actor's inventory.
/// Pure DTO - no behavior, just data for the handler.
/// </summary>
/// <param name="ActorId">Actor whose inventory will receive the item</param>
/// <param name="ItemId">Item to add</param>
public sealed record AddItemCommand(
    ActorId ActorId,
    ItemId ItemId
) : IRequest<Result>;
