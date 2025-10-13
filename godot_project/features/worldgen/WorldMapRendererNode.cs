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
                RenderColoredElevation(_worldData.Heightmap);  // Original raw [0-20]
                break;

            case MapViewMode.ColoredPostProcessedElevation:
                if (_worldData.PostProcessedHeightmap != null)
                {
                    RenderColoredElevation(_worldData.PostProcessedHeightmap);  // Post-processed raw [0.1-20]
                }
                else
                {
                    _logger?.LogWarning("Post-processed heightmap not available, falling back to original");
                    RenderColoredElevation(_worldData.Heightmap);
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
    /// </summary>
    private void RenderColoredElevation(float[,] heightmap)
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

        // Step 2: Calculate quantiles on NORMALIZED data
        float q15 = FindQuantile(normalizedHeightmap, 0.15f);
        float q70 = FindQuantile(normalizedHeightmap, 0.70f);
        float q75 = FindQuantile(normalizedHeightmap, 0.75f);
        float q90 = FindQuantile(normalizedHeightmap, 0.90f);
        float q95 = FindQuantile(normalizedHeightmap, 0.95f);
        float q99 = FindQuantile(normalizedHeightmap, 0.99f);

        _logger?.LogDebug("ColoredElevation quantiles: q15={Q15:F3} q70={Q70:F3} q75={Q75:F3} q90={Q90:F3} q95={Q95:F3} q99={Q99:F3}",
            q15, q70, q75, q90, q95, q99);

        // Step 3: Render with quantile-based terrain colors using normalized values
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float elevation = normalizedHeightmap[y, x];  // Use normalized [0,1] value
                Color color = GetQuantileTerrainColor(elevation, q15, q70, q75, q90, q95, q99);
                image.SetPixel(x, y, color);
            }
        }

        Texture = ImageTexture.CreateFromImage(image);
        _logger?.LogInformation("Rendered ColoredElevation: {Width}x{Height}", w, h);
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
    /// Maps elevation to terrain color using quantile thresholds.
    /// Matches plate-tectonics reference color palette exactly.
    /// </summary>
    private Color GetQuantileTerrainColor(float h, float q15, float q70, float q75, float q90, float q95, float q99)
    {
        // Reference color palette (RGB 0-255 converted to 0-1):
        // Deep ocean: (0,0,255) → (0,20,200)
        // Ocean: (0,20,200) → (50,80,225)
        // Shallow water: (50,80,225) → (135,237,235)
        // Land/grass: (88,173,49) → (218,226,58)
        // Hills: (218,226,58) → (251,252,42)
        // Mountains: (251,252,42) → (91,28,13)
        // Peaks: (91,28,13) → (51,0,4)

        if (h < q15)
            return Gradient(h, 0.0f, q15,
                new Color(0f, 0f, 1f),
                new Color(0f, 0.078f, 0.784f));

        if (h < q70)
            return Gradient(h, q15, q70,
                new Color(0f, 0.078f, 0.784f),
                new Color(0.196f, 0.314f, 0.882f));

        if (h < q75)
            return Gradient(h, q70, q75,
                new Color(0.196f, 0.314f, 0.882f),
                new Color(0.529f, 0.929f, 0.922f));

        if (h < q90)
            return Gradient(h, q75, q90,
                new Color(0.345f, 0.678f, 0.192f),
                new Color(0.855f, 0.886f, 0.227f));

        if (h < q95)
            return Gradient(h, q90, q95,
                new Color(0.855f, 0.886f, 0.227f),
                new Color(0.984f, 0.988f, 0.165f));

        if (h < q99)
            return Gradient(h, q95, q99,
                new Color(0.984f, 0.988f, 0.165f),
                new Color(0.357f, 0.110f, 0.051f));

        return Gradient(h, q99, 1.0f,
            new Color(0.357f, 0.110f, 0.051f),
            new Color(0.200f, 0f, 0.016f));
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
