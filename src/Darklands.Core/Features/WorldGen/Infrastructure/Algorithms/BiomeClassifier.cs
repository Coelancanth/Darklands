using System;
using Darklands.Core.Features.WorldGen.Domain;

namespace Darklands.Core.Features.WorldGen.Infrastructure.Algorithms;

/// <summary>
/// Biome classification based on simplified Holdridge life zones model.
/// Combines temperature, precipitation, and elevation to determine biome types.
/// </summary>
public static class BiomeClassifier
{
    /// <summary>
    /// Classifies biomes using temperature, precipitation, and elevation.
    /// </summary>
    /// <param name="heightmap">Elevation data</param>
    /// <param name="oceanMask">Ocean mask (water biomes)</param>
    /// <param name="precipitationMap">Precipitation (0.0-1.0)</param>
    /// <param name="temperatureMap">Temperature (0.0-1.0)</param>
    /// <param name="seaLevel">Sea level threshold</param>
    /// <returns>Biome classification for each cell</returns>
    public static BiomeType[,] Classify(
        float[,] heightmap,
        bool[,] oceanMask,
        float[,] precipitationMap,
        float[,] temperatureMap,
        float seaLevel)
    {
        int height = heightmap.GetLength(0);
        int width = heightmap.GetLength(1);

        var biomes = new BiomeType[height, width];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float elevation = heightmap[y, x];
                float temp = temperatureMap[y, x];
                float precip = precipitationMap[y, x];

                // Water biomes
                if (oceanMask[y, x])
                {
                    biomes[y, x] = elevation < seaLevel * 0.8f
                        ? BiomeType.Ocean
                        : BiomeType.ShallowWater;
                    continue;
                }

                // Ice (high elevation or very cold)
                if (elevation > 0.85f || (elevation > 0.7f && temp < 0.2f))
                {
                    biomes[y, x] = BiomeType.Ice;
                    continue;
                }

                // Tundra (cold, low precipitation)
                if (temp < 0.25f)
                {
                    biomes[y, x] = BiomeType.Tundra;
                    continue;
                }

                // Boreal forest (cold, moderate precipitation)
                if (temp < 0.45f && precip > 0.4f)
                {
                    biomes[y, x] = BiomeType.BorealForest;
                    continue;
                }

                // Desert (hot, very dry)
                if (precip < 0.3f && temp > 0.6f)
                {
                    biomes[y, x] = BiomeType.Desert;
                    continue;
                }

                // Tropical rainforest (hot, very wet)
                if (temp > 0.7f && precip > 0.7f)
                {
                    biomes[y, x] = BiomeType.TropicalRainforest;
                    continue;
                }

                // Tropical seasonal forest (hot, moderate precipitation)
                if (temp > 0.7f && precip > 0.4f)
                {
                    biomes[y, x] = BiomeType.TropicalSeasonalForest;
                    continue;
                }

                // Savanna (hot, low-moderate precipitation)
                if (temp > 0.6f && precip > 0.3f)
                {
                    biomes[y, x] = BiomeType.Savanna;
                    continue;
                }

                // Temperate rainforest (moderate temp, very wet)
                if (temp > 0.4f && temp < 0.7f && precip > 0.7f)
                {
                    biomes[y, x] = BiomeType.TemperateRainforest;
                    continue;
                }

                // Temperate forest (moderate temp, moderate precipitation)
                if (temp > 0.4f && temp < 0.7f && precip > 0.4f)
                {
                    biomes[y, x] = BiomeType.TemperateForest;
                    continue;
                }

                // Grassland (moderate temp, low precipitation)
                if (precip > 0.3f)
                {
                    biomes[y, x] = BiomeType.Grassland;
                    continue;
                }

                // Default fallback (should rarely happen)
                biomes[y, x] = BiomeType.Grassland;
            }
        }

        return biomes;
    }
}
