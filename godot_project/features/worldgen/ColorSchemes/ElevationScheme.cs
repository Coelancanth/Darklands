using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Darklands.Core.Features.WorldGen.Application.Common;
using Darklands.Core.Features.WorldGen.Application.DTOs;

namespace Darklands.Features.WorldGen.ColorSchemes;

/// <summary>
/// Elevation visualization: Quantile-based 7-band terrain gradient.
/// Matches plate-tectonics library reference implementation (map_drawing.cpp).
/// Deep ocean → Ocean → Shallow water → Grass → Hills → Mountains → Peaks.
/// Used by ColoredOriginalElevation, ColoredPostProcessedElevation view modes.
/// </summary>
public class ElevationScheme : IColorScheme
{
    public string Name => "Elevation";

    // SSOT: 7-band terrain color gradient (plate-tectonics reference palette)
    // Deep ocean → Ocean
    private static readonly Color DeepOceanStart = new(0f, 0f, 1f);
    private static readonly Color DeepOceanEnd = new(0f, 0.078f, 0.784f);

    // Ocean → Shallow water
    private static readonly Color OceanStart = new(0f, 0.078f, 0.784f);
    private static readonly Color OceanEnd = new(0.196f, 0.314f, 0.882f);

    // Shallow water → Land transition
    private static readonly Color ShallowStart = new(0.196f, 0.314f, 0.882f);
    private static readonly Color ShallowEnd = new(0.529f, 0.929f, 0.922f);

    // Land/grass → Hills
    private static readonly Color LandStart = new(0.345f, 0.678f, 0.192f);
    private static readonly Color LandEnd = new(0.855f, 0.886f, 0.227f);

    // Hills → Mountains
    private static readonly Color HillsStart = new(0.855f, 0.886f, 0.227f);
    private static readonly Color HillsEnd = new(0.984f, 0.988f, 0.165f);

    // Mountains → High peaks
    private static readonly Color MountainsStart = new(0.984f, 0.988f, 0.165f);
    private static readonly Color MountainsEnd = new(0.357f, 0.110f, 0.051f);

    // High peaks → Extreme peaks
    private static readonly Color PeaksStart = new(0.357f, 0.110f, 0.051f);
    private static readonly Color PeaksEnd = new(0.200f, 0f, 0.016f);

    public List<LegendEntry> GetLegendEntries()
    {
        return new List<LegendEntry>
        {
            new("Deep Blue", DeepOceanStart, "Deep ocean"),
            new("Blue", OceanStart, "Ocean"),
            new("Cyan", ShallowEnd, "Shallow water"),
            new("Green", LandStart, "Grass/Lowlands"),
            new("Yellow-Green", LandEnd, "Hills"),
            new("Yellow", HillsEnd, "Mountains"),
            new("Brown", MountainsEnd, "Peaks")
        };
    }

    public Color GetColor(float normalizedValue, params object[] context)
    {
        // Context must contain quantiles: float[] { q15, q70, q75, q90, q95, q99 }
        if (context.Length == 0 || context[0] is not float[] quantiles || quantiles.Length != 6)
        {
            throw new ArgumentException("ElevationScheme requires 6 quantile thresholds in context[0]");
        }

        float q15 = quantiles[0];
        float q70 = quantiles[1];
        float q75 = quantiles[2];
        float q90 = quantiles[3];
        float q95 = quantiles[4];
        float q99 = quantiles[5];

        // Quantile-based color bands
        if (normalizedValue < q15)
            return Gradient(normalizedValue, 0.0f, q15, DeepOceanStart, DeepOceanEnd);

        if (normalizedValue < q70)
            return Gradient(normalizedValue, q15, q70, OceanStart, OceanEnd);

        if (normalizedValue < q75)
            return Gradient(normalizedValue, q70, q75, ShallowStart, ShallowEnd);

        if (normalizedValue < q90)
            return Gradient(normalizedValue, q75, q90, LandStart, LandEnd);

        if (normalizedValue < q95)
            return Gradient(normalizedValue, q90, q95, HillsStart, HillsEnd);

        if (normalizedValue < q99)
            return Gradient(normalizedValue, q95, q99, MountainsStart, MountainsEnd);

        return Gradient(normalizedValue, q99, 1.0f, PeaksStart, PeaksEnd);
    }

    /// <summary>
    /// Linear interpolation between two colors.
    /// </summary>
    private static Color Gradient(float value, float min, float max, Color colorA, Color colorB)
    {
        float delta = max - min;
        if (delta < 0.00001f) return colorA;

        float t = Mathf.Clamp((value - min) / delta, 0f, 1f);
        return colorA.Lerp(colorB, t);
    }

    /// <summary>
    /// [TD_025] Complete rendering pipeline - renders quantile-based elevation with water/land separation.
    /// Migrated from WorldMapRendererNode.RenderColoredElevation() (~260 lines).
    /// </summary>
    /// <remarks>
    /// This is the most complex scheme with 4-layer architecture:
    /// 1. Statistical Analysis: Land-only quantile calculation (excludes ALL water)
    /// 2. Water Rendering: Unified blue gradient (ocean + basins) with depth-based coloring
    /// 3. Land Rendering: ColorBrewer2 hypsometric tinting (7-band terrain gradient)
    /// 4. Shoreline Blending: Seamless transitions at ALL water boundaries
    /// </remarks>
    public Image? Render(WorldGenerationResult data, MapViewMode viewMode)
    {
        // Select heightmap based on view mode
        float[,]? heightmap = viewMode == MapViewMode.ColoredOriginalElevation
            ? data.Heightmap  // Original raw [0-20]
            : data.PostProcessedHeightmap;  // Post-processed raw [0.1-20]

        if (heightmap == null || data.OceanMask == null)
            return null;

        bool[,] oceanMask = data.OceanMask;
        List<BasinMetadata>? preservedBasins = data.Phase1Erosion?.PreservedBasins;

        int h = heightmap.GetLength(0);
        int w = heightmap.GetLength(1);
        var image = Image.CreateEmpty(w, h, false, Image.Format.Rgb8);

        // ═══════════════════════════════════════════════════════════════════════
        // STEP 1: Normalize heightmap to [0, 1] range
        // ═══════════════════════════════════════════════════════════════════════

        float min = float.MaxValue, max = float.MinValue;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float v = heightmap[y, x];
                if (v < min) min = v;
                if (v > max) max = v;
            }
        }

        float delta = Math.Max(1e-6f, max - min);

        // Create normalized heightmap [0, 1]
        var normalizedHeightmap = new float[h, w];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                normalizedHeightmap[y, x] = (heightmap[y, x] - min) / delta;
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // STEP 2: Calculate quantiles on LAND-ONLY normalized data
        // ═══════════════════════════════════════════════════════════════════════

        // Build water body exclusion set (ocean + inner seas + lakes)
        var waterCells = new HashSet<(int x, int y)>();

        // Add ocean cells
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (oceanMask[y, x])
                    waterCells.Add((x, y));
            }
        }

        // Add basin cells (inner seas + lakes)
        if (preservedBasins != null)
        {
            foreach (var basin in preservedBasins)
            {
                foreach (var cell in basin.Cells)
                {
                    waterCells.Add(cell);
                }
            }
        }

        // Calculate quantiles on land cells only (exclude ALL water!)
        var quantiles = CalculateQuantilesLandOnly(normalizedHeightmap, waterCells);
        float q15 = quantiles[0], q70 = quantiles[1], q75 = quantiles[2];
        float q90 = quantiles[3], q95 = quantiles[4], q99 = quantiles[5];

        // ═══════════════════════════════════════════════════════════════════════
        // STEP 3: Build water body lookup (for fast basin cell checks)
        // ═══════════════════════════════════════════════════════════════════════

        var basinCellLookup = new Dictionary<(int x, int y), BasinMetadata>();
        if (preservedBasins != null)
        {
            foreach (var basin in preservedBasins)
            {
                foreach (var cell in basin.Cells)
                {
                    basinCellLookup[cell] = basin;
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        // STEP 4: Unified water/land rendering with seamless shoreline blend
        // ═══════════════════════════════════════════════════════════════════════

        // Color constants (shared waterline color = convergence point for ALL boundaries)
        Color seaLevelColor = new Color(0.620f, 0.792f, 0.882f);   // #9ECAE1 - WATERLINE
        Color oceanDeep = new Color(0.031f, 0.318f, 0.612f);       // #08519C - DEEP WATER

        // Blend width: 1.5% of elevation range
        float shorelineBlendWidth = 0.015f * (max - min);
        const float seaLevelRaw = 1.0f;  // WorldGenConstants.SEA_LEVEL_RAW

        // Calculate per-basin minimum elevations (for basin-relative depth normalization)
        var basinMinElevations = new Dictionary<int, float>();
        if (preservedBasins != null)
        {
            foreach (var basin in preservedBasins)
            {
                float basinMin = float.MaxValue;
                foreach (var (cellX, cellY) in basin.Cells)
                {
                    if (cellX >= 0 && cellX < w && cellY >= 0 && cellY < h)
                    {
                        float elev = heightmap[cellY, cellX];
                        if (elev < basinMin) basinMin = elev;
                    }
                }
                basinMinElevations[basin.BasinId] = basinMin;
            }
        }

        int innerSeaCount = 0, lakeCount = 0;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float cellElevRaw = heightmap[y, x];

                // Determine water body classification
                bool isBasinCell = basinCellLookup.TryGetValue((x, y), out var basin);
                bool isOceanCell = !isBasinCell && oceanMask[y, x];

                // Determine local waterline (sea level for ocean, surface elevation for basins)
                float waterlineRaw = isBasinCell ? basin!.SurfaceElevation : seaLevelRaw;

                // Calculate distance to waterline (negative = below water, positive = above water)
                float distToWaterline = cellElevRaw - waterlineRaw;

                // WATER RENDERING (below waterline)
                if (isBasinCell || isOceanCell)
                {
                    // Unified water gradient: depth from 0 (waterline) to 1 (max depth)
                    float depthNorm;

                    if (isBasinCell)
                    {
                        // Basin: normalize depth relative to basin range
                        float basinMinElev = basinMinElevations[basin!.BasinId];
                        float denom = Math.Max(0.001f, basin.SurfaceElevation - basinMinElev);
                        depthNorm = Mathf.Clamp((basin.SurfaceElevation - cellElevRaw) / denom, 0f, 1f);

                        // Stats
                        if (basin.Area >= 1000) innerSeaCount++; else lakeCount++;
                    }
                    else
                    {
                        // Ocean: use SeaDepth array if available, otherwise fallback calculation
                        if (data.SeaDepth != null)
                        {
                            depthNorm = Mathf.Clamp(data.SeaDepth[y, x], 0f, 1f);
                        }
                        else
                        {
                            // Fallback: normalize ocean depth by distance below sea level
                            float oceanDepth = seaLevelRaw - cellElevRaw;
                            float oceanDepthMax = seaLevelRaw - min;
                            depthNorm = Mathf.Clamp(oceanDepth / Math.Max(0.001f, oceanDepthMax), 0f, 1f);
                        }
                    }

                    // Water color gradient: shallow (seaLevelColor) → deep (oceanDeep)
                    Color waterColor = seaLevelColor.Lerp(oceanDeep, depthNorm);

                    // Shoreline blend: apply smoothstep near waterline
                    if (distToWaterline < shorelineBlendWidth && distToWaterline > -shorelineBlendWidth)
                    {
                        float blendT = SmoothStep(-shorelineBlendWidth, 0f, distToWaterline);
                        waterColor = waterColor.Lerp(seaLevelColor, blendT);
                    }

                    image.SetPixel(x, y, waterColor);
                }
                // LAND RENDERING (above waterline)
                else
                {
                    // Quantile-based terrain color (ColorBrewer hypsometric tinting)
                    float elevNorm = normalizedHeightmap[y, x];
                    Color landColor = GetQuantileTerrainColor(elevNorm, q15, q70, q75, q90, q95, q99);

                    // Shoreline blend on land side
                    if (distToWaterline < shorelineBlendWidth)
                    {
                        float blendT = SmoothStep(0f, shorelineBlendWidth, distToWaterline);
                        Color finalColor = seaLevelColor.Lerp(landColor, blendT);
                        image.SetPixel(x, y, finalColor);
                    }
                    else
                    {
                        // Far from waterline: pure terrain color
                        image.SetPixel(x, y, landColor);
                    }
                }
            }
        }

        return image;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Helper Methods (migrated from WorldMapRendererNode)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Calculates quantiles on LAND-ONLY elevations (excludes ALL water bodies).
    /// Returns array of 6 quantile values: [q15, q70, q75, q90, q95, q99].
    /// </summary>
    private float[] CalculateQuantilesLandOnly(float[,] normalizedHeightmap, HashSet<(int x, int y)> waterCells)
    {
        int h = normalizedHeightmap.GetLength(0);
        int w = normalizedHeightmap.GetLength(1);

        // Extract land-only elevations (exclude ALL water: ocean + basins)
        var landElevations = new List<float>();
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (!waterCells.Contains((x, y)))  // Land cell only
                {
                    landElevations.Add(normalizedHeightmap[y, x]);
                }
            }
        }

        // Guard: If no land cells, return default quantiles
        if (landElevations.Count == 0)
        {
            return new float[] { 0.15f, 0.70f, 0.75f, 0.90f, 0.95f, 0.99f };
        }

        // Sort for quantile calculation
        landElevations.Sort();

        // Calculate 6 quantiles on land distribution
        return new float[]
        {
            GetPercentileFromSorted(landElevations, 0.15f),
            GetPercentileFromSorted(landElevations, 0.70f),
            GetPercentileFromSorted(landElevations, 0.75f),
            GetPercentileFromSorted(landElevations, 0.90f),
            GetPercentileFromSorted(landElevations, 0.95f),
            GetPercentileFromSorted(landElevations, 0.99f)
        };
    }

    /// <summary>
    /// Maps elevation to terrain color using quantile thresholds.
    /// ColorBrewer2 "RdYlGn" (reversed) adapted for terrain: Green lowlands → Brown peaks.
    /// </summary>
    private Color GetQuantileTerrainColor(float h, float q15, float q70, float q75, float q90, float q95, float q99)
    {
        // Band 1 (0 - q15): Below lowlands
        if (h < q15)
            return Gradient(h, 0.0f, q15,
                new Color(0.651f, 0.851f, 0.416f),  // Light green
                new Color(0.400f, 0.741f, 0.388f)); // Green

        // Band 2 (q15 - q70): Lowlands & Plains (55% of land)
        if (h < q70)
            return Gradient(h, q15, q70,
                new Color(0.400f, 0.741f, 0.388f),  // Green
                new Color(0.851f, 0.937f, 0.545f)); // Yellow-green

        // Band 3 (q70 - q75): Low Hills (5% transition)
        if (h < q75)
            return Gradient(h, q70, q75,
                new Color(0.851f, 0.937f, 0.545f),  // Yellow-green
                new Color(1.000f, 1.000f, 0.749f)); // Yellow

        // Band 4 (q75 - q90): Hills (15% of land)
        if (h < q90)
            return Gradient(h, q75, q90,
                new Color(1.000f, 1.000f, 0.749f),  // Yellow
                new Color(0.992f, 0.682f, 0.380f)); // Orange

        // Band 5 (q90 - q95): Mountains (5%)
        if (h < q95)
            return Gradient(h, q90, q95,
                new Color(0.992f, 0.682f, 0.380f),  // Orange
                new Color(0.957f, 0.427f, 0.263f)); // Dark orange

        // Band 6 (q95 - q99): High Mountains (4%)
        if (h < q99)
            return Gradient(h, q95, q99,
                new Color(0.957f, 0.427f, 0.263f),  // Dark orange
                new Color(0.843f, 0.188f, 0.153f)); // Brown-red

        // Band 7 (q99 - 1.0): Peaks (top 1%)
        return Gradient(h, q99, 1.0f,
            new Color(0.843f, 0.188f, 0.153f),  // Brown-red
            new Color(0.647f, 0.000f, 0.149f)); // Dark brown
    }

    /// <summary>
    /// Gets percentile value from a sorted list.
    /// </summary>
    private float GetPercentileFromSorted(List<float> sortedValues, float percentile)
    {
        if (sortedValues.Count == 0) return 0f;
        int index = (int)Mathf.Floor(percentile * (sortedValues.Count - 1));
        index = Mathf.Clamp(index, 0, sortedValues.Count - 1);
        return sortedValues[index];
    }

    /// <summary>
    /// Smoothstep interpolation for soft transitions at boundaries.
    /// </summary>
    private float SmoothStep(float edge0, float edge1, float x)
    {
        float t = Mathf.Clamp((x - edge0) / Math.Max(1e-6f, edge1 - edge0), 0f, 1f);
        return t * t * (3f - 2f * t);
    }
}
