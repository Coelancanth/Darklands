using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using MediatR;

namespace Darklands.Core.Features.Inventory.Application.Commands;

/// <summary>
/// Command to rotate an item in an actor's inventory (Phase 3).
/// Validates that rotated item still fits without collisions.
/// </summary>
/// <param name="ActorId">Actor whose inventory contains the item</param>
/// <param name="ItemId">Item to rotate</param>
/// <param name="NewRotation">New rotation state (0°, 90°, 180°, 270°)</param>
public sealed record RotateItemCommand(
    ActorId ActorId,
    ItemId ItemId,
    Rotation NewRotation
) : IRequest<Result>;
