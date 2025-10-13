using Godot;
using Darklands.Core.Features.WorldGen.Application.Common;
using Darklands.Features.WorldGen.ColorSchemes;
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
            Text = "Legend"
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

        // ═══════════════════════════════════════════════════════════════════════
        // TRUE SSOT: Auto-extract legend from ViewModeSchemeRegistry
        // ═══════════════════════════════════════════════════════════════════════
        // The registry maps ViewMode → ColorScheme, ensuring Renderer and Legend
        // ALWAYS use the same scheme. Change the mapping once, both update!

        var scheme = ViewModeSchemeRegistry.GetScheme(mode);

        if (scheme != null)
        {
            // Add view mode title (context header)
            string title = ViewModeSchemeRegistry.GetLegendTitle(mode);
            AddLegendEntry(scheme.Name, new Color(0.8f, 0.8f, 0.8f), title);

            // Auto-generate legend entries from color scheme!
            var entries = scheme.GetLegendEntries();

            if (entries == null || entries.Count == 0)
            {
                _logger?.LogWarning("ColorScheme {SchemeName} returned null or empty legend entries for {ViewMode}",
                    scheme.Name, mode);
                AddLegendEntry("Error", new Color(1, 0, 0), "Scheme returned no entries");
            }
            else
            {
                foreach (var entry in entries)
                {
                    AddLegendEntry(entry.Label, entry.Color, entry.Description);
                }

                _logger?.LogDebug("Legend auto-generated from {SchemeName} for {ViewMode} ({Count} entries)",
                    scheme.Name, mode, entries.Count);
            }
        }
        else
        {
            // Fallback: Custom legends for non-scheme views (Plates, etc.)
            _logger?.LogDebug("No scheme for {ViewMode}, using custom legend", mode);
            RenderCustomLegend(mode);
        }
    }

    /// <summary>
    /// Renders custom legends for view modes that don't use color schemes.
    /// These views have procedural or special-case rendering logic.
    /// </summary>
    private void RenderCustomLegend(MapViewMode mode)
    {
        switch (mode)
        {
            case MapViewMode.Plates:
                // Procedural random colors per plate
                AddLegendEntry("Each color", new Color(0.8f, 0.8f, 0.8f), "= unique plate");
                AddLegendEntry("(10 plates total)", new Color(0.6f, 0.6f, 0.6f), "");
                break;

            case MapViewMode.PreservedLakes:
                // TD_023: Preserved lakes visualization
                AddLegendEntry("Grayscale", new Color(0.5f, 0.5f, 0.5f), "Elevation (terrain context)");
                AddLegendEntry("Dark Blue", new Color(0f, 0f, 0.545f), "Ocean (border-connected water)");
                AddLegendEntry("Colored regions", new Color(0.8f, 0.6f, 1.0f), "Lake boundaries (inner seas / endorheic basins)");
                AddLegendEntry("Red dot", new Color(1f, 0f, 0f), "Pour point (outlet - where water exits)");
                AddLegendEntry("Cyan dot", new Color(0f, 1f, 1f), "Lake center (deepest point)");
                AddLegendEntry("", new Color(0.7f, 0.7f, 0.7f), "");
                AddLegendEntry("Purpose", new Color(0.7f, 0.7f, 0.7f), "Validate lake detection for VS_030");
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
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };

        // Larger font for better readability (was 11, now 13)
        textLabel.AddThemeFontSizeOverride("font_size", 13);

        entry.AddChild(textLabel);
    }
}
