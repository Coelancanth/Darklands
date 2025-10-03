using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using MediatR;

namespace Darklands.Core.Features.Inventory.Application.Commands;

/// <summary>
/// Command to move an item from one inventory to another (or within the same inventory to a new position).
/// Pure DTO - no behavior, just data for the handler.
/// </summary>
/// <param name="SourceActorId">Actor whose inventory currently contains the item</param>
/// <param name="TargetActorId">Actor whose inventory will receive the item</param>
/// <param name="ItemId">Item to move</param>
/// <param name="TargetPosition">Grid position in target inventory</param>
/// <param name="Rotation">Rotation state for the item at target position (Phase 3, optional, defaults to 0°)</param>
public sealed record MoveItemBetweenContainersCommand(
    ActorId SourceActorId,
    ActorId TargetActorId,
    ItemId ItemId,
    GridPosition TargetPosition,
    Rotation Rotation = default
) : IRequest<Result>;
