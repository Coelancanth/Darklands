using System;
using Darklands.Application.Common;

namespace Darklands.Application.Infrastructure.Logging;

/// <summary>
/// Unified logger that formats a single line and forwards to configured outputs.
/// Applies IDebugConfiguration filters for level and category.
/// </summary>
public sealed class UnifiedCategoryLogger : ICategoryLogger
{
    private readonly ILogOutput _output;
    private readonly IDebugConfiguration _config;
    private readonly string _timestampFormat = "HH:mm:ss.fff";

    public UnifiedCategoryLogger(ILogOutput output, IDebugConfiguration config)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public void Log(LogCategory category, string message)
    {
        Log(LogLevel.Information, category, message);
    }

    public void Log(LogCategory category, string template, params object[] args)
    {
        Log(LogLevel.Information, category, template, args);
    }

    public void Log(LogLevel level, LogCategory category, string message)
    {
        if (!_config.ShouldLog(level, category))
            return;

        var timestamp = DateTime.Now.ToString(_timestampFormat);
        var categoryText = category.ToString();
        var formatted = $"[{timestamp}] [{level.ToString()[..3]}] [{categoryText}] {message}";
        _output.WriteLine(level, categoryText, message, formatted);
    }

    public void Log(LogLevel level, LogCategory category, string template, params object[] args)
    {
        if (!_config.ShouldLog(level, category))
            return;

        string message;
        try
        {
            if (args == null || args.Length == 0)
            {
                message = template;
            }
            else
            {
                // Try positional formatting first (e.g., {0}, {1})
                try
                {
                    message = string.Format(template, args);
                }
                catch (FormatException)
                {
                    // If positional fails, try named placeholder substitution
                    message = SubstituteNamedPlaceholders(template, args);
                }
            }
        }
        catch (FormatException)
        {
            message = template; // Final fallback on bad format
        }

        var timestamp = DateTime.Now.ToString(_timestampFormat);
        var categoryText = category.ToString();
        var formatted = $"[{timestamp}] [{level.ToString()[..3]}] [{categoryText}] {message}";
        _output.WriteLine(level, categoryText, message, formatted);
    }

    public bool IsEnabled(LogCategory category)
    {
        return _config.ShouldLog(category);
    }

    public bool IsEnabled(LogLevel level, LogCategory category)
    {
        return _config.ShouldLog(level, category);
    }

    public void Flush() => _output.Flush();


    /// <summary>
    /// Substitutes named placeholders like {ActorId}, {Position} with provided arguments.
    /// This provides compatibility with existing logging code that uses named placeholders.
    /// Arguments are substituted in order of appearance in the template.
    /// </summary>
    private static string SubstituteNamedPlaceholders(string template, object[] args)
    {
        if (args == null || args.Length == 0)
            return template;

        var result = template;
        int argIndex = 0;

        // List of common named placeholders in order of priority
        string[] placeholders = {
            "{ActorId}", "{FromPosition}", "{ToPosition}", "{Position}",
            "{Damage}", "{Source}", "{Health}", "{Error}", "{Exception}",
            "{Visible}", "{Explored}", "{Turn}", "{Range}", "{X}", "{Y}"
        };

        // Replace placeholders with arguments in the order they appear
        foreach (var placeholder in placeholders)
        {
            if (result.Contains(placeholder, StringComparison.OrdinalIgnoreCase) && argIndex < args.Length)
            {
                var argValue = args[argIndex]?.ToString() ?? "null";
                result = result.Replace(placeholder, argValue, StringComparison.OrdinalIgnoreCase);
                argIndex++;
            }
        }

        return result;
    }
}


