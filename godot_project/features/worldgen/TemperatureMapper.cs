using Godot;

namespace Darklands.Features.WorldGen;

/// <summary>
/// Maps normalized temperature values [0,1] from climate simulation to real-world Celsius.
/// Pure presentation utility - NOT used by algorithms (they use normalized values).
/// </summary>
/// <remarks>
/// VS_025: Separation of concerns -
/// - **Algorithms** use normalized [0,1] temperature (for future biome classification via quantile thresholds)
/// - **Display/UI** use Celsius mapping for human-readable output ("-15.2°C")
///
/// Mapping approach:
/// - Temperature range: [0,1] → [-60°C, +40°C]
/// - Matches WorldEngine's proven climate simulation results
/// - Frozen peaks (-60°C) to hot lowlands (+40°C)
///
/// This creates realistic temperatures for player-facing UI without complicating algorithm logic.
/// Pattern matches ElevationMapper (normalized elevation → meters).
/// </remarks>
public static class TemperatureMapper
{
    // Real-world temperature range (Celsius)
    private const float MIN_TEMP_CELSIUS = -60f;  // Frozen peaks at poles
    private const float MAX_TEMP_CELSIUS = 40f;   // Hot lowlands at equator

    /// <summary>
    /// Converts normalized [0,1] temperature to Celsius for display.
    /// </summary>
    /// <param name="normalizedTemp">Normalized temperature from simulation [0,1]</param>
    /// <returns>Temperature in Celsius (negative = freezing, positive = warm)</returns>
    public static float ToCelsius(float normalizedTemp)
    {
        return normalizedTemp * (MAX_TEMP_CELSIUS - MIN_TEMP_CELSIUS) + MIN_TEMP_CELSIUS;
        // [0,1] → [-60°C, +40°C]
    }

    /// <summary>
    /// Formats temperature for probe/tooltip display.
    /// </summary>
    /// <param name="normalizedTemp">Normalized temperature from simulation [0,1]</param>
    /// <returns>Formatted temperature string (e.g., "-15.2°C")</returns>
    public static string FormatTemperature(float normalizedTemp)
    {
        float celsius = ToCelsius(normalizedTemp);
        return $"{celsius:F1}°C";
    }

    /// <summary>
    /// Formats temperature with climate zone hint (future enhancement).
    /// </summary>
    /// <param name="normalizedTemp">Normalized temperature from simulation [0,1]</param>
    /// <returns>Formatted temperature with zone (e.g., "-15.2°C (Polar)")</returns>
    public static string FormatTemperatureWithZone(float normalizedTemp)
    {
        float celsius = ToCelsius(normalizedTemp);

        string zone = celsius switch
        {
            < -40f => "Polar",
            < -20f => "Alpine",
            < 0f => "Boreal",
            < 10f => "Cool",
            < 20f => "Warm",
            < 30f => "Subtropical",
            _ => "Tropical"
        };

        return $"{celsius:F1}°C ({zone})";
    }
}
