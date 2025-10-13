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

            case MapViewMode.PrecipitationNoiseOnly:
                // 3-band moisture gradient (VS_026: Debug Stage 1)
                AddLegendEntry("Noise Only", new Color(0.8f, 0.8f, 0.8f), "(base coherent noise)");
                AddLegendEntry("Yellow", new Color(255f/255f, 255f/255f, 0f), "Dry (random)");
                AddLegendEntry("Green", new Color(0f, 200f/255f, 0f), "Moderate (random)");
                AddLegendEntry("Blue", new Color(0f, 0f, 1f), "Wet (random)");
                break;

            case MapViewMode.PrecipitationTemperatureShaped:
                // 3-band moisture gradient (VS_026: Debug Stage 2)
                AddLegendEntry("+ Temp Gamma Curve", new Color(0.8f, 0.8f, 0.8f), "(physics shaping)");
                AddLegendEntry("Yellow", new Color(255f/255f, 255f/255f, 0f), "Dry (cold = low evap)");
                AddLegendEntry("Green", new Color(0f, 200f/255f, 0f), "Moderate");
                AddLegendEntry("Blue", new Color(0f, 0f, 1f), "Wet (hot = high evap)");
                break;

            case MapViewMode.PrecipitationBase:
                // 3-band moisture gradient (VS_026: Production Stage 3)
                AddLegendEntry("Base Precipitation", new Color(0.8f, 0.8f, 0.8f), "(before rain shadow)");
                AddLegendEntry("Yellow", new Color(255f/255f, 255f/255f, 0f), "Low (<400mm/year)");
                AddLegendEntry("Green", new Color(0f, 200f/255f, 0f), "Medium (400-800mm)");
                AddLegendEntry("Blue", new Color(0f, 0f, 1f), "High (>800mm/year)");
                break;

            case MapViewMode.PrecipitationWithRainShadow:
                // 3-band moisture gradient with rain shadow effects (VS_027: Production Stage 4)
                AddLegendEntry("+ Rain Shadow", new Color(0.8f, 0.8f, 0.8f), "(orographic blocking)");
                AddLegendEntry("Yellow", new Color(255f/255f, 255f/255f, 0f), "Dry (leeward deserts)");
                AddLegendEntry("Green", new Color(0f, 200f/255f, 0f), "Moderate");
                AddLegendEntry("Blue", new Color(0f, 0f, 1f), "Wet (windward coasts)");
                break;

            case MapViewMode.PrecipitationFinal:
                // 3-band moisture gradient FINAL (VS_028: Production Stage 5 - coastal moisture)
                AddLegendEntry("FINAL (+ Coastal)", new Color(0.8f, 0.8f, 0.8f), "(maritime vs continental)");
                AddLegendEntry("Yellow", new Color(255f/255f, 255f/255f, 0f), "Arid (interior)");
                AddLegendEntry("Green", new Color(0f, 200f/255f, 0f), "Moderate");
                AddLegendEntry("Blue", new Color(0f, 0f, 1f), "Wet (maritime coasts)");
                break;

            case MapViewMode.SinksPreFilling:
                // VS_029 Step 0A: Pre-filling sinks diagnostic
                AddLegendEntry("Sinks (PRE-Filling)", new Color(0.8f, 0.8f, 0.8f), "Baseline before pit-filling");
                AddLegendEntry("Grayscale", new Color(0.5f, 0.5f, 0.5f), "Elevation (low→high)");
                AddLegendEntry("Red Dots", new Color(1f, 0f, 0f), "Local minima (sinks)");
                AddLegendEntry("Expected", new Color(0.8f, 0.8f, 0.8f), "5-20% land (noisy)");
                break;

            case MapViewMode.SinksPostFilling:
                // VS_029 Step 0B: Post-filling sinks diagnostic
                AddLegendEntry("Sinks (POST-Filling)", new Color(0.8f, 0.8f, 0.8f), "After pit-filling");
                AddLegendEntry("Grayscale", new Color(0.5f, 0.5f, 0.5f), "Elevation (filled)");
                AddLegendEntry("Red Dots", new Color(1f, 0f, 0f), "Preserved lakes");
                AddLegendEntry("Expected", new Color(0.8f, 0.8f, 0.8f), "<5% land (70-90% reduction)");
                break;

            case MapViewMode.FlowDirections:
                // VS_029 Step 2: D-8 flow direction visualization
                AddLegendEntry("Flow Directions (D-8)", new Color(0.8f, 0.8f, 0.8f), "Steepest descent");
                AddLegendEntry("Red", new Color(1f, 0f, 0f), "North");
                AddLegendEntry("Yellow", new Color(1f, 1f, 0f), "NE");
                AddLegendEntry("Green", new Color(0f, 1f, 0f), "East");
                AddLegendEntry("Cyan", new Color(0f, 1f, 1f), "SE");
                AddLegendEntry("Blue", new Color(0f, 0f, 1f), "South");
                AddLegendEntry("Purple", new Color(0.5f, 0f, 0.5f), "SW");
                AddLegendEntry("Magenta", new Color(1f, 0f, 1f), "West");
                AddLegendEntry("Orange", new Color(1f, 0.5f, 0f), "NW");
                AddLegendEntry("Black", new Color(0f, 0f, 0f), "Sink (no flow)");
                break;

            case MapViewMode.FlowAccumulation:
                // VS_029 Step 3: Flow accumulation heat map
                AddLegendEntry("Flow Accumulation", new Color(0.8f, 0.8f, 0.8f), "Drainage basin size");
                AddLegendEntry("Black", new Color(0f, 0f, 0f), "Ocean (no flow)");
                AddLegendEntry("Blue", new Color(0f, 0f, 1f), "Low (hilltops)");
                AddLegendEntry("Green", new Color(0f, 1f, 0f), "Medium (slopes)");
                AddLegendEntry("Yellow", new Color(1f, 1f, 0f), "High (valleys)");
                AddLegendEntry("Red", new Color(1f, 0f, 0f), "Very high (rivers)");
                break;

            case MapViewMode.RiverSources:
                // VS_029 Step 4: River source detection (corrected algorithm)
                AddLegendEntry("River Sources", new Color(0.8f, 0.8f, 0.8f), "Threshold-crossing algorithm");
                AddLegendEntry("Grayscale", new Color(0.5f, 0.5f, 0.5f), "Elevation (low→high)");
                AddLegendEntry("Red Dots", new Color(1f, 0f, 0f), "River origins");
                AddLegendEntry("Expected", new Color(0.8f, 0.8f, 0.8f), "100s → filtered to 5-15 major");
                break;

            case MapViewMode.ErosionHotspots:
                // VS_029: Erosion hotspots (old algorithm repurposed)
                AddLegendEntry("Erosion Hotspots", new Color(0.8f, 0.8f, 0.8f), "High-energy zones");
                AddLegendEntry("Colored Terrain", new Color(0.5f, 0.5f, 0.5f), "Elevation gradient");
                AddLegendEntry("Magenta Dots", new Color(1f, 0f, 1f), "High elev + high flow");
                AddLegendEntry("Purpose", new Color(0.8f, 0.8f, 0.8f), "Canyon erosion masking");
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
