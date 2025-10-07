namespace Darklands.Core.Features.WorldGen.Domain;

/// <summary>
/// Extension methods for BiomeType to support simplified visualization.
/// Provides mapping from Holdridge's 41 detailed biomes to Ricklefs' 9 major categories.
/// </summary>
public static class BiomeExtensions
{
    /// <summary>
    /// Maps Holdridge biome (41 types) to Ricklefs category (9 types) for simplified visualization.
    ///
    /// Mapping rationale:
    /// - Groups biomes by dominant temperature/moisture characteristics
    /// - Preserves ecological meaning (tundra vs forest vs desert)
    /// - Optimized for visual distinction at strategic map scale
    ///
    /// Temperature progression: Polar → Subpolar → Boreal → Cool Temperate → Warm Temperate → Subtropical → Tropical
    /// Moisture progression: Desert → Scrub → Grassland → Dry Forest → Moist Forest → Wet Forest → Rain Forest
    /// </summary>
    public static BiomeCategory ToRicklefs(this BiomeType biome) => biome switch
    {
        // ═══════════════════════════════════════════════════════════════════════
        // WATER BIOMES (2 types → 1 category)
        // ═══════════════════════════════════════════════════════════════════════
        BiomeType.Ocean => BiomeCategory.Water,
        BiomeType.ShallowWater => BiomeCategory.Water,

        // ═══════════════════════════════════════════════════════════════════════
        // TUNDRA (Polar + Subpolar, 6 types → 1 category)
        // Rationale: All treeless, cold regions group together visually
        // ═══════════════════════════════════════════════════════════════════════
        BiomeType.PolarIce => BiomeCategory.Tundra,
        BiomeType.PolarDesert => BiomeCategory.Tundra,
        BiomeType.SubpolarDryTundra => BiomeCategory.Tundra,
        BiomeType.SubpolarMoistTundra => BiomeCategory.Tundra,
        BiomeType.SubpolarWetTundra => BiomeCategory.Tundra,
        BiomeType.SubpolarRainTundra => BiomeCategory.Tundra,

        // ═══════════════════════════════════════════════════════════════════════
        // BOREAL FOREST (5 types → 1 category)
        // Rationale: Cold coniferous forests (taiga)
        // Includes: Boreal deserts/scrub (transition zones still recognizable as boreal)
        // ═══════════════════════════════════════════════════════════════════════
        BiomeType.BorealDesert => BiomeCategory.BorealForest,
        BiomeType.BorealDryScrub => BiomeCategory.BorealForest,
        BiomeType.BorealMoistForest => BiomeCategory.BorealForest,
        BiomeType.BorealWetForest => BiomeCategory.BorealForest,
        BiomeType.BorealRainForest => BiomeCategory.BorealForest,

        // ═══════════════════════════════════════════════════════════════════════
        // TEMPERATE SEASONAL FOREST (4 types → 1 category)
        // Rationale: Cool-temperate deciduous/mixed forests
        // Moisture: Moderate (moist to wet, excludes rain forest)
        // ═══════════════════════════════════════════════════════════════════════
        BiomeType.CoolTemperateMoistForest => BiomeCategory.TemperateSeasonalForest,
        BiomeType.CoolTemperateWetForest => BiomeCategory.TemperateSeasonalForest,
        BiomeType.WarmTemperateMoistForest => BiomeCategory.TemperateSeasonalForest,
        BiomeType.WarmTemperateDryForest => BiomeCategory.TemperateSeasonalForest,

        // ═══════════════════════════════════════════════════════════════════════
        // TEMPERATE RAIN FOREST (3 types → 1 category)
        // Rationale: Very high precipitation temperate forests
        // Examples: Pacific Northwest, New Zealand
        // ═══════════════════════════════════════════════════════════════════════
        BiomeType.CoolTemperateRainForest => BiomeCategory.TemperateRainForest,
        BiomeType.WarmTemperateWetForest => BiomeCategory.TemperateRainForest,
        BiomeType.WarmTemperateRainForest => BiomeCategory.TemperateRainForest,

        // ═══════════════════════════════════════════════════════════════════════
        // TROPICAL RAIN FOREST (1 type → 1 category)
        // Rationale: Year-round hot + wet (Amazon, Congo)
        // ═══════════════════════════════════════════════════════════════════════
        BiomeType.TropicalRainForest => BiomeCategory.TropicalRainForest,

        // ═══════════════════════════════════════════════════════════════════════
        // TROPICAL SEASONAL FOREST / SAVANNA (6 types → 1 category)
        // Rationale: Tropical/subtropical with seasonal rainfall
        // Includes: Moist/wet forests (monsoon), dry forests, thorn woodlands
        // ═══════════════════════════════════════════════════════════════════════
        BiomeType.SubtropicalDryForest => BiomeCategory.TropicalSeasonalForestSavanna,
        BiomeType.SubtropicalMoistForest => BiomeCategory.TropicalSeasonalForestSavanna,
        BiomeType.SubtropicalWetForest => BiomeCategory.TropicalSeasonalForestSavanna,
        BiomeType.SubtropicalRainForest => BiomeCategory.TropicalSeasonalForestSavanna,
        BiomeType.TropicalVeryDryForest => BiomeCategory.TropicalSeasonalForestSavanna,
        BiomeType.TropicalDryForest => BiomeCategory.TropicalSeasonalForestSavanna,
        BiomeType.TropicalMoistForest => BiomeCategory.TropicalSeasonalForestSavanna,
        BiomeType.TropicalWetForest => BiomeCategory.TropicalSeasonalForestSavanna,

        // ═══════════════════════════════════════════════════════════════════════
        // SUBTROPICAL DESERT (2 types → 1 category)
        // Rationale: Hot, extremely arid (Sahara, Arabian)
        // ═══════════════════════════════════════════════════════════════════════
        BiomeType.SubtropicalDesert => BiomeCategory.SubtropicalDesert,
        BiomeType.SubtropicalDesertScrub => BiomeCategory.SubtropicalDesert,
        BiomeType.TropicalDesert => BiomeCategory.SubtropicalDesert,
        BiomeType.TropicalDesertScrub => BiomeCategory.SubtropicalDesert,

        // ═══════════════════════════════════════════════════════════════════════
        // TEMPERATE GRASSLAND / DESERT (4 types → 1 category)
        // Rationale: Cool-temperate dry regions (prairie, steppe, cold deserts)
        // ═══════════════════════════════════════════════════════════════════════
        BiomeType.CoolTemperateSteppe => BiomeCategory.TemperateGrasslandDesert,
        BiomeType.CoolTemperateDesert => BiomeCategory.TemperateGrasslandDesert,
        BiomeType.CoolTemperateDesertScrub => BiomeCategory.TemperateGrasslandDesert,
        BiomeType.WarmTemperateDesert => BiomeCategory.TemperateGrasslandDesert,

        // ═══════════════════════════════════════════════════════════════════════
        // WOODLAND / SHRUBLAND (5 types → 1 category)
        // Rationale: Warm-temperate scrublands and woodlands (Mediterranean, chaparral)
        // Includes: Subtropical/tropical thorn woodlands
        // ═══════════════════════════════════════════════════════════════════════
        BiomeType.WarmTemperateThornScrub => BiomeCategory.WoodlandShrubland,
        BiomeType.WarmTemperateDesertScrub => BiomeCategory.WoodlandShrubland,
        BiomeType.SubtropicalThornWoodland => BiomeCategory.WoodlandShrubland,
        BiomeType.TropicalThornWoodland => BiomeCategory.WoodlandShrubland,

        // Fallback (should never reach here if all 41 biomes are mapped)
        _ => BiomeCategory.TemperateGrasslandDesert
    };
}
