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
    /// Linear interpolation helper (Core layer has no built-in Lerp).
    /// </summary>
    /// <param name="a">Start value</param>
    /// <param name="b">End value</param>
    /// <param name="t">Interpolation factor [0,1]</param>
    /// <returns>Interpolated value between a and b</returns>
    private static float Lerp(float a, float b, float t) => a + (b - a) * t;

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

        // ═══════════════════════════════════════════════════════════════════════
        // GRADIENT BLENDING: Smooth transitions between wind bands
        // ═══════════════════════════════════════════════════════════════════════
        // Eliminates visible horizontal seams by gradually transitioning wind
        // direction across ±5° zones around 30° and 60° boundaries.
        //
        // Transition zones create natural "calm belts" at band centers where
        // wind strength approaches zero, matching real atmospheric circulation.

        const float transitionWidth = 5f;  // ±5° transition zone width

        // ───────────────────────────────────────────────────────────────────────
        // Band 1: Polar Easterlies [65°-90°] + Transition [60°-65°]
        // ───────────────────────────────────────────────────────────────────────
        if (absLat >= 65f)
        {
            // Pure Polar Easterlies (westward)
            return (-1f, 0f);
        }
        else if (absLat >= 60f)
        {
            // Transition: 0.0 (at 60°) → -1.0 (at 65°)
            // Polar Easterlies strengthen as you move poleward
            float t = (absLat - 60f) / transitionWidth;  // [0,1] over 60°-65°
            float windX = Lerp(0f, -1f, t);
            return (windX, 0f);
        }
        // ───────────────────────────────────────────────────────────────────────
        // Band 2: Westerlies [35°-55°] + Transitions [55°-60°] and [30°-35°]
        // ───────────────────────────────────────────────────────────────────────
        else if (absLat >= 55f)
        {
            // Transition: +1.0 (at 55°) → 0.0 (at 60°)
            // Westerlies weaken approaching polar boundary
            float t = (absLat - 55f) / transitionWidth;  // [0,1] over 55°-60°
            float windX = Lerp(1f, 0f, t);
            return (windX, 0f);
        }
        else if (absLat >= 35f)
        {
            // Pure Westerlies (eastward)
            return (1f, 0f);
        }
        else if (absLat >= 30f)
        {
            // Transition: 0.0 (at 30°) → +1.0 (at 35°)
            // Westerlies strengthen moving poleward from tropics
            float t = (absLat - 30f) / transitionWidth;  // [0,1] over 30°-35°
            float windX = Lerp(0f, 1f, t);
            return (windX, 0f);
        }
        // ───────────────────────────────────────────────────────────────────────
        // Band 3: Trade Winds [0°-25°] + Transition [25°-30°]
        // ───────────────────────────────────────────────────────────────────────
        else if (absLat >= 25f)
        {
            // Transition: -1.0 (at 25°) → 0.0 (at 30°)
            // Trade Winds weaken approaching subtropical boundary
            float t = (absLat - 25f) / transitionWidth;  // [0,1] over 25°-30°
            float windX = Lerp(-1f, 0f, t);
            return (windX, 0f);
        }
        else
        {
            // Pure Trade Winds (westward)
            return (-1f, 0f);
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
