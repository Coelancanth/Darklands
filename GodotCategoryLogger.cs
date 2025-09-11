using Darklands.Core.Domain.Debug;
using Darklands.Core.Infrastructure.Debug;
using Godot;
using Serilog;
using System.Collections.Generic;

namespace Darklands;

/// <summary>
/// Godot-specific extension of CategoryFilteredLogger that adds colored console output.
/// Wraps the core CategoryFilteredLogger while adding Godot console integration.
/// Provides visual distinction between log categories using Godot's rich text colors.
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
        [LogCategory.System] = "#FFFFFF",      // White for system messages
        [LogCategory.Command] = "#00FF00",     // Green for commands
        [LogCategory.Event] = "#FFFF00",       // Yellow for events
        [LogCategory.Thread] = "#FF00FF",      // Magenta for threading
        [LogCategory.AI] = "#FF8000",          // Orange for AI
        [LogCategory.Performance] = "#00FFFF", // Cyan for performance
        [LogCategory.Network] = "#8080FF",     // Light blue for network
        [LogCategory.Developer] = "#808080",   // Gray for developer
        [LogCategory.Vision] = "#FF0080",      // Pink for vision
        [LogCategory.Pathfinding] = "#80FF80", // Light green for pathfinding
        [LogCategory.Combat] = "#FF4040"       // Red for combat
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
    /// Logs a message with specified level and category, with colored Godot console output.
    /// Provides both Serilog structured logging and visual console feedback.
    /// </summary>
    /// <param name="level">The log level of this message</param>
    /// <param name="category">The category this message belongs to</param>
    /// <param name="message">The message to log</param>
    public void Log(LogLevel level, LogCategory category, string message)
    {
        if (!_coreLogger.IsEnabled(level, category))
            return;
            
        // Use core logger for Serilog output with level handling
        _coreLogger.Log(level, category, message);
        
        // Add Godot console output with color coding and level prefix
        var color = CategoryColors.GetValueOrDefault(category, "#FFFFFF");
        var levelPrefix = GetLevelPrefix(level);
        GD.PrintRich($"[color={color}]{levelPrefix}[{category}][/color] {message}");
    }
    
    /// <summary>
    /// Logs a formatted message with specified level and category.
    /// Combines structured logging with visual console feedback.
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
            Log(level, category, formattedMessage);
        }
        catch (System.FormatException)
        {
            // Fallback if string formatting fails
            Log(LogLevel.Warning, category, $"[FORMAT ERROR] {template}");
        }
    }
    
    /// <summary>
    /// Gets a prefix string for the log level to show in console output.
    /// Provides visual indication of message importance.
    /// </summary>
    /// <param name="level">The log level</param>
    /// <returns>Prefix string for console display</returns>
    private static string GetLevelPrefix(LogLevel level) => level switch
    {
        LogLevel.Debug => "[DBG]",
        LogLevel.Information => "[INF]", 
        LogLevel.Warning => "[WRN]",
        LogLevel.Error => "[ERR]",
        _ => "[INF]"
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