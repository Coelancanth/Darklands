using Godot;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Darklands.Core.Application.Infrastructure;
using Darklands.Core.Features.WorldGen.Application.Commands;
using Darklands.Core.Features.WorldGen.Application.Common;
using Darklands.Core.Features.WorldGen.Application.DTOs;
using Darklands.Core.Features.WorldGen.Application.Rendering;
using Darklands.Core.Features.WorldGen.Domain;
using Darklands.Core.Features.WorldGen.Infrastructure.Algorithms;
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
    private HashSet<(int x, int y)> _riverCells = new HashSet<(int x, int y)>();
    private HashSet<(int x, int y)> _lakeCells = new HashSet<(int x, int y)>();
    private ProbeHighlighter? _probeHighlighter;

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

        // Highlighter overlay
        _probeHighlighter = new ProbeHighlighter
        {
            Name = "ProbeHighlighter",
            ZAsRelative = false,
            ZIndex = 1000
        };
        AddChild(_probeHighlighter);

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
                case Key.Key5:
                    SetViewMode(MapViewMode.RawElevation);
                    break;
                case Key.Key6:
                    SetViewMode(MapViewMode.Plates);
                    break;
                case Key.Key7:
                    SetViewMode(MapViewMode.RawElevationColored);
                    break;
                case Key.P:
                    ProbeUnderMouse();
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

        // Build quick-lookup sets for rivers/lakes to support probing
        _riverCells.Clear();
        _lakeCells.Clear();
        foreach (var river in _cachedWorldData.Rivers)
        {
            foreach (var pt in river.Path)
            {
                _riverCells.Add(pt);
            }
        }
        foreach (var lake in _cachedWorldData.Lakes)
        {
            _lakeCells.Add(lake);
        }

        // Render using current view mode
        RenderCurrentView();

        // Update UI label with world info
        UpdateUILabel();

        // Update legend to show current view mode colors
        UpdateLegend();
    }

    /// <summary>
    /// Logs detailed worldgen data for the cell under the mouse cursor.
    /// Press 'P' to probe.
    /// </summary>
    private void ProbeUnderMouse()
    {
        if (_cachedWorldData == null || _terrainSprite == null || _logger == null)
            return;

        // Convert global mouse position to sprite-local pixel coordinates
        var mouseGlobal = GetGlobalMousePosition();
        var local = _terrainSprite.ToLocal(mouseGlobal);

        int x = (int)MathF.Floor(local.X);
        int y = (int)MathF.Floor(local.Y);

        if (x < 0 || y < 0 || x >= _cachedWorldData.Width || y >= _cachedWorldData.Height)
        {
            _logger.LogInformation("Probe: outside map at ({X},{Y})", x, y);
            _probeHighlighter?.SetRect(new Rect2());
            return;
        }

        bool isOcean = _cachedWorldData.OceanMask[y, x];
        float elev = _cachedWorldData.Heightmap[y, x];
        float temp = _cachedWorldData.TemperatureMap[y, x];
        float precip = _cachedWorldData.PrecipitationMap[y, x];
        float humidity = _cachedWorldData.HumidityMap[y, x];
        float irrigation = _cachedWorldData.IrrigationMap[y, x];
        float watermap = _cachedWorldData.WatermapData[y, x];
        var biome = _cachedWorldData.BiomeMap[y, x];
        bool inRiver = _riverCells.Contains((x, y));
        bool inLake = _lakeCells.Contains((x, y));

        _logger.LogInformation(
            "Probe ({X},{Y}) | ocean={Ocean} elev={Elev:F3} temp={Temp:F3} precip={Precip:F3} humidity={Hum:F3} irr={Irr:F3} watermap={Water:F3} biome={Biome} river_cell={River} lake_cell={Lake}",
            x, y, isOcean, elev, temp, precip, humidity, irrigation, watermap, biome, inRiver, inLake);

        _probeHighlighter?.SetRect(new Rect2(x, y, 1, 1));
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
            MapViewMode.RawElevation => "Raw Elevation (native heightmap grayscale)",
            MapViewMode.Plates => "Plates (plate ownership)",
            MapViewMode.RawElevationColored => "Raw Elevation (WorldEngine colors)",
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
                RenderPrecipitationMap(_cachedWorldData);
                break;

            case MapViewMode.Temperature:
                RenderTemperatureMap(_cachedWorldData);
                break;

            case MapViewMode.RawElevation:
                RenderRawElevationMap(_cachedWorldData);
                break;

            case MapViewMode.Plates:
                RenderPlatesMap(_cachedWorldData);
                break;
            case MapViewMode.RawElevationColored:
                RenderRawElevationColoredMap(_cachedWorldData);
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

        // WorldEngine draw_simple_elevation exact rescaling:
        // - If there is ocean: c[ocean] = (e - minSea) / (maxSea - minSea)
        //                      c[land]  = (e - minLand) / ((maxLand - minLand) / 11) + 1
        // - Else:               c        = (e - minLand) / ((maxLand - minLand) / 11) + 1

        bool hasOcean = false;
        for (int y = 0; y < data.Height && !hasOcean; y++)
            for (int x = 0; x < data.Width && !hasOcean; x++)
                if (data.OceanMask[y, x]) hasOcean = true;

        float minLand = float.PositiveInfinity;
        float maxLand = float.NegativeInfinity;
        float minSea = float.PositiveInfinity;
        float maxSea = float.NegativeInfinity;

        for (int y = 0; y < data.Height; y++)
        {
            for (int x = 0; x < data.Width; x++)
            {
                float e = data.Heightmap[y, x];
                if (data.OceanMask[y, x])
                {
                    if (e < minSea) minSea = e;
                    if (e > maxSea) maxSea = e;
                }
                else
                {
                    if (e < minLand) minLand = e;
                    if (e > maxLand) maxLand = e;
                }
            }
        }

        float elevDeltaSea = Math.Max(1e-6f, maxSea - minSea);
        float elevDeltaLand = Math.Max(1e-6f, (maxLand - minLand) / 11.0f);

        _logger?.LogInformation("Elevation stats (WE rescale): Ocean[{MinS:F3}-{MaxS:F3}] ΔS={DS:F3}, Land[{MinL:F3}-{MaxL:F3}] ΔL/11={DL:F3}",
            minSea, maxSea, elevDeltaSea, minLand, maxLand, elevDeltaLand);

        const float SeaLevel = 1.0f; // WorldEngine uses 1.0 threshold after rescaling

        for (int y = 0; y < data.Height; y++)
        {
            for (int x = 0; x < data.Width; x++)
            {
                float e = data.Heightmap[y, x];
                bool isOcean = data.OceanMask[y, x];

                float c;
                if (hasOcean)
                {
                    if (isOcean)
                    {
                        c = (e - minSea) / elevDeltaSea; // [0,1]
                    }
                    else
                    {
                        c = ((e - minLand) / elevDeltaLand) + 1.0f; // [1,12]
                    }
                }
                else
                {
                    c = ((e - minLand) / elevDeltaLand) + 1.0f;
                }

                var (r, g, b) = ElevationMapColorizer.GetElevationColor(c, SeaLevel);
                image.SetPixel(x, y, new Color(r, g, b));
            }
        }

        var texture = ImageTexture.CreateFromImage(image);
        _terrainSprite.Texture = texture;
        _logger?.LogDebug("Elevation map texture created and assigned to sprite");
    }

    // Render humidity (precipitation) view using WorldEngine quantiles/colors
    private void RenderPrecipitationMap(PlateSimulationResult data)
    {
        if (_terrainSprite == null)
        {
            _logger?.LogError("Cannot render precipitation map: Terrain sprite is null");
            return;
        }
        _logger?.LogDebug("Rendering precipitation map: {Width}x{Height} cells", data.Width, data.Height);
        var image = Image.CreateEmpty(data.Width, data.Height, false, Image.Format.Rgb8);
        var q = ComputeHumidityQuantiles(data.HumidityMap, data.OceanMask);
        for (int y = 0; y < data.Height; y++)
        {
            for (int x = 0; x < data.Width; x++)
            {
                if (data.OceanMask[y, x])
                {
                    image.SetPixel(x, y, new Color(0f, 0f, 1f));
                }
                else
                {
                    image.SetPixel(x, y, GetHumidityColor(data.HumidityMap[y, x], q));
                }
            }
        }
        var texture = ImageTexture.CreateFromImage(image);
        _terrainSprite.Texture = texture;
        _logger?.LogDebug("Precipitation map texture created and assigned to sprite");
    }

    // Render temperature bands using fixed thresholds
    private void RenderTemperatureMap(PlateSimulationResult data)
    {
        if (_terrainSprite == null)
        {
            _logger?.LogError("Cannot render temperature map: Terrain sprite is null");
            return;
        }
        _logger?.LogDebug("Rendering temperature map: {Width}x{Height} cells", data.Width, data.Height);
        var image = Image.CreateEmpty(data.Width, data.Height, false, Image.Format.Rgb8);
        float[] t = new float[] { 0.124f, 0.366f, 0.439f, 0.594f, 0.765f, 0.874f };
        for (int y = 0; y < data.Height; y++)
        {
            for (int x = 0; x < data.Width; x++)
            {
                image.SetPixel(x, y, GetTemperatureColor(data.TemperatureMap[y, x], t));
            }
        }
        var texture = ImageTexture.CreateFromImage(image);
        _terrainSprite.Texture = texture;
        _logger?.LogDebug("Temperature map texture created and assigned to sprite");
    }

    // Render the raw heightmap straight from native simulation (grayscale)
    private void RenderRawElevationMap(PlateSimulationResult data)
    {
        if (_terrainSprite == null)
        {
            _logger?.LogError("Cannot render raw elevation map: Terrain sprite is null");
            return;
        }

        var raw = data.RawHeightmap ?? data.Heightmap; // fallback if not present
        int h = raw.GetLength(0), w = raw.GetLength(1);
        var image = Image.CreateEmpty(w, h, false, Image.Format.Rgb8);

        float min = float.MaxValue, max = float.MinValue;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float v = raw[y, x];
                if (v < min) min = v; if (v > max) max = v;
            }
        float delta = Math.Max(1e-6f, max - min);

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float t = (raw[y, x] - min) / delta;
                image.SetPixel(x, y, new Color(t, t, t));
            }

        _terrainSprite.Texture = ImageTexture.CreateFromImage(image);
    }

    // Render tectonic plates ownership map (categorical colors per plate id)
    private void RenderPlatesMap(PlateSimulationResult data)
    {
        if (_terrainSprite == null)
        {
            _logger?.LogError("Cannot render plates map: Terrain sprite is null");
            return;
        }

        if (data.PlatesMap == null)
        {
            _logger?.LogWarning("No plates map available on result; did native sim return plates?");
        }

        int h = data.Height;
        int w = data.Width;
        var image = Image.CreateEmpty(w, h, false, Image.Format.Rgb8);
        var rng = new Random(12345);

        // Generate deterministic color for each plate id encountered
        var colorCache = new Dictionary<uint, Color>();
        Color ColorFor(uint id)
        {
            if (colorCache.TryGetValue(id, out var c)) return c;
            // Simple hash to color
            rng = new Random((int)id * 2654435761u.GetHashCode());
            c = new Color((float)rng.NextDouble(), (float)rng.NextDouble(), (float)rng.NextDouble());
            colorCache[id] = c;
            return c;
        }

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                uint id = data.PlatesMap?[y, x] ?? 0u;
                image.SetPixel(x, y, ColorFor(id));
            }

        _terrainSprite.Texture = ImageTexture.CreateFromImage(image);
    }

    // Render the raw elevation with same WorldEngine colorization as Elevation view
    private void RenderRawElevationColoredMap(PlateSimulationResult data)
    {
        if (_terrainSprite == null)
        {
            _logger?.LogError("Cannot render raw elevation colored map: Terrain sprite is null");
            return;
        }

        var raw = data.RawHeightmap ?? data.Heightmap;
        int h = raw.GetLength(0), w = raw.GetLength(1);
        var image = Image.CreateEmpty(w, h, false, Image.Format.Rgb8);

        // Compute WE rescale stats with processed ocean mask but raw elevations
        bool hasOcean = false;
        for (int y = 0; y < h && !hasOcean; y++)
            for (int x = 0; x < w && !hasOcean; x++)
                if (data.OceanMask[y, x]) hasOcean = true;

        float minLand = float.PositiveInfinity, maxLand = float.NegativeInfinity;
        float minSea = float.PositiveInfinity, maxSea = float.NegativeInfinity;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float e = raw[y, x];
                if (data.OceanMask[y, x]) { if (e < minSea) minSea = e; if (e > maxSea) maxSea = e; }
                else { if (e < minLand) minLand = e; if (e > maxLand) maxLand = e; }
            }
        float elevDeltaSea = Math.Max(1e-6f, maxSea - minSea);
        float elevDeltaLand = Math.Max(1e-6f, (maxLand - minLand) / 11.0f);

        const float SeaLevel = 1.0f;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float e = raw[y, x];
                bool isOcean = data.OceanMask[y, x];
                float c;
                if (hasOcean)
                {
                    if (isOcean) c = (e - minSea) / elevDeltaSea; else c = ((e - minLand) / elevDeltaLand) + 1.0f;
                }
                else c = ((e - minLand) / elevDeltaLand) + 1.0f;
                var (r, g, b) = ElevationMapColorizer.GetElevationColor(c, SeaLevel);
                image.SetPixel(x, y, new Color(r, g, b));
            }

        _terrainSprite.Texture = ImageTexture.CreateFromImage(image);
    }

    private static Color GetTemperatureColor(float v, float[] th)
    {
        if (v < th[0]) return new Color(0f, 0f, 1f);
        if (v < th[1]) return FromByte(42, 0, 213);
        if (v < th[2]) return FromByte(85, 0, 170);
        if (v < th[3]) return FromByte(128, 0, 128);
        if (v < th[4]) return FromByte(170, 0, 85);
        if (v < th[5]) return FromByte(213, 0, 42);
        return FromByte(255, 0, 0);
    }

    private static Color GetHumidityColor(float h, (float q12, float q25, float q37, float q50, float q62, float q75, float q87) q)
    {
        if (h < q.q87) return FromByte(0, 32, 32);
        if (h < q.q75) return FromByte(0, 64, 64);
        if (h < q.q62) return FromByte(0, 96, 96);
        if (h < q.q50) return FromByte(0, 128, 128);
        if (h < q.q37) return FromByte(0, 160, 160);
        if (h < q.q25) return FromByte(0, 192, 192);
        if (h < q.q12) return FromByte(0, 224, 224);
        return FromByte(0, 255, 255);
    }

    private static (float q12, float q25, float q37, float q50, float q62, float q75, float q87) ComputeHumidityQuantiles(float[,] humidity, bool[,] ocean)
    {
        int h = humidity.GetLength(0);
        int w = humidity.GetLength(1);
        var land = new List<float>(h * w);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (!ocean[y, x]) land.Add(humidity[y, x]);
        land.Sort();
        float P(float p)
        {
            if (land.Count == 0) return 0f;
            int idx = Math.Clamp((int)(land.Count * p), 0, land.Count - 1);
            return land[idx];
        }
        return (P(0.002f), P(0.014f), P(0.073f), P(0.236f), P(0.507f), P(0.778f), P(0.941f));
    }

    private static Color FromByte(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f);

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
            Position = new Vector2(10, 120), // Below header label, top-left corner
        };

        // Add to UI layer (not world space)
        var uiLayer = GetParent().GetNode<CanvasLayer>("UI");
        uiLayer?.AddChild(_legendPanel);
        // Make legend larger for readability regardless of zoom
        _legendPanel.Scale = new Vector2(1.6f, 1.6f);

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
        title.AddThemeFontSizeOverride("font_size", 24);
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
                AddLegendEntry("Temperate Seasonal Forest", FromHex("97B669"));
                AddLegendEntry("Temperate Rain Forest", FromHex("75A95E"));
                AddLegendEntry("Tropical Rain Forest", FromHex("317A22"));
                AddLegendEntry("Tropical Seasonal Forest / Savanna", FromHex("A09700"));
                AddLegendEntry("Subtropical Desert", FromHex("DCBB50"));
                AddLegendEntry("Temperate Grassland / Cold Desert", FromHex("FCD57A"));
                AddLegendEntry("Woodland / Shrubland", FromHex("D16E3F"));
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
            case MapViewMode.RawElevation:
                AddLegendEntry("Low (black)", new Color(0f, 0f, 0f));
                AddLegendEntry("High (white)", new Color(1f, 1f, 1f));
                break;
            case MapViewMode.Plates:
                AddLegendEntry("Distinct color = plate ID", new Color(0.8f, 0.8f, 0.8f));
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
            CustomMinimumSize = new Vector2(24, 24)
        };
        entry.AddChild(swatch);

        // Label text
        var labelNode = new Label
        {
            Text = label
        };
        labelNode.AddThemeFontSizeOverride("font_size", 18);
        entry.AddChild(labelNode);

        _legendPanel.AddChild(entry);
    }

    // Simple overlay to highlight a probed cell
    private partial class ProbeHighlighter : Node2D
    {
        private Rect2 _rect;
        public void SetRect(Rect2 rect)
        {
            _rect = rect;
            QueueRedraw();
        }
        public override void _Draw()
        {
            if (_rect.Size.X <= 0 || _rect.Size.Y <= 0) return;
            var border = new Color(1f, 1f, 0f, 1f);
            var fill = new Color(1f, 1f, 0f, 0.15f);
            DrawRect(_rect, fill, filled: true);
            DrawRect(_rect, border, filled: false, width: 2);
        }
    }
}
