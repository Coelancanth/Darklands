using System.Collections.Generic;

namespace Darklands.Core.Features.WorldGen.Application.DTOs;

/// <summary>
/// Represents a single river path from source to termination.
/// Created during erosion simulation (VS_029).
/// </summary>
/// <remarks>
/// VS_029 Phase 2: River tracing from mountain sources to ocean/lakes.
/// Termination types:
/// - Ocean: River reached ocean (ReachedOcean = true)
/// - Lake: River reached endorheic basin (ReachedOcean = false)
/// - Merge: River merged into existing river (handled during tracing)
/// </remarks>
public record River
{
    /// <summary>
    /// Ordered list of (x, y) coordinates from source (first) to termination (last).
    /// Guaranteed to flow downhill monotonically after Phase 4 cleanup.
    /// </summary>
    public List<(int x, int y)> Path { get; init; }

    /// <summary>
    /// Whether this river reached ocean (true) or terminated in a lake (false).
    /// Lakes are endorheic basins (local minima that couldn't be filled).
    /// </summary>
    public bool ReachedOcean { get; init; }

    public River(List<(int x, int y)> path, bool reachedOcean)
    {
        Path = path;
        ReachedOcean = reachedOcean;
    }
}
