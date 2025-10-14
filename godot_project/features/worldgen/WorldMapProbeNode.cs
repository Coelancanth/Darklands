using System;
using System.Linq;
using Godot;
using Darklands.Core.Features.WorldGen.Application.Common;
using Darklands.Core.Features.WorldGen.Application.DTOs;
using Darklands.Core.Features.WorldGen.Domain;
using Darklands.Features.WorldGen.ProbeDataProviders;
using Microsoft.Extensions.Logging;

namespace Darklands.Features.WorldGen;

/// <summary>
/// Handles mouse input for probing world data at cursor position.
/// Emits probe events with cell coordinates and data values.
/// Pure input handling - no rendering, no UI.
///
/// TD_026: Refactored to use IProbeDataProvider strategy pattern (mirrors TD_025 IColorScheme pattern).
/// Reduced from ~1166 lines → ~280 lines (76% reduction) by extracting probe logic into 14 provider classes.
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

        // [TD_026] Build probe string using provider pattern (mirrors TD_025 color scheme pattern)
        var viewMode = _renderer.GetCurrentViewMode();
        var provider = GetProviderForViewMode(viewMode);

        string probeData;
        if (provider != null)
        {
            // Provider found - delegate to strategy implementation
            probeData = provider.GetProbeText(
                data: worldData,
                x: x,
                y: y,
                viewMode: viewMode,
                debugTexture: _renderer.Texture as ImageTexture
            );
        }
        else
        {
            // Fallback for unmapped view modes
            probeData = $"Cell ({x},{y})\nUnknown view";
        }

        // Log probe result - output the SAME formatted data to console for easy debugging
        // This makes the Godot Output panel show exactly what the UI panel displays
        _logger?.LogInformation("[WorldGen] PROBE ({ViewMode}):\n{ProbeData}",
            viewMode, probeData);

        EmitSignal(SignalName.CellProbed, x, y, probeData);
    }

    /// <summary>
    /// [TD_026] Maps view modes to probe data providers.
    /// Mirrors GetSchemeForViewMode pattern from TD_025 for architectural consistency.
    /// Returns null for unknown view modes (fallback to generic probe text).
    /// </summary>
    private IProbeDataProvider? GetProviderForViewMode(MapViewMode mode)
    {
        return mode switch
        {
            // Simple providers
            MapViewMode.RawElevation => ProbeDataProviders.ProbeDataProviders.RawElevation,
            MapViewMode.Plates => ProbeDataProviders.ProbeDataProviders.Plates,

            // Elevation providers (both colored modes use same provider)
            MapViewMode.ColoredOriginalElevation => ProbeDataProviders.ProbeDataProviders.Elevation,
            MapViewMode.ColoredPostProcessedElevation => ProbeDataProviders.ProbeDataProviders.Elevation,

            // Temperature providers (multi-mode: 4 temperature stages)
            MapViewMode.TemperatureLatitudeOnly => ProbeDataProviders.ProbeDataProviders.Temperature,
            MapViewMode.TemperatureWithNoise => ProbeDataProviders.ProbeDataProviders.Temperature,
            MapViewMode.TemperatureWithDistance => ProbeDataProviders.ProbeDataProviders.Temperature,
            MapViewMode.TemperatureFinal => ProbeDataProviders.ProbeDataProviders.Temperature,

            // Precipitation providers (multi-mode: 3 base stages)
            MapViewMode.PrecipitationNoiseOnly => ProbeDataProviders.ProbeDataProviders.Precipitation,
            MapViewMode.PrecipitationTemperatureShaped => ProbeDataProviders.ProbeDataProviders.Precipitation,
            MapViewMode.PrecipitationBase => ProbeDataProviders.ProbeDataProviders.Precipitation,

            // Rain shadow provider (Stage 4)
            MapViewMode.PrecipitationWithRainShadow => ProbeDataProviders.ProbeDataProviders.RainShadow,

            // Coastal moisture provider (Stage 5)
            MapViewMode.PrecipitationFinal => ProbeDataProviders.ProbeDataProviders.CoastalMoisture,

            // Erosion/flow providers
            MapViewMode.SinksPreFilling => ProbeDataProviders.ProbeDataProviders.SinksPreFilling,
            MapViewMode.SinksPostFilling => ProbeDataProviders.ProbeDataProviders.SinksPostFilling,
            MapViewMode.PreservedLakes => ProbeDataProviders.ProbeDataProviders.BasinMetadata,
            MapViewMode.FlowDirections => ProbeDataProviders.ProbeDataProviders.FlowDirections,
            MapViewMode.FlowAccumulation => ProbeDataProviders.ProbeDataProviders.FlowAccumulation,
            MapViewMode.RiverSources => ProbeDataProviders.ProbeDataProviders.RiverSources,
            MapViewMode.ErosionHotspots => ProbeDataProviders.ProbeDataProviders.ErosionHotspots,

            _ => null  // Unknown mode - fallback to generic probe text
        };
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
}
