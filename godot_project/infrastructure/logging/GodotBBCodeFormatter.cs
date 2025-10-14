using System;
using System.IO;
using Serilog.Events;
using Serilog.Formatting;

namespace Darklands.Presentation.Infrastructure.Logging;

/// <summary>
/// Formats log events as BBCode for Godot's RichTextLabel.
/// Godot uses BBCode markup (not ANSI escape codes) for rich text rendering.
/// </summary>
public class GodotBBCodeFormatter : ITextFormatter
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

        // Render as BBCode with color-coded levels
        var levelColor = GetColorForLevel(level);
        var levelText = level.ToString().ToUpper().PadRight(5);

        output.Write($"[color={levelColor}][b]{levelText}[/b][/color] ");
        output.Write($"[color=gray]{timestamp}[/color] ");
        output.Write($"[color=cyan]{category}[/color]: ");
        output.WriteLine(message);
    }

    /// <summary>
    /// Get BBCode color hex for log level.
    /// Colors chosen for visibility and semantic meaning.
    /// </summary>
    private static string GetColorForLevel(LogEventLevel level) => level switch
    {
        LogEventLevel.Verbose => "#666666",      // Dark gray (low importance)
        LogEventLevel.Debug => "#808080",        // Gray (development info)
        LogEventLevel.Information => "#00CED1",  // Cyan (normal operation)
        LogEventLevel.Warning => "#FFD700",      // Gold (attention needed)
        LogEventLevel.Error => "#FF4500",        // OrangeRed (problems)
        LogEventLevel.Fatal => "#FF0000",        // Red (critical failures)
        _ => "#FFFFFF"                           // White (fallback)
    };
}