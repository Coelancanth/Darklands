using Godot;
using System;
using System.Linq;
using Darklands.Core.Features.WorldGen.Application.Common;
using Darklands.Core.Features.WorldGen.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace Darklands.Features.WorldGen;

/// <summary>
/// Renders WorldGenerationResult data as a texture.
/// Supports multiple view modes including triple-heightmap elevation views (VS_024).
/// Pure rendering - no UI, no input handling.
/// </summary>
public partial class WorldMapRendererNode : Sprite2D
{
    private ILogger<WorldMapRendererNode>? _logger;
    private WorldGenerationResult? _worldData;
    private MapViewMode _currentViewMode = MapViewMode.ColoredOriginalElevation;  // Default to original elevation

    [Signal]
    public delegate void RenderCompleteEventHandler(int width, int height);

    public override void _Ready()
    {
        _logger?.LogDebug("WorldMapRendererNode ready");
    }

    /// <summary>
    /// Sets the world data and renders it using the current view mode.
    /// </summary>
    public void SetWorldData(WorldGenerationResult data, ILogger<WorldMapRendererNode> logger)
    {
        _worldData = data;
        _logger = logger;
        RenderCurrentView();
        EmitSignal(SignalName.RenderComplete, data.Width, data.Height);
    }

    /// <summary>
    /// Changes the view mode and re-renders.
    /// </summary>
    public void SetViewMode(MapViewMode mode)
    {
        if (_worldData == null)
        {
            _logger?.LogWarning("Cannot change view mode: No world data loaded");
            return;
        }

        _currentViewMode = mode;
        RenderCurrentView();
    }

    /// <summary>
    /// Gets the current view mode.
    /// </summary>
    public MapViewMode GetCurrentViewMode() => _currentViewMode;

    /// <summary>
    /// Gets the loaded world data (null if not loaded).
    /// </summary>
    public WorldGenerationResult? GetWorldData() => _worldData;

    private void RenderCurrentView()
    {
        if (_worldData == null) return;

        switch (_currentViewMode)
        {
            case MapViewMode.RawElevation:
                RenderRawElevation(_worldData.Heightmap);
                break;

            case MapViewMode.Plates:
                RenderPlates(_worldData.PlatesMap);
                break;

            case MapViewMode.ColoredOriginalElevation:
                RenderColoredElevation(_worldData.Heightmap, _worldData.OceanMask, _worldData.Phase1Erosion?.PreservedBasins);  // Original raw [0-20] + ocean mask + lakes
                break;

            case MapViewMode.ColoredPostProcessedElevation:
                if (_worldData.PostProcessedHeightmap != null)
                {
                    RenderColoredElevation(_worldData.PostProcessedHeightmap, _worldData.OceanMask, _worldData.Phase1Erosion?.PreservedBasins);  // Post-processed raw [0.1-20] + ocean mask + lakes
                }
                else
                {
                    _logger?.LogWarning("Post-processed heightmap not available, falling back to original");
                    RenderColoredElevation(_worldData.Heightmap, _worldData.OceanMask, _worldData.Phase1Erosion?.PreservedBasins);
                }
                break;

            case MapViewMode.TemperatureLatitudeOnly:
                if (_worldData.TemperatureLatitudeOnly != null)
                {
                    RenderTemperatureMap(_worldData.TemperatureLatitudeOnly);
                }
                else
                {
                    _logger?.LogWarning("Temperature (LatitudeOnly) not available");
                }
                break;

            case MapViewMode.TemperatureWithNoise:
                if (_worldData.TemperatureWithNoise != null)
                {
                    RenderTemperatureMap(_worldData.TemperatureWithNoise);
                }
                else
                {
                    _logger?.LogWarning("Temperature (WithNoise) not available");
                }
                break;

            case MapViewMode.TemperatureWithDistance:
                if (_worldData.TemperatureWithDistance != null)
                {
                    RenderTemperatureMap(_worldData.TemperatureWithDistance);
                }
                else
                {
                    _logger?.LogWarning("Temperature (WithDistance) not available");
                }
                break;

            case MapViewMode.TemperatureFinal:
                if (_worldData.TemperatureFinal != null)
                {
                    RenderTemperatureMap(_worldData.TemperatureFinal);
                }
                else
                {
                    _logger?.LogWarning("Temperature (Final) not available");
                }
                break;

            case MapViewMode.PrecipitationNoiseOnly:
                if (_worldData.BaseNoisePrecipitationMap != null)
                {
                    RenderPrecipitationMap(_worldData.BaseNoisePrecipitationMap);
                }
                else
                {
                    _logger?.LogWarning("Precipitation (NoiseOnly) not available");
                }
                break;

            case MapViewMode.PrecipitationTemperatureShaped:
                if (_worldData.TemperatureShapedPrecipitationMap != null)
                {
                    RenderPrecipitationMap(_worldData.TemperatureShapedPrecipitationMap);
                }
                else
                {
                    _logger?.LogWarning("Precipitation (TemperatureShaped) not available");
                }
                break;

            case MapViewMode.PrecipitationBase:
                if (_worldData.FinalPrecipitationMap != null)
                {
                    RenderPrecipitationMap(_worldData.FinalPrecipitationMap);
                }
                else
                {
                    _logger?.LogWarning("Precipitation (Final) not available");
                }
                break;

            case MapViewMode.PrecipitationWithRainShadow:
                if (_worldData.WithRainShadowPrecipitationMap != null)
                {
                    RenderPrecipitationMap(_worldData.WithRainShadowPrecipitationMap);
                }
                else
                {
                    _logger?.LogWarning("Precipitation (WithRainShadow) not available");
                }
                break;

            case MapViewMode.PrecipitationFinal:
                if (_worldData.PrecipitationFinal != null)
                {
                    RenderPrecipitationMap(_worldData.PrecipitationFinal);
                }
                else
                {
                    _logger?.LogWarning("Precipitation (Final) not available");
                }
                break;

            case MapViewMode.SinksPreFilling:
                if (_worldData.PreFillingLocalMinima != null && _worldData.PostProcessedHeightmap != null && _worldData.OceanMask != null)
                {
                    RenderSinksPreFilling(_worldData.PostProcessedHeightmap, _worldData.OceanMask, _worldData.PreFillingLocalMinima);
                }
                else
                {
                    _logger?.LogWarning("SinksPreFilling data not available");
                }
                break;

            case MapViewMode.SinksPostFilling:
                if (_worldData.Phase1Erosion != null && _worldData.Phase1Erosion.FilledHeightmap != null && _worldData.OceanMask != null)
                {
                    // TD_023: PreservedBasins contains BasinMetadata - extract centers for rendering
                    var lakeCenters = _worldData.Phase1Erosion.PreservedBasins.Select(b => b.Center).ToList();
                    RenderSinksPostFilling(_worldData.Phase1Erosion.FilledHeightmap, _worldData.OceanMask, lakeCenters);
                }
                else
                {
                    _logger?.LogWarning("SinksPostFilling data not available");
                }
                break;

            case MapViewMode.PreservedLakes:
                if (_worldData.Phase1Erosion != null && _worldData.Phase1Erosion.FilledHeightmap != null && _worldData.OceanMask != null)
                {
                    RenderPreservedLakes(_worldData.Phase1Erosion.FilledHeightmap, _worldData.OceanMask, _worldData.Phase1Erosion.PreservedBasins);
                }
                else
                {
                    _logger?.LogWarning("PreservedLakes data not available");
                }
                break;

            case MapViewMode.FlowDirections:
                if (_worldData.Phase1Erosion != null)
                {
                    RenderFlowDirections(_worldData.Phase1Erosion.FlowDirections);
                }
                else
                {
                    _logger?.LogWarning("FlowDirections not available");
                }
                break;

            case MapViewMode.FlowAccumulation:
                if (_worldData.Phase1Erosion != null && _worldData.OceanMask != null)
                {
                    RenderFlowAccumulation(_worldData.Phase1Erosion.FlowAccumulation, _worldData.OceanMask);
                }
                else
                {
                    _logger?.LogWarning("FlowAccumulation not available");
                }
                break;

            case MapViewMode.RiverSources:
                if (_worldData.Phase1Erosion != null && _worldData.Phase1Erosion.FilledHeightmap != null)
                {
                    RenderRiverSources(_worldData.Phase1Erosion.FilledHeightmap, _worldData.Phase1Erosion.RiverSources);
                }
                else
                {
                    _logger?.LogWarning("RiverSources not available");
                }
                break;

            case MapViewMode.ErosionHotspots:
                if (_worldData.Phase1Erosion != null && _worldData.Phase1Erosion.FilledHeightmap != null && _worldData.Phase1Erosion.FlowAccumulation != null && _worldData.Thresholds != null)
                {
                    RenderErosionHotspots(_worldData.Phase1Erosion.FilledHeightmap, _worldData.Phase1Erosion.FlowAccumulation, _worldData.Thresholds);
                }
                else
                {
                    _logger?.LogWarning("ErosionHotspots not available");
                }
                break;

            default:
                _logger?.LogError("Unknown view mode: {ViewMode}", _currentViewMode);
                break;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Rendering Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Renders raw heightmap as grayscale (min=black, max=white).
    /// </summary>
    private void RenderRawElevation(float[,] heightmap)
    {
        int h = heightmap.GetLength(0);
        int w = heightmap.GetLength(1);
        var image = Image.CreateEmpty(w, h, false, Image.Format.Rgb8);

        // Find min/max for normalization
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
        _logger?.LogDebug("RawElevation: min={Min:F3} max={Max:F3} delta={Delta:F3}", min, max, delta);

        // Render grayscale
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float t = (heightmap[y, x] - min) / delta;
                image.SetPixel(x, y, new Color(t, t, t));
            }
        }

        Texture = ImageTexture.CreateFromImage(image);
        _logger?.LogInformation("Rendered RawElevation: {Width}x{Height}", w, h);
    }

    /// <summary>
    /// Renders plate ownership with unique color per plate.
    /// </summary>
    private void RenderPlates(uint[,] platesMap)
    {
        int h = platesMap.GetLength(0);
        int w = platesMap.GetLength(1);
        var image = Image.CreateEmpty(w, h, false, Image.Format.Rgb8);

        // Generate deterministic colors for plates (seed=42 for consistency)
        var rng = new Random(42);
        var plateColors = new System.Collections.Generic.Dictionary<uint, Color>();

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                uint plateId = platesMap[y, x];

                if (!plateColors.ContainsKey(plateId))
                {
                    // Generate vibrant color (avoid too dark or too light)
                    plateColors[plateId] = new Color(
                        (float)rng.NextDouble() * 0.6f + 0.2f,
                        (float)rng.NextDouble() * 0.6f + 0.2f,
                        (float)rng.NextDouble() * 0.6f + 0.2f
                    );
                }

                image.SetPixel(x, y, plateColors[plateId]);
            }
        }

        Texture = ImageTexture.CreateFromImage(image);
        _logger?.LogInformation("Rendered Plates: {Width}x{Height}, {PlateCount} unique plates",
            w, h, plateColors.Count);
    }

    /// <summary>
    /// Renders elevation with quantile-based color gradient.
    /// Matches plate-tectonics library reference implementation (map_drawing.cpp).
    /// Uses quantiles to adapt colors to heightmap distribution.
    /// IMPORTANT: Normalizes heightmap to [0,1] range before applying quantile-based gradient.
    /// Works with both raw [0-20] and pre-normalized [0,1] heightmaps (VS_024 triple-heightmap support).
    ///
    /// FIX: Water bodies render as semantic blue colors (not quantile-based terrain colors).
    /// - Ocean: Dark blue (semantic main water body)
    /// - Inner seas: Medium blue (landlocked, large basins)
    /// - Lakes: Light blue/cyan (landlocked, small basins)
    /// Quantiles calculated on LAND-ONLY elevations for accurate terrain distribution.
    /// </summary>
    private void RenderColoredElevation(float[,] heightmap, bool[,]? oceanMask = null, System.Collections.Generic.List<BasinMetadata>? preservedBasins = null)
    {
        int h = heightmap.GetLength(0);
        int w = heightmap.GetLength(1);
        var image = Image.CreateEmpty(w, h, false, Image.Format.Rgb8);

        // Step 1: Normalize heightmap to [0, 1] range (reference implementation expects this!)
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
        _logger?.LogDebug("ColoredElevation: min={Min:F3} max={Max:F3} delta={Delta:F3}", min, max, delta);

        // Create normalized heightmap [0, 1]
        var normalizedHeightmap = new float[h, w];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                normalizedHeightmap[y, x] = (heightmap[y, x] - min) / delta;
            }
        }

        // Step 2: Calculate quantiles on LAND-ONLY normalized data (FIX: exclude ocean from distribution)
        float q15, q70, q75, q90, q95, q99;

        if (oceanMask != null)
        {
            // Calculate quantiles on land cells only (statistically correct!)
            var quantiles = CalculateQuantilesLandOnly(normalizedHeightmap, oceanMask);
            q15 = quantiles[0];
            q70 = quantiles[1];
            q75 = quantiles[2];
            q90 = quantiles[3];
            q95 = quantiles[4];
            q99 = quantiles[5];

            _logger?.LogDebug("ColoredElevation quantiles (LAND-ONLY): q15={Q15:F3} q70={Q70:F3} q75={Q75:F3} q90={Q90:F3} q95={Q95:F3} q99={Q99:F3}",
                q15, q70, q75, q90, q95, q99);
        }
        else
        {
            // Fallback: Calculate quantiles on all cells (backward compatible)
            q15 = FindQuantile(normalizedHeightmap, 0.15f);
            q70 = FindQuantile(normalizedHeightmap, 0.70f);
            q75 = FindQuantile(normalizedHeightmap, 0.75f);
            q90 = FindQuantile(normalizedHeightmap, 0.90f);
            q95 = FindQuantile(normalizedHeightmap, 0.95f);
            q99 = FindQuantile(normalizedHeightmap, 0.99f);

            _logger?.LogDebug("ColoredElevation quantiles (ALL CELLS): q15={Q15:F3} q70={Q70:F3} q75={Q75:F3} q90={Q90:F3} q95={Q95:F3} q99={Q99:F3}",
                q15, q70, q75, q90, q95, q99);
        }

        // Step 3: Build water body lookup (for fast basin cell checks)
        var basinCellLookup = new System.Collections.Generic.Dictionary<(int x, int y), BasinMetadata>();
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

        // Step 4: Render with unified water-land gradient (ColorBrewer-inspired)
        //
        // UNIFIED COLOR SCHEME (converges at sea level 1.0):
        // ┌─────────────────────────────────────────────────────────────┐
        // │ BELOW SEA LEVEL (0.0 - 1.0): Water Bodies (depth gradients) │
        // ├─────────────────────────────────────────────────────────────┤
        // │ Ocean:      Deep #08519C → Sea Level #9ECAE1                │
        // │ Inner Seas: Deep #006D5B → Sea Level #9ECAE1 (teal hues)    │
        // │ Lakes:      Deep #0077B6 → Sea Level #9ECAE1 (turquoise)    │
        // ├─────────────────────────────────────────────────────────────┤
        // │ ABOVE SEA LEVEL (1.0+): Land (elevation gradients)          │
        // ├─────────────────────────────────────────────────────────────┤
        // │ Lowlands:   Sea Level #9ECAE1 → Green #66BD63               │
        // │ Hills:      Yellow #FFFFBF → Orange #FDAE61                  │
        // │ Mountains:  Orange → Brown-Red #D73027 → Dark Brown #A50026 │
        // └─────────────────────────────────────────────────────────────┘

        // SEA LEVEL convergence point (all water types meet here)
        Color seaLevelColor = new Color(0.620f, 0.792f, 0.882f);   // #9ECAE1 (light cyan-blue)

        // BELOW SEA LEVEL: Water body deep colors (starting points)
        Color oceanDeep = new Color(0.031f, 0.318f, 0.612f);       // Dark blue #08519C (deep ocean)
        Color innerSeaDeep = new Color(0.000f, 0.427f, 0.357f);    // Dark teal #006D5B (deep inner sea)
        Color lakeDeep = new Color(0.000f, 0.467f, 0.714f);        // Dark turquoise #0077B6 (deep lake)

        // Calculate per-basin minimum elevations (for basin-relative normalization)
        var basinMinElevations = new System.Collections.Generic.Dictionary<int, float>();
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
                Color color;

                // Check WATER BODIES FIRST - water gradient (below sea level) vs land gradient (above sea level)

                float cellElevation = heightmap[y, x];  // Raw elevation value

                // 1. Ocean (border-connected water) - DEPTH GRADIENT (global min → 1.0 sea level)
                if (oceanMask != null && oceanMask[y, x])
                {
                    // Use normalized elevation for ocean depth gradient
                    // Lower elevation = deeper ocean = darker blue
                    // Higher elevation (→ 1.0) = shallow ocean = converges to sea level color
                    float oceanDepthNorm = normalizedHeightmap[y, x];

                    // Ocean gradient: Deep blue → Sea level color (smooth convergence to coastline)
                    color = Gradient(oceanDepthNorm, 0.0f, 1.0f, oceanDeep, seaLevelColor);
                }
                // 2. Inner seas & lakes (landlocked water) - PER-BASIN DEPTH GRADIENT
                else if (basinCellLookup.TryGetValue((x, y), out var basin))
                {
                    const int INNER_SEA_THRESHOLD = 1000;  // Cells (matches TD_023 classification)

                    // Get basin-relative normalized depth (basin floor = 0.0, sea level 1.0 = 1.0)
                    float basinMinElev = basinMinElevations[basin.BasinId];
                    float seaLevel = 1.0f;  // WorldGenConstants.SEA_LEVEL_RAW
                    float basinDepthNorm = (cellElevation - basinMinElev) / Math.Max(0.001f, seaLevel - basinMinElev);
                    basinDepthNorm = Mathf.Clamp(basinDepthNorm, 0f, 1f);  // Safety clamp

                    if (basin.Area >= INNER_SEA_THRESHOLD)
                    {
                        // Inner sea: Dark teal → Sea level color (teal spectrum for distinction from ocean)
                        color = Gradient(basinDepthNorm, 0.0f, 1.0f, innerSeaDeep, seaLevelColor);
                        innerSeaCount++;
                    }
                    else
                    {
                        // Lake: Dark turquoise → Sea level color (turquoise spectrum distinct from seas)
                        color = Gradient(basinDepthNorm, 0.0f, 1.0f, lakeDeep, seaLevelColor);
                        lakeCount++;
                    }
                }
                // 3. Land (everything else) - use quantile-based terrain colors
                else
                {
                    float elevation = normalizedHeightmap[y, x];
                    color = GetQuantileTerrainColor(elevation, q15, q70, q75, q90, q95, q99);
                }

                image.SetPixel(x, y, color);
            }
        }

        Texture = ImageTexture.CreateFromImage(image);

        // Calculate water body statistics
        int innerSeaBasins = preservedBasins?.Count(b => b.Area >= 1000) ?? 0;
        int lakeBasins = preservedBasins?.Count(b => b.Area < 1000) ?? 0;

        _logger?.LogInformation(
            "Rendered ColoredElevation: {Width}x{Height} | Water bodies: Ocean (dark blue), {InnerSeaBasins} inner seas ({InnerSeaCells} cells, medium blue), {LakeBasins} lakes ({LakeCells} cells, cyan) | Land: ColorBrewer terrain gradient",
            w, h, innerSeaBasins, innerSeaCount, lakeBasins, lakeCount);
    }

    /// <summary>
    /// Finds elevation value at given quantile (percentile).
    /// Uses binary search approximation matching reference implementation.
    /// </summary>
    private float FindQuantile(float[,] heightmap, float quantile)
    {
        int height = heightmap.GetLength(0);
        int width = heightmap.GetLength(1);
        int totalCells = height * width;

        float value = 0.5f;
        float step = 0.5f;

        while (step > 0.00001f)
        {
            int count = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (heightmap[y, x] < value) count++;
                }
            }

            step *= 0.5f;
            if (count / (float)totalCells < quantile)
                value += step;
            else
                value -= step;
        }

        return value;
    }

    /// <summary>
    /// Calculates quantiles on LAND-ONLY elevations (excludes ocean cells).
    /// Returns array of 6 quantile values: [q15, q70, q75, q90, q95, q99].
    /// This ensures accurate terrain color distribution by excluding ocean from statistics.
    /// </summary>
    private float[] CalculateQuantilesLandOnly(float[,] normalizedHeightmap, bool[,] oceanMask)
    {
        int h = normalizedHeightmap.GetLength(0);
        int w = normalizedHeightmap.GetLength(1);

        // Extract land-only elevations
        var landElevations = new System.Collections.Generic.List<float>();
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (!oceanMask[y, x])  // Land cell only
                {
                    landElevations.Add(normalizedHeightmap[y, x]);
                }
            }
        }

        // Guard: If no land cells (all ocean), return default quantiles
        if (landElevations.Count == 0)
        {
            _logger?.LogWarning("CalculateQuantilesLandOnly: No land cells found (all ocean), using default quantiles");
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
    /// Uses ColorBrewer2-inspired land-only hypsometric tinting (green → yellow → orange → brown).
    /// Ocean cells excluded from quantile calculation and rendered separately as dark blue.
    /// </summary>
    /// <remarks>
    /// ColorBrewer2 "RdYlGn" (reversed) adapted for terrain elevation:
    /// - Lowlands (q15-q70): Green shades - valleys, plains, coastal lowlands
    /// - Hills (q70-q90): Yellow-green to yellow - rolling hills, plateaus
    /// - Mountains (q90-q95): Orange - high elevation, mountain ranges
    /// - Peaks (q95+): Brown-red - highest peaks, alpine zones
    ///
    /// This creates classic cartographic hypsometric tinting matching worldwide topographic maps.
    /// </remarks>
    private Color GetQuantileTerrainColor(float h, float q15, float q70, float q75, float q90, float q95, float q99)
    {
        // Band 1 (0 - q15): BELOW LOWLANDS (failsafe for edge cases)
        // Light green #A6D96A (166,217,106) → Dark green #66BD63 (102,189,99)
        if (h < q15)
            return Gradient(h, 0.0f, q15,
                new Color(0.651f, 0.851f, 0.416f),  // #A6D96A light green
                new Color(0.400f, 0.741f, 0.388f)); // #66BD63 green

        // Band 2 (q15 - q70): LOWLANDS & PLAINS (55% of land)
        // Dark green #66BD63 (102,189,99) → Yellow-green #D9EF8B (217,239,139)
        if (h < q70)
            return Gradient(h, q15, q70,
                new Color(0.400f, 0.741f, 0.388f),  // #66BD63 green
                new Color(0.851f, 0.937f, 0.545f)); // #D9EF8B yellow-green

        // Band 3 (q70 - q75): LOW HILLS (5% transition)
        // Yellow-green #D9EF8B (217,239,139) → Yellow #FFFFBF (255,255,191)
        if (h < q75)
            return Gradient(h, q70, q75,
                new Color(0.851f, 0.937f, 0.545f),  // #D9EF8B yellow-green
                new Color(1.000f, 1.000f, 0.749f)); // #FFFFBF yellow

        // Band 4 (q75 - q90): HILLS (15% of land)
        // Yellow #FFFFBF (255,255,191) → Orange #FDAE61 (253,174,97)
        if (h < q90)
            return Gradient(h, q75, q90,
                new Color(1.000f, 1.000f, 0.749f),  // #FFFFBF yellow
                new Color(0.992f, 0.682f, 0.380f)); // #FDAE61 orange

        // Band 5 (q90 - q95): MOUNTAINS (5%)
        // Orange #FDAE61 (253,174,97) → Dark orange #F46D43 (244,109,67)
        if (h < q95)
            return Gradient(h, q90, q95,
                new Color(0.992f, 0.682f, 0.380f),  // #FDAE61 orange
                new Color(0.957f, 0.427f, 0.263f)); // #F46D43 dark orange

        // Band 6 (q95 - q99): HIGH MOUNTAINS (4%)
        // Dark orange #F46D43 (244,109,67) → Brown-red #D73027 (215,48,39)
        if (h < q99)
            return Gradient(h, q95, q99,
                new Color(0.957f, 0.427f, 0.263f),  // #F46D43 dark orange
                new Color(0.843f, 0.188f, 0.153f)); // #D73027 brown-red

        // Band 7 (q99 - 1.0): PEAKS (top 1%)
        // Brown-red #D73027 (215,48,39) → Dark brown #A50026 (165,0,38)
        return Gradient(h, q99, 1.0f,
            new Color(0.843f, 0.188f, 0.153f),  // #D73027 brown-red
            new Color(0.647f, 0.000f, 0.149f)); // #A50026 dark brown
    }

    /// <summary>
    /// Renders temperature map with WorldEngine-style quantile-based color bands (VS_025).
    /// Input: Normalized [0,1] temperature values.
    /// Output: 7 discrete color bands (polar → alpine → boreal → cool → warm → subtropical → tropical).
    /// </summary>
    /// <remarks>
    /// WorldEngine approach (temperature.py + draw.py):
    /// - Uses quantile thresholds to create discrete climate zones (NOT smooth gradient)
    /// - 7 bands: polar (coldest 12.5%), alpine, boreal, cool, warm, subtropical, tropical (hottest 12.5%)
    /// - Colors: Blue → Blue-Purple → Purple → Magenta → Purple-Red → Red-Purple → Red
    ///
    /// Why quantiles? Each world has different temperature distribution (hot vs cold planets).
    /// Quantiles adapt to show climate variation regardless of absolute temperatures.
    ///
    /// Reused by all 4 temperature view modes (LatitudeOnly, WithNoise, WithDistance, Final).
    /// </remarks>
    private void RenderTemperatureMap(float[,] temperatureMap)
    {
        int h = temperatureMap.GetLength(0);
        int w = temperatureMap.GetLength(1);
        var image = Image.CreateEmpty(w, h, false, Image.Format.Rgb8);

        _logger?.LogDebug("RenderTemperatureMap: {Width}x{Height}", w, h);

        // Calculate quantile thresholds (7 temperature zones, WorldEngine-style)
        var quantiles = CalculateTemperatureQuantiles(temperatureMap);

        _logger?.LogDebug("Temperature quantiles: q12.5={Q0:F3}, q25={Q1:F3}, q37.5={Q2:F3}, q50={Q3:F3}, q62.5={Q4:F3}, q75={Q5:F3}, q87.5={Q6:F3}",
            quantiles[0], quantiles[1], quantiles[2], quantiles[3], quantiles[4], quantiles[5], quantiles[6]);

        // Render with discrete color bands based on quantiles
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float t = temperatureMap[y, x];  // Normalized [0, 1]
                Color color = GetTemperatureColorQuantile(t, quantiles);
                image.SetPixel(x, y, color);
            }
        }

        Texture = ImageTexture.CreateFromImage(image);
        _logger?.LogInformation("Rendered TemperatureMap: {Width}x{Height}", w, h);
    }

    /// <summary>
    /// Calculates temperature quantiles for discrete climate zone bands (WorldEngine approach).
    /// Returns 7 quantile thresholds: 12.5%, 25%, 37.5%, 50%, 62.5%, 75%, 87.5%.
    /// </summary>
    private float[] CalculateTemperatureQuantiles(float[,] temperatureMap)
    {
        int h = temperatureMap.GetLength(0);
        int w = temperatureMap.GetLength(1);

        // Collect all temperature values
        var temps = new System.Collections.Generic.List<float>(h * w);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                temps.Add(temperatureMap[y, x]);
            }
        }

        // Sort for quantile calculation
        temps.Sort();

        // Calculate 7 quantiles (8 bands: < q0, q0-q1, q1-q2, ... q6+)
        return new float[]
        {
            GetPercentileFromSorted(temps, 0.125f),  // 12.5% - polar
            GetPercentileFromSorted(temps, 0.25f),   // 25% - alpine
            GetPercentileFromSorted(temps, 0.375f),  // 37.5% - boreal
            GetPercentileFromSorted(temps, 0.50f),   // 50% - cool
            GetPercentileFromSorted(temps, 0.625f),  // 62.5% - warm
            GetPercentileFromSorted(temps, 0.75f),   // 75% - subtropical
            GetPercentileFromSorted(temps, 0.875f)   // 87.5% - tropical
        };
    }

    /// <summary>
    /// Gets percentile value from a sorted list (helper for quantile calculation).
    /// </summary>
    private float GetPercentileFromSorted(System.Collections.Generic.List<float> sortedValues, float percentile)
    {
        if (sortedValues.Count == 0) return 0f;

        int index = (int)Mathf.Floor(percentile * (sortedValues.Count - 1));
        index = Mathf.Clamp(index, 0, sortedValues.Count - 1);

        return sortedValues[index];
    }

    /// <summary>
    /// Maps normalized temperature to discrete color bands using quantiles (WorldEngine colors).
    /// 7 climate zones: polar, alpine, boreal, cool, warm, subtropical, tropical.
    /// Colors match WorldEngine draw.py exactly: Blue → Purple spectrum → Red.
    /// </summary>
    private Color GetTemperatureColorQuantile(float t, float[] quantiles)
    {
        // WorldEngine colors (RGB 0-255 → 0-1):
        // Polar:       (0, 0, 255)      → Blue
        // Alpine:      (42, 0, 213)     → Blue-Purple
        // Boreal:      (85, 0, 170)     → Purple
        // Cool:        (128, 0, 128)    → Magenta
        // Warm:        (170, 0, 85)     → Purple-Red
        // Subtropical: (213, 0, 42)     → Red-Purple
        // Tropical:    (255, 0, 0)      → Red

        if (t < quantiles[0])
            return new Color(0f, 0f, 1f);           // Polar: Blue

        if (t < quantiles[1])
            return new Color(42f/255f, 0f, 213f/255f);  // Alpine: Blue-Purple

        if (t < quantiles[2])
            return new Color(85f/255f, 0f, 170f/255f);  // Boreal: Purple

        if (t < quantiles[3])
            return new Color(128f/255f, 0f, 128f/255f); // Cool: Magenta

        if (t < quantiles[4])
            return new Color(170f/255f, 0f, 85f/255f);  // Warm: Purple-Red

        if (t < quantiles[5])
            return new Color(213f/255f, 0f, 42f/255f);  // Subtropical: Red-Purple

        return new Color(1f, 0f, 0f);               // Tropical: Red
    }

    /// <summary>
    /// Renders precipitation map with smooth gradient (Yellow → Green → Blue).
    /// Input: Normalized [0,1] precipitation values.
    /// Output: 3-stop color gradient (smooth, not discrete bands like temperature).
    /// </summary>
    /// <remarks>
    /// VS_026: Precipitation visualization with intuitive moisture spectrum.
    ///
    /// Color scheme (semantically distinct from temperature):
    /// - Yellow (0.0): Dry desert regions
    /// - Green (0.5): Moderate rainfall (vegetation)
    /// - Blue (1.0): Wet tropical/rainforest
    ///
    /// Smooth gradient (unlike temperature's discrete quantile bands) matches elevation rendering style.
    /// Reused by all 3 precipitation view modes (NoiseOnly, TemperatureShaped, Final).
    /// </remarks>
    private void RenderPrecipitationMap(float[,] precipitationMap)
    {
        int h = precipitationMap.GetLength(0);
        int w = precipitationMap.GetLength(1);
        var image = Image.CreateEmpty(w, h, false, Image.Format.Rgb8);

        _logger?.LogDebug("RenderPrecipitationMap: {Width}x{Height}", w, h);

        // Define 3-stop color gradient (Yellow → Green → Blue)
        // Intuitive moisture spectrum: dry deserts → moderate vegetation → wet tropics
        Color dryColor = new Color(255f/255f, 255f/255f, 0f);           // Yellow (RGB: 255, 255, 0)
        Color moderateColor = new Color(0f, 200f/255f, 0f);             // Green (RGB: 0, 200, 0)
        Color wetColor = new Color(0f, 0f, 255f/255f);                  // Blue (RGB: 0, 0, 255)

        // Render with smooth 3-stop gradient
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float p = precipitationMap[y, x];  // Normalized [0, 1]

                // 3-stop gradient: Yellow (0.0) → Green (0.5) → Blue (1.0)
                Color color;
                if (p < 0.5f)
                {
                    // Dry to moderate: Yellow → Green
                    color = Gradient(p, 0.0f, 0.5f, dryColor, moderateColor);
                }
                else
                {
                    // Moderate to wet: Green → Blue
                    color = Gradient(p, 0.5f, 1.0f, moderateColor, wetColor);
                }

                image.SetPixel(x, y, color);
            }
        }

        Texture = ImageTexture.CreateFromImage(image);
        _logger?.LogInformation("Rendered PrecipitationMap: {Width}x{Height}", w, h);
    }

    /// <summary>
    /// Linear interpolation between two colors based on value range.
    /// </summary>
    private Color Gradient(float value, float min, float max, Color colorA, Color colorB)
    {
        float delta = max - min;
        if (delta < 0.00001f) return colorA;

        float t = Mathf.Clamp((value - min) / delta, 0f, 1f);
        return colorA.Lerp(colorB, t);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // VS_029: D-8 Flow Visualization Rendering Methods
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Renders sinks BEFORE pit-filling (VS_029 Step 0A).
    /// Grayscale elevation + Red markers for ALL local minima (artifacts + real pits).
    /// Purpose: Baseline for pit-filling effectiveness comparison.
    /// </summary>
    private void RenderSinksPreFilling(float[,] heightmap, bool[,] oceanMask, System.Collections.Generic.List<(int x, int y)> sinks)
    {
        int h = heightmap.GetLength(0);
        int w = heightmap.GetLength(1);
        var image = Image.CreateEmpty(w, h, false, Image.Format.Rgb8);

        // Step 1: Render grayscale elevation base
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

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float t = (heightmap[y, x] - min) / delta;
                image.SetPixel(x, y, new Color(t, t, t));
            }
        }

        // Step 2: Mark sinks with red markers
        Color sinkMarker = new Color(1f, 0f, 0f);  // Bright red
        foreach (var (x, y) in sinks)
        {
            if (x >= 0 && x < w && y >= 0 && y < h)
            {
                image.SetPixel(x, y, sinkMarker);
            }
        }

        Texture = ImageTexture.CreateFromImage(image);

        // Calculate land cell percentage
        int landCells = 0;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (!oceanMask[y, x]) landCells++;
            }
        }

        float landPercentage = landCells > 0 ? (sinks.Count / (float)landCells) * 100f : 0f;

        _logger?.LogInformation(
            "PRE-FILLING SINKS: Total={Count} ({Percentage:F1}% of land cells) | BASELINE for pit-filling | Ocean sinks excluded",
            sinks.Count, landPercentage);
    }

    /// <summary>
    /// Renders sinks AFTER pit-filling (VS_029 Step 0B).
    /// Grayscale elevation + Red markers for preserved lakes.
    /// Purpose: Validate pit-filling algorithm (artifacts filled, real lakes preserved).
    /// </summary>
    private void RenderSinksPostFilling(float[,] heightmap, bool[,] oceanMask, System.Collections.Generic.List<(int x, int y)> lakes)
    {
        int h = heightmap.GetLength(0);
        int w = heightmap.GetLength(1);
        var image = Image.CreateEmpty(w, h, false, Image.Format.Rgb8);

        // Step 1: Render grayscale elevation base (filled heightmap)
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

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float t = (heightmap[y, x] - min) / delta;
                image.SetPixel(x, y, new Color(t, t, t));
            }
        }

        // Step 2: Mark preserved lakes with red markers
        Color lakeMarker = new Color(1f, 0f, 0f);  // Bright red
        foreach (var (x, y) in lakes)
        {
            if (x >= 0 && x < w && y >= 0 && y < h)
            {
                image.SetPixel(x, y, lakeMarker);
            }
        }

        Texture = ImageTexture.CreateFromImage(image);

        // Calculate land cell percentage
        int landCells = 0;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (!oceanMask[y, x]) landCells++;
            }
        }

        float landPercentage = landCells > 0 ? (lakes.Count / (float)landCells) * 100f : 0f;

        _logger?.LogInformation(
            "POST-FILLING SINKS: Total={Count} ({Percentage:F1}% of land cells) | Lakes preserved={LakeCount}",
            lakes.Count, landPercentage, lakes.Count);
    }

    /// <summary>
    /// Renders basin metadata from pit-filling (TD_023).
    /// Grayscale elevation base + colored basin boundaries + markers (red pour points, cyan centers).
    /// Purpose: Validate basin detection for VS_030 (boundaries for inlet detection, pour points for pathfinding).
    /// </summary>
    private void RenderPreservedLakes(float[,] heightmap, bool[,] oceanMask, System.Collections.Generic.List<BasinMetadata> basins)
    {
        int h = heightmap.GetLength(0);
        int w = heightmap.GetLength(1);
        var image = Image.CreateEmpty(w, h, false, Image.Format.Rgb8);

        // Step 1: Render grayscale elevation base
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

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float t = (heightmap[y, x] - min) / delta;
                image.SetPixel(x, y, new Color(t, t, t));
            }
        }

        // Step 1.5: Render ocean as dark blue (TD_023 tweak - better visual clarity)
        Color oceanColor = new Color(0f, 0f, 0.545f);  // Dark Blue (RGB: 0, 0, 139)
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (oceanMask[y, x])
                {
                    image.SetPixel(x, y, oceanColor);
                }
            }
        }

        // Step 2: Generate distinct colors for each basin (vibrant, deterministic)
        var rng = new Random(42);  // Seed for consistency
        var basinColors = new System.Collections.Generic.Dictionary<int, Color>();

        foreach (var basin in basins)
        {
            // Generate vibrant, saturated colors (avoid grayscale to distinguish from background)
            basinColors[basin.BasinId] = new Color(
                (float)rng.NextDouble() * 0.7f + 0.3f,  // RGB [0.3-1.0] - bright range
                (float)rng.NextDouble() * 0.7f + 0.3f,
                (float)rng.NextDouble() * 0.7f + 0.3f
            );
        }

        // Step 3: Render basin boundaries (color cells by basin ID)
        foreach (var basin in basins)
        {
            Color basinColor = basinColors[basin.BasinId];

            foreach (var (cellX, cellY) in basin.Cells)
            {
                if (cellX >= 0 && cellX < w && cellY >= 0 && cellY < h)
                {
                    // Blend basin color with elevation (50% opacity) for context
                    Color elevationBase = image.GetPixel(cellX, cellY);
                    Color blended = elevationBase.Lerp(basinColor, 0.6f);  // 60% basin color, 40% elevation
                    image.SetPixel(cellX, cellY, blended);
                }
            }
        }

        // Step 4: Mark pour points (red) and basin centers (cyan) on TOP of boundaries
        Color pourPointMarker = new Color(1f, 0f, 0f);     // Bright red (outlets)
        Color centerMarker = new Color(0f, 1f, 1f);        // Cyan (basin centers)

        foreach (var basin in basins)
        {
            // Mark pour point (outlet)
            var (pourX, pourY) = basin.PourPoint;
            if (pourX >= 0 && pourX < w && pourY >= 0 && pourY < h)
            {
                image.SetPixel(pourX, pourY, pourPointMarker);
            }

            // Mark basin center (local minimum)
            var (centerX, centerY) = basin.Center;
            if (centerX >= 0 && centerX < w && centerY >= 0 && centerY < h)
            {
                image.SetPixel(centerX, centerY, centerMarker);
            }
        }

        Texture = ImageTexture.CreateFromImage(image);

        // Step 5: Calculate statistics for diagnostic logging
        if (basins.Count > 0)
        {
            float minDepth = basins.Min(b => b.Depth);
            float maxDepth = basins.Max(b => b.Depth);
            float meanDepth = basins.Average(b => b.Depth);

            int minArea = basins.Min(b => b.Area);
            int maxArea = basins.Max(b => b.Area);
            int totalCells = basins.Sum(b => b.Area);

            // Calculate land percentage
            int landCells = 0;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (!oceanMask[y, x]) landCells++;
                }
            }

            float landPercent = landCells > 0 ? (totalCells / (float)landCells) * 100f : 0f;

            _logger?.LogInformation(
                "BASIN METADATA: {Count} preserved basins | Depths: min={MinDepth:F1}, max={MaxDepth:F1}, mean={MeanDepth:F1} | Basin sizes: min={MinArea} cells, max={MaxArea} cells, total={TotalCells} cells ({LandPercent:F1}% of land)",
                basins.Count, minDepth, maxDepth, meanDepth, minArea, maxArea, totalCells, landPercent);
        }
        else
        {
            _logger?.LogInformation("BASIN METADATA: 0 preserved basins (all pits filled)");
        }
    }

    /// <summary>
    /// Renders D-8 flow directions (VS_029 Step 2).
    /// 8-color gradient: N=Red, NE=Yellow, E=Green, SE=Cyan, S=Blue, SW=Purple, W=Magenta, NW=Orange, Sink=Black.
    /// Purpose: Validate D-8 algorithm correctness (steepest descent).
    /// </summary>
    private void RenderFlowDirections(int[,] flowDirections)
    {
        int h = flowDirections.GetLength(0);
        int w = flowDirections.GetLength(1);
        var image = Image.CreateEmpty(w, h, false, Image.Format.Rgb8);

        // Direction colors (8 directions + sink)
        Color[] directionColors = new Color[9]
        {
            new Color(1f, 0f, 0f),      // 0: North - Red
            new Color(1f, 1f, 0f),      // 1: NE - Yellow
            new Color(0f, 1f, 0f),      // 2: East - Green
            new Color(0f, 1f, 1f),      // 3: SE - Cyan
            new Color(0f, 0f, 1f),      // 4: South - Blue
            new Color(0.5f, 0f, 0.5f),  // 5: SW - Purple
            new Color(1f, 0f, 1f),      // 6: West - Magenta
            new Color(1f, 0.5f, 0f),    // 7: NW - Orange
            new Color(0f, 0f, 0f)       // -1: Sink - Black
        };

        // Count direction distribution for logging
        int[] directionCounts = new int[9];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int dir = flowDirections[y, x];
                int colorIndex = dir == -1 ? 8 : dir;  // -1 (sink) → index 8
                image.SetPixel(x, y, directionColors[colorIndex]);

                // Count for stats
                directionCounts[colorIndex]++;
            }
        }

        Texture = ImageTexture.CreateFromImage(image);

        // Calculate statistics
        int totalCells = w * h;
        float sinkPercent = (directionCounts[8] / (float)totalCells) * 100f;

        _logger?.LogInformation(
            "FLOW DIRECTIONS: Distribution N={N:F1}% NE={NE:F1}% E={E:F1}% SE={SE:F1}% S={S:F1}% SW={SW:F1}% W={W:F1}% NW={NW:F1}% Sinks={Sinks:F1}% ({SinkCount} cells)",
            (directionCounts[0] / (float)totalCells) * 100f,
            (directionCounts[1] / (float)totalCells) * 100f,
            (directionCounts[2] / (float)totalCells) * 100f,
            (directionCounts[3] / (float)totalCells) * 100f,
            (directionCounts[4] / (float)totalCells) * 100f,
            (directionCounts[5] / (float)totalCells) * 100f,
            (directionCounts[6] / (float)totalCells) * 100f,
            (directionCounts[7] / (float)totalCells) * 100f,
            sinkPercent,
            directionCounts[8]);
    }

    /// <summary>
    /// Renders flow accumulation with naturalistic two-layer design (VS_029 Step 3 - NATURALISTIC).
    /// Layer 1: Subtle terrain canvas (earth tones based on elevation)
    /// Layer 2: Bright water network (cyan overlay with alpha based on flow magnitude)
    /// Purpose: Beautiful, intuitive visualization - brighter water = bigger rivers!
    /// </summary>
    /// <remarks>
    /// Design Philosophy (from tmp.md):
    /// - INTUITIVE: Bright cyan water instantly recognizable (no legend needed!)
    /// - LAYERED: Terrain provides context, water provides focus
    /// - NATURAL: Mimics real-world colors (earth tones + water blues)
    /// - CLEAR: High contrast for drainage network, muted background for context
    ///
    /// This is a visual design upgrade from debug heat map → production-quality rendering.
    /// </remarks>
    private void RenderFlowAccumulation(float[,] flowAccumulation, bool[,] oceanMask)
    {
        int h = flowAccumulation.GetLength(0);
        int w = flowAccumulation.GetLength(1);
        var image = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);  // RGBA for alpha blending!

        // Get elevation data for terrain layer (need both heightmap and ocean mask)
        float[,]? heightmap = _worldData?.Phase1Erosion?.FilledHeightmap ?? _worldData?.PostProcessedHeightmap ?? _worldData?.Heightmap;
        if (heightmap == null)
        {
            _logger?.LogWarning("Cannot render naturalistic flow accumulation: No heightmap available");
            return;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // STEP 1: Calculate statistics for normalization
        // ═══════════════════════════════════════════════════════════════════════

        float minFlow = float.MaxValue, maxFlow = float.MinValue;
        float minElev = float.MaxValue, maxElev = float.MinValue;
        double sumFlow = 0;
        int count = 0;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (oceanMask[y, x]) continue;  // Land cells only

                float flow = flowAccumulation[y, x];
                float elev = heightmap[y, x];

                if (flow < minFlow) minFlow = flow;
                if (flow > maxFlow) maxFlow = flow;
                if (elev < minElev) minElev = elev;
                if (elev > maxElev) maxElev = elev;
                sumFlow += flow;
                count++;
            }
        }

        float meanFlow = (float)(sumFlow / count);
        float deltaFlow = Math.Max(1e-6f, maxFlow - minFlow);
        float deltaElev = Math.Max(1e-6f, maxElev - minElev);

        // Calculate 95th percentile for statistics
        var sortedFlow = new System.Collections.Generic.List<float>();
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (!oceanMask[y, x])
                    sortedFlow.Add(flowAccumulation[y, x]);
            }
        }
        sortedFlow.Sort();
        float p95 = GetPercentileFromSorted(sortedFlow, 0.95f);

        // ═══════════════════════════════════════════════════════════════════════
        // STEP 2: Define color palettes (Layer 1: Terrain, Layer 2: Water)
        // ═══════════════════════════════════════════════════════════════════════

        // Layer 1: Terrain Canvas (muted earth tones - subtle background)
        // NOTE: Lowlands BRIGHTENED to avoid "false lake" effect (was too dark at RGB 47,79,79)
        Color terrainLowlands = new Color(180f/255f, 170f/255f, 150f/255f);  // Sandy Beige (light, clearly land!)
        Color terrainHills = new Color(189f/255f, 183f/255f, 107f/255f);     // Khaki (warm mid-tones)
        Color terrainPeaks = new Color(176f/255f, 196f/255f, 222f/255f);     // Light Steel Blue (cool peaks)

        // Layer 2: Water Overlay (bright cyan with varying alpha - eye-catching rivers!)
        Color waterLowFlow = new Color(0f, 0f, 139f/255f, 0.05f);           // Deep blue, 5% alpha (barely visible)
        Color waterHighFlow = new Color(0f, 191f/255f, 255f/255f, 1.0f);    // Deep Sky Blue, 100% alpha (vivid!)

        // Ocean: Deep prussian blue (desaturated, mysterious depths)
        Color oceanColor = new Color(0f, 49f/255f, 83f/255f);               // Prussian Blue

        // Minimum visible flow threshold (below this, no water overlay drawn)
        float minVisibleFlowThreshold = minFlow + deltaFlow * 0.01f;  // Bottom 1% completely transparent

        // ═══════════════════════════════════════════════════════════════════════
        // STEP 3: Render two-layer composite (Terrain + Water blend)
        // ═══════════════════════════════════════════════════════════════════════

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // OCEAN: Deep blue, no water overlay
                if (oceanMask[y, x])
                {
                    image.SetPixel(x, y, oceanColor);
                    continue;
                }

                // LAND: Two-layer blend

                // --- Layer 1: Get terrain base color from elevation ---
                float elevNorm = (heightmap[y, x] - minElev) / deltaElev;
                Color terrainColor = GetTerrainColor(elevNorm, terrainLowlands, terrainHills, terrainPeaks);

                // --- Layer 2: Get water overlay color from flow (LOG SCALED) ---
                float flow = flowAccumulation[y, x];

                // Below threshold? Show pure terrain (no water overlay)
                if (flow < minVisibleFlowThreshold)
                {
                    image.SetPixel(x, y, terrainColor);
                    continue;
                }

                // Log scale the flow for better visual distribution
                float logFlowNorm = (float)Math.Log(1 + flow - minFlow) / (float)Math.Log(1 + maxFlow - minFlow);

                // Interpolate water color (brightness + alpha increase together!)
                Color waterColor = waterLowFlow.Lerp(waterHighFlow, logFlowNorm);

                // --- Blend terrain + water using water's alpha ---
                Color finalColor = terrainColor.Lerp(waterColor, waterColor.A);

                image.SetPixel(x, y, finalColor);
            }
        }

        Texture = ImageTexture.CreateFromImage(image);

        float p95ToMeanRatio = meanFlow > 0 ? p95 / meanFlow : 0f;

        _logger?.LogInformation(
            "FLOW ACCUMULATION (NATURALISTIC): min={Min:F4}, max={Max:F4}, mean={Mean:F4}, p95={P95:F4} | p95/mean={Ratio:F1}x | Two-layer earth tones + bright cyan rivers",
            minFlow, maxFlow, meanFlow, p95, p95ToMeanRatio);
    }

    /// <summary>
    /// Gets terrain color from normalized elevation (0-1).
    /// Smooth 3-stop gradient: Lowlands (dark) → Hills (warm) → Peaks (cool).
    /// </summary>
    private Color GetTerrainColor(float elevNorm, Color lowlands, Color hills, Color peaks)
    {
        if (elevNorm < 0.4f)
        {
            // Lowlands to Hills (0.0 - 0.4)
            return Gradient(elevNorm, 0.0f, 0.4f, lowlands, hills);
        }
        else
        {
            // Hills to Peaks (0.4 - 1.0)
            return Gradient(elevNorm, 0.4f, 1.0f, hills, peaks);
        }
    }

    /// <summary>
    /// Renders river sources (VS_029 Step 4).
    /// Grayscale elevation base + Red markers at spawn points.
    /// Purpose: Validate source detection thresholds (expect 5-15 major rivers).
    /// </summary>
    private void RenderRiverSources(float[,] heightmap, System.Collections.Generic.List<(int x, int y)> riverSources)
    {
        int h = heightmap.GetLength(0);
        int w = heightmap.GetLength(1);
        var image = Image.CreateEmpty(w, h, false, Image.Format.Rgb8);

        // Step 1: Render grayscale elevation base (consistent with sinks views)
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

        // Render grayscale
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float t = (heightmap[y, x] - min) / delta;
                image.SetPixel(x, y, new Color(t, t, t));
            }
        }

        // Step 2: Mark river sources with red markers (consistent with sinks views)
        Color sourceMarker = new Color(1f, 0f, 0f);  // Bright red
        foreach (var (x, y) in riverSources)
        {
            if (x >= 0 && x < w && y >= 0 && y < h)
            {
                image.SetPixel(x, y, sourceMarker);
            }
        }

        Texture = ImageTexture.CreateFromImage(image);

        // Calculate density (sources per land area)
        int totalCells = w * h;
        float sourceDensity = (riverSources.Count / (float)totalCells) * 100f;

        _logger?.LogInformation(
            "RIVER SOURCES (CORRECTED): {Count} major rivers (threshold-crossing algorithm)",
            riverSources.Count);
    }

    /// <summary>
    /// Renders erosion hotspots (VS_029 - repurposed from old algorithm).
    /// Colored elevation base + Magenta markers at high-energy zones.
    /// Purpose: Erosion masking for VS_030+ particle erosion (canyon/gorge formation).
    /// </summary>
    private void RenderErosionHotspots(float[,] heightmap, float[,] flowAccumulation, ElevationThresholds thresholds)
    {
        int h = heightmap.GetLength(0);
        int w = heightmap.GetLength(1);
        var image = Image.CreateEmpty(w, h, false, Image.Format.Rgb8);

        // Step 1: Render colored elevation base
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

        // Normalize and calculate quantiles
        var normalizedHeightmap = new float[h, w];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                normalizedHeightmap[y, x] = (heightmap[y, x] - min) / delta;
            }
        }

        float q15 = FindQuantile(normalizedHeightmap, 0.15f);
        float q70 = FindQuantile(normalizedHeightmap, 0.70f);
        float q75 = FindQuantile(normalizedHeightmap, 0.75f);
        float q90 = FindQuantile(normalizedHeightmap, 0.90f);
        float q95 = FindQuantile(normalizedHeightmap, 0.95f);
        float q99 = FindQuantile(normalizedHeightmap, 0.99f);

        // Render colored elevation
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float elevation = normalizedHeightmap[y, x];
                Color color = GetQuantileTerrainColor(elevation, q15, q70, q75, q90, q95, q99);
                image.SetPixel(x, y, color);
            }
        }

        // Step 2: Detect and mark erosion hotspots (high elevation + high flow accumulation)
        // Calculate p95 accumulation threshold for "high flow"
        var sortedAccumulation = new System.Collections.Generic.List<float>(w * h);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                sortedAccumulation.Add(flowAccumulation[y, x]);
            }
        }
        sortedAccumulation.Sort();
        float accumulationP95 = GetPercentileFromSorted(sortedAccumulation, 0.95f);

        // Mark hotspots with magenta markers
        Color hotspotMarker = new Color(1f, 0f, 1f);  // Magenta (high-energy zones)
        int hotspotCount = 0;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // High elevation + high flow = erosion hotspot
                bool isHighElevation = heightmap[y, x] >= thresholds.MountainLevel;
                bool isHighFlow = flowAccumulation[y, x] >= accumulationP95;

                if (isHighElevation && isHighFlow)
                {
                    image.SetPixel(x, y, hotspotMarker);
                    hotspotCount++;
                }
            }
        }

        Texture = ImageTexture.CreateFromImage(image);

        // Calculate density
        int totalCells = w * h;
        float hotspotDensity = (hotspotCount / (float)totalCells) * 100f;

        _logger?.LogInformation(
            "EROSION HOTSPOTS: {Count} detected | High elevation + high flow (p95) = Maximum erosive potential | Density={Density:F3}% (canyon/gorge zones for VS_030+)",
            hotspotCount, hotspotDensity);
    }
}
