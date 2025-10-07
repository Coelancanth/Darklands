namespace Darklands.Core.Features.WorldGen.Domain;

/// <summary>
/// Biome classification based on Holdridge life zones model (41 biomes).
/// Based on WorldEngine's proven biome system with 3-axis classification:
/// - Temperature: Polar → Tropical (6 bands)
/// - Moisture: Desert → Rainforest (7 levels)
/// - Elevation: Determines ice caps
/// Reference: References/worldengine/docs/Biomes.html
/// </summary>
public enum BiomeType
{
    // Water biomes
    /// <summary>Ocean or deep water body</summary>
    Ocean = 0,
    /// <summary>Shallow coastal water or lake</summary>
    ShallowWater = 1,

    // Polar (coldest zone)
    /// <summary>Polar ice cap</summary>
    PolarIce = 2,
    /// <summary>Polar desert (dry, freezing)</summary>
    PolarDesert = 3,

    // Subpolar/Tundra zone
    /// <summary>Subpolar moist tundra</summary>
    SubpolarMoistTundra = 4,
    /// <summary>Subpolar wet tundra</summary>
    SubpolarWetTundra = 5,
    /// <summary>Subpolar rain tundra</summary>
    SubpolarRainTundra = 6,
    /// <summary>Subpolar dry tundra</summary>
    SubpolarDryTundra = 7,

    // Boreal zone (cold temperate)
    /// <summary>Boreal desert</summary>
    BorealDesert = 8,
    /// <summary>Boreal dry scrub</summary>
    BorealDryScrub = 9,
    /// <summary>Boreal moist forest</summary>
    BorealMoistForest = 10,
    /// <summary>Boreal wet forest</summary>
    BorealWetForest = 11,
    /// <summary>Boreal rain forest</summary>
    BorealRainForest = 12,

    // Cool temperate zone
    /// <summary>Cool temperate moist forest</summary>
    CoolTemperateMoistForest = 13,
    /// <summary>Cool temperate wet forest</summary>
    CoolTemperateWetForest = 14,
    /// <summary>Cool temperate rain forest</summary>
    CoolTemperateRainForest = 15,
    /// <summary>Cool temperate steppes/grassland</summary>
    CoolTemperateSteppe = 16,
    /// <summary>Cool temperate desert</summary>
    CoolTemperateDesert = 17,
    /// <summary>Cool temperate desert scrub</summary>
    CoolTemperateDesertScrub = 18,

    // Warm temperate zone
    /// <summary>Warm temperate moist forest</summary>
    WarmTemperateMoistForest = 19,
    /// <summary>Warm temperate wet forest</summary>
    WarmTemperateWetForest = 20,
    /// <summary>Warm temperate rain forest</summary>
    WarmTemperateRainForest = 21,
    /// <summary>Warm temperate thorn scrub</summary>
    WarmTemperateThornScrub = 22,
    /// <summary>Warm temperate dry forest</summary>
    WarmTemperateDryForest = 23,
    /// <summary>Warm temperate desert</summary>
    WarmTemperateDesert = 24,
    /// <summary>Warm temperate desert scrub</summary>
    WarmTemperateDesertScrub = 25,

    // Subtropical zone
    /// <summary>Subtropical thorn woodland</summary>
    SubtropicalThornWoodland = 26,
    /// <summary>Subtropical dry forest</summary>
    SubtropicalDryForest = 27,
    /// <summary>Subtropical moist forest</summary>
    SubtropicalMoistForest = 28,
    /// <summary>Subtropical wet forest</summary>
    SubtropicalWetForest = 29,
    /// <summary>Subtropical rain forest</summary>
    SubtropicalRainForest = 30,
    /// <summary>Subtropical desert</summary>
    SubtropicalDesert = 31,
    /// <summary>Subtropical desert scrub</summary>
    SubtropicalDesertScrub = 32,

    // Tropical zone (hottest)
    /// <summary>Tropical thorn woodland</summary>
    TropicalThornWoodland = 33,
    /// <summary>Tropical very dry forest</summary>
    TropicalVeryDryForest = 34,
    /// <summary>Tropical dry forest</summary>
    TropicalDryForest = 35,
    /// <summary>Tropical moist forest</summary>
    TropicalMoistForest = 36,
    /// <summary>Tropical wet forest</summary>
    TropicalWetForest = 37,
    /// <summary>Tropical rain forest</summary>
    TropicalRainForest = 38,
    /// <summary>Tropical desert</summary>
    TropicalDesert = 39,
    /// <summary>Tropical desert scrub</summary>
    TropicalDesertScrub = 40
}
