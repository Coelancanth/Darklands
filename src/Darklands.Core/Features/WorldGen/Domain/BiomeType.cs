namespace Darklands.Core.Features.WorldGen.Domain;

/// <summary>
/// Biome classification based on elevation, precipitation, and temperature.
/// Uses simplified Holdridge life zones model.
/// </summary>
public enum BiomeType
{
    /// <summary>Ocean or deep water body</summary>
    Ocean = 0,

    /// <summary>Shallow coastal water or lake</summary>
    ShallowWater = 1,

    /// <summary>Ice cap or permanent snow cover (high elevation or polar)</summary>
    Ice = 2,

    /// <summary>Tundra (cold, low precipitation)</summary>
    Tundra = 3,

    /// <summary>Boreal forest / Taiga (cold, moderate precipitation)</summary>
    BorealForest = 4,

    /// <summary>Temperate grassland (moderate temp, low precipitation)</summary>
    Grassland = 5,

    /// <summary>Temperate forest (moderate temp, moderate precipitation)</summary>
    TemperateForest = 6,

    /// <summary>Temperate rainforest (moderate temp, high precipitation)</summary>
    TemperateRainforest = 7,

    /// <summary>Desert (hot, very low precipitation)</summary>
    Desert = 8,

    /// <summary>Savanna (hot, low to moderate precipitation)</summary>
    Savanna = 9,

    /// <summary>Tropical seasonal forest (hot, moderate precipitation)</summary>
    TropicalSeasonalForest = 10,

    /// <summary>Tropical rainforest (hot, high precipitation)</summary>
    TropicalRainforest = 11
}
