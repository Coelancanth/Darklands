using Darklands.Core.Domain.Debug;
using Serilog;
using System;
using System.Collections.Generic;

namespace Darklands.Core.Infrastructure.Debug;

/// <summary>
/// Logger implementation that filters messages based on debug configuration categories.
/// Wraps Serilog ILogger while adding category-based filtering and color-coded console output.
/// Respects debug configuration settings to provide runtime control over log verbosity.
/// </summary>
public sealed class CategoryFilteredLogger : ICategoryLogger
{
    private readonly ILogger _serilogLogger;
    private readonly IDebugConfiguration _config;

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

    public CategoryFilteredLogger(ILogger serilogLogger, IDebugConfiguration config)
    {
        _serilogLogger = serilogLogger ?? throw new ArgumentNullException(nameof(serilogLogger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Logs a message under the specified category if category logging is enabled.
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
    /// Logs a message with specified level and category, respecting both filters.
    /// Only outputs if both the category is enabled AND the level meets the threshold.
    /// </summary>
    /// <param name="level">The log level of this message</param>
    /// <param name="category">The category this message belongs to</param>
    /// <param name="message">The message to log</param>
    public void Log(LogLevel level, LogCategory category, string message)
    {
        if (!_config.ShouldLog(level, category))
            return;

        // Map our log level to Serilog level and log with category context
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

    /// <summary>
    /// Logs a formatted message with specified level and category.
    /// Only outputs if both the category is enabled AND the level meets the threshold.
    /// </summary>
    /// <param name="level">The log level of this message</param>
    /// <param name="category">The category this message belongs to</param>
    /// <param name="template">The message template with placeholders</param>
    /// <param name="args">Arguments to substitute in the template</param>
    public void Log(LogLevel level, LogCategory category, string template, params object[] args)
    {
        if (!_config.ShouldLog(level, category))
            return;

        try
        {
            var formattedMessage = string.Format(template, args);
            Log(level, category, formattedMessage);
        }
        catch (FormatException ex)
        {
            // Fallback if string formatting fails
            _serilogLogger.Warning("Failed to format log message template '{Template}': {Error}",
                template, ex.Message);
            Log(LogLevel.Warning, category, $"[FORMAT ERROR] {template}");
        }
    }

    /// <summary>
    /// Checks if logging is enabled for the specified category.
    /// Useful for performance-sensitive scenarios where message construction is expensive.
    /// </summary>
    /// <param name="category">The category to check</param>
    /// <returns>True if logging is enabled for this category</returns>
    public bool IsEnabled(LogCategory category) => _config.ShouldLog(category);

    /// <summary>
    /// Checks if logging is enabled for the specified level and category.
    /// Useful for performance-sensitive scenarios where message construction is expensive.
    /// </summary>
    /// <param name="level">The log level to check</param>
    /// <param name="category">The category to check</param>
    /// <returns>True if logging is enabled for this level and category</returns>
    public bool IsEnabled(LogLevel level, LogCategory category) => _config.ShouldLog(level, category);
}
