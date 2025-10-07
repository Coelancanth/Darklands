using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;
using Microsoft.Extensions.Logging;
using Darklands.Core.Infrastructure.DependencyInjection;
using Darklands.Infrastructure.Logging;

namespace Darklands.Infrastructure;

/// <summary>
/// Global debug console controller (Autoload singleton).
///
/// Press F12 to toggle category filter controls for Godot Output panel logging.
/// This is developer infrastructure - no in-game log display (future feature).
///
/// Architecture:
/// - CanvasLayer ensures rendering above all scenes (layer 100)
/// - Lazy initialization on first F12 press (no overhead until needed)
/// - Scene-based UI (DebugConsole.tscn - visual editing, hot-reload)
/// - Resource-based config (DebugConsoleConfig.tres - designer defaults)
/// - Hybrid config priority: JSON user state → .tres defaults → hardcoded fallback
/// - Graceful degradation if LoggingService unavailable
/// - Persistence via user://debug_console_state.json (survives restarts)
///
/// Usage: Registered as autoload in project.godot, works globally in all scenes.
/// </summary>
public partial class DebugConsoleController : CanvasLayer
{
    private const string StateFilePath = "user://debug_console_state.json";
    private const string ScenePath = "res://godot_project/infrastructure/DebugConsole.tscn";

    private Control? _container;
    private VBoxContainer? _categoryFiltersContainer;
    private OptionButton? _logLevelDropdown;
    private LoggingService? _loggingService;
    private Serilog.Core.LoggingLevelSwitch? _levelSwitch;
    private Microsoft.Extensions.Logging.ILogger? _logger;
    private bool _initialized = false;

    public override void _Ready()
    {
        // Set high layer to render above all game content
        Layer = 100;

        // CRITICAL: Allow this node to process input even when game is paused
        // This lets debug console work while it pauses the game
        ProcessMode = ProcessModeEnum.Always;

        // Note: Can't log here - DI container not initialized yet (autoload runs before GameStrapper)
        // But we can read persisted state file to show what settings will be applied
        var stateInfo = GetPersistedStateInfo();
        GD.Print($"Debug Console ready (F12 to toggle) | {stateInfo}");
    }

    /// <summary>
    /// Handle F12 input globally across all scenes.
    /// Uses _UnhandledInput to only trigger if no other node handled it.
    /// </summary>
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent &&
            keyEvent.Pressed &&
            !keyEvent.IsEcho() &&
            keyEvent.Keycode == Key.F12)
        {
            // Lazy initialization on first F12 press
            if (!_initialized)
            {
                InitializeDebugConsole();
            }

            // Toggle visibility
            if (_container != null)
            {
                _container.Visible = !_container.Visible;

                // Block/unblock game input when console is visible
                _container.MouseFilter = _container.Visible
                    ? Control.MouseFilterEnum.Stop
                    : Control.MouseFilterEnum.Ignore;

                // CRITICAL: Pause game tree when console visible to block all game input
                GetTree().Paused = _container.Visible;

                // Move to front of input processing order
                if (_container.Visible)
                {
                    // Grab focus to ensure input reaches our controls first
                    var panel = _container.GetNodeOrNull<Control>("Panel");
                    if (panel != null)
                    {
                        panel.GrabFocus();
                    }
                }

                GD.Print($"Debug Console: {(_container.Visible ? "VISIBLE" : "HIDDEN")}");
            }

            // Mark as handled to prevent propagation
            GetViewport().SetInputAsHandled();
        }
    }

    /// <summary>
    /// Lazy initialization - only runs on first F12 press.
    /// Loads scene, resource config, and wires up category filters.
    ///
    /// WHY LAZY: Autoloads run before GameStrapper initializes DI container.
    /// Deferring until first use ensures ServiceLocator is ready.
    ///
    /// CONFIG LOADING:
    /// - JSON user state (user://debug_console_state.json) loads runtime preferences
    /// - If no JSON exists, uses hardcoded defaults (Information level, all categories)
    /// </summary>
    private void InitializeDebugConsole()
    {
        GD.Print("\n=== Initializing Debug Console (Lazy) ===");

        // Try to get logger from DI container first
        var loggerResult = ServiceLocator.GetService<Microsoft.Extensions.Logging.ILogger<DebugConsoleController>>();
        if (loggerResult.IsSuccess)
        {
            _logger = loggerResult.Value;
            _logger.LogInformation("Debug Console lazy initialization started");
        }

        // Try to get LoggingService from DI container
        // This will fail gracefully if GameStrapper hasn't initialized yet
        var loggingServiceResult = ServiceLocator.GetService<LoggingService>();
        if (loggingServiceResult.IsFailure)
        {
            var errorMsg = $"Failed to resolve LoggingService: {loggingServiceResult.Error}";
            if (_logger != null)
            {
                _logger.LogError("{Message}. Make sure GameStrapper.Initialize() has been called in your scene", errorMsg);
            }
            else
            {
                GD.PrintErr($"❌ {errorMsg}");
                GD.PrintErr("   Make sure GameStrapper.Initialize() has been called in your scene");
            }
            _initialized = true; // Mark as attempted to avoid repeated errors
            return;
        }

        _loggingService = loggingServiceResult.Value;
        _logger?.LogInformation("LoggingService resolved from DI container");

        // Try to get LoggingLevelSwitch from DI container
        var levelSwitchResult = ServiceLocator.GetService<Serilog.Core.LoggingLevelSwitch>();
        if (levelSwitchResult.IsSuccess)
        {
            _levelSwitch = levelSwitchResult.Value;
            _logger?.LogInformation("LoggingLevelSwitch resolved from DI container");
        }
        else
        {
            _logger?.LogWarning("LoggingLevelSwitch not found: {Error}. Log level control will be unavailable", levelSwitchResult.Error);
        }

        // Load saved category state from disk (if exists)
        LoadCategoryState();

        // Load scene and wire up UI
        LoadSceneAndWireUI();

        // Populate category filters
        PopulateCategoryFilters();

        // Initialize log level dropdown to match current level
        InitializeLogLevelDropdown();

        _initialized = true;
        _logger?.LogInformation("Debug Console initialized and ready");
    }

    /// <summary>
    /// Load DebugConsole.tscn scene and wire up signals to C# handlers.
    ///
    /// WHY SCENE: Visual editing in Godot editor, proper container layout (no Z-index bugs),
    ///            hot-reload for quick iteration, Godot-native mouse filtering.
    ///
    /// WIRING: GetNode() finds controls defined in scene, connect signals to C# methods.
    /// </summary>
    private void LoadSceneAndWireUI()
    {
        try
        {
            // Load scene from file
            var scene = ResourceLoader.Load<PackedScene>(ScenePath);
            if (scene == null)
            {
                _logger?.LogError("Failed to load scene: {ScenePath}", ScenePath);
                return;
            }

            // Instantiate scene and add as child
            _container = scene.Instantiate<Control>();
            _container.Visible = false; // Hidden by default (F12 to toggle)
            _container.ProcessMode = ProcessModeEnum.Always; // Work while game paused
            AddChild(_container);

            // Get references to scene controls
            _categoryFiltersContainer = _container.GetNode<VBoxContainer>("Panel/MarginContainer/VBoxContainer/ScrollContainer/CategoryFiltersContainer");
            _logLevelDropdown = _container.GetNode<OptionButton>("Panel/MarginContainer/VBoxContainer/LogLevelSection/LogLevelDropdown");

            // Wire up log level dropdown signal
            if (_logLevelDropdown != null)
            {
                _logLevelDropdown.ItemSelected += OnLogLevelChanged;
            }

            _logger?.LogInformation("Debug Console scene loaded and wired");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading scene");
        }
    }

    /// <summary>
    /// Initialize log level dropdown to match current LoggingLevelSwitch value.
    /// Called after scene loaded and level switch resolved from DI.
    ///
    /// WHY: Ensures UI reflects actual log level (not just default selection).
    /// </summary>
    private void InitializeLogLevelDropdown()
    {
        if (_levelSwitch == null || _logLevelDropdown == null)
            return;

        var currentLevel = _levelSwitch.MinimumLevel;
        var dropdownIndex = DebugConsoleConfig.SerilogLevelToIndex(currentLevel);
        _logLevelDropdown.Selected = dropdownIndex;

        _logger?.LogDebug("Log level dropdown initialized to {Level} (index {Index})", currentLevel, dropdownIndex);
    }

    /// <summary>
    /// Handle log level dropdown selection change.
    /// Maps OptionButton index to Serilog level, updates global switch, and persists to disk.
    /// </summary>
    private void OnLogLevelChanged(long index)
    {
        if (_levelSwitch == null)
        {
            _logger?.LogWarning("LoggingLevelSwitch not available, cannot change log level");
            return;
        }

        var newLevel = DebugConsoleConfig.IndexToSerilogLevel((int)index);
        _levelSwitch.MinimumLevel = newLevel;
        _logger?.LogInformation("Log level changed to {Level}", newLevel);

        // Persist log level change to disk (survives restarts)
        SaveCategoryState();
    }

    /// <summary>
    /// Populate category filter checkboxes dynamically.
    ///
    /// Categories are discovered from:
    /// 1. CQRS classes (Commands.{Category}, Queries.{Category}) if they exist
    /// 2. Fallback to currently enabled categories if no CQRS classes yet
    ///
    /// Each checkbox controls whether that category's logs appear in Godot Output.
    /// </summary>
    private void PopulateCategoryFilters()
    {
        if (_loggingService == null || _categoryFiltersContainer == null)
            return;

        var availableCategories = _loggingService.GetAvailableCategories();
        var enabledCategories = _loggingService.GetEnabledCategories();

        // FALLBACK: If no feature categories discovered, show Infrastructure category only
        if (availableCategories.Count == 0)
        {
            _logger?.LogWarning("No feature categories discovered (Features.* namespaces not found). Using Infrastructure as fallback");
            availableCategories = new List<string> { "Infrastructure" };
        }
        else
        {
            _logger?.LogInformation("Discovered {Count} categories from Features", availableCategories.Count);
        }

        // Create checkbox for each category
        foreach (var category in availableCategories)
        {
            var checkbox = new CheckBox
            {
                Text = category,
                ButtonPressed = enabledCategories.Contains(category)
            };
            checkbox.AddThemeFontSizeOverride("font_size", 18); // Larger font for visibility

            // Wire up toggle handler
            // IMPORTANT: Capture category in local variable to avoid closure issues
            var capturedCategory = category;
            checkbox.Toggled += (isEnabled) =>
            {
                if (isEnabled)
                {
                    _loggingService?.EnableCategory(capturedCategory);
                    _logger?.LogInformation("Enabled category: {Category}", capturedCategory);
                }
                else
                {
                    _loggingService?.DisableCategory(capturedCategory);
                    _logger?.LogInformation("Disabled category: {Category}", capturedCategory);
                }

                // Save state after every toggle
                SaveCategoryState();
            };

            _categoryFiltersContainer.AddChild(checkbox);
            _logger?.LogDebug("Added category filter: {Category} [{State}]", category, checkbox.ButtonPressed ? "ON" : "OFF");
        }

        _logger?.LogInformation("Created {Count} category filter checkboxes", availableCategories.Count);
    }

    /// <summary>
    /// Load saved category filter state AND log level from user://debug_console_state.json.
    /// If file doesn't exist or is corrupted, uses default enabled categories and log level.
    ///
    /// WHY: Persist developer preferences across game restarts.
    /// FILE LOCATION: user:// maps to OS-specific user data directory:
    /// - Windows: %APPDATA%\Godot\app_userdata\Darklands\
    /// - Linux: ~/.local/share/godot/app_userdata/Darklands/
    /// - macOS: ~/Library/Application Support/Godot/app_userdata/Darklands/
    /// </summary>
    private void LoadCategoryState()
    {
        if (_loggingService == null)
            return;

        var godotPath = ProjectSettings.GlobalizePath(StateFilePath);

        if (!File.Exists(godotPath))
        {
            _logger?.LogDebug("No saved state found at {Path}, using defaults", StateFilePath);
            return;
        }

        try
        {
            var json = File.ReadAllText(godotPath);
            var state = JsonSerializer.Deserialize<DebugConsoleState>(json);

            if (state != null)
            {
                // Restore category filters
                if (state.EnabledCategories != null && state.EnabledCategories.Count > 0)
                {
                    // Get current enabled categories
                    var currentEnabled = _loggingService.GetEnabledCategories().ToHashSet();

                    // Disable all current categories
                    foreach (var category in currentEnabled)
                    {
                        _loggingService.DisableCategory(category);
                    }

                    // Enable saved categories
                    foreach (var category in state.EnabledCategories)
                    {
                        _loggingService.EnableCategory(category);
                    }

                    _logger?.LogInformation("Loaded saved categories: {Count} enabled [{Categories}]",
                        state.EnabledCategories.Count,
                        string.Join(", ", state.EnabledCategories));
                }

                // Restore log level
                if (!string.IsNullOrEmpty(state.LogLevel) && _levelSwitch != null)
                {
                    if (Enum.TryParse<Serilog.Events.LogEventLevel>(state.LogLevel, out var logLevel))
                    {
                        _levelSwitch.MinimumLevel = logLevel;
                        _logger?.LogInformation("Loaded saved log level: {Level}", logLevel);
                    }
                    else
                    {
                        _logger?.LogWarning("Invalid log level in saved state: {Level}, using current level", state.LogLevel);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to load state from {Path}, using defaults", StateFilePath);
        }
    }

    /// <summary>
    /// Save current category filter state AND log level to user://debug_console_state.json.
    /// Called automatically on every checkbox toggle or log level change.
    ///
    /// WHY: Persist preferences so they survive game restarts.
    /// FORMAT: JSON {"EnabledCategories": ["Combat"], "LogLevel": "Debug"}
    /// </summary>
    private void SaveCategoryState()
    {
        if (_loggingService == null)
            return;

        var godotPath = ProjectSettings.GlobalizePath(StateFilePath);

        try
        {
            var state = new DebugConsoleState
            {
                EnabledCategories = _loggingService.GetEnabledCategories().OrderBy(c => c).ToList(),
                LogLevel = _levelSwitch?.MinimumLevel.ToString() // Save current log level
            };

            var json = JsonSerializer.Serialize(state, new JsonSerializerOptions
            {
                WriteIndented = true // Pretty-print for human readability
            });

            // Ensure directory exists
            var directory = Path.GetDirectoryName(godotPath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(godotPath, json);
            _logger?.LogDebug("Saved state: {Count} categories, log level: {Level}",
                state.EnabledCategories.Count, state.LogLevel ?? "default");
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to save state to {Path}", StateFilePath);
        }
    }

    /// <summary>
    /// Read persisted state file to display current settings in startup message.
    /// Called during _Ready() before DI container is available.
    /// </summary>
    private string GetPersistedStateInfo()
    {
        try
        {
            var godotPath = ProjectSettings.GlobalizePath(StateFilePath);

            if (!File.Exists(godotPath))
            {
                return "Log: Information (default), Categories: All (default)";
            }

            var json = File.ReadAllText(godotPath);
            var state = JsonSerializer.Deserialize<DebugConsoleState>(json);

            if (state == null)
            {
                return "Log: Information (default), Categories: All (default)";
            }

            var logLevel = state.LogLevel ?? "Information";
            var categoryCount = state.EnabledCategories?.Count ?? 0;
            var categoryNames = state.EnabledCategories != null && state.EnabledCategories.Count > 0
                ? string.Join(", ", state.EnabledCategories.Take(3)) + (state.EnabledCategories.Count > 3 ? $" +{state.EnabledCategories.Count - 3} more" : "")
                : "All";

            return $"Log: {logLevel}, Categories: {categoryNames} ({categoryCount} enabled)";
        }
        catch
        {
            return "Log: Information (default), Categories: All (default)";
        }
    }

    /// <summary>
    /// Simple data structure for JSON persistence.
    /// Stores both category filters AND log level preference.
    /// </summary>
    private class DebugConsoleState
    {
        public List<string> EnabledCategories { get; set; } = new();
        public string? LogLevel { get; set; } // Serilog level name: "Debug", "Information", "Warning", "Error"
    }
}