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
    /// Emit a log event to Godot's output console.
    /// Uses GD.Print() for normal logs, GD.PrintErr() for errors/fatal.
    /// </summary>
    public void Emit(LogEvent logEvent)
    {
        try
        {
            // Format the log message
            var writer = new StringWriter();
            _formatter.Format(logEvent, writer);
            var formatted = writer.ToString();

            // Route to appropriate Godot print function based on level
            // GD.PrintErr shows in red in Godot's output panel
            if (logEvent.Level >= LogEventLevel.Error)
            {
                GD.PrintErr(formatted);
            }
            else
            {
                GD.Print(formatted);
            }
        }
        catch (Exception ex)
        {
            // Sink failures should not crash application
            GD.PrintErr($"GodotConsoleSink.Emit failed: {ex.Message}");
        }
    }
}