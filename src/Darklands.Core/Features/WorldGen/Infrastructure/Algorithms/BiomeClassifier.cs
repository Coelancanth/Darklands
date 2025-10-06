using System;
using System.Linq;
using Darklands.Core.Features.WorldGen.Domain;

namespace Darklands.Core.Features.WorldGen.Infrastructure.Algorithms;

/// <summary>
/// Biome classification based on WorldEngine's Holdridge life zones model (41 biomes).
/// Uses 6 temperature bands × 7 moisture levels = 42 terrestrial biomes + water biomes.
///
/// Temperature bands (WorldEngine defaults: [0.874, 0.765, 0.594, 0.439, 0.366, 0.124]):
/// - Polar: < 0.124
/// - Alpine/Subpolar: 0.124 - 0.366
/// - Boreal: 0.366 - 0.439
/// - Cool Temperate: 0.439 - 0.594
/// - Warm Temperate: 0.594 - 0.765
/// - Subtropical: 0.765 - 0.874
/// - Tropical: > 0.874
///
/// Moisture levels (percentile-based for balanced distribution):
/// - Superarid: < 87th percentile
/// - Perarid: 75th-87th percentile
/// - Arid: 62nd-75th percentile
/// - Semiarid: 50th-62nd percentile
/// - Subhumid: 37th-50th percentile
/// - Humid: 25th-37th percentile
/// - Perhumid: 12th-25th percentile
/// - Superhumid: < 12th percentile
///
/// Reference: References/worldengine/docs/Biomes.html
/// </summary>
public static class BiomeClassifier
{
    // WorldEngine default temperature thresholds (0.0 = coldest, 1.0 = hottest)
    private static readonly float[] DefaultTemperatureThresholds =
        { 0.124f, 0.366f, 0.439f, 0.594f, 0.765f, 0.874f };

    /// <summary>
    /// Classifies biomes using WorldEngine's proven Holdridge life zones algorithm.
    /// Uses HUMIDITY (precipitation + irrigation) instead of raw precipitation for moisture classification.
    /// This ensures biomes near rivers/lakes are wetter even if direct rainfall is low.
    /// </summary>
    /// <param name="heightmap">Elevation data</param>
    /// <param name="oceanMask">Ocean mask (water biomes)</param>
    /// <param name="humidityMap">Humidity (precip + irrigation, 0.0-1.0)</param>
    /// <param name="quantiles">Humidity quantile thresholds for 8-level moisture classification</param>
    /// <param name="temperatureMap">Temperature (0.0-1.0)</param>
    /// <param name="seaLevel">Sea level threshold</param>
    /// <returns>Biome classification for each cell</returns>
    public static BiomeType[,] Classify(
        float[,] heightmap,
        bool[,] oceanMask,
        float[,] humidityMap,
        HumidityQuantiles quantiles,
        float[,] temperatureMap,
        float seaLevel)
    {
        int height = heightmap.GetLength(0);
        int width = heightmap.GetLength(1);

        var biomes = new BiomeType[height, width];

        // NOTE: No need to calculate percentiles here - they're already computed
        // by HumidityCalculator and passed as 'quantiles' parameter

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float elevation = heightmap[y, x];
                float temp = temperatureMap[y, x];
                float humidity = humidityMap[y, x];

                // Water biomes
                if (oceanMask[y, x])
                {
                    biomes[y, x] = elevation < seaLevel * 0.8f
                        ? BiomeType.Ocean
                        : BiomeType.ShallowWater;
                    continue;
                }

                // Land biomes: temperature × humidity (moisture) classification
                biomes[y, x] = ClassifyLandBiome(temp, humidity, quantiles);
            }
        }

        return biomes;
    }

    /// <summary>
    /// Classifies a land cell based on temperature and HUMIDITY (not precipitation!).
    /// Follows WorldEngine's exact biome classification logic.
    /// </summary>
    private static BiomeType ClassifyLandBiome(float temp, float humidity, HumidityQuantiles quantiles)
    {
        // Determine temperature band
        var tempBand = GetTemperatureBand(temp);

        // Determine moisture level from humidity (precip + irrigation)
        var moistureLevel = GetMoistureLevelFromHumidity(humidity, quantiles);

        // Map (temperature, moisture) → biome
        return (tempBand, moistureLevel) switch
        {
            // Polar zone (coldest)
            (TempBand.Polar, MoistureLevel.Superarid) => BiomeType.PolarDesert,
            (TempBand.Polar, _) => BiomeType.PolarIce,

            // Alpine/Subpolar zone (tundra)
            (TempBand.Alpine, MoistureLevel.Superarid) => BiomeType.SubpolarDryTundra,
            (TempBand.Alpine, MoistureLevel.Perarid) => BiomeType.SubpolarMoistTundra,
            (TempBand.Alpine, MoistureLevel.Arid) => BiomeType.SubpolarWetTundra,
            (TempBand.Alpine, _) => BiomeType.SubpolarRainTundra,

            // Boreal zone (cold temperate)
            (TempBand.Boreal, MoistureLevel.Superarid) => BiomeType.BorealDesert,
            (TempBand.Boreal, MoistureLevel.Perarid) => BiomeType.BorealDryScrub,
            (TempBand.Boreal, MoistureLevel.Arid) => BiomeType.BorealMoistForest,
            (TempBand.Boreal, MoistureLevel.Semiarid) => BiomeType.BorealWetForest,
            (TempBand.Boreal, _) => BiomeType.BorealRainForest,

            // Cool Temperate zone
            (TempBand.CoolTemperate, MoistureLevel.Superarid) => BiomeType.CoolTemperateDesert,
            (TempBand.CoolTemperate, MoistureLevel.Perarid) => BiomeType.CoolTemperateDesertScrub,
            (TempBand.CoolTemperate, MoistureLevel.Arid) => BiomeType.CoolTemperateSteppe,
            (TempBand.CoolTemperate, MoistureLevel.Semiarid) => BiomeType.CoolTemperateMoistForest,
            (TempBand.CoolTemperate, MoistureLevel.Subhumid) => BiomeType.CoolTemperateWetForest,
            (TempBand.CoolTemperate, _) => BiomeType.CoolTemperateRainForest,

            // Warm Temperate zone
            (TempBand.WarmTemperate, MoistureLevel.Superarid) => BiomeType.WarmTemperateDesert,
            (TempBand.WarmTemperate, MoistureLevel.Perarid) => BiomeType.WarmTemperateDesertScrub,
            (TempBand.WarmTemperate, MoistureLevel.Arid) => BiomeType.WarmTemperateThornScrub,
            (TempBand.WarmTemperate, MoistureLevel.Semiarid) => BiomeType.WarmTemperateDryForest,
            (TempBand.WarmTemperate, MoistureLevel.Subhumid) => BiomeType.WarmTemperateMoistForest,
            (TempBand.WarmTemperate, MoistureLevel.Humid) => BiomeType.WarmTemperateWetForest,
            (TempBand.WarmTemperate, _) => BiomeType.WarmTemperateRainForest,

            // Subtropical zone
            (TempBand.Subtropical, MoistureLevel.Superarid) => BiomeType.SubtropicalDesert,
            (TempBand.Subtropical, MoistureLevel.Perarid) => BiomeType.SubtropicalDesertScrub,
            (TempBand.Subtropical, MoistureLevel.Arid) => BiomeType.SubtropicalThornWoodland,
            (TempBand.Subtropical, MoistureLevel.Semiarid) => BiomeType.SubtropicalDryForest,
            (TempBand.Subtropical, MoistureLevel.Subhumid) => BiomeType.SubtropicalMoistForest,
            (TempBand.Subtropical, MoistureLevel.Humid) => BiomeType.SubtropicalWetForest,
            (TempBand.Subtropical, _) => BiomeType.SubtropicalRainForest,

            // Tropical zone (hottest)
            (TempBand.Tropical, MoistureLevel.Superarid) => BiomeType.TropicalDesert,
            (TempBand.Tropical, MoistureLevel.Perarid) => BiomeType.TropicalDesertScrub,
            (TempBand.Tropical, MoistureLevel.Arid) => BiomeType.TropicalThornWoodland,
            (TempBand.Tropical, MoistureLevel.Semiarid) => BiomeType.TropicalVeryDryForest,
            (TempBand.Tropical, MoistureLevel.Subhumid) => BiomeType.TropicalDryForest,
            (TempBand.Tropical, MoistureLevel.Humid) => BiomeType.TropicalMoistForest,
            (TempBand.Tropical, MoistureLevel.Perhumid) => BiomeType.TropicalWetForest,
            (TempBand.Tropical, _) => BiomeType.TropicalRainForest,

            _ => BiomeType.PolarDesert // Fallback (should not reach)
        };
    }

    /// <summary>
    /// Determines temperature band using WorldEngine thresholds.
    /// </summary>
    private static TempBand GetTemperatureBand(float temp)
    {
        if (temp < DefaultTemperatureThresholds[0]) return TempBand.Polar;
        if (temp < DefaultTemperatureThresholds[1]) return TempBand.Alpine;
        if (temp < DefaultTemperatureThresholds[2]) return TempBand.Boreal;
        if (temp < DefaultTemperatureThresholds[3]) return TempBand.CoolTemperate;
        if (temp < DefaultTemperatureThresholds[4]) return TempBand.WarmTemperate;
        if (temp < DefaultTemperatureThresholds[5]) return TempBand.Subtropical;
        return TempBand.Tropical;
    }

    /// <summary>
    /// Determines moisture level from humidity using pre-calculated quantile thresholds.
    /// Humidity = precipitation + irrigation (weighted 1:3), so this properly accounts
    /// for both direct rainfall AND proximity to water bodies.
    /// </summary>
    private static MoistureLevel GetMoistureLevelFromHumidity(float humidity, HumidityQuantiles quantiles)
    {
        // HumidityQuantiles are ordered from highest (wettest) to lowest (driest)
        // Quantile12 = top 94.1% (only 5.9% of land is wetter) = Superhumid threshold
        // Quantile87 = top 0.2% (99.8% of land is wetter) = Superarid threshold

        if (humidity >= quantiles.Quantile12) return MoistureLevel.Superhumid;  // Wettest 12%
        if (humidity >= quantiles.Quantile25) return MoistureLevel.Perhumid;    // 12-25%
        if (humidity >= quantiles.Quantile37) return MoistureLevel.Humid;       // 25-37%
        if (humidity >= quantiles.Quantile50) return MoistureLevel.Subhumid;    // 37-50%
        if (humidity >= quantiles.Quantile62) return MoistureLevel.Semiarid;    // 50-62%
        if (humidity >= quantiles.Quantile75) return MoistureLevel.Arid;        // 62-75%
        if (humidity >= quantiles.Quantile87) return MoistureLevel.Perarid;     // 75-87%
        return MoistureLevel.Superarid;  // Driest 13%
    }

    // Temperature bands (from coldest to hottest)
    private enum TempBand
    {
        Polar,          // < 0.124
        Alpine,         // 0.124 - 0.366 (Subpolar/Tundra)
        Boreal,         // 0.366 - 0.439
        CoolTemperate,  // 0.439 - 0.594
        WarmTemperate,  // 0.594 - 0.765
        Subtropical,    // 0.765 - 0.874
        Tropical        // > 0.874
    }

    // Moisture levels (from driest to wettest)
    private enum MoistureLevel
    {
        Superarid,   // < p87 (driest 13%)
        Perarid,     // p87-p75
        Arid,        // p75-p62
        Semiarid,    // p62-p50
        Subhumid,    // p50-p37
        Humid,       // p37-p25
        Perhumid,    // p25-p12
        Superhumid   // > p12 (wettest 12%)
    }
}
