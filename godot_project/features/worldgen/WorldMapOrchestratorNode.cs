using Godot;
using System.Threading.Tasks;
using Darklands.Core.Application.Infrastructure;
using Darklands.Core.Features.WorldGen.Application.Commands;
using Darklands.Core.Features.WorldGen.Application.Common;
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

    // Initial seed
    [Export]
    public int InitialSeed { get; set; } = 12345;

    public override void _Ready()
    {
        // Get services
        _logger = ServiceLocator.Get<ILogger<WorldMapOrchestratorNode>>();
        _mediator = ServiceLocator.Get<IMediator>();

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

        // Connect UI signals
        if (_ui != null)
        {
            _ui.ViewModeChanged += OnViewModeChanged;
            _ui.RegenerateRequested += OnRegenerateRequested;
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
        _ui?.SetStatus($"Generating world (seed: {seed})...");

        _logger?.LogInformation("Starting world generation: seed={Seed}", seed);

        var command = new GenerateWorldCommand(seed, worldSize: 512, plateCount: 10);

        var result = await _mediator.Send(command);

        if (result.IsSuccess)
        {
            _logger?.LogInformation("World generation succeeded: {Width}x{Height}",
                result.Value.Width, result.Value.Height);

            // Send data to renderer
            _renderer?.SetWorldData(result.Value, ServiceLocator.Get<ILogger<WorldMapRendererNode>>());

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
