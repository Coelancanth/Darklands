using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using MediatR;

namespace Darklands.Core.Features.Grid.Application.Commands;

/// <summary>
/// Command to move an actor to a target position on the grid.
/// Validates terrain passability before updating position.
/// </summary>
/// <param name="ActorId">Unique identifier of the actor to move</param>
/// <param name="TargetPosition">Destination grid position</param>
public record MoveActorCommand(ActorId ActorId, Position TargetPosition) : IRequest<Result>;
