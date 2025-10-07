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
    private bool _isProbingEnabled = true;

    [Signal]
    public delegate void CellProbedEventHandler(int x, int y, string probeData);

    public override void _Ready()
    {
        _logger?.LogDebug("WorldMapProbeNode ready");
    }

    /// <summary>
    /// Links this probe to a renderer node.
    /// </summary>
    public void SetRenderer(WorldMapRendererNode renderer, ILogger<WorldMapProbeNode> logger)
    {
        _renderer = renderer;
        _logger = logger;
    }

    /// <summary>
    /// Enables or disables probing.
    /// </summary>
    public void SetProbingEnabled(bool enabled)
    {
        _isProbingEnabled = enabled;
    }

    public override void _Input(InputEvent @event)
    {
        if (!_isProbingEnabled || _renderer == null) return;

        // Detect 'P' key press for probe
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            if (keyEvent.Keycode == Key.P)
            {
                ProbeAtMousePosition();
            }
        }

        // Alternative: Click to probe (optional, can enable later)
        // if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
        // {
        //     if (mouseEvent.ButtonIndex == MouseButton.Left)
        //     {
        //         ProbeAtMousePosition();
        //     }
        // }
    }

    private void ProbeAtMousePosition()
    {
        var worldData = _renderer?.GetWorldData();
        if (worldData == null)
        {
            _logger?.LogWarning("Cannot probe: No world data loaded");
            return;
        }

        // Get mouse position in viewport
        var mousePos = _renderer!.GetViewport().GetMousePosition();

        // Convert to sprite local coordinates
        var spriteTransform = _renderer.GetGlobalTransform().AffineInverse();
        var localPos = spriteTransform * mousePos;

        // Convert to texture coordinates
        var texture = _renderer.Texture;
        if (texture == null) return;

        var textureSize = texture.GetSize();
        int x = (int)(localPos.X + textureSize.X / 2);
        int y = (int)(localPos.Y + textureSize.Y / 2);

        // Bounds check
        if (x < 0 || x >= worldData.Width || y < 0 || y >= worldData.Height)
        {
            _logger?.LogDebug("Probe out of bounds: ({X},{Y})", x, y);
            return;
        }

        // Get data at cell
        float elevation = worldData.Heightmap[y, x];
        uint plateId = worldData.PlatesMap[y, x];

        // Build probe string based on current view mode
        var viewMode = _renderer.GetCurrentViewMode();
        string probeData = viewMode switch
        {
            MapViewMode.RawElevation =>
                $"Cell ({x},{y}) | Elevation: {elevation:F3}",
            MapViewMode.Plates =>
                $"Cell ({x},{y}) | Plate ID: {plateId} | Elevation: {elevation:F3}",
            _ => $"Cell ({x},{y}) | Unknown view"
        };

        _logger?.LogInformation(probeData);
        EmitSignal(SignalName.CellProbed, x, y, probeData);
    }
}
