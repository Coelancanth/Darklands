namespace Darklands.Core.Domain.Debug;

/// <summary>
/// Interface for accessing debug configuration settings.
/// Provides clean separation between domain logic and Godot-specific Resource implementation.
/// Allows runtime modification of debug settings and category-based logging control.
/// </summary>
public interface IDebugConfiguration
{
    // Pathfinding Debug Settings
    bool ShowPaths { get; }
    bool ShowPathCosts { get; }
    string PathColor { get; }
    int PathAlpha { get; }

    // Vision & FOV Debug Settings
    bool ShowVisionRanges { get; }
    bool ShowFOVCalculations { get; }
    bool ShowExploredOverlay { get; }
    bool ShowLineOfSight { get; }

    // Combat Debug Settings
    bool ShowDamageNumbers { get; }
    bool ShowHitChances { get; }
    bool ShowTurnOrder { get; }
    bool ShowAttackRanges { get; }

    // AI & Behavior Debug Settings
    bool ShowAIStates { get; }
    bool ShowAIDecisionScores { get; }
    bool ShowAITargeting { get; }

    // Performance Debug Settings
    bool ShowFPS { get; }
    bool ShowFrameTime { get; }
    bool ShowMemoryUsage { get; }
    bool EnableProfiling { get; }

    // Gameplay Debug Settings
    bool GodMode { get; }
    bool UnlimitedActions { get; }
    bool InstantKills { get; }

    // Logging Category Controls
    bool ShowThreadMessages { get; }
    bool ShowCommandMessages { get; }
    bool ShowEventMessages { get; }
    bool ShowSystemMessages { get; }
    bool ShowAIMessages { get; }
    bool ShowPerformanceMessages { get; }
    bool ShowNetworkMessages { get; }
    bool ShowDeveloperMessages { get; }
    bool ShowVisionMessages { get; }
    bool ShowPathfindingMessages { get; }
    bool ShowCombatMessages { get; }

    // Global Log Level Control
    LogLevel CurrentLogLevel { get; }

    /// <summary>
    /// Determines if logging for the specified category should be displayed.
    /// </summary>
    /// <param name="category">The log category to check</param>
    /// <returns>True if logging should be displayed for this category</returns>
    bool ShouldLog(LogCategory category);

    /// <summary>
    /// Determines if logging should be displayed for the specified level and category.
    /// Messages are only shown if both the category is enabled AND the level is sufficient.
    /// </summary>
    /// <param name="level">The log level of the message</param>
    /// <param name="category">The log category of the message</param>
    /// <returns>True if logging should be displayed for this level and category</returns>
    bool ShouldLog(LogLevel level, LogCategory category);
}
