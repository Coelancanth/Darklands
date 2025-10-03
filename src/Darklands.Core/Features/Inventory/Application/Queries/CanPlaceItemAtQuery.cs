using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using MediatR;

namespace Darklands.Core.Features.Inventory.Application.Queries;

/// <summary>
/// Query to check if an item can be placed at a specific position with rotation.
/// Returns boolean result (used for UI validation before drag-drop).
/// </summary>
/// <param name="ActorId">Actor whose inventory to check</param>
/// <param name="ItemId">Item to validate placement for</param>
/// <param name="Position">Grid position to check</param>
/// <param name="Rotation">Rotation state to apply (Phase 3+)</param>
public sealed record CanPlaceItemAtQuery(
    ActorId ActorId,
    ItemId ItemId,
    GridPosition Position,
    Rotation Rotation = default
) : IRequest<Result<bool>>;
