using System;

namespace Darklands.Core.Features.WorldGen.Infrastructure.Algorithms;

/// <summary>
/// Provides prevailing wind direction based on latitude using Earth's atmospheric circulation model.
/// Based on simplified Hadley/Ferrel/Polar cell patterns (horizontal-only wind vectors).
/// </summary>
/// <remarks>
/// VS_027: Latitude-based prevailing winds for rain shadow calculations.
///
/// Earth's Atmospheric Circulation (simplified to horizontal component):
/// - **Polar Easterlies** (60°-90° N/S): Cold air sinks at poles, flows equatorward → Westward winds
/// - **Westerlies** (30°-60° N/S): Mid-latitude circulation → Eastward winds
/// - **Trade Winds** (0°-30° N/S): Hadley cell descending air → Westward winds
///
/// Design Decision: Uses **horizontal-only vectors** (pure E/W, no Y-component):
/// - Reality: Trade winds are diagonal (NE→SW in Northern Hemisphere)
/// - Simplification: Extract dominant E/W component, ignore meridional (N/S) flow
/// - Rationale: E/W component dominates rain shadow effects, Y-component adds complexity for minimal gain
/// - Tradeoff: Simpler algorithm, clearer gameplay, loses diagonal mountain blocking
///
/// TODO (Future Enhancement - VS_028+):
/// Could extend to diagonal winds by adding Y-component:
/// - Trade Winds: Vector2(-1, latDegrees > 0 ? -0.3f : 0.3f) for NE→SW diagonal
/// - Westerlies: Vector2(1, latDegrees > 0 ? 0.5f : -0.5f) for SW→NE diagonal
/// - Would require Y-boundary handling in upwind trace + normalized vectors
/// - Benefit: More realistic diagonal mountain blocking (e.g., Andes NW-SE orientation)
/// - Cost: Algorithm complexity, harder debugging, minimal gameplay impact
///
/// Reference: Atmospheric circulation bands (Hadley/Ferrel/Polar cells)
/// https://en.wikipedia.org/wiki/Atmospheric_circulation
/// </remarks>
public static class PrevailingWinds
{
    /// <summary>
    /// Gets the prevailing wind direction for a given latitude.
    /// Returns a normalized horizontal vector (X-component only, Y=0).
    /// </summary>
    /// <param name="normalizedLatitude">
    /// Latitude in [0,1] where:
    /// - 0.0 = South Pole (90°S)
    /// - 0.5 = Equator (0°)
    /// - 1.0 = North Pole (90°N)
    /// </param>
    /// <returns>
    /// Wind direction vector (horizontal-only):
    /// - Vector2(-1, 0): Westward winds (Polar Easterlies, Trade Winds)
    /// - Vector2(1, 0): Eastward winds (Westerlies)
    /// </returns>
    /// <example>
    /// <code>
    /// // Equator (0°) - Trade Winds
    /// var equatorWind = PrevailingWinds.GetWindDirection(0.5f);
    /// // Returns: Vector2(-1, 0) - Westward
    ///
    /// // Mid-latitude (45°N) - Westerlies
    /// var midLatWind = PrevailingWinds.GetWindDirection(0.75f);
    /// // Returns: Vector2(1, 0) - Eastward
    ///
    /// // Polar (75°N) - Polar Easterlies
    /// var polarWind = PrevailingWinds.GetWindDirection(0.92f);
    /// // Returns: Vector2(-1, 0) - Westward
    /// </code>
    /// </example>
    public static (float X, float Y) GetWindDirection(float normalizedLatitude)
    {
        // Convert [0,1] to latitude degrees: 0=South Pole, 0.5=Equator, 1=North Pole
        // Formula: lat_degrees = (normalized - 0.5) * 180
        //   0.0 → -90° (South Pole)
        //   0.5 →   0° (Equator)
        //   1.0 → +90° (North Pole)
        float latDegrees = (normalizedLatitude - 0.5f) * 180f;

        // Earth's atmospheric circulation bands (both hemispheres symmetrical)
        // Use absolute value to treat Northern/Southern hemispheres identically
        float absLat = MathF.Abs(latDegrees);

        // Band 1: Polar Easterlies [60°-90°]
        // Cold air descends at poles → Westward surface winds
        // Note: Uses >= to handle floating-point precision at exact 60° boundary
        if (absLat >= 60f)
        {
            return (-1f, 0f);  // Westward (easterlies = wind FROM east TO west)
        }
        // Band 2: Westerlies [30°-60°)
        // Mid-latitude Ferrel cell → Eastward surface winds
        // Note: Uses >= to handle floating-point precision at exact 30° boundary
        else if (absLat >= 30f)
        {
            return (1f, 0f);   // Eastward (westerlies = wind FROM west TO east)
        }
        // Band 3: Trade Winds [0°-30°)
        // Hadley cell descending air → Westward surface winds
        else
        {
            return (-1f, 0f);  // Westward (northeast/southeast trades)
        }
    }

    /// <summary>
    /// Gets a human-readable name for the wind band at a given latitude.
    /// Useful for debug displays and probe tooltips.
    /// </summary>
    /// <param name="normalizedLatitude">Latitude in [0,1]</param>
    /// <returns>Wind band name (e.g., "Westerlies", "Trade Winds")</returns>
    public static string GetWindBandName(float normalizedLatitude)
    {
        float latDegrees = (normalizedLatitude - 0.5f) * 180f;
        float absLat = MathF.Abs(latDegrees);

        if (absLat > 60f)
            return "Polar Easterlies";
        else if (absLat > 30f)
            return "Westerlies";
        else
            return "Trade Winds";
    }

    /// <summary>
    /// Gets a human-readable wind direction string (e.g., "→ Eastward", "← Westward").
    /// Includes Unicode arrows for visual clarity in probe/UI.
    /// </summary>
    /// <param name="normalizedLatitude">Latitude in [0,1]</param>
    /// <returns>Directional string with arrow (e.g., "← Westward")</returns>
    public static string GetWindDirectionString(float normalizedLatitude)
    {
        var (x, _) = GetWindDirection(normalizedLatitude);

        if (x > 0)
            return "→ Eastward";
        else
            return "← Westward";
    }
}
