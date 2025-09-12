using Darklands.Core.Domain.Debug;
using Darklands.Core.Infrastructure.Debug;
using Godot;
using Serilog;
using System;
using System.Collections.Generic;

namespace Darklands;

/// <summary>
/// Godot-specific logger that provides category-based filtering for game logs.
/// Routes all output through the unified Serilog pipeline to GodotConsoleSink,
/// ensuring consistent formatting across all log sources with rich text colors.
/// </summary>
public sealed class GodotCategoryLogger : ICategoryLogger
{
    private readonly CategoryFilteredLogger _coreLogger;
    
    /// <summary>
    /// Maps log categories to console colors for visual distinction.
    /// Uses Godot's rich text color codes for enhanced readability.
    /// </summary>
    private static readonly Dictionary<LogCategory, string> CategoryColors = new()
    {
        [LogCategory.System] = "#AAAAAA",      // Light gray for system messages
        [LogCategory.Command] = "#00FF00",     // Green for commands
        [LogCategory.Event] = "#FFFF00",       // Yellow for events
        [LogCategory.Thread] = "#FF00FF",      // Magenta for threading
        [LogCategory.AI] = "#FF8000",          // Orange for AI
        [LogCategory.Performance] = "#00FFFF", // Cyan for performance
        [LogCategory.Network] = "#8080FF",     // Light blue for network
        [LogCategory.Developer] = "#606060",   // Dark gray for developer
        [LogCategory.Gameplay] = "#4080FF",    // Medium blue for gameplay
        [LogCategory.Vision] = "#FF0080",      // Pink for vision
        [LogCategory.Pathfinding] = "#80FF80", // Light green for pathfinding
        [LogCategory.Combat] = "#FF4040"       // Light red for combat
    };

    /// <summary>
    /// Maps log levels to console colors for visual distinction.
    /// </summary>
    private static readonly Dictionary<LogLevel, string> LevelColors = new()
    {
        [LogLevel.Debug] = "#808080",      // Gray for debug
        [LogLevel.Information] = "#00AAFF", // Light blue for information
        [LogLevel.Warning] = "#FFA500",     // Orange for warnings
        [LogLevel.Error] = "#FF0000"        // Red for errors
    };


    public GodotCategoryLogger(ILogger serilogLogger, IDebugConfiguration config)
    {
        _coreLogger = new CategoryFilteredLogger(serilogLogger, config);
    }

    /// <summary>
    /// Logs a message under the specified category with both Serilog and Godot console output.
    /// Uses Information level by default for backward compatibility.
    /// </summary>
    /// <param name="category">The category this message belongs to</param>
    /// <param name="message">The message to log</param>
    public void Log(LogCategory category, string message)
    {
        Log(LogLevel.Information, category, message);
    }

    /// <summary>
    /// Logs a formatted message under the specified category with arguments.
    /// Uses Information level by default for backward compatibility.
    /// </summary>
    /// <param name="category">The category this message belongs to</param>
    /// <param name="template">The message template with placeholders</param>
    /// <param name="args">Arguments to substitute in the template</param>
    public void Log(LogCategory category, string template, params object[] args)
    {
        Log(LogLevel.Information, category, template, args);
    }

    /// <summary>
    /// Logs a message with specified level and category through the unified Serilog pipeline.
    /// </summary>
    /// <param name="level">The log level of this message</param>
    /// <param name="category">The category this message belongs to</param>
    /// <param name="message">The message to log</param>
    public void Log(LogLevel level, LogCategory category, string message)
    {
        if (!_coreLogger.IsEnabled(level, category))
            return;

        // Use core logger for Serilog output (file logging, etc)
        _coreLogger.Log(level, category, message);
        
        // Output to Godot console with rich text formatting for visual distinction
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var levelName = GetAbbreviatedLevel(level);
        var levelColor = LevelColors.GetValueOrDefault(level, "#FFFFFF");
        var categoryColor = CategoryColors.GetValueOrDefault(category, "#FFFFFF");
        
        // Enhanced message formatting with coordinate highlighting
        var enhancedMessage = EnhanceMessageFormatting(message);
        
        GD.PrintRich($"[color=#666666][{timestamp}][/color] [color={levelColor}][b][{levelName}][/b][/color] [color={categoryColor}][{category}][/color] {enhancedMessage}");
    }

    /// <summary>
    /// Logs a formatted message with specified level and category through the unified pipeline.
    /// </summary>
    /// <param name="level">The log level of this message</param>
    /// <param name="category">The category this message belongs to</param>
    /// <param name="template">The message template with placeholders</param>
    /// <param name="args">Arguments to substitute in the template</param>
    public void Log(LogLevel level, LogCategory category, string template, params object[] args)
    {
        if (!_coreLogger.IsEnabled(level, category))
            return;

        try
        {
            var formattedMessage = string.Format(template, args);
            // Use the core logger for file output
            _coreLogger.Log(level, category, formattedMessage);
            
            // Output to Godot console with rich formatting
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var levelName = GetAbbreviatedLevel(level);
            var levelColor = LevelColors.GetValueOrDefault(level, "#FFFFFF");
            var categoryColor = CategoryColors.GetValueOrDefault(category, "#FFFFFF");
            
            // Enhanced message formatting
            var enhancedMessage = EnhanceMessageFormatting(formattedMessage);
            
            GD.PrintRich($"[color=#666666][{timestamp}][/color] [color={levelColor}][b][{levelName}][/b][/color] [color={categoryColor}][{category}][/color] {enhancedMessage}");
        }
        catch (System.FormatException)
        {
            // Fallback if string formatting fails
            var errorMsg = $"[FORMAT ERROR] {template}";
            _coreLogger.Log(LogLevel.Warning, category, errorMsg);
            
            // Show error in Godot console with warning colors
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var warningColor = LevelColors[LogLevel.Warning];
            var categoryColor = CategoryColors.GetValueOrDefault(category, "#FFFFFF");
            
            GD.PrintRich($"[color=#666666][{timestamp}][/color] [color={warningColor}][b][WRN][/b][/color] [color={categoryColor}][{category}][/color] [color={warningColor}]{errorMsg}[/color]");
        }
    }


    /// <summary>
    /// Enhances log messages with rich text formatting for common patterns.
    /// Highlights coordinates, actor IDs, health values, etc. for better readability.
    /// </summary>
    private static string EnhanceMessageFormatting(string message)
    {
        // Highlight coordinate patterns like (X, Y) or (X,Y) with orange color
        message = System.Text.RegularExpressions.Regex.Replace(message,
            @"\((\d+),\s*(\d+)\)",
            "[color=orange]($1, $2)[/color]");

        // Highlight Actor IDs like "Actor_123abc" with light blue
        message = System.Text.RegularExpressions.Regex.Replace(message,
            @"\b(Actor_[a-f0-9]+)\b",
            "[color=#88CCFF]$1[/color]");

        // Highlight health/damage numbers with appropriate colors
        message = System.Text.RegularExpressions.Regex.Replace(message,
            @"\b(\d+)\s+(damage|health|hp)\b",
            "[color=#FF6666]$1[/color] $2", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Highlight success/failure keywords
        message = System.Text.RegularExpressions.Regex.Replace(message,
            @"\b(success|successful|succeeded|complete|completed)\b",
            "[color=#66FF66]$1[/color]",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        message = System.Text.RegularExpressions.Regex.Replace(message,
            @"\b(fail|failed|error|invalid|missing)\b",
            "[color=#FF6666]$1[/color]",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return message;
    }

    /// <summary>
    /// Gets abbreviated level name for consistent formatting with Serilog.
    /// Matches the format used by {Level:u3} in templates.
    /// </summary>
    private static string GetAbbreviatedLevel(LogLevel level) => level switch
    {
        LogLevel.Debug => "DBG",
        LogLevel.Information => "INF", 
        LogLevel.Warning => "WRN",
        LogLevel.Error => "ERR",
        _ => "INF"
    };

    /// <summary>
    /// Checks if logging is enabled for the specified category.
    /// Delegates to core logger for consistency.
    /// </summary>
    /// <param name="category">The category to check</param>
    /// <returns>True if logging is enabled for this category</returns>
    public bool IsEnabled(LogCategory category) => _coreLogger.IsEnabled(category);

    /// <summary>
    /// Checks if logging is enabled for the specified level and category.
    /// Delegates to core logger for consistency.
    /// </summary>
    /// <param name="level">The log level to check</param>
    /// <param name="category">The category to check</param>
    /// <returns>True if logging is enabled for this level and category</returns>
    public bool IsEnabled(LogLevel level, LogCategory category) => _coreLogger.IsEnabled(level, category);
}
