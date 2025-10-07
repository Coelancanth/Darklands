using Godot;
using Darklands.Core.Features.WorldGen.Application.Common;
using Microsoft.Extensions.Logging;

namespace Darklands.Features.WorldGen;

/// <summary>
/// Displays color legend for current map view mode.
/// Updates dynamically when view mode changes.
/// Pure display - no interaction, no logic.
/// </summary>
public partial class WorldMapLegendNode : Control
{
    private ILogger<WorldMapLegendNode>? _logger;
    private VBoxContainer? _container;
    private MapViewMode _currentViewMode = MapViewMode.RawElevation;

    public override void _Ready()
    {
        // Position in bottom-left corner
        AnchorLeft = 0;
        AnchorTop = 1;
        AnchorRight = 0;
        AnchorBottom = 1;
        OffsetLeft = 10;
        OffsetTop = -200;
        OffsetRight = 210;
        OffsetBottom = -10;

        BuildLegendContainer();
        UpdateLegend(_currentViewMode);

        _logger?.LogDebug("WorldMapLegendNode ready");
    }

    /// <summary>
    /// Sets the logger for this node.
    /// </summary>
    public void SetLogger(ILogger<WorldMapLegendNode> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Updates the legend for the given view mode.
    /// </summary>
    public void UpdateForViewMode(MapViewMode mode)
    {
        _currentViewMode = mode;
        UpdateLegend(mode);
    }

    private void BuildLegendContainer()
    {
        _container = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        AddChild(_container);

        // Title
        var titleLabel = new Label
        {
            Text = "Legend",
            Theme = GD.Load<Theme>("res://addons/default_theme.tres")
        };
        _container.AddChild(titleLabel);
        _container.AddChild(new HSeparator());
    }

    private void UpdateLegend(MapViewMode mode)
    {
        if (_container == null) return;

        // Clear existing legend entries (keep title + separator)
        while (_container.GetChildCount() > 2)
        {
            var child = _container.GetChild(_container.GetChildCount() - 1);
            _container.RemoveChild(child);
            child.QueueFree();
        }

        // Add legend entries based on view mode
        switch (mode)
        {
            case MapViewMode.RawElevation:
                AddLegendEntry("Black", new Color(0, 0, 0), "Low elevation");
                AddLegendEntry("Gray", new Color(0.5f, 0.5f, 0.5f), "Mid elevation");
                AddLegendEntry("White", new Color(1, 1, 1), "High elevation");
                break;

            case MapViewMode.Plates:
                AddLegendEntry("Each color", new Color(0.8f, 0.8f, 0.8f), "= unique plate");
                AddLegendEntry("(10 plates total)", new Color(0.6f, 0.6f, 0.6f), "");
                break;

            default:
                AddLegendEntry("Unknown view", new Color(1, 0, 0), "");
                break;
        }
    }

    private void AddLegendEntry(string label, Color color, string description)
    {
        var entry = new HBoxContainer
        {
            CustomMinimumSize = new Vector2(0, 24)
        };
        _container!.AddChild(entry);

        // Color swatch
        var colorRect = new ColorRect
        {
            Color = color,
            CustomMinimumSize = new Vector2(20, 20)
        };
        entry.AddChild(colorRect);

        // Spacing
        var spacer = new Control { CustomMinimumSize = new Vector2(8, 0) };
        entry.AddChild(spacer);

        // Label
        var textLabel = new Label
        {
            Text = string.IsNullOrEmpty(description) ? label : $"{label} - {description}",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        entry.AddChild(textLabel);
    }
}
