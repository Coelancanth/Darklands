using System;
using System.IO;
using Godot;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;

namespace Darklands.Infrastructure.Logging;

/// <summary>
/// Serilog sink that writes to Godot's built-in output console using GD.Print().
/// This appears in Godot Editor's "Output" panel at the bottom during development.
/// </summary>
public class GodotConsoleSink : ILogEventSink
{
    private readonly ITextFormatter _formatter;

    /// <summary>
    /// Create sink that writes to Godot's console output.
    /// </summary>
    /// <param name="formatter">Formatter (typically GodotConsoleFormatter for colored output)</param>
    public GodotConsoleSink(ITextFormatter formatter)
    {
        _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
    }

    /// <summary>
    /// Emit a log event to Godot's output console with multi-color BBCode formatting.
    /// Uses GD.PrintRich() for all levels to support colored components (level, timestamp, category).
    /// Format: [LVL] [timestamp] [category] message
    /// Colors: Level-specific, gray timestamp, cyan category, white message
    /// </summary>
    public void Emit(LogEvent logEvent)
    {
        try
        {
            // Format the base log message (plain text)
            var writer = new StringWriter();
            _formatter.Format(logEvent, writer);
            var formatted = writer.ToString().TrimEnd();

            // Parse components for individual coloring
            // Expected format: "[LVL] [HH:mm:ss.fff] [Category] Message"
            var coloredOutput = ApplyColors(formatted, logEvent.Level);

            // Use PrintRich for all levels to support BBCode colors
            // Errors also use PrintRich (not PrintErr) to maintain consistent formatting
            GD.PrintRich(coloredOutput);
        }
        catch (Exception ex)
        {
            // Sink failures should not crash application
            GD.PrintErr($"GodotConsoleSink.Emit failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Apply BBCode color tags to different parts of the log message.
    /// Colors: Level (level-specific), Timestamp (gray), Category (cyan), Message (white/level-specific)
    /// </summary>
    private static string ApplyColors(string formatted, LogEventLevel level)
    {
        // Expected format: "[LVL] [HH:mm:ss.fff] [Category] Message"
        // Split by ']' to identify components
        var parts = formatted.Split(']', 4);

        if (parts.Length < 4)
        {
            // Fallback if format doesn't match expected pattern
            return GetLevelColor(level, formatted);
        }

        var levelPart = parts[0] + "]";      // "[LVL]"
        var timePart = parts[1] + "]";       // " [HH:mm:ss.fff]"
        var categoryPart = parts[2] + "]";   // " [Category]"
        var messagePart = parts[3];          // " Message"

        // Apply colors to each component
        var levelColor = GetLevelColorName(level);
        var coloredLevel = $"[color={levelColor}]{levelPart}[/color]";
        var coloredTime = $"[color=gray]{timePart}[/color]";
        var coloredCategory = $"[color=cyan]{categoryPart}[/color]";

        // Message inherits level color for warnings/errors, white for others
        var messageColor = level >= LogEventLevel.Warning ? levelColor : "white";
        var coloredMessage = $"[color={messageColor}]{messagePart}[/color]";

        return coloredLevel + coloredTime + coloredCategory + coloredMessage;
    }

    /// <summary>
    /// Get BBCode color name for log level.
    /// </summary>
    private static string GetLevelColorName(LogEventLevel level) => level switch
    {
        LogEventLevel.Verbose => "gray",
        LogEventLevel.Debug => "light_gray",
        LogEventLevel.Information => "light_blue",
        LogEventLevel.Warning => "yellow",
        LogEventLevel.Error => "orange_red",
        LogEventLevel.Fatal => "red",
        _ => "white"
    };

    /// <summary>
    /// Fallback: Apply single color to entire message.
    /// </summary>
    private static string GetLevelColor(LogEventLevel level, string message)
    {
        var color = GetLevelColorName(level);
        return $"[color={color}]{message}[/color]";
    }
}