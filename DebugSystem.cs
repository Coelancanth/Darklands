using System;
using Darklands.Core.Domain.Debug;
using Darklands.Core.Infrastructure.Debug;
using Darklands.Core.Infrastructure.DependencyInjection;
using Godot;
using Microsoft.Extensions.DependencyInjection;
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
    /// Updates the logger instance. Used by GameManager to provide the UnifiedLogger
    /// after DI initialization is complete.
    /// </summary>
    public void SetLogger(ICategoryLogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

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
        
        // Listen for configuration changes to update standard logging and persistence
        Config.SettingChanged += OnDebugConfigChanged;

        IsInitialized = true;

        Logger.Log(LogLevel.Information, LogCategory.System, "DebugSystem initialized successfully");

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
        
        // Set the live configuration for the Core project's DefaultDebugConfiguration
        DefaultDebugConfiguration.SetLiveConfiguration(Config);
    }

    /// <summary>
    /// Gets the unified category-filtered logger from dependency injection.
    /// Falls back to creating a basic logger if DI is not initialized.
    /// </summary>
    private void InitializeLogger()
    {
        // Try to get the unified logger from dependency injection
        var servicesResult = GameStrapper.GetServices();
        if (servicesResult.IsSucc)
        {
            servicesResult.Match(
                Succ: provider =>
                {
                    // Get the unified logger from DI
                    var categoryLogger = provider.GetService<ICategoryLogger>();
                    if (categoryLogger != null)
                    {
                        Logger = categoryLogger;
                        return provider;
                    }
                    return provider;
                },
                Fail: _ => (ServiceProvider?)null);
            
            // Return early if we successfully got the logger from DI
            if (Logger != null)
                return;
        }

        // Fallback: Create a minimal unified logger using console output
        var fallbackOutput = new Darklands.Core.Infrastructure.Logging.TestConsoleOutput();
        Logger = new Darklands.Core.Infrastructure.Logging.UnifiedCategoryLogger(fallbackOutput, Config);
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
            Logger.Log(LogLevel.Information, LogCategory.Developer, "Debug window opened");
        }
        else
        {
            Logger.Log(LogLevel.Information, LogCategory.Developer, "Debug window closed");
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
    /// Also handles persistence of configuration changes to the resource file.
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
                Logger.Log(LogLevel.Information, LogCategory.Developer, 
                    $"Updated global log level to {Config.CurrentLogLevel} (Serilog: {serilogLevel})");
            }
        }
        else
        {
            // Log specific setting changes with current value
            LogSettingChange(propertyName);
        }
        
        // Save configuration changes to persist settings
        SaveConfiguration(propertyName);
    }
    
    /// <summary>
    /// Logs specific debug setting changes with descriptive messages and current values.
    /// </summary>
    /// <param name="propertyName">Name of the property that changed</param>
    private void LogSettingChange(string propertyName)
    {
        var message = propertyName switch
        {
            // Logging categories
            nameof(Config.ShowDeveloperMessages) => $"Developer messages: {Config.ShowDeveloperMessages}",
            nameof(Config.ShowSystemMessages) => $"System messages: {Config.ShowSystemMessages}",
            nameof(Config.ShowCommandMessages) => $"Command messages: {Config.ShowCommandMessages}",
            nameof(Config.ShowEventMessages) => $"Event messages: {Config.ShowEventMessages}",
            nameof(Config.ShowThreadMessages) => $"Thread messages: {Config.ShowThreadMessages}",
            nameof(Config.ShowAIMessages) => $"AI messages: {Config.ShowAIMessages}",
            nameof(Config.ShowPerformanceMessages) => $"Performance messages: {Config.ShowPerformanceMessages}",
            nameof(Config.ShowNetworkMessages) => $"Network messages: {Config.ShowNetworkMessages}",
            nameof(Config.ShowVisionMessages) => $"Vision messages: {Config.ShowVisionMessages}",
            nameof(Config.ShowPathfindingMessages) => $"Pathfinding messages: {Config.ShowPathfindingMessages}",
            nameof(Config.ShowCombatMessages) => $"Combat messages: {Config.ShowCombatMessages}",
            
            // Debug visualization
            nameof(Config.ShowPaths) => $"Show paths: {Config.ShowPaths}",
            nameof(Config.ShowPathCosts) => $"Show path costs: {Config.ShowPathCosts}",
            nameof(Config.ShowVisionRanges) => $"Show vision ranges: {Config.ShowVisionRanges}",
            nameof(Config.ShowFOVCalculations) => $"Show FOV calculations: {Config.ShowFOVCalculations}",
            nameof(Config.ShowExploredOverlay) => $"Show explored overlay: {Config.ShowExploredOverlay}",
            nameof(Config.ShowLineOfSight) => $"Show line of sight: {Config.ShowLineOfSight}",
            nameof(Config.ShowDamageNumbers) => $"Show damage numbers: {Config.ShowDamageNumbers}",
            nameof(Config.ShowHitChances) => $"Show hit chances: {Config.ShowHitChances}",
            nameof(Config.ShowTurnOrder) => $"Show turn order: {Config.ShowTurnOrder}",
            nameof(Config.ShowAttackRanges) => $"Show attack ranges: {Config.ShowAttackRanges}",
            nameof(Config.ShowAIStates) => $"Show AI states: {Config.ShowAIStates}",
            nameof(Config.ShowAIDecisionScores) => $"Show AI decision scores: {Config.ShowAIDecisionScores}",
            nameof(Config.ShowAITargeting) => $"Show AI targeting: {Config.ShowAITargeting}",
            
            // Performance monitoring
            nameof(Config.ShowFPS) => $"Show FPS: {Config.ShowFPS}",
            nameof(Config.ShowFrameTime) => $"Show frame time: {Config.ShowFrameTime}",
            nameof(Config.ShowMemoryUsage) => $"Show memory usage: {Config.ShowMemoryUsage}",
            nameof(Config.EnableProfiling) => $"Enable profiling: {Config.EnableProfiling}",
            
            // Gameplay debug
            nameof(Config.GodMode) => $"God mode: {Config.GodMode}",
            nameof(Config.UnlimitedActions) => $"Unlimited actions: {Config.UnlimitedActions}",
            nameof(Config.InstantKills) => $"Instant kills: {Config.InstantKills}",
            
            // Window settings
            nameof(Config.DebugWindowFontSize) => $"Debug window font size: {Config.DebugWindowFontSize}",
            nameof(Config.DebugWindowSize) => $"Debug window size: {Config.DebugWindowSize}",
            nameof(Config.DebugWindowPosition) => $"Debug window position: {Config.DebugWindowPosition}",
            
            // Default for unknown properties
            _ => $"Setting '{propertyName}' changed"
        };
        
        Logger.Log(LogLevel.Information, LogCategory.Developer, $"Debug setting changed: {message}");
    }
    
    /// <summary>
    /// Saves the current configuration to the resource file for persistence.
    /// Called automatically when configuration changes to maintain state across sessions.
    /// </summary>
    /// <param name="propertyName">Name of the property that changed (optional, for logging)</param>
    private void SaveConfiguration(string? propertyName = null)
    {
        const string configPath = "res://debug_config.tres";
        
        var result = ResourceSaver.Save(Config, configPath);
        if (result == Error.Ok)
        {
            var message = propertyName != null 
                ? $"Debug configuration saved: {propertyName} persisted"
                : "Debug configuration saved successfully";
            Logger.Log(LogLevel.Debug, LogCategory.Developer, message);
        }
        else
        {
            Logger.Log(LogLevel.Warning, LogCategory.Developer, $"Failed to save debug configuration: {result}");
        }
    }
}
