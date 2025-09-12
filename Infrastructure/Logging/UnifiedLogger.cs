using Darklands.Core.Domain.Debug;
using Godot;
using Serilog;
using System;
using System.Collections.Generic;

namespace Darklands.Infrastructure.Logging;

/// <summary>
/// Single, unified logger for the entire Darklands application.
/// Handles all logging needs with context-aware output, lazy configuration,
/// and zero indirection layers. Replaces all previous logging implementations.
/// </summary>
public sealed class UnifiedLogger : ICategoryLogger
{
    private readonly ILogger _serilogLogger;
    private readonly Lazy<IDebugConfiguration> _config;
    private readonly bool _isGodotContext;

    /// <summary>
    /// Color mappings for rich console output in Godot context.
    /// </summary>
    private static readonly Dictionary<LogCategory, string> CategoryColors = new()
    {
        [LogCategory.System] = "#AAAAAA",      // Light gray
        [LogCategory.Command] = "#00FF00",     // Green  
        [LogCategory.Event] = "#FFFF00",       // Yellow
        [LogCategory.Thread] = "#FF00FF",      // Magenta
        [LogCategory.AI] = "#FF8000",          // Orange
        [LogCategory.Performance] = "#00FFFF", // Cyan
        [LogCategory.Network] = "#8080FF",     // Light blue
        [LogCategory.Developer] = "#606060",   // Dark gray
        [LogCategory.Gameplay] = "#4080FF",    // Medium blue
        [LogCategory.Vision] = "#FF0080",      // Pink
        [LogCategory.Pathfinding] = "#80FF80", // Light green
        [LogCategory.Combat] = "#FF4040"       // Light red
    };

    private static readonly Dictionary<LogLevel, string> LevelColors = new()
    {
        [LogLevel.Debug] = "#808080",      // Gray
        [LogLevel.Information] = "#00AAFF", // Light blue
        [LogLevel.Warning] = "#FFA500",    // Orange  
        [LogLevel.Error] = "#FF0000"       // Red
    };

    public UnifiedLogger(ILogger serilogLogger, IDebugConfiguration config)
    {
        _serilogLogger = serilogLogger ?? throw new ArgumentNullException(nameof(serilogLogger));
        _config = new Lazy<IDebugConfiguration>(() => config ?? new FallbackDebugConfiguration());
        _isGodotContext = DetectGodotContext();
    }

    /// <summary>
    /// Detects if we're running in Godot context vs unit tests.
    /// </summary>
    private static bool DetectGodotContext()
    {
        try
        {
            // Try to access Godot's Engine - will throw in unit test context
            var _ = Engine.GetVersionInfo();
            return true; // Engine exists
        }
        catch
        {
            return false; // Unit test or headless context
        }
    }

    /// <summary>
    /// Logs a message under the specified category if category logging is enabled.
    /// Uses Information level by default for backward compatibility.
    /// </summary>
    public void Log(LogCategory category, string message)
    {
        Log(LogLevel.Information, category, message);
    }

    /// <summary>
    /// Logs a formatted message under the specified category with arguments.
    /// Uses Information level by default for backward compatibility.
    /// </summary>
    public void Log(LogCategory category, string template, params object[] args)
    {
        Log(LogLevel.Information, category, template, args);
    }

    /// <summary>
    /// Logs a message with specified level and category, respecting both filters.
    /// Only outputs if both the category is enabled AND the level meets the threshold.
    /// </summary>
    public void Log(LogLevel level, LogCategory category, string message)
    {
        var config = _config.Value;
        
        if (!config.ShouldLog(level, category))
            return;

        // Create rich console output for Godot context
        if (_isGodotContext)
        {
            var richMessage = FormatRichMessage(level, category, message);
            GD.PrintRich(richMessage);
        }

        // Always log to Serilog for file output and structured logging
        LogToSerilog(level, category, message);
    }

    /// <summary>
    /// Logs a formatted message with specified level and category.
    /// Only outputs if both the category is enabled AND the level meets the threshold.
    /// Passes structured templates directly to Serilog for proper formatting.
    /// </summary>
    public void Log(LogLevel level, LogCategory category, string template, params object[] args)
    {
        var config = _config.Value;
        
        if (!config.ShouldLog(level, category))
            return;

        // Create rich console output for Godot context
        if (_isGodotContext)
        {
            // Use a simple approach: let Serilog format it through a temporary string builder
            var formattedMessage = FormatSerilogTemplate(template, args);
            var richMessage = FormatRichMessage(level, category, formattedMessage);
            GD.PrintRich(richMessage);
        }

        // Always log to Serilog with structured template (this handles formatting correctly)
        LogToSerilogWithTemplate(level, category, template, args);
    }

    /// <summary>
    /// Checks if logging is enabled for the specified category.
    /// Useful for performance-sensitive scenarios where message construction is expensive.
    /// </summary>
    public bool IsEnabled(LogCategory category) => _config.Value.ShouldLog(category);

    /// <summary>
    /// Checks if logging is enabled for the specified level and category.
    /// Useful for performance-sensitive scenarios where message construction is expensive.
    /// </summary>
    public bool IsEnabled(LogLevel level, LogCategory category) => _config.Value.ShouldLog(level, category);

    private string FormatRichMessage(LogLevel level, LogCategory category, string message)
    {
        var categoryColor = CategoryColors.GetValueOrDefault(category, "#FFFFFF");
        var levelColor = LevelColors.GetValueOrDefault(level, "#FFFFFF");
        
        return $"[color={levelColor}]{level}[/color] [color={categoryColor}][{category}][/color] {message}";
    }

    private string FormatSerilogTemplate(string template, object[] args)
    {
        if (args.Length == 0)
            return template;

        try
        {
            // Convert Serilog-style templates to string.Format style
            // Replace {PropertyName} with {0}, {1}, {2} etc. in order
            var result = template;
            var argIndex = 0;
            
            // Use regex to find all {PropertyName} patterns and replace them sequentially
            result = System.Text.RegularExpressions.Regex.Replace(result, @"\{[^}]+\}", 
                match => argIndex < args.Length ? $"{{{argIndex++}}}" : match.Value);
            
            return string.Format(result, args);
        }
        catch (Exception)
        {
            // Fallback: return template with args listed
            return $"{template} ({string.Join(", ", args)})";
        }
    }

    private void LogToSerilog(LogLevel level, LogCategory category, string message)
    {
        // Map our LogLevel to Serilog's LogEventLevel and log with category context
        switch (level)
        {
            case LogLevel.Debug:
                _serilogLogger.Debug("[{Category}] {Message}", category, message);
                break;
            case LogLevel.Information:
                _serilogLogger.Information("[{Category}] {Message}", category, message);
                break;
            case LogLevel.Warning:
                _serilogLogger.Warning("[{Category}] {Message}", category, message);
                break;
            case LogLevel.Error:
                _serilogLogger.Error("[{Category}] {Message}", category, message);
                break;
            default:
                _serilogLogger.Information("[{Category}] {Message}", category, message);
                break;
        }
    }

    private void LogToSerilogWithTemplate(LogLevel level, LogCategory category, string template, object[] args)
    {
        // Prefix the template with category and pass directly to Serilog for structured logging
        var categoryTemplate = $"[{category}] {template}";

        // Map our log level to Serilog level and log with structured template
        switch (level)
        {
            case LogLevel.Debug:
                _serilogLogger.Debug(categoryTemplate, args);
                break;
            case LogLevel.Information:
                _serilogLogger.Information(categoryTemplate, args);
                break;
            case LogLevel.Warning:
                _serilogLogger.Warning(categoryTemplate, args);
                break;
            case LogLevel.Error:
                _serilogLogger.Error(categoryTemplate, args);
                break;
            default:
                _serilogLogger.Information(categoryTemplate, args);
                break;
        }
    }
}

/// <summary>
/// Fallback configuration for production Godot context.
/// Provides sensible defaults when live configuration unavailable.
/// </summary>
internal sealed class FallbackDebugConfiguration : IDebugConfiguration
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

    // Logging Category Controls - Enable key categories
    public bool ShowThreadMessages => true;
    public bool ShowCommandMessages => true;
    public bool ShowEventMessages => true;
    public bool ShowSystemMessages => true;
    public bool ShowAIMessages => false;
    public bool ShowPerformanceMessages => false;
    public bool ShowNetworkMessages => false;
    public bool ShowDeveloperMessages => true;
    public bool ShowVisionMessages => true;
    public bool ShowPathfindingMessages => false;
    public bool ShowCombatMessages => true;

    public LogLevel CurrentLogLevel => LogLevel.Information;

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
        LogCategory.Gameplay => true,
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