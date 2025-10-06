using Godot;
using System;
using System.Threading.Tasks;
using Darklands.Core.Application.Infrastructure;
using Darklands.Core.Features.WorldGen.Application.Commands;
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

    // Camera control settings
    private const float PanSpeed = 500f;
    private const float ZoomSpeed = 0.1f;
    private const float MinZoom = 0.5f;
    private const float MaxZoom = 20f; // Increased from 3x to 20x for detailed inspection

    // World generation settings
    [Export] public int Seed { get; set; } = 42;
    [Export] public int WorldSize { get; set; } = 512;

    public override void _Ready()
    {
        base._Ready();

        _mediator = ServiceLocator.GetService<IMediator>().Value;
        _logger = ServiceLocator.GetService<ILogger<WorldMapNode>>().Value;

        _camera = GetParent().GetNode<Camera2D>("Camera2D");
        _uiLabel = GetNode<Label>("../UI/Label");

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

        // Mouse wheel zoom
        if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
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

        _logger.LogInformation("World generation complete, creating terrain texture {Width}x{Height}",
            result.Value.Width, result.Value.Height);

        // Create Image from biome data
        var image = Image.CreateEmpty(result.Value.Width, result.Value.Height, false, Image.Format.Rgb8);

        for (int y = 0; y < result.Value.Height; y++)
        {
            for (int x = 0; x < result.Value.Width; x++)
            {
                var biome = result.Value.BiomeMap[y, x];
                var color = GetBiomeColor(biome);
                image.SetPixel(x, y, color);
            }
        }

        // Convert Image to Texture2D and assign to sprite
        var texture = ImageTexture.CreateFromImage(image);
        _terrainSprite.Texture = texture;

        _logger.LogInformation("Terrain texture created and assigned to sprite");

        if (_uiLabel != null)
        {
            _uiLabel.Text = $"WorldGen Test Scene\nWASD: Pan camera\nMouse Wheel: Zoom\n\nWorld: {result.Value.Width}x{result.Value.Height} (Seed: {Seed})";
        }
    }

    /// <summary>
    /// Maps biome types to colors using WorldEngine's proven color scheme.
    /// Colors are from References/worldengine/docs/Biomes.html (hex codes converted to RGB).
    /// </summary>
    private static Color GetBiomeColor(BiomeType biome) => biome switch
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
}
