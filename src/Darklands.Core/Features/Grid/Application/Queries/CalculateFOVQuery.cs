using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using MediatR;

namespace Darklands.Core.Features.Grid.Application.Queries;

/// <summary>
/// Query to calculate Field of View from an observer position.
/// Returns all positions visible within the specified radius.
/// </summary>
/// <param name="Observer">Position of the observing actor</param>
/// <param name="Radius">Maximum vision distance in grid cells</param>
public record CalculateFOVQuery(Position Observer, int Radius) : IRequest<Result<HashSet<Position>>>;
