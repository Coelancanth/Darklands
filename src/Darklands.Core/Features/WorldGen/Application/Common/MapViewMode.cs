namespace Darklands.Core.Features.WorldGen.Application.Common;

/// <summary>
/// View modes for world generation visualization.
/// Includes raw native output, post-processed elevations, and debug views.
/// </summary>
public enum MapViewMode
{
    /// <summary>
    /// Display raw heightmap as grayscale gradient.
    /// Shows unnormalized elevation values from native plate tectonics simulation (no post-processing).
    /// </summary>
    RawElevation,

    /// <summary>
    /// Display tectonic plates ownership map.
    /// Each plate ID rendered with a unique color.
    /// </summary>
    Plates,

    /// <summary>
    /// Display ORIGINAL elevation with quantile-based terrain color gradient (VS_024).
    /// Shows raw native output [0-20] BEFORE post-processing.
    /// Colors: deep blue (ocean) → green (lowlands) → yellow (hills) → brown (peaks).
    /// </summary>
    ColoredOriginalElevation,

    /// <summary>
    /// Display POST-PROCESSED elevation with quantile-based terrain color gradient (VS_024).
    /// Shows raw [0.1-20] AFTER 4 WorldEngine algorithms (add_noise, fill_ocean, harmonize_ocean, sea_depth).
    /// Should differ from ColoredOriginalElevation (noise added, ocean smoothed).
    /// </summary>
    ColoredPostProcessedElevation,

    /// <summary>
    /// Display temperature - Stage 1: Latitude-only (VS_025 debug).
    /// Pure latitude banding with axial tilt. Normalized [0,1] → [-60°C, +40°C].
    /// Visual signature: Horizontal bands, hot zone shifts with per-world tilt.
    /// </summary>
    TemperatureLatitudeOnly,

    /// <summary>
    /// Display temperature - Stage 2: + Noise (VS_025 debug).
    /// Latitude (92%) + climate noise (8%). Normalized [0,1] → [-60°C, +40°C].
    /// Visual signature: Subtle fuzz on latitude bands (not dramatic).
    /// </summary>
    TemperatureWithNoise,

    /// <summary>
    /// Display temperature - Stage 3: + Distance to sun (VS_025 debug).
    /// Latitude + noise / distance². Normalized [0,1] → [-60°C, +40°C].
    /// Visual signature: Hot/cold planet variation (per-world multiplier).
    /// </summary>
    TemperatureWithDistance,

    /// <summary>
    /// Display temperature - Stage 4: FINAL (VS_025 production).
    /// Complete algorithm with mountain cooling. Normalized [0,1] → [-60°C, +40°C].
    /// Visual signature: Mountains blue at ALL latitudes (even equator).
    /// </summary>
    TemperatureFinal,

    /// <summary>
    /// Display precipitation - Stage 1: Base Noise Only (VS_026 debug).
    /// Pure coherent noise (6 octaves). Normalized [0,1].
    /// Visual signature: Random wet/dry patterns (no temperature correlation).
    /// Colors: Brown (dry) → Yellow (moderate) → Blue (wet).
    /// </summary>
    PrecipitationNoiseOnly,

    /// <summary>
    /// Display precipitation - Stage 2: + Temperature Gamma Curve (VS_026 debug).
    /// Base noise × gamma curve (cold = less evaporation). Normalized [0,1].
    /// Visual signature: Tropical regions wetter, polar regions drier (strong correlation).
    /// Colors: Brown (dry) → Yellow (moderate) → Blue (wet).
    /// </summary>
    PrecipitationTemperatureShaped,

    /// <summary>
    /// Display precipitation - Stage 3: Base (VS_026 production).
    /// Complete base algorithm with renormalization. Normalized [0,1].
    /// This is the BASE precipitation before rain shadow effects (VS_027).
    /// Visual signature: Full dynamic range restored after temperature shaping.
    /// Colors: Brown (dry) → Yellow (moderate) → Blue (wet).
    /// Display as mm/year: Low (&lt;400), Medium (400-800), High (&gt;800).
    /// </summary>
    PrecipitationBase,

    /// <summary>
    /// Display precipitation - Stage 4: + Rain Shadow Effect (VS_027 production).
    /// Final precipitation WITH orographic blocking (mountains create leeward deserts).
    /// Visual signature: Dry zones east/west of mountains (latitude-dependent winds).
    /// Sahara (trade winds), Gobi (westerlies), Atacama (trade winds) patterns.
    /// Colors: Brown (rain shadow deserts) → Yellow (moderate) → Blue (windward coasts).
    /// </summary>
    PrecipitationWithRainShadow,

    /// <summary>
    /// Display precipitation - Stage 5: FINAL (VS_028 production).
    /// Complete precipitation with coastal moisture enhancement.
    /// Visual signature: Coastal regions wetter than interior (maritime vs continental climates).
    /// Physics: Exponential decay with distance from ocean, elevation resistance.
    /// Real-world: Seattle (wet coast) vs Spokane (dry interior), UK maritime vs central Asia.
    /// Colors: Brown (continental interior) → Yellow (moderate) → Blue (maritime coasts).
    /// THIS IS THE FINAL PRECIPITATION used by erosion/rivers (VS_029).
    /// </summary>
    PrecipitationFinal,

    /// <summary>
    /// [DEBUG] Display local minima BEFORE pit-filling (VS_029 Step 0A).
    /// Grayscale elevation + Red markers for ALL sinks (artifacts + real pits).
    /// Expected: 5-20% of land cells (noisy raw heightmap).
    /// Purpose: Baseline for pit-filling effectiveness comparison.
    /// </summary>
    SinksPreFilling,

    /// <summary>
    /// [DEBUG] Display local minima AFTER pit-filling (VS_029 Step 0B).
    /// Grayscale elevation + Red markers for remaining sinks (preserved lakes).
    /// Expected: <5% of land cells (70-90% reduction from pre-filling).
    /// Purpose: Validate pit-filling algorithm (fills artifacts, preserves real lakes).
    /// </summary>
    SinksPostFilling,

    /// <summary>
    /// [DEBUG] Display basin metadata from pit-filling (TD_023).
    /// Grayscale elevation base + colored basin boundaries + markers (red pour points, cyan centers).
    /// Each preserved basin rendered with distinct color (basin ID % palette).
    /// Visual signature: Colored regions for large lakes/pits, red dots at outlets, cyan at centers.
    /// Purpose: Validate basin detection for VS_030 (boundaries for inlet detection, pour points for pathfinding).
    /// </summary>
    BasinMetadata,

    /// <summary>
    /// [DEBUG] Display D-8 flow directions (VS_029 Step 2).
    /// 8-color gradient: N=Red, NE=Yellow, E=Green, SE=Cyan, S=Blue, SW=Purple, W=Magenta, NW=Orange, Sink=Black.
    /// Visual signature: Colors flow downhill (mountains→valleys→ocean).
    /// Purpose: Validate D-8 algorithm correctness (steepest descent).
    /// </summary>
    FlowDirections,

    /// <summary>
    /// [DEBUG] Display flow accumulation (VS_029 Step 3).
    /// Heat map: Blue (low) → Green → Yellow → Red (high drainage).
    /// Visual signature: River valleys appear as red hot spots (drainage concentration).
    /// Purpose: Validate topological sort (upstream→downstream order).
    /// </summary>
    FlowAccumulation,

    /// <summary>
    /// [DEBUG] Display river sources (VS_029 Step 4 - CORRECTED ALGORITHM).
    /// Grayscale elevation base + Red markers at TRUE river origins.
    /// Uses threshold-crossing detection: Where flow FIRST becomes "a river".
    /// Visual signature: Sources scattered across mountains (may be many).
    /// Purpose: Validate corrected algorithm (expect hundreds → filtered to 5-15 major).
    /// </summary>
    RiverSources,

    /// <summary>
    /// [DEBUG] Display erosion hotspots (VS_029 - OLD ALGORITHM, repurposed).
    /// Colored elevation base + Magenta markers at high-energy zones.
    /// High elevation + HIGH flow accumulation = Maximum erosive potential.
    /// Visual signature: Where BIG rivers flow through mountains (canyons/gorges).
    /// Purpose: Erosion masking for VS_030+ particle erosion (Grand Canyon zones).
    /// </summary>
    ErosionHotspots
}
