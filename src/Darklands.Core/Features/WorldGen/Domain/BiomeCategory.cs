namespace Darklands.Core.Features.WorldGen.Domain;

/// <summary>
/// Whittaker-style major biome categories for simplified visualization.
/// Groups Holdridge's 41 detailed biomes into 9 visually distinct categories.
///
/// Purpose: Strategic layer readability - easier to distinguish biomes at world map scale.
/// The simulation still uses full 41-biome Holdridge classification internally.
///
/// Reference: Robert E. Ricklefs' "The Economy of Nature" (ecology textbook standard)
/// Color palette: Field-tested Ricklefs colors from ecology literature
///
/// Ricklefs Colors (RGB hex):
/// - Tundra:                            #C1E1DD (pale cyan)
/// - BorealForest:                      #A5C790 (sage green)
/// - TemperateSeasonalForest:           #97B669 (olive green)
/// - TemperateRainForest:               #75A95E (forest green)
/// - TropicalRainForest:                #317A22 (deep green)
/// - TropicalSeasonalForestSavanna:     #A09700 (olive yellow)
/// - SubtropicalDesert:                 #DCBB50 (sandy yellow)
/// - TemperateGrasslandDesert:          #FCD57A (pale yellow)
/// - WoodlandShrubland:                 #D16E3F (rust orange)
/// - Water:                             #0077be (ocean blue - not in Ricklefs, added for our use)
/// </summary>
public enum BiomeCategory
{
    /// <summary>
    /// Ocean and shallow water biomes (not in Ricklefs, added for completeness).
    /// Corresponds to: Ocean, ShallowWater (2 Holdridge biomes)
    /// Color: Ocean blue (to be determined - not in Ricklefs palette)
    /// </summary>
    Water,

    /// <summary>
    /// Cold, treeless plains with permafrost (includes polar ice).
    /// Corresponds to: PolarIce, PolarDesert, Subpolar tundra variants (6 Holdridge biomes)
    /// Temperature: Very cold (below 10°C mean annual)
    /// Precipitation: Variable (dry to wet tundra)
    /// Ricklefs Color: #C1E1DD (pale cyan)
    /// </summary>
    Tundra,

    /// <summary>
    /// Cold coniferous forests (spruce, fir, pine).
    /// Corresponds to: Boreal forests (5 Holdridge biomes)
    /// Temperature: Cold (0-10°C mean annual)
    /// Precipitation: Moderate to high
    /// Also called: Taiga
    /// Ricklefs Color: #A5C790 (sage green)
    /// </summary>
    BorealForest,

    /// <summary>
    /// Cool-temperate deciduous and mixed forests.
    /// Corresponds to: Cool temperate moist/wet forests (4 Holdridge biomes)
    /// Temperature: Moderate (10-20°C mean annual)
    /// Precipitation: Moderate (500-1500mm)
    /// Examples: Oak-maple forests, beech forests
    /// Ricklefs Color: #97B669 (olive green)
    /// </summary>
    TemperateSeasonalForest,

    /// <summary>
    /// Cool-temperate rainforests with high precipitation.
    /// Corresponds to: Cool temperate rain forest, warm temperate wet/rain forests (3 Holdridge biomes)
    /// Temperature: Moderate (10-20°C mean annual)
    /// Precipitation: Very high (>2000mm)
    /// Examples: Pacific Northwest rainforests
    /// Ricklefs Color: #75A95E (forest green)
    /// </summary>
    TemperateRainForest,

    /// <summary>
    /// Tropical rainforests with year-round high precipitation.
    /// Corresponds to: Tropical rain forest (1 Holdridge biome)
    /// Temperature: Hot (25-28°C mean annual)
    /// Precipitation: Very high, year-round (>2000mm)
    /// Examples: Amazon, Congo Basin
    /// Ricklefs Color: #317A22 (deep green)
    /// </summary>
    TropicalRainForest,

    /// <summary>
    /// Tropical seasonal forests and savannas with distinct wet/dry periods.
    /// Corresponds to: Subtropical/tropical moist/dry forests, tropical very dry forest (6 Holdridge biomes)
    /// Temperature: Hot (20-28°C mean annual)
    /// Precipitation: Moderate, seasonal (500-1500mm)
    /// Examples: Monsoon forests, African savanna
    /// Ricklefs Color: #A09700 (olive yellow)
    /// </summary>
    TropicalSeasonalForestSavanna,

    /// <summary>
    /// Hot, extremely arid subtropical regions.
    /// Corresponds to: Subtropical desert, subtropical desert scrub (2 Holdridge biomes)
    /// Temperature: Hot (20-25°C mean annual)
    /// Precipitation: Very low (<250mm)
    /// Examples: Sahara, Arabian Desert
    /// Ricklefs Color: #DCBB50 (sandy yellow)
    /// </summary>
    SubtropicalDesert,

    /// <summary>
    /// Cool-temperate grasslands and cold deserts.
    /// Corresponds to: Cool temperate steppe/desert, boreal desert/scrub (5 Holdridge biomes)
    /// Temperature: Moderate (5-20°C mean annual)
    /// Precipitation: Low (250-500mm)
    /// Examples: Prairie, steppe, Gobi Desert
    /// Ricklefs Color: #FCD57A (pale yellow)
    /// </summary>
    TemperateGrasslandDesert,

    /// <summary>
    /// Warm-temperate shrublands and woodlands.
    /// Corresponds to: Warm temperate thorn scrub/dry forest, subtropical thorn woodland (5 Holdridge biomes)
    /// Temperature: Warm (15-25°C mean annual)
    /// Precipitation: Low to moderate (250-750mm)
    /// Examples: Mediterranean scrubland, chaparral
    /// Ricklefs Color: #D16E3F (rust orange)
    /// </summary>
    WoodlandShrubland
}
