using Godot;
using System.Threading.Tasks;
using Darklands.Core.Application.Infrastructure;
using Darklands.Core.Features.WorldGen.Application.Commands;
using Darklands.Core.Features.WorldGen.Application.Common;
using Darklands.Core.Features.WorldGen.Application.DTOs;
using Darklands.Core.Infrastructure.DependencyInjection;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Features.WorldGen;

/// <summary>
/// Orchestrates the world map visualization system.
/// Wires together Renderer, Probe, UI, and Legend nodes.
/// Handles world generation via MediatR commands.
/// </summary>
public partial class WorldMapOrchestratorNode : Node
{
    // Child nodes (set via scene tree or dynamically)
    private WorldMapRendererNode? _renderer;
    private WorldMapProbeNode? _probe;
    private WorldMapUINode? _ui;
    private WorldMapLegendNode? _legend;
    private WorldMapCameraNode? _cameraController;
    private Camera2D? _camera;

    // Services
    private ILogger<WorldMapOrchestratorNode>? _logger;
    private IMediator? _mediator;
    private WorldMapSerializationService? _serializationService;

    // State
    private WorldGenerationResult? _currentWorld;
    private int _currentSeed = 42;

    // Initial seed
    [Export]
    public int InitialSeed { get; set; } = 42;

    public override void _Ready()
    {
        // Get services
        _logger = ServiceLocator.Get<ILogger<WorldMapOrchestratorNode>>();
        _mediator = ServiceLocator.Get<IMediator>();
        _serializationService = new WorldMapSerializationService(
            ServiceLocator.Get<ILogger<WorldMapSerializationService>>());

        // Find child nodes
        _renderer = GetNode<WorldMapRendererNode>("Renderer");
        _probe = GetNode<WorldMapProbeNode>("Probe");
        _cameraController = GetNode<WorldMapCameraNode>("CameraController");

        // Camera is sibling of Orchestrator
        _camera = GetNode<Camera2D>("../Camera2D");

        // UI nodes are in the UILayer (sibling of Orchestrator)
        _ui = GetNode<WorldMapUINode>("../UILayer/UI");
        _legend = GetNode<WorldMapLegendNode>("../UILayer/Legend");

        // Wire up connections
        WireNodes();

        // Generate initial world
        _ = GenerateWorldAsync(InitialSeed);

        _logger?.LogInformation("WorldMapOrchestratorNode ready");
    }

    private void WireNodes()
    {
        // Connect probe to renderer
        if (_probe != null && _renderer != null)
        {
            _probe.SetRenderer(_renderer, ServiceLocator.Get<ILogger<WorldMapProbeNode>>());
        }

        // Connect probe to camera controller (to detect pan mode)
        if (_probe != null && _cameraController != null)
        {
            _probe.SetCameraController(_cameraController);
        }

        // Connect UI signals
        if (_ui != null)
        {
            _ui.ViewModeChanged += OnViewModeChanged;
            _ui.RegenerateRequested += OnRegenerateRequested;
            _ui.SaveRequested += OnSaveRequested;
            _ui.LoadRequested += OnLoadRequested;
        }

        // Connect probe signal to UI
        if (_probe != null && _ui != null)
        {
            _probe.CellProbed += OnCellProbed;
        }

        // Set logger for legend
        if (_legend != null)
        {
            _legend.SetLogger(ServiceLocator.Get<ILogger<WorldMapLegendNode>>());
        }

        // Initialize camera controller
        if (_cameraController != null && _camera != null)
        {
            _cameraController.Initialize(_camera, ServiceLocator.Get<ILogger<WorldMapCameraNode>>());
        }

        _logger?.LogDebug("Nodes wired together");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Event Handlers
    // ═══════════════════════════════════════════════════════════════════════

    private void OnViewModeChanged(MapViewMode mode)
    {
        _logger?.LogInformation("View mode changed to: {Mode}", mode);

        _renderer?.SetViewMode(mode);
        _legend?.UpdateForViewMode(mode);
        _probe?.UpdateHighlightColor(mode); // Update highlight color for contrast
    }

    private void OnRegenerateRequested(int seed)
    {
        _logger?.LogInformation("Regenerate requested with seed: {Seed}", seed);
        _cameraController?.ResetCamera();
        _ = GenerateWorldAsync(seed);
    }

    private void OnCellProbed(int x, int y, string probeData)
    {
        _ui?.SetStatus(probeData);
    }

    private void OnSaveRequested()
    {
        if (_currentWorld == null)
        {
            _logger?.LogWarning("Cannot save: No world loaded");
            _ui?.SetStatus("Error: No world to save");
            return;
        }

        string filename = $"world_{_currentSeed}.dwld";
        // Save raw native output (serialization uses PlateSimulationResult)
        bool success = _serializationService!.SaveWorld(_currentWorld.RawNativeOutput, _currentSeed, filename);

        if (success)
        {
            _ui?.SetStatus($"Saved: {filename}");
        }
        else
        {
            _ui?.SetStatus("Save failed (check logs)");
        }
    }

    private void OnLoadRequested()
    {
        // Simple approach: Load most recent save
        // Future: Add file picker UI
        var savedFiles = _serializationService!.ListSavedWorlds();
        if (savedFiles.Length == 0)
        {
            _logger?.LogWarning("No saved worlds found");
            _ui?.SetStatus("No saved worlds found");
            return;
        }

        // Load first file (alphabetically)
        string filename = savedFiles[0];
        var (success, world, seed) = _serializationService.LoadWorld(filename);

        if (success && world != null)
        {
            // Wrap loaded PlateSimulationResult into WorldGenerationResult (no post-processing)
            _currentWorld = new WorldGenerationResult(
                heightmap: world.Heightmap,
                platesMap: world.PlatesMap,
                rawNativeOutput: world,
                oceanMask: null,
                temperatureMap: null,
                precipitationMap: null
            );
            _currentSeed = seed;

            // Update renderer and UI
            _renderer?.SetWorldData(world, ServiceLocator.Get<ILogger<WorldMapRendererNode>>());
            _ui?.SetSeed(seed);
            _ui?.SetStatus($"Loaded: {filename} (seed: {seed})");

            _logger?.LogInformation("Loaded world from file: {Filename}", filename);
        }
        else
        {
            _ui?.SetStatus("Load failed (check logs)");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // World Generation
    // ═══════════════════════════════════════════════════════════════════════

    private async Task GenerateWorldAsync(int seed)
    {
        if (_mediator == null)
        {
            _logger?.LogError("Cannot generate world: IMediator not available");
            return;
        }

        _ui?.SetGenerating(true);

        // ═══════════════════════════════════════════════════════════════════════
        // STEP 1: Try auto-load from cache (if exists)
        // ═══════════════════════════════════════════════════════════════════════

        string cacheFilename = $"world_{seed}.dwld";
        var (loadSuccess, cachedWorld, cachedSeed) = _serializationService!.LoadWorld(cacheFilename);

        if (loadSuccess && cachedWorld != null)
        {
            _logger?.LogInformation("Auto-loaded world from cache: seed={Seed} (skipped generation)", seed);

            // Wrap cached result
            _currentWorld = new WorldGenerationResult(
                heightmap: cachedWorld.Heightmap,
                platesMap: cachedWorld.PlatesMap,
                rawNativeOutput: cachedWorld,
                oceanMask: null,
                temperatureMap: null,
                precipitationMap: null
            );
            _currentSeed = seed;

            // Update renderer
            _renderer?.SetWorldData(cachedWorld, ServiceLocator.Get<ILogger<WorldMapRendererNode>>());

            // Sync legend and probe
            if (_renderer != null && _legend != null)
            {
                _legend.UpdateForViewMode(_renderer.GetCurrentViewMode());
            }
            if (_renderer != null && _probe != null)
            {
                _probe.UpdateHighlightColor(_renderer.GetCurrentViewMode());
            }

            _ui?.SetSeed(seed);
            _ui?.SetStatus($"Loaded from cache: {cachedWorld.Width}x{cachedWorld.Height} (instant)");
            _ui?.SetGenerating(false);
            return; // Skip generation!
        }

        // ═══════════════════════════════════════════════════════════════════════
        // STEP 2: Cache miss - Generate new world
        // ═══════════════════════════════════════════════════════════════════════

        _ui?.SetStatus($"Generating world (seed: {seed})...");
        _logger?.LogInformation("Cache miss - starting world generation: seed={Seed}", seed);

        var command = new GenerateWorldCommand(seed, worldSize: 512, plateCount: 10);
        var result = await _mediator.Send(command);

        if (result.IsSuccess)
        {
            _logger?.LogInformation("World generation succeeded: {Width}x{Height}",
                result.Value.Width, result.Value.Height);

            // Store current world and seed
            _currentWorld = result.Value;
            _currentSeed = seed;

            // Send raw native output to renderer
            _renderer?.SetWorldData(result.Value.RawNativeOutput, ServiceLocator.Get<ILogger<WorldMapRendererNode>>());

            // Auto-save to cache for next time
            bool saveSuccess = _serializationService.SaveWorld(result.Value.RawNativeOutput, seed, cacheFilename);
            if (saveSuccess)
            {
                _logger?.LogInformation("Auto-saved world to cache: {Filename}", cacheFilename);
            }

            _ui?.SetSeed(seed);
            _ui?.SetStatus($"World generated: {result.Value.Width}x{result.Value.Height}");
        }
        else
        {
            _logger?.LogError("World generation failed: {Error}", result.Error);
            _ui?.SetStatus($"Generation failed: {result.Error}");
        }

        _ui?.SetGenerating(false);
    }
}
