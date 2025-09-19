using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Godot;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;
using Serilog.Parsing;

namespace Darklands.Infrastructure.Logging;

/// <summary>
/// Custom Serilog sink that outputs to the Godot Editor console using GD.PrintRich.
/// Provides rich formatting with colors, coordinate highlighting, and enhanced readability
/// for game development debugging in the Godot Editor environment.
/// 
/// This sink bridges Serilog's structured logging with Godot's Editor Output panel,
/// eliminating the need for dual logging patterns (ILogger + GD.Print).
/// </summary>
public class GodotConsoleSink : ILogEventSink
{
    private readonly ITextFormatter _formatter;

    public GodotConsoleSink(string outputTemplate, IFormatProvider? formatProvider)
    {
        _formatter = new TemplateRenderer(outputTemplate, formatProvider);
    }

    public void Emit(LogEvent logEvent)
    {
        try
        {
            using var writer = new StringWriter();
            _formatter.Format(logEvent, writer);
            writer.Flush();

            var color = GetLogLevelColor(logEvent.Level);

            // Split into lines and process each line separately to avoid empty line artifacts
            var lines = writer.ToString()?.Split('\n') ?? Array.Empty<string>();

            foreach (var line in lines)
            {
                if (!string.IsNullOrEmpty(line))
                {
                    var enhancedLine = EnhanceLineFormatting(line);
                    GD.PrintRich($"[color=#{color}]{enhancedLine}[/color]");
                }
            }

            // Handle exceptions with appropriate Godot error reporting
            if (logEvent.Exception != null)
            {
                if (logEvent.Level >= LogEventLevel.Error)
                    GD.PushError(logEvent.Exception.ToString());
                else
                    GD.PushWarning(logEvent.Exception.ToString());
            }
        }
        catch
        {
            // CRITICAL: Never crash the application due to logging failure
            // Silent failure is acceptable for logging infrastructure
        }
    }

    /// <summary>
    /// Maps Serilog log levels to Godot color hex codes for rich console output.
    /// </summary>
    private static string GetLogLevelColor(LogEventLevel level)
    {
        return level switch
        {
            LogEventLevel.Verbose => Colors.Gray.ToHtml(false),
            LogEventLevel.Debug => Colors.SpringGreen.ToHtml(false),
            LogEventLevel.Information => Colors.Cyan.ToHtml(false),
            LogEventLevel.Warning => Colors.Yellow.ToHtml(false),
            LogEventLevel.Error => Colors.Red.ToHtml(false),
            LogEventLevel.Fatal => Colors.Purple.ToHtml(false),
            _ => Colors.LightGray.ToHtml(false),
        };
    }

    /// <summary>
    /// Enhances log lines with rich formatting for common patterns like coordinates,
    /// timing values, and status indicators to improve debugging readability.
    /// </summary>
    private static string EnhanceLineFormatting(string line)
    {
        // Highlight coordinate patterns like (X, Y) or (X,Y) with orange color
        line = System.Text.RegularExpressions.Regex.Replace(line,
            @"\((\d+),\s*(\d+)\)",
            "[/color][color=orange]($1, $2)[/color][color=inherit]");

        // Highlight grid dimensions like "10x8" with lime color
        line = System.Text.RegularExpressions.Regex.Replace(line,
            @"\b(\d+)x(\d+)\b",
            "[/color][color=lime]$1x$2[/color][color=inherit]");

        // Highlight timing values (e.g., "7ms", "150ms") with yellow
        line = System.Text.RegularExpressions.Regex.Replace(line,
            @"\b(\d+)ms\b",
            "[/color][color=yellow]$1ms[/color][color=inherit]");

        // Highlight positive status words with light green
        line = System.Text.RegularExpressions.Regex.Replace(line,
            @"\b(SUCCESS|COMPLETED|READY|INITIALIZED)\b",
            "[/color][color=lightgreen]$1[/color][color=inherit]");

        // Highlight negative status words with red
        line = System.Text.RegularExpressions.Regex.Replace(line,
            @"\b(FAILED|ERROR|CRITICAL|MISSING)\b",
            "[/color][color=red]$1[/color][color=inherit]");

        // Highlight actor/entity IDs like "Actor_123" or "ActorId: 456"
        line = System.Text.RegularExpressions.Regex.Replace(line,
            @"\b(Actor|Entity)([_\s:]+)(\d+)\b",
            "[/color][color=lightblue]$1$2$3[/color][color=inherit]");

        return line;
    }

    /// <summary>
    /// Custom template renderer that processes Serilog message templates
    /// and formats them for Godot console output.
    /// </summary>
    private class TemplateRenderer : ITextFormatter
    {
        private delegate void Renderer(LogEvent logEvent, TextWriter output);

        private readonly Renderer[] _renderers;
        private readonly IFormatProvider? _formatProvider;

        public TemplateRenderer(string outputTemplate, IFormatProvider? formatProvider)
        {
            _formatProvider = formatProvider;

            var template = new MessageTemplateParser().Parse(outputTemplate);
            _renderers = template.Tokens.Select(CreateRenderer).OfType<Renderer>().ToArray();
        }

        public void Format(LogEvent logEvent, TextWriter output)
        {
            foreach (var renderer in _renderers)
                renderer.Invoke(logEvent, output);
        }

        private Renderer? CreateRenderer(MessageTemplateToken token)
        {
            return token switch
            {
                TextToken textToken =>
                    (_, output) => output.Write(textToken.Text),

                PropertyToken propertyToken => propertyToken.PropertyName switch
                {
                    OutputProperties.LevelPropertyName =>
                        (logEvent, output) => output.Write(logEvent.Level),
                    OutputProperties.MessagePropertyName =>
                        (logEvent, output) => logEvent.RenderMessage(output, _formatProvider),
                    OutputProperties.NewLinePropertyName =>
                        (_, output) => output.Write('\n'),
                    OutputProperties.TimestampPropertyName =>
                        RenderTimestamp(propertyToken.Format),
                    "SourceContext" =>
                        RenderSourceContext(propertyToken.Format),
                    _ =>
                        RenderProperty(propertyToken.PropertyName, propertyToken.Format),
                },
                _ => null,
            };
        }

        private Renderer RenderTimestamp(string? format)
        {
            var formatter = _formatProvider?.GetFormat(typeof(ICustomFormatter)) as ICustomFormatter;

            return (logEvent, output) =>
            {
                var timestampText = formatter != null
                    ? formatter.Format(format, logEvent.Timestamp, _formatProvider)
                    : logEvent.Timestamp.ToString(format, _formatProvider ?? CultureInfo.InvariantCulture);

                output.Write(timestampText);
            };
        }

        private Renderer RenderSourceContext(string? format)
        {
            return (logEvent, output) =>
            {
                if (logEvent.Properties.TryGetValue("SourceContext", out var sourceContext))
                {
                    // Extract class name from full namespace for cleaner output
                    var contextValue = sourceContext.ToString().Trim('"');
                    var shortContext = contextValue.Split('.').LastOrDefault() ?? contextValue;
                    output.Write(shortContext);
                }
                else
                {
                    output.Write("System");
                }
            };
        }

        private Renderer RenderProperty(string propertyName, string? format)
        {
            return (logEvent, output) =>
            {
                if (logEvent.Properties.TryGetValue(propertyName, out var propertyValue))
                    propertyValue.Render(output, format, _formatProvider);
            };
        }
    }
}

/// <summary>
/// Extension methods for configuring the GodotConsoleSink in Serilog configurations.
/// </summary>
public static class GodotSinkExtensions
{
    /// <summary>
    /// Default output template optimized for Godot console readability.
    /// Shows timestamp, level, short source context, and message with exception details.
    /// </summary>
    public const string DefaultGodotOutputTemplate =
        "[{Timestamp:HH:mm:ss}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}";

    /// <summary>
    /// Adds the GodotConsoleSink to the Serilog configuration.
    /// </summary>
    /// <param name="configuration">The Serilog sink configuration.</param>
    /// <param name="outputTemplate">Custom output template (optional).</param>
    /// <param name="formatProvider">Custom format provider (optional).</param>
    /// <returns>Logger configuration for method chaining.</returns>
    public static LoggerConfiguration Godot(
        this LoggerSinkConfiguration configuration,
        string outputTemplate = DefaultGodotOutputTemplate,
        IFormatProvider? formatProvider = null)
    {
        return configuration.Sink(new GodotConsoleSink(outputTemplate, formatProvider));
    }
}
