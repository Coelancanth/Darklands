using System.Collections.Generic;

namespace Darklands.Core.Features.WorldGen.Application.DTOs;

/// <summary>
/// Phase 1 erosion data: Pit filling + flow accumulation + source detection.
/// Intermediate output from VS_029 Phase 1 (foundation for river tracing).
/// </summary>
/// <remarks>
/// VS_029 Phase 1 Pipeline:
/// 1a. Selective pit filling → FilledHeightmap, Lakes
/// 1b. Flow direction computation → FlowDirections
/// 1c. Topological sort → (internal, not exposed)
/// 1d. Flow accumulation → FlowAccumulation
/// 1e. River source detection → RiverSources
///
/// Phase 2 will use this data to trace river paths from sources to ocean/lakes.
/// </remarks>
public record Phase1ErosionData
{
    /// <summary>
    /// Heightmap after selective pit filling.
    /// Small pits (depth &lt; 50 OR area &lt; 100) filled to spillway level.
    /// Large pits preserved as lakes (endorheic basins).
    /// Still in raw elevation scale [0.1-20] from plate tectonics.
    /// </summary>
    public float[,] FilledHeightmap { get; init; }

    /// <summary>
    /// Flow direction map (8-connected + sink).
    /// Values: 0-7 (N, NE, E, SE, S, SW, W, NW) or -1 (sink/pit/ocean).
    /// Computed on FILLED heightmap (after pit filling).
    /// </summary>
    public int[,] FlowDirections { get; init; }

    /// <summary>
    /// Flow accumulation map (drainage basin sizes).
    /// Values represent accumulated precipitation from all upstream cells.
    /// Computed via topological sort (upstream→downstream order).
    /// Units: Same as precipitation map (normalized [0,1] accumulation).
    /// </summary>
    public float[,] FlowAccumulation { get; init; }

    /// <summary>
    /// River source locations (mountain cells with large accumulated flow).
    /// Criteria: elevation ≥ MountainLevel AND flowAccum ≥ accumulationThreshold.
    /// These are starting points for river tracing (Phase 2).
    /// </summary>
    public List<(int x, int y)> RiverSources { get; init; }

    /// <summary>
    /// Lake locations (preserved large pits from selective filling).
    /// These are endorheic basins (local minima too large to fill).
    /// Real-world analogs: Dead Sea, Great Salt Lake, Caspian Sea.
    /// </summary>
    public List<(int x, int y)> Lakes { get; init; }

    public Phase1ErosionData(
        float[,] filledHeightmap,
        int[,] flowDirections,
        float[,] flowAccumulation,
        List<(int x, int y)> riverSources,
        List<(int x, int y)> lakes)
    {
        FilledHeightmap = filledHeightmap;
        FlowDirections = flowDirections;
        FlowAccumulation = flowAccumulation;
        RiverSources = riverSources;
        Lakes = lakes;
    }
}
