using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;
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
/// - Procedural UI creation (self-contained, no .tscn dependency)
/// - Graceful degradation if LoggingService unavailable
/// - Persistence via user://debug_console_state.json (survives restarts)
///
/// Usage: Registered as autoload in project.godot, works globally in all scenes.
/// </summary>
public partial class DebugConsoleController : CanvasLayer
{
    private const string StateFilePath = "user://debug_console_state.json";

    private Control? _container;
    private VBoxContainer? _categoryFiltersContainer;
    private LoggingService? _loggingService;
    private Serilog.Core.LoggingLevelSwitch? _levelSwitch;
    private bool _initialized = false;

    public override void _Ready()
    {
        // Set high layer to render above all game content
        Layer = 100;

        GD.Print("🎮 DebugConsoleController autoload ready (press F12 to toggle)");
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
                GD.Print($"🔧 Debug Console: {(_container.Visible ? "VISIBLE" : "HIDDEN")}");
            }

            // Mark as handled to prevent propagation
            GetViewport().SetInputAsHandled();
        }
    }

    /// <summary>
    /// Lazy initialization - only runs on first F12 press.
    /// Creates UI procedurally and wires up category filters.
    ///
    /// WHY LAZY: Autoloads run before GameStrapper initializes DI container.
    /// Deferring until first use ensures ServiceLocator is ready.
    /// </summary>
    private void InitializeDebugConsole()
    {
        GD.Print("\n=== Initializing Debug Console (Lazy) ===");

        // Try to get LoggingService from DI container
        // This will fail gracefully if GameStrapper hasn't initialized yet
        var loggingServiceResult = ServiceLocator.GetService<LoggingService>();
        if (loggingServiceResult.IsFailure)
        {
            GD.PrintErr($"❌ Failed to resolve LoggingService: {loggingServiceResult.Error}");
            GD.PrintErr("   Make sure GameStrapper.Initialize() has been called in your scene");
            GD.PrintErr("   Debug Console will be unavailable until DI container is initialized");
            _initialized = true; // Mark as attempted to avoid repeated errors
            return;
        }

        _loggingService = loggingServiceResult.Value;
        GD.Print("✅ LoggingService resolved from DI container");

        // Try to get LoggingLevelSwitch from DI container
        var levelSwitchResult = ServiceLocator.GetService<Serilog.Core.LoggingLevelSwitch>();
        if (levelSwitchResult.IsSuccess)
        {
            _levelSwitch = levelSwitchResult.Value;
            GD.Print("✅ LoggingLevelSwitch resolved from DI container");
        }
        else
        {
            GD.PrintErr($"⚠️ LoggingLevelSwitch not found: {levelSwitchResult.Error}");
            GD.PrintErr("   Log level control will be unavailable");
        }

        // Load saved category state from disk (if exists)
        LoadCategoryState();

        // Build UI procedurally
        CreateUI();

        // Populate category filters
        PopulateCategoryFilters();

        _initialized = true;
        GD.Print("✅ Debug Console initialized and ready");
    }

    /// <summary>
    /// Create debug console UI procedurally.
    ///
    /// Structure:
    /// CanvasLayer (this)
    ///   └── Control (container, full-screen overlay)
    ///       ├── ColorRect (semi-transparent background)
    ///       ├── Label (title)
    ///       ├── Label (subtitle)
    ///       ├── Label (category label)
    ///       └── VBoxContainer (category filters)
    /// </summary>
    private void CreateUI()
    {
        // Main container (full-screen, hidden by default)
        _container = new Control
        {
            Name = "DebugConsoleContainer",
            Visible = false,
            AnchorRight = 1.0f,
            AnchorBottom = 1.0f,
            MouseFilter = Control.MouseFilterEnum.Stop // Block input to game when visible
        };
        AddChild(_container);

        // Semi-transparent dark background
        var background = new ColorRect
        {
            Name = "Background",
            Color = new Color(0.05f, 0.05f, 0.1f, 0.95f),
            AnchorRight = 1.0f,
            AnchorBottom = 1.0f
        };
        _container.AddChild(background);

        // Title
        var title = new Label
        {
            Name = "Title",
            Text = "Debug Console (F12 to toggle)",
            Position = new Vector2(40, 40),
            Size = new Vector2(500, 40)
        };
        title.AddThemeFontSizeOverride("font_size", 28);
        _container.AddChild(title);

        // Subtitle
        var subtitle = new Label
        {
            Name = "Subtitle",
            Text = "Control which logs appear in Godot Output panel",
            Position = new Vector2(40, 90),
            Size = new Vector2(700, 30)
        };
        subtitle.AddThemeFontSizeOverride("font_size", 16);
        _container.AddChild(subtitle);

        // Log Level label and dropdown
        var logLevelLabel = new Label
        {
            Name = "LogLevelLabel",
            Text = "Minimum Log Level:",
            Position = new Vector2(40, 140),
            Size = new Vector2(240, 30)
        };
        logLevelLabel.AddThemeFontSizeOverride("font_size", 20);
        _container.AddChild(logLevelLabel);

        var logLevelDropdown = new OptionButton
        {
            Name = "LogLevelDropdown",
            Position = new Vector2(280, 140),
            Size = new Vector2(200, 40)
        };
        logLevelDropdown.AddItem("Debug", 0);
        logLevelDropdown.AddItem("Information", 1);
        logLevelDropdown.AddItem("Warning", 2);
        logLevelDropdown.AddItem("Error", 3);
        logLevelDropdown.Selected = 1; // Default to Information
        logLevelDropdown.AddThemeFontSizeOverride("font_size", 18);
        logLevelDropdown.ItemSelected += OnLogLevelChanged;
        _container.AddChild(logLevelDropdown);

        // Category label
        var categoryLabel = new Label
        {
            Name = "CategoryLabel",
            Text = "Log Categories:",
            Position = new Vector2(40, 200),
            Size = new Vector2(240, 30)
        };
        categoryLabel.AddThemeFontSizeOverride("font_size", 20);
        _container.AddChild(categoryLabel);

        // Category filters container
        _categoryFiltersContainer = new VBoxContainer
        {
            Name = "CategoryFilters",
            Position = new Vector2(40, 240),
            Size = new Vector2(400, 400)
        };
        _categoryFiltersContainer.AddThemeConstantOverride("separation", 8); // 8px spacing between checkboxes
        _container.AddChild(_categoryFiltersContainer);

        GD.Print("✅ Debug Console UI created procedurally");
    }

    /// <summary>
    /// Handle log level dropdown selection change.
    /// </summary>
    private void OnLogLevelChanged(long index)
    {
        if (_levelSwitch == null)
        {
            GD.PrintErr("⚠️ LoggingLevelSwitch not available, cannot change log level");
            return;
        }

        var newLevel = index switch
        {
            0 => Serilog.Events.LogEventLevel.Debug,
            1 => Serilog.Events.LogEventLevel.Information,
            2 => Serilog.Events.LogEventLevel.Warning,
            3 => Serilog.Events.LogEventLevel.Error,
            _ => Serilog.Events.LogEventLevel.Information
        };

        _levelSwitch.MinimumLevel = newLevel;
        GD.Print($"🔧 Log level changed to: {newLevel}");
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
            GD.Print("⚠️ No feature categories discovered (Features.* namespaces not found)");
            GD.Print("📋 Using Infrastructure as fallback category");
            availableCategories = new List<string> { "Infrastructure" };
        }
        else
        {
            GD.Print($"📋 Discovered {availableCategories.Count} categories from Features:");
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
                    GD.Print($"✅ Enabled category: {capturedCategory}");
                }
                else
                {
                    _loggingService?.DisableCategory(capturedCategory);
                    GD.Print($"❌ Disabled category: {capturedCategory}");
                }

                // Save state after every toggle
                SaveCategoryState();
            };

            _categoryFiltersContainer.AddChild(checkbox);
            GD.Print($"   - {category} [{(checkbox.ButtonPressed ? "ON" : "OFF")}]");
        }

        GD.Print($"✅ Created {availableCategories.Count} category filter checkboxes");
    }

    /// <summary>
    /// Load saved category filter state from user://debug_console_state.json.
    /// If file doesn't exist or is corrupted, uses default enabled categories.
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
            GD.Print($"💾 No saved state found at {StateFilePath} (using defaults)");
            return;
        }

        try
        {
            var json = File.ReadAllText(godotPath);
            var state = JsonSerializer.Deserialize<DebugConsoleState>(json);

            if (state?.EnabledCategories != null && state.EnabledCategories.Count > 0)
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

                GD.Print($"💾 Loaded saved state: {state.EnabledCategories.Count} categories enabled");
                GD.Print($"   Categories: {string.Join(", ", state.EnabledCategories)}");
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"⚠️ Failed to load state from {StateFilePath}: {ex.Message}");
            GD.PrintErr("   Using default categories instead");
        }
    }

    /// <summary>
    /// Save current category filter state to user://debug_console_state.json.
    /// Called automatically on every checkbox toggle.
    ///
    /// WHY: Persist preferences so they survive game restarts.
    /// FORMAT: Simple JSON {"EnabledCategories": ["Combat", "Infrastructure"]}
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
                EnabledCategories = _loggingService.GetEnabledCategories().OrderBy(c => c).ToList()
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
            GD.Print($"💾 Saved state: {state.EnabledCategories.Count} enabled categories");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"⚠️ Failed to save state to {StateFilePath}: {ex.Message}");
        }
    }

    /// <summary>
    /// Simple data structure for JSON persistence.
    /// </summary>
    private class DebugConsoleState
    {
        public List<string> EnabledCategories { get; set; } = new();
    }
}