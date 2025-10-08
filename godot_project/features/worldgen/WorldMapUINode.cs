using Godot;
using System;
using System.Threading.Tasks;
using Darklands.Core.Application.Infrastructure;
using Darklands.Core.Features.WorldGen.Application.Commands;
using Darklands.Core.Features.WorldGen.Application.Common;
using Darklands.Core.Infrastructure.DependencyInjection;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Darklands.Features.WorldGen;

/// <summary>
/// UI controls for world map visualization.
/// Provides view mode dropdown, seed input, regenerate button, and status display.
/// Pure UI - no rendering logic.
/// </summary>
public partial class WorldMapUINode : Control
{
    private ILogger<WorldMapUINode>? _logger;
    private IMediator? _mediator;

    // UI elements
    private OptionButton? _viewModeDropdown;
    private Label? _statusLabel;
    private LineEdit? _seedInput;
    private Button? _regenerateButton;
    private Button? _saveButton;
    private Button? _loadButton;

    // State
    private int _currentSeed = 42;
    private bool _isGenerating = false;

    [Signal]
    public delegate void ViewModeChangedEventHandler(MapViewMode mode);

    [Signal]
    public delegate void RegenerateRequestedEventHandler(int seed);

    [Signal]
    public delegate void SaveRequestedEventHandler();

    [Signal]
    public delegate void LoadRequestedEventHandler();

    public override void _Ready()
    {
        // Get services
        _logger = ServiceLocator.Get<ILogger<WorldMapUINode>>();
        _mediator = ServiceLocator.Get<IMediator>();

        // Anchor to upper-right corner
        AnchorLeft = 1;
        AnchorTop = 0;
        AnchorRight = 1;
        AnchorBottom = 0;
        OffsetLeft = -230;  // Width of panel (negative to extend left from right edge)
        OffsetTop = 10;
        OffsetRight = -10;  // 10px from right edge
        OffsetBottom = 400; // Height of panel

        BuildUI();
        _logger?.LogDebug("WorldMapUINode ready");
    }

    private void BuildUI()
    {
        // Panel background for visibility
        var panel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        AddChild(panel);

        // Main container
        var container = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        panel.AddChild(container);

        // Title
        var titleLabel = new Label
        {
            Text = "World Map Viewer",
            Theme = GD.Load<Theme>("res://addons/default_theme.tres")
        };
        container.AddChild(titleLabel);

        container.AddChild(new HSeparator());

        // View mode dropdown
        var viewLabel = new Label { Text = "View Mode:" };
        container.AddChild(viewLabel);

        _viewModeDropdown = new OptionButton();

        // VS_024: Dual-heightmap elevation views (original vs post-processed)
        _viewModeDropdown.AddItem("Colored Original Elevation", (int)MapViewMode.ColoredOriginalElevation);
        _viewModeDropdown.AddItem("Colored Post-Processed Elevation", (int)MapViewMode.ColoredPostProcessedElevation);
        _viewModeDropdown.AddItem("Raw Elevation (Grayscale)", (int)MapViewMode.RawElevation);
        _viewModeDropdown.AddItem("Plates", (int)MapViewMode.Plates);

        // VS_025: 4-stage temperature debug views
        _viewModeDropdown.AddSeparator("─── Temperature Debug ───");
        _viewModeDropdown.AddItem("Temperature: 1. Latitude Only", (int)MapViewMode.TemperatureLatitudeOnly);
        _viewModeDropdown.AddItem("Temperature: 2. + Noise", (int)MapViewMode.TemperatureWithNoise);
        _viewModeDropdown.AddItem("Temperature: 3. + Distance", (int)MapViewMode.TemperatureWithDistance);
        _viewModeDropdown.AddItem("Temperature: 4. Final", (int)MapViewMode.TemperatureFinal);

        // VS_026: 3-stage precipitation debug views
        _viewModeDropdown.AddSeparator("─── Precipitation Debug ───");
        _viewModeDropdown.AddItem("Precipitation: 1. Noise Only", (int)MapViewMode.PrecipitationNoiseOnly);
        _viewModeDropdown.AddItem("Precipitation: 2. + Temp Curve", (int)MapViewMode.PrecipitationTemperatureShaped);
        _viewModeDropdown.AddItem("Precipitation: 3. Final", (int)MapViewMode.PrecipitationFinal);
        _viewModeDropdown.AddItem("Precipitation: 4. + Rain Shadow", (int)MapViewMode.PrecipitationWithRainShadow);

        _viewModeDropdown.Selected = 0;  // ColoredOriginalElevation is default
        _viewModeDropdown.ItemSelected += OnViewModeSelected;
        container.AddChild(_viewModeDropdown);

        container.AddChild(new HSeparator());

        // Seed input
        var seedContainer = new HBoxContainer();
        container.AddChild(seedContainer);

        var seedLabel = new Label { Text = "Seed:" };
        seedContainer.AddChild(seedLabel);

        _seedInput = new LineEdit
        {
            Text = _currentSeed.ToString(),
            CustomMinimumSize = new Vector2(100, 0)
        };
        _seedInput.TextSubmitted += OnSeedSubmitted;
        seedContainer.AddChild(_seedInput);

        // Regenerate button
        _regenerateButton = new Button
        {
            Text = "Regenerate"
        };
        _regenerateButton.Pressed += OnRegeneratePressed;
        container.AddChild(_regenerateButton);

        container.AddChild(new HSeparator());

        // Save/Load buttons
        var saveLoadContainer = new HBoxContainer();
        container.AddChild(saveLoadContainer);

        _saveButton = new Button
        {
            Text = "Save World",
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _saveButton.Pressed += OnSavePressed;
        saveLoadContainer.AddChild(_saveButton);

        _loadButton = new Button
        {
            Text = "Load World",
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _loadButton.Pressed += OnLoadPressed;
        saveLoadContainer.AddChild(_loadButton);

        container.AddChild(new HSeparator());

        // Status label
        _statusLabel = new Label
        {
            Text = "Ready",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(200, 0)
        };
        container.AddChild(_statusLabel);

        container.AddChild(new HSeparator());

        // Instructions
        var instructionsLabel = new Label
        {
            Text = "Controls:\nQ: Probe cell\nMiddle Mouse (hold): Pan\nMouse Wheel: Zoom",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(200, 0)
        };
        container.AddChild(instructionsLabel);
    }

    /// <summary>
    /// Updates the status label text.
    /// </summary>
    public void SetStatus(string text)
    {
        if (_statusLabel != null)
        {
            _statusLabel.Text = text;
        }
    }

    /// <summary>
    /// Shows generation progress.
    /// </summary>
    public void SetGenerating(bool isGenerating)
    {
        _isGenerating = isGenerating;
        if (_regenerateButton != null)
        {
            _regenerateButton.Disabled = isGenerating;
        }
        if (_seedInput != null)
        {
            _seedInput.Editable = !isGenerating;
        }
        if (_saveButton != null)
        {
            _saveButton.Disabled = isGenerating;
        }
        if (_loadButton != null)
        {
            _loadButton.Disabled = isGenerating;
        }
    }

    private void OnViewModeSelected(long index)
    {
        var mode = (MapViewMode)_viewModeDropdown!.GetItemId((int)index);
        _logger?.LogInformation("View mode changed to: {Mode}", mode);
        EmitSignal(SignalName.ViewModeChanged, (int)mode);
    }

    private void OnSeedSubmitted(string newText)
    {
        if (int.TryParse(newText, out int seed))
        {
            _currentSeed = seed;
            OnRegeneratePressed();
        }
        else
        {
            _logger?.LogWarning("Invalid seed: {Seed}", newText);
            _seedInput!.Text = _currentSeed.ToString();
        }
    }

    private void OnRegeneratePressed()
    {
        if (_isGenerating)
        {
            _logger?.LogWarning("Generation already in progress");
            return;
        }

        _logger?.LogInformation("Regenerate requested with seed: {Seed}", _currentSeed);
        EmitSignal(SignalName.RegenerateRequested, _currentSeed);
    }

    private void OnSavePressed()
    {
        _logger?.LogInformation("Save world requested");
        EmitSignal(SignalName.SaveRequested);
    }

    private void OnLoadPressed()
    {
        _logger?.LogInformation("Load world requested");
        EmitSignal(SignalName.LoadRequested);
    }

    /// <summary>
    /// Updates the seed input field (e.g., when world is loaded).
    /// </summary>
    public void SetSeed(int seed)
    {
        _currentSeed = seed;
        if (_seedInput != null)
        {
            _seedInput.Text = seed.ToString();
        }
    }
}
