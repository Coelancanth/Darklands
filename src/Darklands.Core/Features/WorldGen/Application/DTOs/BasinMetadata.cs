using System.Collections.Generic;

namespace Darklands.Core.Features.WorldGen.Application.DTOs;

/// <summary>
/// Complete metadata for a basin (pit/lake) detected during pit-filling algorithm.
/// Exposes rich hydrological data for water body classification and pathfinding (TD_023).
/// </summary>
/// <remarks>
/// Purpose: Enable VS_030 Phase 1 (water classification) and Phase 2 (thalweg pathfinding).
///
/// Hydrological Semantics:
/// - Basin: A topographic depression (local minimum surrounded by higher terrain)
/// - Spillway: The highest elevation water can reach before overflowing
/// - Pour Point: The ACTUAL outlet location where water flows out (lowest point on boundary)
/// - Surface Elevation: Water level = pour point elevation (where overflow occurs)
///
/// Key Distinction (Critical for VS_030):
/// - SpillwayElev = MAX(boundary neighbors) → Used for depth calculation
/// - PourPoint = location of MIN(boundary neighbors) → Used for pathfinding outlet
///
/// Example: Volcanic caldera lake with rim varying 1000m-2000m elevation:
/// - Depth = 2000m - 800m = 1200m (spillway = highest rim)
/// - PourPoint = (x, y) at 1000m location (actual outlet for pathfinding)
///
/// Real-World Analogs:
/// - Dead Sea: Depth ~430m, no outlet (spillwayElev = infinite), preserved as endorheic basin
/// - Crater Lake (OR): Pour point at lowest rim section, thalweg path exits there
/// - Great Salt Lake: Multiple inlets (Weber, Jordan rivers), one pour point
/// </remarks>
public record BasinMetadata
{
    /// <summary>
    /// Unique basin identifier (0-indexed from pit-filling detection order).
    /// Used for visualization (color coding) and VS_030 water classification.
    /// </summary>
    public int BasinId { get; init; }

    /// <summary>
    /// Basin center location (local minimum = deepest point).
    /// Coordinates in heightmap space (0-based grid coordinates).
    /// </summary>
    public (int x, int y) Center { get; init; }

    /// <summary>
    /// ALL cells within basin boundary (complete coverage).
    /// Critical for VS_030 Phase 1: Detect inlets where land rivers enter lake.
    /// Size: Typically 100-10000 cells for preserved large basins.
    /// </summary>
    public List<(int x, int y)> Cells { get; init; }

    /// <summary>
    /// Pour point location (outlet where water overflows basin).
    /// This is the MINIMUM elevation boundary neighbor (actual outlet).
    /// Critical for VS_030 Phase 2: Pathfinding target for thalweg routing.
    /// Coordinates in heightmap space (0-based grid coordinates).
    /// </summary>
    /// <remarks>
    /// Hydrological Note: Pour point is on the BOUNDARY (rim) of basin, not inside basin.
    /// For endorheic basins (no outlet), this is the lowest rim point (even if water doesn't actually overflow).
    /// </remarks>
    public (int x, int y) PourPoint { get; init; }

    /// <summary>
    /// Water surface elevation (height of water level at equilibrium).
    /// Equals pour point elevation (water rises until it can overflow at outlet).
    /// Units: Raw elevation scale [0.1-20] from heightmap.
    /// Critical for VS_030 Phase 2: Depth calculation for thalweg cost function.
    /// Cost = 1 / (SurfaceElevation - cellElevation + ε) → Prefers deeper channels.
    /// </summary>
    public float SurfaceElevation { get; init; }

    /// <summary>
    /// Basin depth (water column height from floor to surface).
    /// Calculated as: SpillwayElevation - CenterElevation.
    /// Units: Same as heightmap (meters equivalent).
    /// Used for pit-filling classification: Depth ≥ 50 → Preserve as lake.
    /// </summary>
    public float Depth { get; init; }

    /// <summary>
    /// Basin area (total cell count within boundary).
    /// Used for pit-filling classification: Area ≥ 100 → Preserve as lake.
    /// Also useful for volume calculation: Volume ≈ Area × (Depth / 2) for cone approximation.
    /// </summary>
    public int Area { get; init; }

    public BasinMetadata(
        int basinId,
        (int x, int y) center,
        List<(int x, int y)> cells,
        (int x, int y) pourPoint,
        float surfaceElevation,
        float depth,
        int area)
    {
        BasinId = basinId;
        Center = center;
        Cells = cells;
        PourPoint = pourPoint;
        SurfaceElevation = surfaceElevation;
        Depth = depth;
        Area = area;
    }
}
