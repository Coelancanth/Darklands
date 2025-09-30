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
    /// Colors: Level+timestamp share color, category (cyan), message (white or level-color for warnings/errors)
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
    /// Colors: Level, Timestamp, and Message all share the same level-specific color. Category is cyan.
    /// This ensures visual consistency across the entire log line.
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

        // Apply colors to each component (Gruvbox theme)
        var levelColor = GetLevelColorName(level);
        var coloredLevel = $"[color={levelColor}]{levelPart}[/color]";
        var coloredTime = $"[color={levelColor}]{timePart}[/color]";  // Same color as level
        var coloredCategory = $"[color=#8ec07c]{categoryPart}[/color]";  // Gruvbox aqua (bright cyan)

        // Message always inherits level color for consistency
        var coloredMessage = $"[color={levelColor}]{messagePart}[/color]";

        return coloredLevel + coloredTime + coloredCategory + coloredMessage;
    }

    /// <summary>
    /// Get BBCode hex color for log level using Gruvbox Dark theme palette.
    /// Gruvbox: Retro groove color scheme with warm, muted tones (easy on eyes).
    /// Base16 compatible for consistency across terminals/editors.
    /// </summary>
    private static string GetLevelColorName(LogEventLevel level) => level switch
    {
        LogEventLevel.Verbose => "#928374",      // Gruvbox gray (fg4)
        LogEventLevel.Debug => "#a89984",        // Gruvbox light gray (fg3)
        LogEventLevel.Information => "#83a598",  // Gruvbox blue (bright blue)
        LogEventLevel.Warning => "#fabd2f",      // Gruvbox yellow (bright yellow)
        LogEventLevel.Error => "#fb4934",        // Gruvbox red (bright red)
        LogEventLevel.Fatal => "#cc241d",        // Gruvbox dark red (red)
        _ => "#ebdbb2"                           // Gruvbox fg0 (default foreground)
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