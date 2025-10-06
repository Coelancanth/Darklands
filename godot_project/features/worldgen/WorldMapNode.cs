using Godot;
using System.Threading.Tasks;
using Darklands.Core.Application.Infrastructure;
using Darklands.Core.Features.WorldGen.Application.Commands;
using Darklands.Core.Features.WorldGen.Domain;
using Darklands.Core.Infrastructure.DependencyInjection;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Features.WorldGen;

/// <summary>
/// Renders generated world map using TileMapLayer.
/// Handles world generation via GenerateWorldCommand and camera controls.
/// </summary>
public partial class WorldMapNode : Node2D
{
    private IMediator? _mediator;
    private ILogger<WorldMapNode>? _logger;
    private TileMapLayer? _tileMapLayer;
    private Camera2D? _camera;
    private Label? _uiLabel;

    // Camera control settings
    private const float PanSpeed = 500f;
    private const float ZoomSpeed = 0.1f;
    private const float MinZoom = 0.5f;
    private const float MaxZoom = 3f;

    // World generation settings
    [Export] public int Seed { get; set; } = 42;
    [Export] public int WorldSize { get; set; } = 512;

    public override void _Ready()
    {
        base._Ready();

        _mediator = ServiceLocator.GetService<IMediator>().Value;
        _logger = ServiceLocator.GetService<ILogger<WorldMapNode>>().Value;

        _tileMapLayer = GetNode<TileMapLayer>("TileMapLayer");
        _camera = GetParent().GetNode<Camera2D>("Camera2D");
        _uiLabel = GetNode<Label>("../UI/Label");

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
        if (_mediator == null || _logger == null || _tileMapLayer == null)
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

        _logger.LogInformation("World generation complete, rendering {Width}x{Height} tiles",
            result.Value.Width, result.Value.Height);

        RenderWorld(result.Value);

        if (_uiLabel != null)
        {
            _uiLabel.Text = $"WorldGen Test Scene\nWASD: Pan camera\nMouse Wheel: Zoom\n\nWorld: {result.Value.Width}x{result.Value.Height} (Seed: {Seed})";
        }
    }

    private void RenderWorld(Darklands.Core.Features.WorldGen.Application.DTOs.PlateSimulationResult world)
    {
        if (_tileMapLayer == null)
        {
            _logger?.LogError("TileMapLayer not found");
            return;
        }

        _logger?.LogInformation("Rendering world with biome-based colors");

        // Render using biome types (skip placeholder tileset for MVP - use modulate colors)
        for (int y = 0; y < world.Height; y++)
        {
            for (int x = 0; x < world.Width; x++)
            {
                var biome = world.BiomeMap[y, x];
                var color = GetBiomeColor(biome);

                // For MVP: Use single tile ID (0) with modulate colors
                // Future: Create proper tileset with biome sprites
                _tileMapLayer.SetCell(new Vector2I(x, y), sourceId: 0, atlasCoords: Vector2I.Zero);

                // Apply biome color via modulation (requires tile to exist first)
                var tileData = _tileMapLayer.GetCellTileData(new Vector2I(x, y));
                if (tileData != null)
                {
                    tileData.Modulate = color;
                }
            }
        }

        _logger?.LogInformation("World rendering complete");
    }

    private static Color GetBiomeColor(BiomeType biome) => biome switch
    {
        BiomeType.Ocean => new Color(0.1f, 0.2f, 0.5f),              // Dark blue
        BiomeType.ShallowWater => new Color(0.2f, 0.4f, 0.7f),       // Light blue
        BiomeType.Ice => new Color(0.9f, 0.9f, 1.0f),                 // White
        BiomeType.Tundra => new Color(0.6f, 0.7f, 0.6f),             // Gray-green
        BiomeType.BorealForest => new Color(0.2f, 0.5f, 0.3f),       // Dark green
        BiomeType.Grassland => new Color(0.5f, 0.7f, 0.3f),          // Yellow-green
        BiomeType.TemperateForest => new Color(0.3f, 0.6f, 0.3f),    // Green
        BiomeType.TemperateRainforest => new Color(0.2f, 0.5f, 0.4f),// Teal
        BiomeType.Desert => new Color(0.9f, 0.8f, 0.5f),             // Sand
        BiomeType.Savanna => new Color(0.7f, 0.6f, 0.4f),            // Tan
        BiomeType.TropicalSeasonalForest => new Color(0.4f, 0.7f, 0.3f), // Light green
        BiomeType.TropicalRainforest => new Color(0.1f, 0.5f, 0.2f), // Dark jungle green
        _ => new Color(1f, 0f, 1f) // Magenta for unknown
    };
}
