using Godot;
using System;
using System.Threading.Tasks;
using Darklands.Core.Application.Infrastructure;
using Darklands.Core.Features.WorldGen.Application.Commands;
using Darklands.Core.Features.WorldGen.Application.Common;
using Darklands.Core.Features.WorldGen.Application.DTOs;
using Darklands.Core.Features.WorldGen.Application.Rendering;
using Darklands.Core.Features.WorldGen.Domain;
using Darklands.Core.Infrastructure.DependencyInjection;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Features.WorldGen;

/// <summary>
/// Renders generated world map as an Image/Texture2D (base terrain layer).
/// Uses Sprite2D for GPU-accelerated rendering of biome color data.
/// Handles world generation via GenerateWorldCommand and camera controls.
/// </summary>
public partial class WorldMapNode : Node2D
{
    private IMediator? _mediator;
    private ILogger<WorldMapNode>? _logger;
    private Camera2D? _camera;
    private Label? _uiLabel;
    private Sprite2D? _terrainSprite;
    private Control? _legendPanel;

    // Cached world data (session-scoped, enables instant view switching)
    private PlateSimulationResult? _cachedWorldData;
    private MapViewMode _currentViewMode = MapViewMode.Biomes;

    // Camera control settings
    private const float PanSpeed = 500f;
    private const float ZoomSpeed = 0.3f; // Increased from 0.1 to 0.3 for faster zoom
    private const float MinZoom = 0.5f;
    private const float MaxZoom = 20f; // Increased from 3x to 20x for detailed inspection

    // Middle mouse button drag state
    private bool _isDragging = false;
    private Vector2 _dragStartPosition;
    private Vector2 _cameraStartPosition;

    // World generation settings
    [Export] public int Seed { get; set; } = 42;
    [Export] public int WorldSize { get; set; } = 512;

    /// <summary>
    /// Use Ricklefs-style simplified biome categories (9 major biomes) instead of detailed Holdridge (41 biomes).
    /// True = Simplified (default - easier to read strategic map)
    /// False = Detailed (full 41-biome Holdridge classification)
    /// </summary>
    [Export] public bool UseSimplifiedBiomes { get; set; } = true;

    public override void _Ready()
    {
        base._Ready();

        _mediator = ServiceLocator.GetService<IMediator>().Value;
        _logger = ServiceLocator.GetService<ILogger<WorldMapNode>>().Value;

        _camera = GetParent().GetNode<Camera2D>("Camera2D");
        _uiLabel = GetNode<Label>("../UI/Label");

        // Create visual legend panel
        CreateLegendPanel();

        // Create terrain sprite as child
        _terrainSprite = new Sprite2D
        {
            Name = "TerrainSprite",
            Centered = false // Position at (0, 0) for easier camera alignment
        };
        AddChild(_terrainSprite);

        _logger.LogInformation("WorldMapNode ready, generating world with seed {Seed}", Seed);

        // Generate world asynchronously
        _ = GenerateAndRenderWorldAsync();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (_camera == null) return;

        // WASD camera panning
        var panDirection = Vector2.Zero;

        if (Input.IsKeyPressed(Key.W)) panDirection.Y -= 1;
        if (Input.IsKeyPressed(Key.S)) panDirection.Y += 1;
        if (Input.IsKeyPressed(Key.A)) panDirection.X -= 1;
        if (Input.IsKeyPressed(Key.D)) panDirection.X += 1;

        if (panDirection != Vector2.Zero)
        {
            _camera.Position += panDirection.Normalized() * PanSpeed * (float)delta / _camera.Zoom.X;
        }
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (_camera == null) return;

        // Keyboard shortcuts for view mode switching (instant testing)
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            switch (keyEvent.Keycode)
            {
                case Key.Key1:
                    SetViewMode(MapViewMode.Biomes);
                    break;
                case Key.Key2:
                    SetViewMode(MapViewMode.Elevation);
                    break;
                case Key.Key3:
                    SetViewMode(MapViewMode.Precipitation);
                    break;
                case Key.Key4:
                    SetViewMode(MapViewMode.Temperature);
                    break;
            }
        }

        // Mouse wheel zoom (faster with increased ZoomSpeed)
        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.Pressed)
            {
                if (mouseButton.ButtonIndex == MouseButton.WheelUp)
                {
                    var newZoom = _camera.Zoom.X + ZoomSpeed;
                    _camera.Zoom = new Vector2(Mathf.Clamp(newZoom, MinZoom, MaxZoom), Mathf.Clamp(newZoom, MinZoom, MaxZoom));
                }
                else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
                {
                    var newZoom = _camera.Zoom.X - ZoomSpeed;
                    _camera.Zoom = new Vector2(Mathf.Clamp(newZoom, MinZoom, MaxZoom), Mathf.Clamp(newZoom, MinZoom, MaxZoom));
                }
                // Middle mouse button drag - start dragging
                else if (mouseButton.ButtonIndex == MouseButton.Middle)
                {
                    _isDragging = true;
                    _dragStartPosition = GetViewport().GetMousePosition();
                    _cameraStartPosition = _camera.Position;
                }
            }
            else // Button released
            {
                // Middle mouse button drag - stop dragging
                if (mouseButton.ButtonIndex == MouseButton.Middle)
                {
                    _isDragging = false;
                }
            }
        }

        // Handle mouse motion for middle mouse button dragging
        if (@event is InputEventMouseMotion && _isDragging)
        {
            var currentMousePosition = GetViewport().GetMousePosition();
            var dragDelta = currentMousePosition - _dragStartPosition;

            // Pan camera inversely to drag direction (natural feeling)
            // Use screen-space delta scaled by zoom for proper world-space movement
            _camera.Position = _cameraStartPosition - dragDelta / _camera.Zoom.X;
        }
    }

    private async Task GenerateAndRenderWorldAsync()
    {
        if (_mediator == null || _logger == null || _terrainSprite == null)
        {
            _logger?.LogError("Required dependencies not initialized");
            return;
        }

        _logger.LogInformation("Generating world: seed={Seed}, size={Size}x{Size}", Seed, WorldSize, WorldSize);

        var command = new GenerateWorldCommand(Seed, WorldSize);
        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            _logger.LogError("World generation failed: {Error}", result.Error);
            if (_uiLabel != null)
            {
                _uiLabel.Text = $"World generation FAILED:\n{result.Error}";
            }
            return;
        }

        _logger.LogInformation("World generation complete, caching result {Width}x{Height}",
            result.Value.Width, result.Value.Height);

        // Cache the generated world data (enables instant view switching)
        _cachedWorldData = result.Value;

        // Render using current view mode
        RenderCurrentView();

        // Update UI label with world info
        UpdateUILabel();

        // Update legend to show current view mode colors
        UpdateLegend();
    }

    /// <summary>
    /// Updates the UI label with current world info and view mode.
    /// Called after generation or view mode change.
    /// </summary>
    private void UpdateUILabel()
    {
        if (_uiLabel == null || _cachedWorldData == null) return;

        var viewModeText = _currentViewMode switch
        {
            MapViewMode.Biomes => UseSimplifiedBiomes
                ? "Biomes (Ricklefs - 9 categories)"
                : "Biomes (Holdridge - 41 types)",
            MapViewMode.Elevation => "Elevation (WorldEngine gradient)",
            MapViewMode.Precipitation => "Precipitation (humidity levels)",
            MapViewMode.Temperature => "Temperature (thermal zones)",
            _ => "Unknown View"
        };

        _uiLabel.Text = $"WorldGen Test Scene\n" +
                        $"WASD: Pan | Middle Mouse: Drag | Mouse Wheel: Zoom\n" +
                        $"Keys 1-4: Switch View Mode\n\n" +
                        $"World: {_cachedWorldData.Width}x{_cachedWorldData.Height} (Seed: {Seed})\n" +
                        $"View: {viewModeText}";
    }

    /// <summary>
    /// Sets the current view mode and re-renders the map using cached data.
    /// Instant view switching (no regeneration) if world data is cached.
    /// </summary>
    /// <param name="viewMode">The view mode to switch to</param>
    public void SetViewMode(MapViewMode viewMode)
    {
        if (_cachedWorldData == null)
        {
            _logger?.LogWarning("Cannot change view mode: No world data cached. Generate a world first.");
            return;
        }

        if (_currentViewMode == viewMode)
        {
            _logger?.LogDebug("View mode already set to {ViewMode}, skipping render", viewMode);
            return;
        }

        _logger?.LogInformation("Switching view mode: {OldMode} -> {NewMode}", _currentViewMode, viewMode);
        _currentViewMode = viewMode;
        RenderCurrentView();
        UpdateUILabel(); // Refresh label to show new view mode
        UpdateLegend(); // Update legend for new view mode
    }

    /// <summary>
    /// Renders the cached world data using the current view mode.
    /// Dispatcher method that routes to appropriate rendering logic.
    /// </summary>
    private void RenderCurrentView()
    {
        if (_cachedWorldData == null)
        {
            _logger?.LogError("Cannot render: No world data cached");
            return;
        }

        if (_terrainSprite == null)
        {
            _logger?.LogError("Cannot render: Terrain sprite not initialized");
            return;
        }

        _logger?.LogDebug("Rendering map in {ViewMode} mode", _currentViewMode);

        switch (_currentViewMode)
        {
            case MapViewMode.Biomes:
                RenderBiomeMap(_cachedWorldData);
                break;

            case MapViewMode.Elevation:
                RenderElevationMap(_cachedWorldData);
                break;

            case MapViewMode.Precipitation:
                _logger?.LogWarning("Precipitation view not yet implemented (TD_010 Phase 2)");
                break;

            case MapViewMode.Temperature:
                _logger?.LogWarning("Temperature view not yet implemented (TD_010 Phase 2)");
                break;

            default:
                _logger?.LogError("Unknown view mode: {ViewMode}", _currentViewMode);
                break;
        }
    }

    /// <summary>
    /// Renders the biome classification map using Ricklefs or Holdridge color schemes.
    /// Extracts biome color for each cell and creates Image/Texture2D for GPU rendering.
    /// </summary>
    private void RenderBiomeMap(PlateSimulationResult data)
    {
        if (_terrainSprite == null)
        {
            _logger?.LogError("Cannot render biome map: Terrain sprite is null");
            return;
        }

        _logger?.LogDebug("Rendering biome map: {Width}x{Height} cells", data.Width, data.Height);

        // Create Image from biome data
        var image = Image.CreateEmpty(data.Width, data.Height, false, Image.Format.Rgb8);

        for (int y = 0; y < data.Height; y++)
        {
            for (int x = 0; x < data.Width; x++)
            {
                var biome = data.BiomeMap[y, x];

                // Choose color scheme based on toggle
                var color = UseSimplifiedBiomes
                    ? GetRicklefsColor(biome.ToRicklefs())  // 9 major categories (readable)
                    : GetHoldridgeColor(biome);              // 41 detailed biomes

                image.SetPixel(x, y, color);
            }
        }

        // Convert Image to Texture2D and assign to sprite (GPU-accelerated rendering)
        var texture = ImageTexture.CreateFromImage(image);
        _terrainSprite.Texture = texture;

        _logger?.LogDebug("Biome map texture created and assigned to sprite");
    }

    /// <summary>
    /// Renders the elevation map using WorldEngine's colored gradient algorithm.
    /// Ocean depths (dark blue) → Coastline (cyan) → Land (green/yellow/orange) → Mountains (white/pink).
    /// Port of worldengine/draw.py::_elevation_color().
    /// </summary>
    private void RenderElevationMap(PlateSimulationResult data)
    {
        if (_terrainSprite == null)
        {
            _logger?.LogError("Cannot render elevation map: Terrain sprite is null");
            return;
        }

        _logger?.LogDebug("Rendering elevation map: {Width}x{Height} cells", data.Width, data.Height);

        // Create Image from elevation data
        var image = Image.CreateEmpty(data.Width, data.Height, false, Image.Format.Rgb8);

        // Our heightmap is 0.0-1.0 normalized, but WorldEngine's _elevation_color expects:
        // - Ocean: 0.0-1.0 (sea level)
        // - Land: 1.0-12.0+ (above sea level, measured in "color step" units)
        // We need to rescale our normalized data to match WorldEngine's expected range.

        // Step 1: Calculate ocean/land statistics from ocean mask
        float minOceanElev = float.MaxValue;
        float maxOceanElev = float.MinValue;
        float minLandElev = float.MaxValue;
        float maxLandElev = float.MinValue;

        for (int y = 0; y < data.Height; y++)
        {
            for (int x = 0; x < data.Width; x++)
            {
                var elev = data.Heightmap[y, x];
                var isOcean = data.OceanMask[y, x];

                if (isOcean)
                {
                    minOceanElev = Math.Min(minOceanElev, elev);
                    maxOceanElev = Math.Max(maxOceanElev, elev);
                }
                else
                {
                    minLandElev = Math.Min(minLandElev, elev);
                    maxLandElev = Math.Max(maxLandElev, elev);
                }
            }
        }

        _logger?.LogInformation("Elevation stats: Ocean [{MinOcean:F3}-{MaxOcean:F3}], Land [{MinLand:F3}-{MaxLand:F3}]",
            minOceanElev, maxOceanElev, minLandElev, maxLandElev);

        // Calculate elevation distribution for better debugging
        var elevRange = maxLandElev - minLandElev;
        _logger?.LogInformation("Land elevation range: {Range:F3} (spread: {Percent:F1}% of heightmap)",
            elevRange, elevRange * 100);

        // Step 2: Render using WorldEngine's EXACT rescaling formula (draw.py lines 340-350)
        // Ocean: normalize to [0.0, 1.0]
        // Land: normalize to [1.0, 12.0] by dividing range by 11
        const float SeaLevel = 1.0f; // WorldEngine's sea level threshold

        var oceanRange = maxOceanElev - minOceanElev;
        var landRange = maxLandElev - minLandElev;
        var landDelta = landRange / 11.0f; // WorldEngine divides by 11 to map land to 1-12 range

        for (int y = 0; y < data.Height; y++)
        {
            for (int x = 0; x < data.Width; x++)
            {
                var elev = data.Heightmap[y, x];
                var isOcean = data.OceanMask[y, x];

                // WorldEngine's exact formula from draw.py
                float rescaledElev;
                if (isOcean && oceanRange > 0)
                {
                    // Ocean: c[ocean] = ((e[ocean] - min_elev_sea) / elev_delta_sea)
                    rescaledElev = (elev - minOceanElev) / oceanRange;
                }
                else if (!isOcean && landDelta > 0)
                {
                    // Land: c[land] = ((e[land] - min_elev_land) / elev_delta_land) + 1
                    rescaledElev = ((elev - minLandElev) / landDelta) + 1.0f;
                }
                else
                {
                    // Fallback for edge cases (flat terrain)
                    rescaledElev = isOcean ? 0.5f : 6.5f;
                }

                // Get WorldEngine color gradient (matching draw.py exactly)
                var (r, g, b) = ElevationMapColorizer.GetElevationColor(rescaledElev, SeaLevel);

                // Convert float (0.0-1.0) to Godot Color
                var color = new Color(r, g, b);
                image.SetPixel(x, y, color);
            }
        }

        // Convert Image to Texture2D and assign to sprite
        var texture = ImageTexture.CreateFromImage(image);
        _terrainSprite.Texture = texture;

        _logger?.LogDebug("Elevation map texture created and assigned to sprite");
    }

    /// <summary>
    /// Maps Ricklefs biome categories (9 types) to colors using field-tested ecology textbook palette.
    /// Reference: Robert E. Ricklefs' "The Economy of Nature" color scheme.
    /// Optimized for visual clarity at strategic map scale.
    /// </summary>
    private static Color GetRicklefsColor(BiomeCategory category) => category switch
    {
        BiomeCategory.Water => new Color(0.1f, 0.3f, 0.6f),                       // Ocean blue (custom)
        BiomeCategory.Tundra => FromHex("C1E1DD"),                                // Pale cyan
        BiomeCategory.BorealForest => FromHex("A5C790"),                          // Sage green
        BiomeCategory.TemperateSeasonalForest => FromHex("97B669"),               // Olive green
        BiomeCategory.TemperateRainForest => FromHex("75A95E"),                   // Forest green
        BiomeCategory.TropicalRainForest => FromHex("317A22"),                    // Deep green
        BiomeCategory.TropicalSeasonalForestSavanna => FromHex("A09700"),         // Olive yellow
        BiomeCategory.SubtropicalDesert => FromHex("DCBB50"),                     // Sandy yellow
        BiomeCategory.TemperateGrasslandDesert => FromHex("FCD57A"),              // Pale yellow
        BiomeCategory.WoodlandShrubland => FromHex("D16E3F"),                     // Rust orange

        _ => new Color(1f, 0f, 1f) // Magenta for unknown (debugging)
    };

    /// <summary>
    /// Maps detailed Holdridge biome types (41 types) to colors using WorldEngine's proven color scheme.
    /// Colors are from References/worldengine/docs/Biomes.html (hex codes converted to RGB).
    /// Provides maximum ecological detail but harder to distinguish at strategic scale.
    /// </summary>
    private static Color GetHoldridgeColor(BiomeType biome) => biome switch
    {
        // Water biomes
        BiomeType.Ocean => new Color(0.1f, 0.2f, 0.5f),                          // Custom: Dark blue
        BiomeType.ShallowWater => new Color(0.2f, 0.4f, 0.7f),                   // Custom: Light blue

        // Polar zone
        BiomeType.PolarIce => FromHex("FFFFFF"),                                 // White
        BiomeType.PolarDesert => FromHex("C0C0C0"),                              // Silver

        // Subpolar/Tundra zone
        BiomeType.SubpolarMoistTundra => FromHex("608080"),                      // Dark gray-green
        BiomeType.SubpolarWetTundra => FromHex("408080"),                        // Darker teal
        BiomeType.SubpolarRainTundra => FromHex("2080C0"),                       // Blue-teal
        BiomeType.SubpolarDryTundra => FromHex("808080"),                        // Gray

        // Boreal zone (cold temperate)
        BiomeType.BorealDesert => FromHex("A0A080"),                             // Pale olive
        BiomeType.BorealDryScrub => FromHex("80A080"),                           // Pale green
        BiomeType.BorealMoistForest => FromHex("60A080"),                        // Teal-green
        BiomeType.BorealWetForest => FromHex("40A090"),                          // Darker teal-green
        BiomeType.BorealRainForest => FromHex("20A0C0"),                         // Cyan-teal

        // Cool temperate zone
        BiomeType.CoolTemperateMoistForest => FromHex("60C080"),                 // Light green
        BiomeType.CoolTemperateWetForest => FromHex("40C090"),                   // Medium green
        BiomeType.CoolTemperateRainForest => FromHex("20C0C0"),                  // Cyan
        BiomeType.CoolTemperateSteppe => FromHex("80C080"),                      // Pale green (grassland)
        BiomeType.CoolTemperateDesert => FromHex("C0C080"),                      // Pale yellow-green
        BiomeType.CoolTemperateDesertScrub => FromHex("A0C080"),                 // Light yellow-green

        // Warm temperate zone
        BiomeType.WarmTemperateMoistForest => FromHex("60E080"),                 // Bright green
        BiomeType.WarmTemperateWetForest => FromHex("40E090"),                   // Emerald green
        BiomeType.WarmTemperateRainForest => FromHex("20E0C0"),                  // Cyan-green
        BiomeType.WarmTemperateThornScrub => FromHex("A0E080"),                  // Light lime
        BiomeType.WarmTemperateDryForest => FromHex("80E080"),                   // Lime green
        BiomeType.WarmTemperateDesert => FromHex("E0E080"),                      // Yellow
        BiomeType.WarmTemperateDesertScrub => FromHex("C0E080"),                 // Yellow-green

        // Subtropical zone
        BiomeType.SubtropicalThornWoodland => FromHex("B0F080"),                 // Light yellow-green
        BiomeType.SubtropicalDryForest => FromHex("80F080"),                     // Bright lime
        BiomeType.SubtropicalMoistForest => FromHex("60F080"),                   // Bright green
        BiomeType.SubtropicalWetForest => FromHex("40F090"),                     // Emerald
        BiomeType.SubtropicalRainForest => FromHex("20F0B0"),                    // Cyan-green
        BiomeType.SubtropicalDesert => FromHex("F0F080"),                        // Bright yellow
        BiomeType.SubtropicalDesertScrub => FromHex("D0F080"),                   // Yellow-lime

        // Tropical zone (hottest)
        BiomeType.TropicalThornWoodland => FromHex("C0FF80"),                    // Pale yellow-green
        BiomeType.TropicalVeryDryForest => FromHex("A0FF80"),                    // Light lime
        BiomeType.TropicalDryForest => FromHex("80FF80"),                        // Bright lime
        BiomeType.TropicalMoistForest => FromHex("60FF80"),                      // Vivid green
        BiomeType.TropicalWetForest => FromHex("40FF90"),                        // Bright emerald
        BiomeType.TropicalRainForest => FromHex("20FFA0"),                       // Jungle green
        BiomeType.TropicalDesert => FromHex("FFFF80"),                           // Bright yellow
        BiomeType.TropicalDesertScrub => FromHex("E0FF80"),                      // Yellow-lime

        _ => new Color(1f, 0f, 1f) // Magenta for unknown (debugging)
    };

    /// <summary>
    /// Converts hex color string (e.g., "FF8800") to Godot Color (RGB 0.0-1.0).
    /// </summary>
    private static Color FromHex(string hex)
    {
        int r = Convert.ToInt32(hex.Substring(0, 2), 16);
        int g = Convert.ToInt32(hex.Substring(2, 2), 16);
        int b = Convert.ToInt32(hex.Substring(4, 2), 16);
        return new Color(r / 255f, g / 255f, b / 255f);
    }

    /// <summary>
    /// Creates the visual legend panel (color swatches + labels).
    /// Positioned in bottom-right corner, updates dynamically based on view mode.
    /// </summary>
    private void CreateLegendPanel()
    {
        _legendPanel = new VBoxContainer
        {
            Name = "LegendPanel",
            Position = new Vector2(10, 400), // Bottom-left corner
        };

        // Add to UI layer (not world space)
        var uiLayer = GetParent().GetNode<CanvasLayer>("../UI");
        uiLayer?.AddChild(_legendPanel);

        UpdateLegend(); // Initialize with current view mode
    }

    /// <summary>
    /// Updates the legend panel with color swatches for current view mode.
    /// Called when view mode changes.
    /// </summary>
    private void UpdateLegend()
    {
        if (_legendPanel == null) return;

        // Clear existing legend entries
        foreach (var child in _legendPanel.GetChildren())
        {
            child.QueueFree();
        }

        // Add title
        var title = new Label
        {
            Text = $"Legend - {_currentViewMode} View",
            Theme = new Theme()
        };
        title.AddThemeFontSizeOverride("font_size", 16);
        _legendPanel.AddChild(title);

        // Add legend entries based on view mode
        switch (_currentViewMode)
        {
            case MapViewMode.Elevation:
                AddLegendEntry("Deep Ocean", new Color(0f, 0f, 0.75f));
                AddLegendEntry("Shallow Water", new Color(0f, 1f, 1f));
                AddLegendEntry("Coastal Plains", new Color(0f, 0.5f, 0f));
                AddLegendEntry("Lowlands", new Color(0f, 1f, 0f));
                AddLegendEntry("Hills", new Color(1f, 1f, 0f));
                AddLegendEntry("Uplands", new Color(1f, 0.5f, 0f));
                AddLegendEntry("Mountains", new Color(1f, 0f, 0f));
                AddLegendEntry("High Peaks", new Color(0.5f, 0.25f, 0f));
                AddLegendEntry("Snow Peaks", new Color(1f, 1f, 1f));
                break;

            case MapViewMode.Biomes:
                AddLegendEntry("Ocean", new Color(0.1f, 0.3f, 0.6f));
                AddLegendEntry("Tundra", FromHex("C1E1DD"));
                AddLegendEntry("Boreal Forest", FromHex("A5C790"));
                AddLegendEntry("Temperate Forest", FromHex("97B669"));
                AddLegendEntry("Tropical Rainforest", FromHex("317A22"));
                AddLegendEntry("Savanna", FromHex("A09700"));
                AddLegendEntry("Desert", FromHex("DCBB50"));
                AddLegendEntry("Grassland", FromHex("FCD57A"));
                break;

            case MapViewMode.Precipitation:
                AddLegendEntry("Arid (dry)", new Color(0f, 0.125f, 0.125f));
                AddLegendEntry("Semiarid", new Color(0f, 0.5f, 0.5f));
                AddLegendEntry("Humid", new Color(0f, 0.75f, 0.75f));
                AddLegendEntry("Wet", new Color(0f, 1f, 1f));
                break;

            case MapViewMode.Temperature:
                AddLegendEntry("Polar (cold)", new Color(0f, 0f, 1f));
                AddLegendEntry("Boreal", new Color(0.5f, 0f, 0.75f));
                AddLegendEntry("Cool Temperate", new Color(0.75f, 0f, 0.5f));
                AddLegendEntry("Warm", new Color(1f, 0f, 0.25f));
                AddLegendEntry("Tropical (hot)", new Color(1f, 0f, 0f));
                break;
        }
    }

    /// <summary>
    /// Adds a single legend entry (color swatch + label).
    /// </summary>
    private void AddLegendEntry(string label, Color color)
    {
        if (_legendPanel == null) return;

        var entry = new HBoxContainer();

        // Color swatch (16x16 ColorRect)
        var swatch = new ColorRect
        {
            Color = color,
            CustomMinimumSize = new Vector2(16, 16)
        };
        entry.AddChild(swatch);

        // Label text
        var labelNode = new Label
        {
            Text = label
        };
        labelNode.AddThemeFontSizeOverride("font_size", 12);
        entry.AddChild(labelNode);

        _legendPanel.AddChild(entry);
    }
}
