using Darklands.Application.Common;
using System;

namespace Darklands.Application.Infrastructure.Debug;

/// <summary>
/// Proxy implementation of IDebugConfiguration that delegates to the live debug configuration.
/// This ensures all loggers in the system use the same runtime-modifiable configuration.
/// The actual configuration instance should be set from the Godot project layer.
/// </summary>
public sealed class DefaultDebugConfiguration : IDebugConfiguration
{
    private static IDebugConfiguration? _liveConfig;
    private static readonly object _lock = new();

    /// <summary>
    /// Sets the live configuration instance from the Godot project layer.
    /// This should be called during application initialization.
    /// </summary>
    public static void SetLiveConfiguration(IDebugConfiguration config)
    {
        lock (_lock)
        {
            _liveConfig = config;
        }
    }

    /// <summary>
    /// Gets the live debug configuration if available, falling back to defaults.
    /// </summary>
    private IDebugConfiguration GetConfig()
    {
        lock (_lock)
        {
            // Use live config if available, otherwise use defaults
            return _liveConfig ?? new DefaultFallbackConfiguration();
        }
    }

    // Pathfinding Debug Settings - all delegate to live config
    public bool ShowPaths => GetConfig().ShowPaths;
    public bool ShowPathCosts => GetConfig().ShowPathCosts;
    public string PathColor => GetConfig().PathColor;
    public int PathAlpha => GetConfig().PathAlpha;

    // Vision & FOV Debug Settings
    public bool ShowVisionRanges => GetConfig().ShowVisionRanges;
    public bool ShowFOVCalculations => GetConfig().ShowFOVCalculations;
    public bool ShowExploredOverlay => GetConfig().ShowExploredOverlay;
    public bool ShowLineOfSight => GetConfig().ShowLineOfSight;

    // Combat Debug Settings
    public bool ShowDamageNumbers => GetConfig().ShowDamageNumbers;
    public bool ShowHitChances => GetConfig().ShowHitChances;
    public bool ShowTurnOrder => GetConfig().ShowTurnOrder;
    public bool ShowAttackRanges => GetConfig().ShowAttackRanges;

    // AI & Behavior Debug Settings
    public bool ShowAIStates => GetConfig().ShowAIStates;
    public bool ShowAIDecisionScores => GetConfig().ShowAIDecisionScores;
    public bool ShowAITargeting => GetConfig().ShowAITargeting;

    // Performance Debug Settings
    public bool ShowFPS => GetConfig().ShowFPS;
    public bool ShowFrameTime => GetConfig().ShowFrameTime;
    public bool ShowMemoryUsage => GetConfig().ShowMemoryUsage;
    public bool EnableProfiling => GetConfig().EnableProfiling;

    // Gameplay Debug Settings
    public bool GodMode => GetConfig().GodMode;
    public bool UnlimitedActions => GetConfig().UnlimitedActions;
    public bool InstantKills => GetConfig().InstantKills;

    // Logging Category Controls
    public bool ShowThreadMessages => GetConfig().ShowThreadMessages;
    public bool ShowCommandMessages => GetConfig().ShowCommandMessages;
    public bool ShowEventMessages => GetConfig().ShowEventMessages;
    public bool ShowSystemMessages => GetConfig().ShowSystemMessages;
    public bool ShowAIMessages => GetConfig().ShowAIMessages;
    public bool ShowPerformanceMessages => GetConfig().ShowPerformanceMessages;
    public bool ShowNetworkMessages => GetConfig().ShowNetworkMessages;
    public bool ShowDeveloperMessages => GetConfig().ShowDeveloperMessages;
    public bool ShowVisionMessages => GetConfig().ShowVisionMessages;
    public bool ShowPathfindingMessages => GetConfig().ShowPathfindingMessages;
    public bool ShowCombatMessages => GetConfig().ShowCombatMessages;

    // Global Log Level Control
    public LogLevel CurrentLogLevel => GetConfig().CurrentLogLevel;

    /// <summary>
    /// Determines if logging for the specified category should be displayed.
    /// Delegates to the live configuration for runtime control.
    /// </summary>
    public bool ShouldLog(LogCategory category) => GetConfig().ShouldLog(category);

    /// <summary>
    /// Determines if logging should be displayed for the specified level and category.
    /// Delegates to the live configuration for runtime control.
    /// </summary>
    public bool ShouldLog(LogLevel level, LogCategory category) => GetConfig().ShouldLog(level, category);

}

/// <summary>
/// Fallback configuration with sensible defaults when no live configuration is available.
/// Used during early initialization before the Godot layer sets the real configuration.
/// </summary>
internal sealed class DefaultFallbackConfiguration : IDebugConfiguration
{
    // Pathfinding Debug Settings
    public bool ShowPaths => false;
    public bool ShowPathCosts => false;
    public string PathColor => "#0000FF";
    public int PathAlpha => 50;

    // Vision & FOV Debug Settings
    public bool ShowVisionRanges => false;
    public bool ShowFOVCalculations => false;
    public bool ShowExploredOverlay => true;
    public bool ShowLineOfSight => false;

    // Combat Debug Settings
    public bool ShowDamageNumbers => true;
    public bool ShowHitChances => false;
    public bool ShowTurnOrder => true;
    public bool ShowAttackRanges => false;

    // AI & Behavior Debug Settings
    public bool ShowAIStates => false;
    public bool ShowAIDecisionScores => false;
    public bool ShowAITargeting => false;

    // Performance Debug Settings
    public bool ShowFPS => false;
    public bool ShowFrameTime => false;
    public bool ShowMemoryUsage => false;
    public bool EnableProfiling => false;

    // Gameplay Debug Settings
    public bool GodMode => false;
    public bool UnlimitedActions => false;
    public bool InstantKills => false;

    // Logging Category Controls - default to showing important categories
    public bool ShowThreadMessages => true;
    public bool ShowCommandMessages => true;
    public bool ShowEventMessages => true;
    public bool ShowSystemMessages => true;
    public bool ShowAIMessages => false;
    public bool ShowPerformanceMessages => false;
    public bool ShowNetworkMessages => false;
    public bool ShowDeveloperMessages => true;  // Enable for debugging
    public bool ShowVisionMessages => true;
    public bool ShowPathfindingMessages => false;
    public bool ShowCombatMessages => true;

    // Global Log Level Control
    public LogLevel CurrentLogLevel => LogLevel.Debug;  // Default to Debug for development

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
        LogCategory.Gameplay => true,  // Always show gameplay
        LogCategory.Vision => ShowVisionMessages,
        LogCategory.Pathfinding => ShowPathfindingMessages,
        LogCategory.Combat => ShowCombatMessages,
        _ => true
    };

    public bool ShouldLog(LogLevel level, LogCategory category)
    {
        return ShouldLog(category) && level >= CurrentLogLevel;
    }
}
