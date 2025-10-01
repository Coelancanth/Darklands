using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using MediatR;

namespace Darklands.Core.Features.Grid.Application.Queries;

/// <summary>
/// Query to get all actors visible from an observer's position.
/// Combines actor positions with FOV calculation to filter visible actors.
/// </summary>
/// <param name="ObserverId">Unique identifier of the observing actor</param>
/// <param name="Radius">Maximum vision distance in grid cells</param>
public record GetVisibleActorsQuery(ActorId ObserverId, int Radius) : IRequest<Result<List<ActorId>>>;
