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
    private const float HIGHLIGHT_ALPHA = 0.3f;

    [Signal]
    public delegate void CellProbedEventHandler(int x, int y, string probeData);

    public override void _Ready()
    {
        // Create highlight overlay (initially hidden)
        _highlightRect = new ColorRect
        {
            Color = new Color(1, 1, 0, HIGHLIGHT_ALPHA), // Yellow with transparency
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

        // Click to probe (left mouse button, not while panning)
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && !isPanModeActive)
        {
            if (mouseEvent.ButtonIndex == MouseButton.Left)
            {
                ProbeAtMousePosition();
                GetViewport().SetInputAsHandled();
            }
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

        // Log probe result with all relevant data
        _logger?.LogInformation("Probed cell ({X},{Y}): Elevation={Elevation:F3}, PlateId={PlateId}, ViewMode={ViewMode}",
            x, y, elevation, plateId, viewMode);

        EmitSignal(SignalName.CellProbed, x, y, probeData);
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
