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
    private MapViewMode _currentViewMode = MapViewMode.RawElevation;

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

        // Generate deterministic colors for plates (seed=12345 for consistency)
        var rng = new Random(12345);
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
}
