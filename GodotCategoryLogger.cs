using Darklands.Core.Domain.Debug;
using Darklands.Core.Infrastructure.Debug;
using Godot;
using Serilog;
using System;
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
    /// Note: Color mapping removed as we now use consistent Serilog formatting
    /// instead of separate GD.PrintRich output for visual consistency.
    /// </summary>

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

        // Add Godot console output with consistent Serilog formatting
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var levelName = GetLevelName(level);
        GD.PrintRich($"[{timestamp}] [{levelName}] [{category}] {message}");
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
            // Use the core logger for file/console output
            _coreLogger.Log(level, category, formattedMessage);
            
            // Add Godot console output with consistent formatting
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var levelName = GetLevelName(level);
            GD.PrintRich($"[{timestamp}] [{levelName}] [{category}] {formattedMessage}");
        }
        catch (System.FormatException)
        {
            // Fallback if string formatting fails
            var errorMsg = $"[FORMAT ERROR] {template}";
            _coreLogger.Log(LogLevel.Warning, category, errorMsg);
            
            // Also show error in Godot console
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            GD.PrintRich($"[{timestamp}] [Warning] [{category}] {errorMsg}");
        }
    }

    /// <summary>
    /// Gets the full level name to match Serilog output format.
    /// </summary>
    /// <param name="level">The log level</param>
    /// <returns>Full level name for consistent formatting</returns>
    private static string GetLevelName(LogLevel level) => level switch
    {
        LogLevel.Debug => "Debug",
        LogLevel.Information => "Information",
        LogLevel.Warning => "Warning",
        LogLevel.Error => "Error",
        _ => "Information"
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
