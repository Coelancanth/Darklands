using System;

namespace Darklands.Core.Features.WorldGen.Infrastructure.Algorithms;

/// <summary>
/// Climate simulation algorithms (precipitation, temperature).
/// Simplified models suitable for strategy game world generation.
/// </summary>
public static class ClimateCalculator
{
    /// <summary>
    /// Calculates precipitation based on latitude.
    /// Simplified model: ITCZ (wet equator), Hadley cells (dry subtropics), wet temperate zones.
    /// </summary>
    /// <param name="heightmap">Elevation data (used for map dimensions)</param>
    /// <param name="oceanMask">Ocean mask (oceans get more precipitation)</param>
    /// <returns>Precipitation map (0.0 = arid, 1.0 = very wet)</returns>
    public static float[,] CalculatePrecipitation(float[,] heightmap, bool[,] oceanMask)
    {
        int height = heightmap.GetLength(0);
        int width = heightmap.GetLength(1);

        var precipitation = new float[height, width];

        for (int y = 0; y < height; y++)
        {
            // Latitude: 0 = equator (center), ±1 = poles (edges)
            float latitude = Math.Abs((y / (float)(height - 1)) * 2f - 1f);

            // Precipitation bands (simplified):
            // - Equator (0.0-0.1): ITCZ, very wet (0.8-1.0)
            // - Subtropics (0.3-0.4): Hadley cell descent, dry (0.2-0.3)
            // - Temperate (0.5-0.7): Frontal systems, wet (0.5-0.7)
            // - Polar (0.8-1.0): Cold, dry (0.3-0.4)

            float basePercip;

            if (latitude < 0.15f)
                basePercip = 0.9f; // Equatorial (ITCZ)
            else if (latitude < 0.35f)
                basePercip = Lerp(0.9f, 0.25f, (latitude - 0.15f) / 0.2f); // Transition to subtropical
            else if (latitude < 0.5f)
                basePercip = 0.25f; // Subtropical high (dry)
            else if (latitude < 0.7f)
                basePercip = Lerp(0.25f, 0.6f, (latitude - 0.5f) / 0.2f); // Temperate (wet)
            else
                basePercip = Lerp(0.6f, 0.35f, (latitude - 0.7f) / 0.3f); // Polar (dry)

            for (int x = 0; x < width; x++)
            {
                float precip = basePercip;

                // Oceans get more precipitation (evaporation source)
                if (oceanMask[y, x])
                    precip = Math.Min(1.0f, precip + 0.1f);

                precipitation[y, x] = Math.Clamp(precip, 0f, 1f);
            }
        }

        return precipitation;
    }

    /// <summary>
    /// Calculates temperature based on latitude and elevation.
    /// Simplified model: Hot equator, cold poles, elevation cooling.
    /// </summary>
    /// <param name="heightmap">Elevation data (for elevation cooling)</param>
    /// <param name="oceanMask">Ocean mask (oceans moderate temperature)</param>
    /// <returns>Temperature map (0.0 = coldest, 1.0 = hottest)</returns>
    public static float[,] CalculateTemperature(float[,] heightmap, bool[,] oceanMask)
    {
        int height = heightmap.GetLength(0);
        int width = heightmap.GetLength(1);

        var temperature = new float[height, width];

        for (int y = 0; y < height; y++)
        {
            // Latitude: 0 = equator (center), ±1 = poles (edges)
            float latitude = Math.Abs((y / (float)(height - 1)) * 2f - 1f);

            // Base temperature from latitude (cosine curve for realistic gradient)
            float baseTemp = (float)Math.Cos(latitude * Math.PI / 2); // 1.0 at equator, 0.0 at poles

            for (int x = 0; x < width; x++)
            {
                float temp = baseTemp;

                // Elevation cooling: -6.5°C per 1000m (standard atmospheric lapse rate)
                // Assume heightmap 0.0-1.0 = 0-5000m elevation
                // So elevation cooling = -0.1 per 0.2 elevation units
                float elevationCooling = heightmap[y, x] * 0.5f;
                temp = Math.Max(0f, temp - elevationCooling);

                // Oceans moderate temperature (reduce extremes)
                if (oceanMask[y, x])
                    temp = Lerp(temp, 0.5f, 0.2f); // Pull towards moderate temp

                temperature[y, x] = Math.Clamp(temp, 0f, 1f);
            }
        }

        return temperature;
    }

    private static float Lerp(float a, float b, float t) => a + t * (b - a);
}
