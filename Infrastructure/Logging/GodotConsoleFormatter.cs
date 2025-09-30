using System;
using System.IO;
using Serilog.Events;
using Serilog.Formatting;

namespace Darklands.Infrastructure.Logging;

/// <summary>
/// Formats log events for Godot's console output panel.
/// Godot console doesn't support BBCode or ANSI, so uses plain text with emoji indicators.
/// </summary>
public class GodotConsoleFormatter : ITextFormatter
{
    public void Format(LogEvent logEvent, TextWriter output)
    {
        var level = logEvent.Level;
        var timestamp = logEvent.Timestamp.ToString("HH:mm:ss.fff");
        var message = logEvent.RenderMessage();

        // Extract category from SourceContext property
        var category = "Infrastructure";  // Default fallback
        if (logEvent.Properties.TryGetValue("SourceContext", out var ctxValue))
        {
            var fullName = ctxValue.ToString().Trim('"');
            category = LoggingService.ExtractCategory(fullName) ?? "Infrastructure";
        }

        // Format with emoji indicators for level (Godot console doesn't support colors)
        var levelIndicator = GetIndicatorForLevel(level);
        var levelText = level.ToString().ToUpper().PadRight(5);

        output.Write($"{levelIndicator} ");
        output.Write($"[{levelText}] ");
        output.Write($"{timestamp} ");
        output.Write($"{category}: ");
        output.WriteLine(message);
    }

    /// <summary>
    /// Get emoji indicator for log level.
    /// Godot's output panel doesn't support ANSI colors, but emojis are visible.
    /// </summary>
    private static string GetIndicatorForLevel(LogEventLevel level) => level switch
    {
        LogEventLevel.Verbose => "⚫",      // Black circle (verbose)
        LogEventLevel.Debug => "🔵",        // Blue circle (debug)
        LogEventLevel.Information => "✅",  // Green check (info)
        LogEventLevel.Warning => "⚠️",      // Warning sign (warning)
        LogEventLevel.Error => "❌",        // Red X (error)
        LogEventLevel.Fatal => "💀",        // Skull (fatal)
        _ => "⚪"                            // White circle (unknown)
    };
}