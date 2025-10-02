using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using MediatR;

namespace Darklands.Core.Features.Inventory.Application.Queries;

/// <summary>
/// Query to check if an item can be placed at a specific position.
/// Returns boolean result (used for UI validation before drag-drop).
/// </summary>
/// <param name="ActorId">Actor whose inventory to check</param>
/// <param name="ItemId">Item to validate placement for</param>
/// <param name="Position">Grid position to check</param>
public sealed record CanPlaceItemAtQuery(
    ActorId ActorId,
    ItemId ItemId,
    GridPosition Position
) : IRequest<Result<bool>>;
