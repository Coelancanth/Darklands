using Darklands.Core.Domain.Debug;
using Godot;

namespace Darklands;

/// <summary>
/// Godot Resource-based debug configuration that provides runtime-editable debug settings.
/// Uses [Export] attributes to expose settings in the Godot editor for easy modification.
/// Implements IDebugConfiguration to provide clean separation from domain logic.
/// Can be saved as a .tres file and modified without code changes.
/// </summary>
[GlobalClass]
public partial class DebugConfig : Resource, IDebugConfiguration
{
    // Pathfinding Debug Settings
    [ExportGroup("🗺️ Pathfinding")]
    [Export] public bool ShowPaths { get; set; } = false;
    [Export] public bool ShowPathCosts { get; set; } = false;
    [Export] public Color PathColorValue { get; set; } = new Color(0, 0, 1, 0.5f);
    [Export] public float PathAlphaValue { get; set; } = 0.5f;

    // Vision & FOV Debug Settings
    [ExportGroup("👁️ Vision & FOV")]
    [Export] public bool ShowVisionRanges { get; set; } = false;
    [Export] public bool ShowFOVCalculations { get; set; } = false;
    [Export] public bool ShowExploredOverlay { get; set; } = true;
    [Export] public bool ShowLineOfSight { get; set; } = false;

    // Combat Debug Settings
    [ExportGroup("⚔️ Combat")]
    [Export] public bool ShowDamageNumbers { get; set; } = true;
    [Export] public bool ShowHitChances { get; set; } = false;
    [Export] public bool ShowTurnOrder { get; set; } = true;
    [Export] public bool ShowAttackRanges { get; set; } = false;

    // AI & Behavior Debug Settings
    [ExportGroup("🤖 AI & Behavior")]
    [Export] public bool ShowAIStates { get; set; } = false;
    [Export] public bool ShowAIDecisionScores { get; set; } = false;
    [Export] public bool ShowAITargeting { get; set; } = false;

    // Performance Debug Settings
    [ExportGroup("📊 Performance")]
    [Export] public bool ShowFPS { get; set; } = false;
    [Export] public bool ShowFrameTime { get; set; } = false;
    [Export] public bool ShowMemoryUsage { get; set; } = false;
    [Export] public bool EnableProfiling { get; set; } = false;

    // Gameplay Debug Settings
    [ExportGroup("🎮 Gameplay")]
    [Export] public bool GodMode { get; set; } = false;
    [Export] public bool UnlimitedActions { get; set; } = false;
    [Export] public bool InstantKills { get; set; } = false;

    // Logging Category Controls
    [ExportGroup("📝 Logging & Console")]
    [Export] public bool ShowThreadMessages { get; set; } = true;
    [Export] public bool ShowCommandMessages { get; set; } = true;
    [Export] public bool ShowEventMessages { get; set; } = true;
    [Export] public bool ShowSystemMessages { get; set; } = true;
    [Export] public bool ShowAIMessages { get; set; } = false;
    [Export] public bool ShowPerformanceMessages { get; set; } = false;
    [Export] public bool ShowNetworkMessages { get; set; } = false;
    [Export] public bool ShowDeveloperMessages { get; set; } = false;
    [Export] public bool ShowGameplayMessages { get; set; } = true;
    [Export] public bool ShowVisionMessages { get; set; } = true;
    [Export] public bool ShowPathfindingMessages { get; set; } = false;
    [Export] public bool ShowCombatMessages { get; set; } = true;

    // Global Log Level Control
    [ExportGroup("🔧 Global Settings")]
    [Export] public LogLevel CurrentLogLevel { get; set; } = LogLevel.Information;
    [Export] public int DebugWindowFontSize { get; set; } = 16;
    [Export] public Vector2I DebugWindowSize { get; set; } = new Vector2I(350, 500);
    [Export] public Vector2I DebugWindowPosition { get; set; } = new Vector2I(20, 20);

    /// <summary>
    /// Signal emitted when any configuration setting changes.
    /// Allows systems to react to runtime configuration updates.
    /// </summary>
    [Signal]
    public delegate void SettingChangedEventHandler(string propertyName);

    /// <summary>
    /// Interface implementation for PathColor - converts Godot Color to string representation.
    /// </summary>
    public string PathColor => PathColorValue.ToHtml();

    /// <summary>
    /// Interface implementation for PathAlpha - converts float (0.0-1.0) to integer percentage (0-100).
    /// </summary>
    public int PathAlpha => (int)(PathAlphaValue * 100);

    /// <summary>
    /// Determines if logging should be enabled for the specified category.
    /// Maps LogCategory enum values to their corresponding boolean properties.
    /// </summary>
    /// <param name="category">The log category to check</param>
    /// <returns>True if logging is enabled for this category</returns>
    public bool ShouldLog(LogCategory category) => category switch
    {
        LogCategory.Thread => ShowThreadMessages,
        LogCategory.Command => ShowCommandMessages,
        LogCategory.Event => ShowEventMessages,
        LogCategory.System => ShowSystemMessages,
        LogCategory.AI => ShowAIMessages,
        LogCategory.Performance => ShowPerformanceMessages,
        LogCategory.Network => ShowNetworkMessages,
        LogCategory.Developer => ShowDeveloperMessages,
        LogCategory.Gameplay => ShowGameplayMessages,
        LogCategory.Vision => ShowVisionMessages,
        LogCategory.Pathfinding => ShowPathfindingMessages,
        LogCategory.Combat => ShowCombatMessages,
        _ => true // Unknown categories default to enabled
    };

    /// <summary>
    /// Determines if logging should be displayed for the specified level and category.
    /// Combines both category filtering and log level thresholds.
    /// </summary>
    /// <param name="level">The log level of the message</param>
    /// <param name="category">The log category of the message</param>
    /// <returns>True if both category is enabled and level is sufficient</returns>
    public bool ShouldLog(LogLevel level, LogCategory category)
    {
        // Must pass both category filter and level threshold
        return ShouldLog(category) && level >= CurrentLogLevel;
    }

    /// <summary>
    /// Notifies listeners when a setting has changed.
    /// Should be called after modifying any property to trigger UI updates and system reactions.
    /// </summary>
    /// <param name="propertyName">Name of the property that changed</param>
    public void NotifySettingChanged(string propertyName)
    {
        EmitSignal(SignalName.SettingChanged, propertyName);
    }

    /// <summary>
    /// Toggles all settings in a category on or off.
    /// Useful for "Enable All" functionality in the debug UI.
    /// </summary>
    /// <param name="categoryName">Name of the category to toggle</param>
    /// <param name="enabled">Whether to enable or disable all settings in the category</param>
    public void ToggleCategory(string categoryName, bool enabled)
    {
        // This would be implemented based on reflection or manual mapping
        // For now, focusing on core functionality
        NotifySettingChanged($"Category_{categoryName}");
    }
    
    /// <summary>
    /// Loads the current log level from the debug configuration resource.
    /// Used during GameStrapper initialization to set the correct initial log level.
    /// </summary>
    /// <returns>The configured log level, or Information as fallback</returns>
    public static Serilog.Events.LogEventLevel LoadInitialLogLevel()
    {
        const string configPath = "res://debug_config.tres";
        
        try
        {
            if (ResourceLoader.Exists(configPath))
            {
                var config = GD.Load<DebugConfig>(configPath);
                return config.CurrentLogLevel switch
                {
                    LogLevel.Debug => Serilog.Events.LogEventLevel.Debug,
                    LogLevel.Information => Serilog.Events.LogEventLevel.Information,
                    LogLevel.Warning => Serilog.Events.LogEventLevel.Warning,
                    LogLevel.Error => Serilog.Events.LogEventLevel.Error,
                    _ => Serilog.Events.LogEventLevel.Information
                };
            }
        }
        catch (System.Exception ex)
        {
            // Log to Godot console if resource loading fails
            GD.PrintErr($"Failed to load debug config for log level: {ex.Message}");
        }
        
        // Fallback to Information level (not Debug like Development config)
        return Serilog.Events.LogEventLevel.Information;
    }
}
