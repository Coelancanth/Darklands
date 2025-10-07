using Godot;
using System;
using Darklands.Core.Features.WorldGen.Application.Common;
using Darklands.Core.Features.WorldGen.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace Darklands.Features.WorldGen;

/// <summary>
/// Renders PlateSimulationResult data as a texture.
/// Supports two view modes: RawElevation (grayscale) and Plates (colored).
/// Pure rendering - no UI, no input handling.
/// </summary>
public partial class WorldMapRendererNode : Sprite2D
{
    private ILogger<WorldMapRendererNode>? _logger;
    private PlateSimulationResult? _worldData;
    private MapViewMode _currentViewMode = MapViewMode.ColoredElevation;  // Default to ColoredElevation

    [Signal]
    public delegate void RenderCompleteEventHandler(int width, int height);

    public override void _Ready()
    {
        _logger?.LogDebug("WorldMapRendererNode ready");
    }

    /// <summary>
    /// Sets the world data and renders it using the current view mode.
    /// </summary>
    public void SetWorldData(PlateSimulationResult data, ILogger<WorldMapRendererNode> logger)
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
    public PlateSimulationResult? GetWorldData() => _worldData;

    private void RenderCurrentView()
    {
        if (_worldData == null) return;

        switch (_currentViewMode)
        {
            case MapViewMode.RawElevation:
                RenderRawElevation(_worldData);
                break;
            case MapViewMode.Plates:
                RenderPlates(_worldData);
                break;
            case MapViewMode.ColoredElevation:
                RenderColoredElevation(_worldData);
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
    private void RenderRawElevation(PlateSimulationResult data)
    {
        int h = data.Height;
        int w = data.Width;
        var image = Image.CreateEmpty(w, h, false, Image.Format.Rgb8);

        // Find min/max for normalization
        float min = float.MaxValue, max = float.MinValue;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float v = data.Heightmap[y, x];
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
                float t = (data.Heightmap[y, x] - min) / delta;
                image.SetPixel(x, y, new Color(t, t, t));
            }
        }

        Texture = ImageTexture.CreateFromImage(image);
        _logger?.LogInformation("Rendered RawElevation: {Width}x{Height}", w, h);
    }

    /// <summary>
    /// Renders plate ownership with unique color per plate.
    /// </summary>
    private void RenderPlates(PlateSimulationResult data)
    {
        int h = data.Height;
        int w = data.Width;
        var image = Image.CreateEmpty(w, h, false, Image.Format.Rgb8);

        // Generate deterministic colors for plates (seed=42 for consistency)
        var rng = new Random(42);
        var plateColors = new System.Collections.Generic.Dictionary<uint, Color>();

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                uint plateId = data.PlatesMap[y, x];

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
    /// </summary>
    private void RenderColoredElevation(PlateSimulationResult data)
    {
        int h = data.Height;
        int w = data.Width;
        var image = Image.CreateEmpty(w, h, false, Image.Format.Rgb8);

        // Step 1: Normalize heightmap to [0, 1] range (reference implementation expects this!)
        float min = float.MaxValue, max = float.MinValue;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float v = data.Heightmap[y, x];
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
                normalizedHeightmap[y, x] = (data.Heightmap[y, x] - min) / delta;
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
    /// Linear interpolation between two colors based on value range.
    /// </summary>
    private Color Gradient(float value, float min, float max, Color colorA, Color colorB)
    {
        float delta = max - min;
        if (delta < 0.00001f) return colorA;

        float t = Mathf.Clamp((value - min) / delta, 0f, 1f);
        return colorA.Lerp(colorB, t);
    }
}
