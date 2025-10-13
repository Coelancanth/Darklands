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
            MapViewMode.PrecipitationBase => HIGHLIGHT_COLOR_COLORED,
            MapViewMode.PrecipitationWithRainShadow => HIGHLIGHT_COLOR_COLORED,
            MapViewMode.PrecipitationFinal => HIGHLIGHT_COLOR_COLORED,

            // VS_029: Erosion debug modes - use magenta on grayscale, red on colored
            MapViewMode.SinksPreFilling => HIGHLIGHT_COLOR_RAW,         // Magenta on grayscale
            MapViewMode.SinksPostFilling => HIGHLIGHT_COLOR_RAW,        // Magenta on grayscale
            MapViewMode.FlowDirections => HIGHLIGHT_COLOR_COLORED,      // Red on 8-color gradient
            MapViewMode.FlowAccumulation => HIGHLIGHT_COLOR_COLORED,    // Red on heat map
            MapViewMode.RiverSources => HIGHLIGHT_COLOR_RAW,            // Magenta on grayscale
            MapViewMode.ErosionHotspots => HIGHLIGHT_COLOR_COLORED,     // Red on colored elevation

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

            MapViewMode.PrecipitationBase =>
                BuildPrecipitationProbeData(x, y, worldData, debugStage: 3),

            // VS_027: Rain shadow mode - show stage 4 with wind direction + blocking info
            MapViewMode.PrecipitationWithRainShadow =>
                BuildRainShadowProbeData(x, y, worldData),

            // VS_028: Coastal moisture mode - show stage 5 with distance + bonus info
            MapViewMode.PrecipitationFinal =>
                BuildCoastalMoistureProbeData(x, y, worldData),

            // VS_029: Erosion debug modes - D-8 flow visualization
            MapViewMode.SinksPreFilling =>
                BuildSinksPreFillingProbeData(x, y, worldData),

            MapViewMode.SinksPostFilling =>
                BuildSinksPostFillingProbeData(x, y, worldData),

            MapViewMode.FlowDirections =>
                BuildFlowDirectionsProbeData(x, y, worldData),

            MapViewMode.FlowAccumulation =>
                BuildFlowAccumulationProbeData(x, y, worldData),

            MapViewMode.RiverSources =>
                BuildRiverSourcesProbeData(x, y, worldData),

            MapViewMode.ErosionHotspots =>
                BuildErosionHotspotsProbeData(x, y, worldData),

            _ => $"Cell ({x},{y})\nUnknown view"
        };

        // Log probe result - output the SAME formatted data to console for easy debugging
        // This makes the Godot Output panel show exactly what the UI panel displays
        _logger?.LogInformation("[WorldGen] PROBE ({ViewMode}):\n{ProbeData}",
            viewMode, probeData);

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

    /// <summary>
    /// Builds coastal moisture probe data (VS_028 Stage 5).
    /// Shows: rain shadow input, final precipitation, distance-to-ocean, coastal bonus %, elevation resistance.
    /// </summary>
    private string BuildCoastalMoistureProbeData(
        int x,
        int y,
        Core.Features.WorldGen.Application.DTOs.WorldGenerationResult worldData)
    {
        float? rainShadowPrecip = worldData.WithRainShadowPrecipitationMap?[y, x];
        float? finalPrecip = worldData.PrecipitationFinal?[y, x];
        var thresholds = worldData.PrecipitationThresholds;
        bool? isOcean = worldData.OceanMask?[y, x];
        float? elevation = worldData.PostProcessedHeightmap?[y, x];

        string header = $"Cell ({x},{y})\nStage 5: FINAL (+ Coastal)\n\n";

        // Ocean cells have no coastal enhancement
        if (isOcean == true)
        {
            string oceanNote = "Ocean Cell:\n(No coastal enhancement)\n\n";
            string precip = $"Precipitation:\n{FormatPrecipitation(finalPrecip ?? 0f, thresholds)}\n";
            return header + oceanNote + precip;
        }

        // Calculate distance-to-ocean (estimate based on BFS - we don't store it in WorldGenerationResult)
        // For probe display, show relative enhancement instead
        float enhancement = 0f;
        if (rainShadowPrecip.HasValue && finalPrecip.HasValue && rainShadowPrecip.Value > 0)
        {
            enhancement = ((finalPrecip.Value - rainShadowPrecip.Value) / rainShadowPrecip.Value) * 100f;
        }

        // Build probe display
        string rainShadow = $"Rain Shadow:\n{FormatPrecipitation(rainShadowPrecip ?? 0f, thresholds)}\n\n";
        string final = $"Final (+ Coastal):\n{FormatPrecipitation(finalPrecip ?? 0f, thresholds)}\n\n";
        string bonus = enhancement > 0.1f
            ? $"Coastal Bonus: +{enhancement:F1}%\n(Maritime climate effect)\n"
            : $"Coastal Bonus: None\n(Deep interior)\n";

        // Show elevation if high (resistance effect)
        string elevInfo = "";
        if (elevation.HasValue && elevation.Value > 5.0f)
        {
            elevInfo = $"\nElevation: {elevation.Value:F1}\n(High altitude resists coastal moisture)\n";
        }

        return header + rainShadow + final + bonus + elevInfo;
    }

    /// <summary>
    /// Builds probe data for Sinks (PRE-Filling) view (VS_029 Step 0A).
    /// Shows: raw elevation, whether this cell is a local minimum (sink), total pre-filling sink count.
    /// </summary>
    private string BuildSinksPreFillingProbeData(
        int x,
        int y,
        Core.Features.WorldGen.Application.DTOs.WorldGenerationResult worldData)
    {
        var data = $"Cell ({x},{y})\nPRE-Filling Sinks\n\n";

        // Get elevation (use post-processed before pit-filling)
        float? elevation = worldData.PostProcessedHeightmap?[y, x];
        bool? isOcean = worldData.OceanMask?[y, x];

        // Check if this cell is a pre-filling sink
        bool isSink = worldData.PreFillingLocalMinima?.Contains((x, y)) ?? false;

        // Show elevation
        if (elevation.HasValue)
            data += $"Elevation: {elevation.Value:F2}\n";

        // Ocean status
        if (isOcean == true)
            data += "Type: Ocean\n";
        else
            data += "Type: Land\n";

        data += "\n";

        // Show sink status
        if (isSink)
            data += "LOCAL MINIMUM\n(Sink before pit-filling)\n";
        else
            data += "Not a sink\n";

        // Show total count
        int totalSinks = worldData.PreFillingLocalMinima?.Count ?? 0;
        data += $"\nTotal Pre-Filling Sinks: {totalSinks}\n";

        // Calculate percentage of land cells
        if (totalSinks > 0 && worldData.OceanMask != null)
        {
            int landCells = 0;
            for (int j = 0; j < worldData.Height; j++)
                for (int i = 0; i < worldData.Width; i++)
                    if (!worldData.OceanMask[j, i]) landCells++;

            float percentage = landCells > 0 ? (totalSinks * 100f / landCells) : 0f;
            data += $"({percentage:F1}% of land cells)\n";
        }

        return data;
    }

    /// <summary>
    /// Builds probe data for Sinks (POST-Filling) view (VS_029 Step 0B).
    /// Shows: filled elevation, whether this cell is still a sink, reduction from pre-filling.
    /// </summary>
    private string BuildSinksPostFillingProbeData(
        int x,
        int y,
        Core.Features.WorldGen.Application.DTOs.WorldGenerationResult worldData)
    {
        var data = $"Cell ({x},{y})\nPOST-Filling Sinks\n\n";

        var erosionData = worldData.Phase1Erosion;
        if (erosionData == null)
        {
            return data + "(No erosion data - regenerate world)";
        }

        // Get filled elevation
        float filledElevation = erosionData.FilledHeightmap[y, x];
        bool? isOcean = worldData.OceanMask?[y, x];

        // Check if this cell is a post-filling sink (flow direction = -1)
        int flowDir = erosionData.FlowDirections[y, x];
        bool isSink = flowDir == -1;

        // Show elevation
        data += $"Elevation: {filledElevation:F2}\n";

        // Ocean status
        if (isOcean == true)
            data += "Type: Ocean\n";
        else
            data += "Type: Land\n";

        data += "\n";

        // Show sink status
        if (isSink)
        {
            // Check if it's a preserved lake
            bool isLake = erosionData.Lakes.Contains((x, y));
            if (isLake)
                data += "PRESERVED LAKE\n(Large endorheic basin)\n";
            else
                data += "LOCAL MINIMUM\n(Remaining sink)\n";
        }
        else
            data += "Not a sink\n(Drains to lower cell)\n";

        // Show reduction statistics
        int preSinks = worldData.PreFillingLocalMinima?.Count ?? 0;
        int postSinks = 0;

        // Count post-filling sinks
        for (int j = 0; j < worldData.Height; j++)
            for (int i = 0; i < worldData.Width; i++)
                if (erosionData.FlowDirections[j, i] == -1) postSinks++;

        data += $"\nPit-Filling Results:\n";
        data += $"Before: {preSinks} sinks\n";
        data += $"After: {postSinks} sinks\n";

        if (preSinks > 0)
        {
            float reduction = ((preSinks - postSinks) * 100f) / preSinks;
            data += $"Reduction: {reduction:F1}%\n";
        }

        return data;
    }

    /// <summary>
    /// Builds probe data for Flow Directions view (VS_029 Step 2).
    /// Shows: flow direction code, compass direction, elevation, whether cell drains.
    /// </summary>
    private string BuildFlowDirectionsProbeData(
        int x,
        int y,
        Core.Features.WorldGen.Application.DTOs.WorldGenerationResult worldData)
    {
        var data = $"Cell ({x},{y})\nFlow Directions\n\n";

        var erosionData = worldData.Phase1Erosion;
        if (erosionData == null)
        {
            return data + "(No erosion data - regenerate world)";
        }

        // Get flow direction
        int flowDir = erosionData.FlowDirections[y, x];
        float elevation = erosionData.FilledHeightmap[y, x];
        bool? isOcean = worldData.OceanMask?[y, x];

        // Show elevation
        data += $"Elevation: {elevation:F2}\n";

        // Ocean status
        if (isOcean == true)
            data += "Type: Ocean (sink)\n\n";
        else
            data += "Type: Land\n\n";

        // Show flow direction
        if (flowDir == -1)
        {
            data += "Direction: SINK\n";
            data += "(Local minimum - no flow)\n";
        }
        else
        {
            string[] dirNames = { "N ↑", "NE ↗", "E →", "SE ↘", "S ↓", "SW ↙", "W ←", "NW ↖" };
            data += $"Direction: {dirNames[flowDir]}\n";
            data += $"Code: {flowDir}\n";

            // Calculate neighbor position
            int[] dx = { 0, 1, 1, 1, 0, -1, -1, -1 };
            int[] dy = { -1, -1, 0, 1, 1, 1, 0, -1 };

            int nx = x + dx[flowDir];
            int ny = y + dy[flowDir];

            // Show downstream elevation if in bounds
            if (nx >= 0 && nx < worldData.Width && ny >= 0 && ny < worldData.Height)
            {
                float downstreamElev = erosionData.FilledHeightmap[ny, nx];
                float drop = elevation - downstreamElev;
                data += $"\nDownstream: {downstreamElev:F2}\n";
                data += $"Drop: {drop:F2}\n";
            }
        }

        return data;
    }

    /// <summary>
    /// Builds probe data for Flow Accumulation view (VS_029 Step 3).
    /// Shows: accumulation value, percentile rank, drainage area estimate.
    /// </summary>
    private string BuildFlowAccumulationProbeData(
        int x,
        int y,
        Core.Features.WorldGen.Application.DTOs.WorldGenerationResult worldData)
    {
        var data = $"Cell ({x},{y})\nFlow Accumulation\n\n";

        var erosionData = worldData.Phase1Erosion;
        if (erosionData == null)
        {
            return data + "(No erosion data - regenerate world)";
        }

        // Get accumulation
        float accumulation = erosionData.FlowAccumulation[y, x];
        bool? isOcean = worldData.OceanMask?[y, x];

        // Show value
        data += $"Accumulation: {accumulation:F3}\n";

        // Ocean cells show as "no flow"
        if (isOcean == true)
        {
            data += "Type: Ocean\n";
            data += "(Terminal sink - no flow data)\n";
            return data;
        }

        // Calculate percentile rank among land cells
        var landAccumulations = new System.Collections.Generic.List<float>();
        for (int j = 0; j < worldData.Height; j++)
        {
            for (int i = 0; i < worldData.Width; i++)
            {
                if (worldData.OceanMask?[j, i] == false)
                {
                    landAccumulations.Add(erosionData.FlowAccumulation[j, i]);
                }
            }
        }

        if (landAccumulations.Count > 0)
        {
            landAccumulations.Sort();
            int rank = landAccumulations.BinarySearch(accumulation);
            if (rank < 0) rank = ~rank; // Handle insertion point
            float percentile = (rank * 100f) / landAccumulations.Count;

            data += $"Percentile: {percentile:F1}%\n";

            // Classification
            if (percentile > 95f)
                data += "Class: Major River\n";
            else if (percentile > 80f)
                data += "Class: River\n";
            else if (percentile > 50f)
                data += "Class: Stream\n";
            else
                data += "Class: Low flow\n";
        }

        return data;
    }

    /// <summary>
    /// Builds probe data for River Sources view (VS_029 Step 4).
    /// Shows: whether cell is a river source, elevation, accumulation threshold.
    /// </summary>
    private string BuildRiverSourcesProbeData(
        int x,
        int y,
        Core.Features.WorldGen.Application.DTOs.WorldGenerationResult worldData)
    {
        var data = $"Cell ({x},{y})\nRiver Sources\n\n";

        var erosionData = worldData.Phase1Erosion;
        if (erosionData == null)
        {
            return data + "(No erosion data - regenerate world)";
        }

        // Get data
        float elevation = erosionData.FilledHeightmap[y, x];
        float accumulation = erosionData.FlowAccumulation[y, x];
        bool isSource = erosionData.RiverSources.Contains((x, y));
        bool? isOcean = worldData.OceanMask?[y, x];

        // Show elevation
        data += $"Elevation: {elevation:F2}\n";
        data += $"Accumulation: {accumulation:F3}\n";

        // Ocean status
        if (isOcean == true)
            data += "Type: Ocean\n\n";
        else
            data += "Type: Land\n\n";

        // Show source status
        if (isSource)
        {
            data += "RIVER SOURCE\n";
            data += "(Major river origin)\n";
        }
        else
        {
            data += "Not a river source\n";

            // Explain why not (elevation or accumulation)
            var thresholds = worldData.Thresholds;
            if (thresholds != null && elevation < thresholds.MountainLevel)
            {
                data += "(Elevation too low)\n";
            }
            else
            {
                data += "(Accumulation below threshold)\n";
            }
        }

        // Show total source count
        int totalSources = erosionData.RiverSources.Count;
        data += $"\nTotal River Sources: {totalSources}\n";

        return data;
    }

    /// <summary>
    /// Builds probe data for Erosion Hotspots view (VS_029 - repurposed old algorithm).
    /// Shows: erosion potential (elevation × accumulation), classification.
    /// </summary>
    private string BuildErosionHotspotsProbeData(
        int x,
        int y,
        Core.Features.WorldGen.Application.DTOs.WorldGenerationResult worldData)
    {
        var data = $"Cell ({x},{y})\nErosion Hotspots\n\n";

        var erosionData = worldData.Phase1Erosion;
        if (erosionData == null)
        {
            return data + "(No erosion data - regenerate world)";
        }

        // Get data
        float elevation = erosionData.FilledHeightmap[y, x];
        float accumulation = erosionData.FlowAccumulation[y, x];
        bool? isOcean = worldData.OceanMask?[y, x];

        // Calculate erosion potential (simple product)
        float erosionPotential = elevation * accumulation;

        // Show values
        data += $"Elevation: {elevation:F2}\n";
        data += $"Accumulation: {accumulation:F3}\n";
        data += $"Erosion Potential: {erosionPotential:F3}\n";

        // Ocean status
        if (isOcean == true)
        {
            data += "\nType: Ocean (no erosion)\n";
            return data;
        }

        data += "\n";

        // Classify erosion potential
        var thresholds = worldData.Thresholds;
        bool isHighElevation = thresholds != null && elevation >= thresholds.MountainLevel;
        bool isHighFlow = accumulation > 0.01f; // Arbitrary threshold for display

        if (isHighElevation && isHighFlow)
        {
            data += "EROSION HOTSPOT\n";
            data += "(High mountains + major river)\n";
            data += "Type: Canyon/gorge formation\n";
        }
        else if (isHighElevation)
        {
            data += "High elevation\n";
            data += "(But low flow - minimal erosion)\n";
        }
        else if (isHighFlow)
        {
            data += "High flow\n";
            data += "(But low elevation - deposition zone)\n";
        }
        else
        {
            data += "Low erosion potential\n";
        }

        return data;
    }
}
