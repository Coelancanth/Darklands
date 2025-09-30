using System;
using System.IO;
using Serilog.Events;
using Serilog.Formatting;

namespace Darklands.Infrastructure.Logging;

/// <summary>
/// Formats log events for Godot's console output panel.
/// Godot console only colors ERROR/FATAL (via GD.PrintErr), others are plain text.
/// Format: [LEVEL] [HH:mm:ss.fff] Category: Message
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

        // Format: [LEVEL] [HH:mm:ss.fff] Category: Message
        var levelText = level.ToString().ToUpper().PadRight(11);  // "INFORMATION" is longest

        output.Write($"[{levelText}] ");
        output.Write($"[{timestamp}] ");
        output.Write($"{category}: ");
        output.WriteLine(message);
    }
}