using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using MediatR;

namespace Darklands.Core.Features.Grid.Application.Queries;

/// <summary>
/// Query to retrieve an actor's current grid position.
/// Used by Presentation layer for input handling (calculate relative movement).
/// </summary>
/// <param name="ActorId">Unique identifier of the actor</param>
public record GetActorPositionQuery(ActorId ActorId) : IRequest<Result<Position>>;
