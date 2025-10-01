using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using MediatR;

namespace Darklands.Core.Features.Grid.Application.Commands;

/// <summary>
/// Command to register an actor at an initial grid position.
/// Used during game initialization to place player and enemies on the grid.
/// </summary>
/// <param name="ActorId">Unique identifier for the actor</param>
/// <param name="InitialPosition">Starting grid position</param>
public record RegisterActorCommand(ActorId ActorId, Position InitialPosition) : IRequest<Result>;
