using System;
using System.IO;
using Serilog.Events;
using Serilog.Formatting;

namespace Darklands.Presentation.Infrastructure.Logging;

/// <summary>
/// Formats log events for Godot's console output panel with BBCode color tags.
/// GD.PrintRich() supports BBCode, enabling multi-color output for better readability.
/// Format: [LVL] [HH:mm:ss.fff] [Category] Message
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

        // Format with BBCode color tags: [LVL] [HH:mm:ss.fff] [Category] Message
        // Note: Color is applied at sink level (GodotConsoleSink), not here
        var levelCode = GetLevelCode(level);

        output.Write($"[{levelCode}] ");
        output.Write($"[{timestamp}] ");
        output.Write($"[{category}] ");
        output.WriteLine(message);

        // TODO: Support structured logging placeholders in future
        // Example: logger.LogInformation("Player {PlayerId} took {Damage} damage", id, dmg)
        // Would render as: "Player 42 took 10 damage" with highlighted values
    }

    /// <summary>
    /// Get 3-letter code for log level.
    /// </summary>
    private static string GetLevelCode(LogEventLevel level) => level switch
    {
        LogEventLevel.Verbose => "VRB",
        LogEventLevel.Debug => "DEB",
        LogEventLevel.Information => "INF",
        LogEventLevel.Warning => "WRN",
        LogEventLevel.Error => "ERR",
        LogEventLevel.Fatal => "FTL",
        _ => "???"
    };
}