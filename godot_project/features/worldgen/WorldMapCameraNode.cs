using Godot;
using Microsoft.Extensions.Logging;

namespace Darklands.Features.WorldGen;

/// <summary>
/// Controls Camera2D for world map navigation.
/// Handles mouse wheel zoom and middle-mouse drag panning.
/// Pure input handling - no rendering logic.
/// </summary>
public partial class WorldMapCameraNode : Node
{
    private ILogger<WorldMapCameraNode>? _logger;
    private Camera2D? _camera;

    // Zoom settings
    private const float MIN_ZOOM = 0.5f;
    private const float MAX_ZOOM = 20.0f;  // Increased for better cell highlight visibility
    private const float ZOOM_SPEED = 0.1f;

    // Pan state with hold-to-activate
    private bool _isPanning = false;
    private bool _middleMouseDown = false;
    private double _middleMouseDownTime = 0;
    private const double PAN_ACTIVATION_THRESHOLD = 0.05; // 200ms hold to activate pan

    /// <summary>
    /// Returns true if currently in pan mode.
    /// </summary>
    public bool IsPanning => _isPanning;

    public override void _Ready()
    {
        _logger?.LogDebug("WorldMapCameraNode ready");
    }

    /// <summary>
    /// Sets the logger and camera reference.
    /// Called by orchestrator after node creation.
    /// </summary>
    public void Initialize(Camera2D camera, ILogger<WorldMapCameraNode> logger)
    {
        _camera = camera;
        _logger = logger;
        _logger?.LogDebug("WorldMapCameraNode initialized with camera");
    }

    /// <summary>
    /// Resets camera to default position and zoom.
    /// Called when regenerating world.
    /// </summary>
    public void ResetCamera()
    {
        if (_camera == null) return;

        _camera.Position = Vector2.Zero;
        _camera.Zoom = Vector2.One;
        _logger?.LogDebug("Camera reset to default position and zoom");
    }

    public override void _Process(double delta)
    {
        // Check if middle-mouse has been held long enough to activate pan mode
        if (_middleMouseDown && !_isPanning)
        {
            var holdTime = Time.GetTicksMsec() / 1000.0 - _middleMouseDownTime;
            if (holdTime >= PAN_ACTIVATION_THRESHOLD)
            {
                _isPanning = true;
                _logger?.LogDebug("Pan mode activated (held for {HoldTime:F2}s)", holdTime);
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (_camera == null) return;

        // Mouse wheel zoom
        if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
        {
            if (mouseButton.ButtonIndex == MouseButton.WheelUp)
            {
                ZoomIn();
                GetViewport().SetInputAsHandled();
            }
            else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
            {
                ZoomOut();
                GetViewport().SetInputAsHandled();
            }
            else if (mouseButton.ButtonIndex == MouseButton.Middle)
            {
                // Start hold timer for pan activation
                _middleMouseDown = true;
                _middleMouseDownTime = Time.GetTicksMsec() / 1000.0;
                GetViewport().SetInputAsHandled();
            }
        }

        // Middle mouse release
        if (@event is InputEventMouseButton mouseButtonRelease && !mouseButtonRelease.Pressed)
        {
            if (mouseButtonRelease.ButtonIndex == MouseButton.Middle)
            {
                _middleMouseDown = false;
                _isPanning = false;
                GetViewport().SetInputAsHandled();
            }
        }

        // Middle mouse drag for panning (only if pan mode activated)
        if (@event is InputEventMouseMotion motion && _isPanning)
        {
            // Pan camera by mouse movement (divided by zoom for consistent feel)
            _camera.Position -= motion.Relative / _camera.Zoom;
            GetViewport().SetInputAsHandled();
        }
    }

    private void ZoomIn()
    {
        if (_camera == null) return;

        var newZoom = _camera.Zoom * (1.0f + ZOOM_SPEED);
        if (newZoom.X <= MAX_ZOOM)
        {
            _camera.Zoom = newZoom;
            _logger?.LogDebug("Zoomed in: {Zoom}", _camera.Zoom.X);
        }
    }

    private void ZoomOut()
    {
        if (_camera == null) return;

        var newZoom = _camera.Zoom * (1.0f - ZOOM_SPEED);
        if (newZoom.X >= MIN_ZOOM)
        {
            _camera.Zoom = newZoom;
            _logger?.LogDebug("Zoomed out: {Zoom}", _camera.Zoom.X);
        }
    }
}
