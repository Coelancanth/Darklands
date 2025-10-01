using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Grid.Domain;

namespace Darklands.Core.Features.Grid.Application.Services;

/// <summary>
/// Abstraction for Field of View (FOV) calculation.
/// Implementations determine which positions are visible from an observer's location.
/// </summary>
public interface IFOVService
{
    /// <summary>
    /// Calculates all positions visible from the observer's position within the given radius.
    /// Uses the map's terrain to determine line-of-sight (opaque terrain blocks vision).
    /// </summary>
    /// <param name="map">Grid map containing terrain data for opacity checks</param>
    /// <param name="observer">Position of the observing actor</param>
    /// <param name="radius">Maximum vision distance in grid cells</param>
    /// <returns>
    /// Success with HashSet of visible positions (for efficient lookup),
    /// or Failure if observer position is invalid or radius is non-positive
    /// </returns>
    Result<HashSet<Position>> CalculateFOV(GridMap map, Position observer, int radius);
}
