using System;
using Godot;
using Darklands.Core.Features.WorldGen.Application.Common;
using Darklands.Core.Features.WorldGen.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace Darklands.Features.WorldGen;

/// <summary>
/// Handles mouse input for probing world data at cursor position.
/// Emits probe events with cell coordinates and data values.
/// Pure input handling - no rendering, no UI.
/// </summary>
public partial class WorldMapProbeNode : Node
{
    private ILogger<WorldMapProbeNode>? _logger;
    private WorldMapRendererNode? _renderer;
    private WorldMapCameraNode? _cameraController;
    private bool _isProbingEnabled = true;

    // Highlight overlay
    private ColorRect? _highlightRect;
    private const float HIGHLIGHT_ALPHA = 0.4f;

    // View-mode-specific highlight colors (chosen for contrast)
    private static readonly Color HIGHLIGHT_COLOR_COLORED = new Color(1, 0, 0, HIGHLIGHT_ALPHA);     // Pure red (contrasts with all terrain colors)
    private static readonly Color HIGHLIGHT_COLOR_RAW = new Color(1, 0, 1, HIGHLIGHT_ALPHA);         // Magenta (contrasts with grayscale)
    private static readonly Color HIGHLIGHT_COLOR_PLATES = new Color(1, 1, 1, HIGHLIGHT_ALPHA * 1.5f); // White (contrasts with all plate colors)

    [Signal]
    public delegate void CellProbedEventHandler(int x, int y, string probeData);

    public override void _Ready()
    {
        // Create highlight overlay (initially hidden)
        _highlightRect = new ColorRect
        {
            Color = HIGHLIGHT_COLOR_COLORED, // Default to ColoredElevation highlight
            Visible = false,
            ZIndex = 100 // Render on top
        };

        _logger?.LogDebug("WorldMapProbeNode ready");
    }

    /// <summary>
    /// Links this probe to a renderer node and sets up highlight overlay.
    /// </summary>
    public void SetRenderer(WorldMapRendererNode renderer, ILogger<WorldMapProbeNode> logger)
    {
        _renderer = renderer;
        _logger = logger;

        // Add highlight as child of renderer (world space)
        if (_highlightRect != null && _renderer != null)
        {
            _renderer.AddChild(_highlightRect);
        }
    }

    /// <summary>
    /// Links this probe to the camera controller to detect pan mode.
    /// </summary>
    public void SetCameraController(WorldMapCameraNode cameraController)
    {
        _cameraController = cameraController;
    }

    /// <summary>
    /// Enables or disables probing.
    /// </summary>
    public void SetProbingEnabled(bool enabled)
    {
        _isProbingEnabled = enabled;
    }

    /// <summary>
    /// Updates highlight color based on current view mode for better contrast.
    /// Should be called when view mode changes.
    /// </summary>
    public void UpdateHighlightColor(MapViewMode viewMode)
    {
        if (_highlightRect == null) return;

        _highlightRect.Color = viewMode switch
        {
            // VS_024: Both colored elevation modes use same highlight color
            MapViewMode.ColoredOriginalElevation => HIGHLIGHT_COLOR_COLORED,
            MapViewMode.ColoredPostProcessedElevation => HIGHLIGHT_COLOR_COLORED,
            MapViewMode.RawElevation => HIGHLIGHT_COLOR_RAW,         // Magenta on grayscale
            MapViewMode.Plates => HIGHLIGHT_COLOR_PLATES,            // White on random colors

            // VS_025: Temperature modes use red highlight (contrasts with blue-green-yellow gradient)
            MapViewMode.TemperatureLatitudeOnly => HIGHLIGHT_COLOR_COLORED,
            MapViewMode.TemperatureWithNoise => HIGHLIGHT_COLOR_COLORED,
            MapViewMode.TemperatureWithDistance => HIGHLIGHT_COLOR_COLORED,
            MapViewMode.TemperatureFinal => HIGHLIGHT_COLOR_COLORED,

            // VS_026: Precipitation modes use red highlight (contrasts with brown-yellow-blue gradient)
            MapViewMode.PrecipitationNoiseOnly => HIGHLIGHT_COLOR_COLORED,
            MapViewMode.PrecipitationTemperatureShaped => HIGHLIGHT_COLOR_COLORED,
            MapViewMode.PrecipitationFinal => HIGHLIGHT_COLOR_COLORED,
            MapViewMode.PrecipitationWithRainShadow => HIGHLIGHT_COLOR_COLORED,

            _ => HIGHLIGHT_COLOR_COLORED
        };

        _logger?.LogDebug("Highlight color updated for view mode: {ViewMode}", viewMode);
    }

    public override void _Input(InputEvent @event)
    {
        if (!_isProbingEnabled || _renderer == null) return;

        // Check if camera is in pan mode
        bool isPanModeActive = _cameraController?.IsPanning ?? false;

        // Update highlight on mouse motion (but not while panning)
        if (@event is InputEventMouseMotion && !isPanModeActive)
        {
            UpdateHighlight();
        }

        // Probe on key press (default: Q key, remappable via InputMap)
        if (@event.IsActionPressed("probe_cell") && !isPanModeActive)
        {
            ProbeAtMousePosition();
            GetViewport().SetInputAsHandled();
        }
    }

    private void ProbeAtMousePosition()
    {
        var worldData = _renderer?.GetWorldData();
        if (worldData == null || _renderer == null)
        {
            _logger?.LogWarning("Cannot probe: No world data loaded");
            return;
        }

        var texture = _renderer!.Texture;
        if (texture == null)
        {
            _logger?.LogWarning("Cannot probe: No texture loaded");
            return;
        }

        // Get mouse position in world space (accounts for camera zoom/pan)
        var mouseWorldPos = _renderer!.GetGlobalMousePosition();

        // Convert to sprite local coordinates (relative to sprite center)
        var spriteTransform = _renderer.GetGlobalTransform().AffineInverse();
        var localPos = spriteTransform * mouseWorldPos;

        // Convert to texture pixel coordinates
        // Sprite2D is centered by default, so (0,0) is texture center
        var textureSize = texture.GetSize();
        int x = (int)(localPos.X + textureSize.X / 2);
        int y = (int)(localPos.Y + textureSize.Y / 2);

        // Bounds check
        if (x < 0 || x >= worldData.Width || y < 0 || y >= worldData.Height)
        {
            _logger?.LogDebug("Probe out of bounds: mouseWorld=({MX:F1},{MY:F1}), local=({LX:F1},{LY:F1}), cell=({X},{Y}), bounds=[0-{W},0-{H}]",
                mouseWorldPos.X, mouseWorldPos.Y, localPos.X, localPos.Y, x, y, worldData.Width - 1, worldData.Height - 1);
            return;
        }

        // Get data at cell (VS_024: Dual elevation + ocean data + thresholds)
        float originalElevation = worldData.Heightmap[y, x];
        float? postProcessedElevation = worldData.PostProcessedHeightmap?[y, x];
        bool? isOcean = worldData.OceanMask?[y, x];
        float? seaDepth = worldData.SeaDepth?[y, x];
        uint plateId = worldData.PlatesMap[y, x];
        var thresholds = worldData.Thresholds;

        // Build probe string based on current view mode
        var viewMode = _renderer.GetCurrentViewMode();
        string probeData = viewMode switch
        {
            MapViewMode.RawElevation =>
                $"Cell ({x},{y})\nRaw: {originalElevation:F3}",

            MapViewMode.Plates =>
                $"Cell ({x},{y})\nPlate ID: {plateId}\nRaw: {originalElevation:F3}",

            // VS_024: Dual-heightmap elevation views with real-world meters mapping
            MapViewMode.ColoredOriginalElevation =>
                BuildElevationProbeData(x, y, originalElevation, postProcessedElevation, isOcean, seaDepth, thresholds),

            MapViewMode.ColoredPostProcessedElevation =>
                BuildElevationProbeData(x, y, originalElevation, postProcessedElevation, isOcean, seaDepth, thresholds),

            // VS_025: Temperature view modes - show all 4 stages + per-world params
            MapViewMode.TemperatureLatitudeOnly =>
                BuildTemperatureProbeData(x, y, worldData, debugStage: 1),

            MapViewMode.TemperatureWithNoise =>
                BuildTemperatureProbeData(x, y, worldData, debugStage: 2),

            MapViewMode.TemperatureWithDistance =>
                BuildTemperatureProbeData(x, y, worldData, debugStage: 3),

            MapViewMode.TemperatureFinal =>
                BuildTemperatureProbeData(x, y, worldData, debugStage: 4),

            // VS_026: Precipitation view modes - show all 3 stages + physics debug
            MapViewMode.PrecipitationNoiseOnly =>
                BuildPrecipitationProbeData(x, y, worldData, debugStage: 1),

            MapViewMode.PrecipitationTemperatureShaped =>
                BuildPrecipitationProbeData(x, y, worldData, debugStage: 2),

            MapViewMode.PrecipitationFinal =>
                BuildPrecipitationProbeData(x, y, worldData, debugStage: 3),

            // VS_027: Rain shadow mode - show stage 4 with wind direction + blocking info
            MapViewMode.PrecipitationWithRainShadow =>
                BuildRainShadowProbeData(x, y, worldData),

            _ => $"Cell ({x},{y})\nUnknown view"
        };

        // Log probe result with all relevant data
        _logger?.LogInformation("Probed cell ({X},{Y}): Original={Original:F3}, PostProcessed={PostProcessed:F3}, Ocean={Ocean}, PlateId={PlateId}",
            x, y, originalElevation, postProcessedElevation, isOcean, plateId);

        EmitSignal(SignalName.CellProbed, x, y, probeData);
    }

    /// <summary>
    /// Builds comprehensive elevation probe data with real-world meters mapping.
    /// VS_024: Uses ElevationMapper for human-readable display, shows raw values for debugging.
    /// </summary>
    private string BuildElevationProbeData(
        int x, int y,
        float original,
        float? postProcessed,
        bool? isOcean,
        float? seaDepth,
        Core.Features.WorldGen.Application.DTOs.ElevationThresholds? thresholds)
    {
        // Get the current elevation value to display (prefer post-processed if available)
        float currentElevation = postProcessed ?? original;

        var worldData = _renderer?.GetWorldData();
        var data = $"Cell ({x},{y})\n";

        // Show human-readable meters (if thresholds AND min/max available for mapping)
        if (thresholds != null && worldData != null)
        {
            string metersDisplay = ElevationMapper.FormatElevationWithTerrain(
                rawElevation: currentElevation,
                seaLevelThreshold: thresholds.SeaLevel,
                minElevation: worldData.MinElevation,     // ← FIX: Use actual min from heightmap
                maxElevation: worldData.MaxElevation,     // ← FIX: Use actual max from heightmap
                hillThreshold: thresholds.HillLevel,
                mountainThreshold: thresholds.MountainLevel,
                peakThreshold: thresholds.PeakLevel);
            data += metersDisplay;
            data += $"\n\nRaw: {original:F2}";
        }
        else
        {
            // Fallback when data unavailable (cached world or old format)
            data += $"Elevation: {original:F2}";
            data += $"\n(Regenerate world for meters)";
        }

        // Show post-processed comparison if available
        if (postProcessed.HasValue)
            data += $"\nPost-Proc: {postProcessed.Value:F2}";

        // Show ocean/depth info
        if (isOcean == true && seaDepth.HasValue && seaDepth.Value > 0)
            data += $"\nDepth: {seaDepth.Value:F2}";

        return data;
    }

    /// <summary>
    /// Updates the highlight overlay to show the hovered cell.
    /// </summary>
    private void UpdateHighlight()
    {
        if (_renderer == null || _highlightRect == null) return;

        var worldData = _renderer.GetWorldData();
        if (worldData == null) return;

        var texture = _renderer.Texture;
        if (texture == null) return;

        // Get mouse position and convert to cell coordinates
        var mouseWorldPos = _renderer.GetGlobalMousePosition();
        var spriteTransform = _renderer.GetGlobalTransform().AffineInverse();
        var localPos = spriteTransform * mouseWorldPos;

        var textureSize = texture.GetSize();
        int cellX = (int)(localPos.X + textureSize.X / 2);
        int cellY = (int)(localPos.Y + textureSize.Y / 2);

        // Show/hide highlight based on bounds
        if (cellX >= 0 && cellX < worldData.Width && cellY >= 0 && cellY < worldData.Height)
        {
            // Convert cell coordinates back to sprite local position
            // Cell (0,0) is at sprite position (-textureSize/2, -textureSize/2)
            float highlightX = cellX - textureSize.X / 2;
            float highlightY = cellY - textureSize.Y / 2;

            _highlightRect.Position = new Vector2(highlightX, highlightY);
            _highlightRect.Size = new Vector2(1, 1); // 1x1 pixel cell
            _highlightRect.Visible = true;
        }
        else
        {
            _highlightRect.Visible = false;
        }
    }

    /// <summary>
    /// Builds comprehensive temperature probe data with all 4 stages + per-world parameters.
    /// VS_025: Shows progression through algorithm stages for debugging.
    /// </summary>
    /// <param name="x">Cell X coordinate</param>
    /// <param name="y">Cell Y coordinate</param>
    /// <param name="worldData">World generation result containing temperature maps</param>
    /// <param name="debugStage">Current debug stage (1=LatitudeOnly, 2=WithNoise, 3=WithDistance, 4=Final)</param>
    private string BuildTemperatureProbeData(
        int x, int y,
        Core.Features.WorldGen.Application.DTOs.WorldGenerationResult worldData,
        int debugStage)
    {
        var data = $"Cell ({x},{y})\n";

        // Get all 4 temperature values at this cell
        float? latitudeOnly = worldData.TemperatureLatitudeOnly?[y, x];
        float? withNoise = worldData.TemperatureWithNoise?[y, x];
        float? withDistance = worldData.TemperatureWithDistance?[y, x];
        float? final = worldData.TemperatureFinal?[y, x];

        // Per-world parameters
        float? axialTilt = worldData.AxialTilt;
        float? distanceToSun = worldData.DistanceToSun;

        // Show current stage prominently with °C conversion
        data += debugStage switch
        {
            1 => $"Stage 1: Latitude Only\n{TemperatureMapper.FormatTemperature(latitudeOnly ?? 0f)}\n",
            2 => $"Stage 2: + Noise\n{TemperatureMapper.FormatTemperature(withNoise ?? 0f)}\n",
            3 => $"Stage 3: + Distance\n{TemperatureMapper.FormatTemperature(withDistance ?? 0f)}\n",
            4 => $"Stage 4: Final\n{TemperatureMapper.FormatTemperature(final ?? 0f)}\n",
            _ => "Unknown Stage\n"
        };

        data += "\n--- Debug: All Stages ---\n";

        // Show all 4 stages for comparison (normalized [0,1] values)
        if (latitudeOnly.HasValue)
            data += $"1. Latitude: {latitudeOnly.Value:F3}\n";

        if (withNoise.HasValue)
            data += $"2. + Noise: {withNoise.Value:F3}\n";

        if (withDistance.HasValue)
            data += $"3. + Distance: {withDistance.Value:F3}\n";

        if (final.HasValue)
            data += $"4. Final: {final.Value:F3}\n";

        // Show per-world parameters (explain hot/cold planets, tilt shifts)
        data += "\n--- World Parameters ---\n";

        if (axialTilt.HasValue)
            data += $"Axial Tilt: {axialTilt.Value:F3}\n";

        if (distanceToSun.HasValue)
            data += $"Distance to Sun: {distanceToSun.Value:F3}×\n";

        return data;
    }

    /// <summary>
    /// Builds comprehensive precipitation probe data with all 3 stages + physics debug.
    /// VS_026: Shows progression through algorithm stages for debugging.
    /// </summary>
    /// <param name="x">Cell X coordinate</param>
    /// <param name="y">Cell Y coordinate</param>
    /// <param name="worldData">World generation result containing precipitation maps</param>
    /// <param name="debugStage">Current debug stage (1=NoiseOnly, 2=TemperatureShaped, 3=Final)</param>
    private string BuildPrecipitationProbeData(
        int x, int y,
        Core.Features.WorldGen.Application.DTOs.WorldGenerationResult worldData,
        int debugStage)
    {
        var data = $"Cell ({x},{y})\n";

        // Get all 3 precipitation values at this cell
        float? noiseOnly = worldData.BaseNoisePrecipitationMap?[y, x];
        float? tempShaped = worldData.TemperatureShapedPrecipitationMap?[y, x];
        float? final = worldData.FinalPrecipitationMap?[y, x];

        // Get temperature at this cell for gamma curve calculation
        float? temperature = worldData.TemperatureFinal?[y, x];

        // Get quantile thresholds for classification
        var thresholds = worldData.PrecipitationThresholds;

        // Show current stage prominently
        data += debugStage switch
        {
            1 => $"Stage 1: Base Noise\n{noiseOnly ?? 0f:F3}\n",
            2 => $"Stage 2: + Temp Curve\n{tempShaped ?? 0f:F3}\n",
            3 => $"Stage 3: Final\n{FormatPrecipitation(final ?? 0f, thresholds)}\n",
            _ => "Unknown Stage\n"
        };

        data += "\n--- Debug: All Stages ---\n";

        // Show all 3 stages for comparison (normalized [0,1] values)
        if (noiseOnly.HasValue)
            data += $"1. Noise: {noiseOnly.Value:F3}\n";

        if (tempShaped.HasValue)
            data += $"2. Temp Shaped: {tempShaped.Value:F3}\n";

        if (final.HasValue)
            data += $"3. Final: {final.Value:F3}\n";

        // Show physics debug info (gamma curve calculation)
        if (temperature.HasValue && noiseOnly.HasValue)
        {
            data += "\n--- Physics Debug ---\n";
            data += $"Temperature: {temperature.Value:F3}\n";

            // Calculate gamma curve value (same formula as PrecipitationCalculator)
            const float gamma = 2.0f;
            const float curveBonus = 0.2f;
            float curve = MathF.Pow(temperature.Value, gamma) * (1.0f - curveBonus) + curveBonus;

            data += $"Gamma Curve: {curve:F3}\n";
            data += $"(cold=0.2, hot=1.0)\n";
        }

        // Show classification based on thresholds
        if (final.HasValue && thresholds != null)
        {
            string classification;
            if (final.Value < thresholds.LowThreshold)
                classification = "Arid";
            else if (final.Value < thresholds.MediumThreshold)
                classification = "Low";
            else if (final.Value < thresholds.HighThreshold)
                classification = "Medium";
            else
                classification = "High";

            data += $"\nClassification: {classification}\n";
        }

        return data;
    }

    /// <summary>
    /// Formats precipitation value with classification label and mm/year estimate.
    /// </summary>
    private string FormatPrecipitation(float precipNormalized, Core.Features.WorldGen.Application.DTOs.PrecipitationThresholds? thresholds)
    {
        // Classification based on quantile thresholds
        string classification;
        string mmPerYear;

        if (thresholds == null)
        {
            return $"{precipNormalized:F3} (no thresholds)";
        }

        if (precipNormalized < thresholds.LowThreshold)
        {
            classification = "Arid";
            mmPerYear = "<200mm/year";
        }
        else if (precipNormalized < thresholds.MediumThreshold)
        {
            classification = "Low";
            mmPerYear = "200-400mm/year";
        }
        else if (precipNormalized < thresholds.HighThreshold)
        {
            classification = "Medium";
            mmPerYear = "400-800mm/year";
        }
        else
        {
            classification = "High";
            mmPerYear = ">800mm/year";
        }

        return $"{precipNormalized:F3}\n{classification}\n{mmPerYear}";
    }

    /// <summary>
    /// Builds rain shadow probe data (VS_027 Stage 4).
    /// Shows: base precipitation, rain shadow reduction, latitude-based wind direction.
    /// </summary>
    private string BuildRainShadowProbeData(
        int x,
        int y,
        Core.Features.WorldGen.Application.DTOs.WorldGenerationResult worldData)
    {
        float? basePrecip = worldData.FinalPrecipitationMap?[y, x];
        float? rainShadowPrecip = worldData.WithRainShadowPrecipitationMap?[y, x];
        var thresholds = worldData.PrecipitationThresholds;

        // Calculate latitude for wind direction
        float normalizedLatitude = worldData.Height > 1 ? (float)y / (worldData.Height - 1) : 0.5f;
        var (windX, windY) = Core.Features.WorldGen.Infrastructure.Algorithms.PrevailingWinds.GetWindDirection(normalizedLatitude);

        // Get wind band name
        string windBand = Core.Features.WorldGen.Infrastructure.Algorithms.PrevailingWinds.GetWindBandName(normalizedLatitude);
        string windDirection = windX < 0 ? "← Westward" : "→ Eastward";

        // Calculate reduction percentage
        float reductionPercent = 0f;
        if (basePrecip.HasValue && rainShadowPrecip.HasValue && basePrecip.Value > 0)
        {
            reductionPercent = ((basePrecip.Value - rainShadowPrecip.Value) / basePrecip.Value) * 100f;
        }

        string header = $"Cell ({x},{y})\nStage 4: + Rain Shadow\n\n";
        string wind = $"Wind: {windDirection} ({windBand})\n\n";
        string precip = $"Base:\n{FormatPrecipitation(basePrecip ?? 0f, thresholds)}\n\n";
        string shadow = $"Rain Shadow:\n{FormatPrecipitation(rainShadowPrecip ?? 0f, thresholds)}\n\n";
        string reduction = reductionPercent > 0.1f
            ? $"Blocking: -{reductionPercent:F1}% (leeward)\n"
            : $"Blocking: None (windward/flat)\n";

        return header + wind + precip + shadow + reduction;
    }
}
