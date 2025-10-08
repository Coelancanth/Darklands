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
    private MapViewMode _currentViewMode = MapViewMode.ColoredOriginalElevation;  // Default to original elevation (VS_024)

    public override void _Ready()
    {
        // Position in upper-left corner
        AnchorLeft = 0;
        AnchorTop = 0;
        AnchorRight = 0;
        AnchorBottom = 0;
        OffsetLeft = 10;
        OffsetTop = 10;
        OffsetRight = 280;  // Width: 270px (increased from 220px)
        OffsetBottom = 320; // Height: 310px (increased from 290px)

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
        // Panel background for visibility
        var panel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        AddChild(panel);

        _container = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        panel.AddChild(_container);

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

            case MapViewMode.ColoredOriginalElevation:
                // 7-band terrain gradient (VS_024: Original native output)
                AddLegendEntry("Original Elevation", new Color(0.8f, 0.8f, 0.8f), "(native raw, unmodified)");
                AddLegendEntry("Deep Blue", new Color(0f, 0f, 1f), "Deep ocean");
                AddLegendEntry("Blue", new Color(0f, 0.078f, 0.784f), "Ocean");
                AddLegendEntry("Cyan", new Color(0.529f, 0.929f, 0.922f), "Shallow water");
                AddLegendEntry("Green", new Color(0.345f, 0.678f, 0.192f), "Grass/Lowlands");
                AddLegendEntry("Yellow-Green", new Color(0.855f, 0.886f, 0.227f), "Hills");
                AddLegendEntry("Yellow", new Color(0.984f, 0.988f, 0.165f), "Mountains");
                AddLegendEntry("Brown", new Color(0.357f, 0.110f, 0.051f), "Peaks");
                break;

            case MapViewMode.ColoredPostProcessedElevation:
                // 7-band terrain gradient (VS_024: After 4 WorldEngine algorithms)
                AddLegendEntry("Post-Processed", new Color(0.8f, 0.8f, 0.8f), "(noise + smooth ocean)");
                AddLegendEntry("Deep Blue", new Color(0f, 0f, 1f), "Deep ocean");
                AddLegendEntry("Blue", new Color(0f, 0.078f, 0.784f), "Ocean");
                AddLegendEntry("Cyan", new Color(0.529f, 0.929f, 0.922f), "Shallow water");
                AddLegendEntry("Green", new Color(0.345f, 0.678f, 0.192f), "Grass/Lowlands");
                AddLegendEntry("Yellow-Green", new Color(0.855f, 0.886f, 0.227f), "Hills");
                AddLegendEntry("Yellow", new Color(0.984f, 0.988f, 0.165f), "Mountains");
                AddLegendEntry("Brown", new Color(0.357f, 0.110f, 0.051f), "Peaks");
                break;

            case MapViewMode.Plates:
                AddLegendEntry("Each color", new Color(0.8f, 0.8f, 0.8f), "= unique plate");
                AddLegendEntry("(10 plates total)", new Color(0.6f, 0.6f, 0.6f), "");
                break;

            case MapViewMode.TemperatureLatitudeOnly:
                // 7-band WorldEngine climate zones (VS_025: Debug Stage 1)
                AddLegendEntry("Latitude Only", new Color(0.8f, 0.8f, 0.8f), "(quantile bands)");
                AddLegendEntry("Blue", new Color(0f, 0f, 1f), "Polar (coldest)");
                AddLegendEntry("Blue-Purple", new Color(42f/255f, 0f, 213f/255f), "Alpine");
                AddLegendEntry("Purple", new Color(85f/255f, 0f, 170f/255f), "Boreal");
                AddLegendEntry("Magenta", new Color(128f/255f, 0f, 128f/255f), "Cool");
                AddLegendEntry("Purple-Red", new Color(170f/255f, 0f, 85f/255f), "Warm");
                AddLegendEntry("Red-Purple", new Color(213f/255f, 0f, 42f/255f), "Subtropical");
                AddLegendEntry("Red", new Color(1f, 0f, 0f), "Tropical (hottest)");
                break;

            case MapViewMode.TemperatureWithNoise:
                // 7-band WorldEngine climate zones (VS_025: Debug Stage 2)
                AddLegendEntry("+ Climate Noise", new Color(0.8f, 0.8f, 0.8f), "(8% variation)");
                AddLegendEntry("Blue", new Color(0f, 0f, 1f), "Polar");
                AddLegendEntry("Blue-Purple", new Color(42f/255f, 0f, 213f/255f), "Alpine");
                AddLegendEntry("Purple", new Color(85f/255f, 0f, 170f/255f), "Boreal");
                AddLegendEntry("Magenta", new Color(128f/255f, 0f, 128f/255f), "Cool");
                AddLegendEntry("Purple-Red", new Color(170f/255f, 0f, 85f/255f), "Warm");
                AddLegendEntry("Red-Purple", new Color(213f/255f, 0f, 42f/255f), "Subtropical");
                AddLegendEntry("Red", new Color(1f, 0f, 0f), "Tropical");
                break;

            case MapViewMode.TemperatureWithDistance:
                // 7-band WorldEngine climate zones (VS_025: Debug Stage 3)
                AddLegendEntry("+ Distance to Sun", new Color(0.8f, 0.8f, 0.8f), "(hot/cold planets)");
                AddLegendEntry("Blue", new Color(0f, 0f, 1f), "Polar");
                AddLegendEntry("Blue-Purple", new Color(42f/255f, 0f, 213f/255f), "Alpine");
                AddLegendEntry("Purple", new Color(85f/255f, 0f, 170f/255f), "Boreal");
                AddLegendEntry("Magenta", new Color(128f/255f, 0f, 128f/255f), "Cool");
                AddLegendEntry("Purple-Red", new Color(170f/255f, 0f, 85f/255f), "Warm");
                AddLegendEntry("Red-Purple", new Color(213f/255f, 0f, 42f/255f), "Subtropical");
                AddLegendEntry("Red", new Color(1f, 0f, 0f), "Tropical");
                break;

            case MapViewMode.TemperatureFinal:
                // 7-band WorldEngine climate zones (VS_025: Production Stage 4)
                AddLegendEntry("Final Temperature", new Color(0.8f, 0.8f, 0.8f), "(+ mountain cooling)");
                AddLegendEntry("Blue", new Color(0f, 0f, 1f), "Polar");
                AddLegendEntry("Blue-Purple", new Color(42f/255f, 0f, 213f/255f), "Alpine");
                AddLegendEntry("Purple", new Color(85f/255f, 0f, 170f/255f), "Boreal");
                AddLegendEntry("Magenta", new Color(128f/255f, 0f, 128f/255f), "Cool");
                AddLegendEntry("Purple-Red", new Color(170f/255f, 0f, 85f/255f), "Warm");
                AddLegendEntry("Red-Purple", new Color(213f/255f, 0f, 42f/255f), "Subtropical");
                AddLegendEntry("Red", new Color(1f, 0f, 0f), "Tropical");
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
            CustomMinimumSize = new Vector2(0, 24)  // Increased height (was 20)
        };
        _container!.AddChild(entry);

        // Color swatch
        var colorRect = new ColorRect
        {
            Color = color,
            CustomMinimumSize = new Vector2(20, 20)  // Larger swatch (was 16×16)
        };
        entry.AddChild(colorRect);

        // Spacing
        var spacer = new Control { CustomMinimumSize = new Vector2(8, 0) };  // More spacing (was 6)
        entry.AddChild(spacer);

        // Combined label (horizontal: "Deep Blue - Deep ocean")
        var textLabel = new Label
        {
            Text = string.IsNullOrEmpty(description) ? label : $"{label} - {description}",
            AutowrapMode = TextServer.AutowrapMode.Off,  // No wrapping for horizontal layout
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Theme = GD.Load<Theme>("res://addons/default_theme.tres")
        };

        // Larger font for better readability (was 11, now 13)
        textLabel.AddThemeFontSizeOverride("font_size", 13);

        entry.AddChild(textLabel);
    }
}
