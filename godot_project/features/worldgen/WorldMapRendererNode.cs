using Godot;
using System;
using System.Linq;
using Darklands.Core.Features.WorldGen.Application.Common;
using Darklands.Core.Features.WorldGen.Application.DTOs;
using Darklands.Features.WorldGen.ColorSchemes;
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

    /// <summary>
    /// [TD_025] Maps view modes to color schemes for new rendering pattern.
    /// Returns null for view modes that haven't been migrated yet (uses legacy rendering).
    /// </summary>
    private IColorScheme? GetSchemeForViewMode(MapViewMode mode)
    {
        return mode switch
        {
            // Elevation schemes (shared scheme for both original and post-processed)
            MapViewMode.ColoredOriginalElevation => ColorSchemes.ColorSchemes.Elevation,
            MapViewMode.ColoredPostProcessedElevation => ColorSchemes.ColorSchemes.Elevation,

            // Grayscale schemes
            MapViewMode.RawElevation => ColorSchemes.ColorSchemes.Grayscale,

            // Temperature schemes (all 4 temperature view modes use same scheme)
            MapViewMode.TemperatureLatitudeOnly => ColorSchemes.ColorSchemes.Temperature,
            MapViewMode.TemperatureWithNoise => ColorSchemes.ColorSchemes.Temperature,
            MapViewMode.TemperatureWithDistance => ColorSchemes.ColorSchemes.Temperature,
            MapViewMode.TemperatureFinal => ColorSchemes.ColorSchemes.Temperature,

            // Precipitation schemes (all 5 precipitation view modes use same scheme)
            MapViewMode.PrecipitationNoiseOnly => ColorSchemes.ColorSchemes.Precipitation,
            MapViewMode.PrecipitationTemperatureShaped => ColorSchemes.ColorSchemes.Precipitation,
            MapViewMode.PrecipitationBase => ColorSchemes.ColorSchemes.Precipitation,
            MapViewMode.PrecipitationWithRainShadow => ColorSchemes.ColorSchemes.Precipitation,
            MapViewMode.PrecipitationFinal => ColorSchemes.ColorSchemes.Precipitation,

            // Flow schemes
            MapViewMode.FlowDirections => ColorSchemes.ColorSchemes.FlowDirections,
            MapViewMode.FlowAccumulation => ColorSchemes.ColorSchemes.FlowAccumulation,

            // Marker-based schemes (sinks, river sources, hotspots)
            MapViewMode.SinksPreFilling => ColorSchemes.ColorSchemes.Sinks,
            MapViewMode.SinksPostFilling => ColorSchemes.ColorSchemes.Sinks,
            MapViewMode.RiverSources => ColorSchemes.ColorSchemes.RiverSources,
            MapViewMode.ErosionHotspots => ColorSchemes.ColorSchemes.Hotspots,

            // Legacy view modes (no scheme - use old rendering)
            MapViewMode.Plates => null,  // Custom rendering (random plate colors)
            MapViewMode.PreservedLakes => null,  // Complex basin visualization

            _ => null  // Unknown mode - fallback to legacy
        };
    }

    private void RenderCurrentView()
    {
        if (_worldData == null) return;

        // [TD_025] NEW PATTERN: Try scheme-based rendering first
        var scheme = GetSchemeForViewMode(_currentViewMode);
        if (scheme != null)
        {
            var renderedImage = scheme.Render(_worldData, _currentViewMode);
            if (renderedImage != null)
            {
                // Scheme implemented new Render() pattern - use it!
                Texture = ImageTexture.CreateFromImage(renderedImage);
                _logger?.LogInformation("Rendered {ViewMode} via scheme: \"{SchemeName}\"",
                    _currentViewMode, scheme.Name);
                return;
            }
            // Scheme returned null - fall through to legacy rendering
        }

        // [TD_025] LEGACY PATTERN: Only non-migrated view modes remain
        switch (_currentViewMode)
        {
            case MapViewMode.Plates:
                RenderPlates(_worldData.PlatesMap);
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

            default:
                _logger?.LogError("Unknown view mode: {ViewMode}", _currentViewMode);
                break;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Legacy Rendering Methods (Non-Migrated View Modes)
    // ═══════════════════════════════════════════════════════════════════════

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
}
