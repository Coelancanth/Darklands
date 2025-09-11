using Darklands.Core.Domain.Debug;
using Godot;
using Serilog;

namespace Darklands;

/// <summary>
/// Global debug system autoload that provides runtime debug configuration and logging.
/// Accessible throughout the game via DebugSystem.Instance for configuration queries.
/// Manages debug window visibility and handles F12 toggle input.
/// Integrates with dependency injection to provide category-filtered logging.
/// </summary>
public partial class DebugSystem : Node
{
    /// <summary>
    /// Global singleton instance accessible throughout the game.
    /// Set during _Ready() to ensure availability after scene tree initialization.
    /// </summary>
    public static DebugSystem Instance { get; private set; } = null!;

    /// <summary>
    /// Debug configuration resource loaded from debug_config.tres.
    /// Provides runtime-editable debug settings with category-based controls.
    /// </summary>
    [Export] public DebugConfig Config { get; set; } = null!;

    /// <summary>
    /// Category-aware logger that respects debug configuration filtering.
    /// Used throughout the game for filtered debug output.
    /// </summary>
    public ICategoryLogger Logger { get; private set; } = null!;

    /// <summary>
    /// Debug window UI for runtime configuration changes.
    /// Toggled with F12 key for easy access during development and testing.
    /// </summary>
    private DebugWindow? _debugWindow;

    /// <summary>
    /// Indicates whether the debug system has been properly initialized.
    /// Used to prevent usage before configuration is loaded.
    /// </summary>
    public bool IsInitialized { get; private set; }

    public override void _Ready()
    {
        // Set global singleton reference
        Instance = this;

        // Ensure this system processes even when the game is paused
        ProcessMode = ProcessModeEnum.Always;

        // Load debug configuration resource
        InitializeConfiguration();

        // Initialize category-filtered logger
        InitializeLogger();

        // Create debug window UI
        InitializeDebugWindow();
        
        // Listen for configuration changes to update standard logging
        Config.SettingChanged += OnDebugConfigChanged;

        IsInitialized = true;

        Logger.Log(LogCategory.System, "DebugSystem initialized successfully");

        // Test log level filtering during initialization
        Logger.Log(LogLevel.Debug, LogCategory.Developer, "Debug level message - should only show if level is Debug");
        Logger.Log(LogLevel.Information, LogCategory.Developer, "Information level message - should show if level is Information or lower");
        Logger.Log(LogLevel.Warning, LogCategory.Developer, "Warning level message - should always show");
    }

    /// <summary>
    /// Loads debug configuration from resource file.
    /// Creates default configuration if file doesn't exist.
    /// </summary>
    private void InitializeConfiguration()
    {
        const string configPath = "res://debug_config.tres";

        if (ResourceLoader.Exists(configPath))
        {
            Config = GD.Load<DebugConfig>(configPath);
        }
        else
        {
            // Create default configuration if resource doesn't exist
            Config = new DebugConfig();
            // Save default configuration for future editing
            ResourceSaver.Save(Config, configPath);
            GD.Print("Created default debug configuration at: ", configPath);
        }
    }

    /// <summary>
    /// Creates category-filtered logger using Serilog and debug configuration.
    /// Uses default Serilog logger if none is configured.
    /// </summary>
    private void InitializeLogger()
    {
        // Create a basic Serilog logger if none exists
        // In a full implementation, this would use the existing logging infrastructure
        var serilogLogger = Log.Logger ?? new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        Logger = new GodotCategoryLogger(serilogLogger, Config);
    }

    /// <summary>
    /// Creates and initializes the debug window UI.
    /// Window is initially hidden and toggled with F12 key.
    /// </summary>
    private void InitializeDebugWindow()
    {
        _debugWindow = new DebugWindow(Config);
        AddChild(_debugWindow);
        _debugWindow.Visible = false;
    }

    /// <summary>
    /// Handles input events, specifically F12 key for debug window toggle.
    /// Processes input even when game is paused for debugging accessibility.
    /// </summary>
    /// <param name="event">The input event to process</param>
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            // F12 key toggles debug window
            if (keyEvent.Keycode == Key.F12)
            {
                ToggleDebugWindow();
                // Accept the event to prevent further processing
                GetViewport().SetInputAsHandled();
            }
        }
    }

    /// <summary>
    /// Toggles the visibility of the debug window.
    /// Provides runtime access to debug configuration settings.
    /// </summary>
    public void ToggleDebugWindow()
    {
        if (_debugWindow == null) return;

        _debugWindow.Visible = !_debugWindow.Visible;

        if (_debugWindow.Visible)
        {
            Logger.Log(LogCategory.Developer, "Debug window opened");
        }
        else
        {
            Logger.Log(LogCategory.Developer, "Debug window closed");
        }
    }

    /// <summary>
    /// Convenience method to check if a specific debug category is enabled.
    /// Provides easy access for systems that need to check debug settings.
    /// </summary>
    /// <param name="category">The category to check</param>
    /// <returns>True if the category is enabled in debug configuration</returns>
    public bool IsDebugEnabled(LogCategory category)
    {
        return Config?.ShouldLog(category) ?? false;
    }
    
    /// <summary>
    /// Handles changes to debug configuration, particularly log level changes.
    /// Updates the standard Microsoft.Extensions.Logging level to match our configuration.
    /// </summary>
    /// <param name="propertyName">Name of the property that changed</param>
    private void OnDebugConfigChanged(string propertyName)
    {
        if (propertyName == nameof(Config.CurrentLogLevel))
        {
            // Update the global Serilog minimum level to match our configuration
            var serilogLevel = Config.CurrentLogLevel switch
            {
                LogLevel.Debug => Serilog.Events.LogEventLevel.Debug,
                LogLevel.Information => Serilog.Events.LogEventLevel.Information,
                LogLevel.Warning => Serilog.Events.LogEventLevel.Warning,
                LogLevel.Error => Serilog.Events.LogEventLevel.Error,
                _ => Serilog.Events.LogEventLevel.Information
            };
            
            // Update the global level switch (elegant SSOT solution)
            if (Core.Infrastructure.DependencyInjection.GameStrapper.GlobalLevelSwitch != null)
            {
                Core.Infrastructure.DependencyInjection.GameStrapper.GlobalLevelSwitch.MinimumLevel = serilogLevel;
                Logger.Log(LogCategory.Developer, 
                    $"Updated global log level to {Config.CurrentLogLevel} (Serilog: {serilogLevel})");
            }
        }
    }
}
