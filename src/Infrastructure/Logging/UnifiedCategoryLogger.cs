using System;
using Darklands.Core.Domain.Debug;

namespace Darklands.Core.Infrastructure.Logging;

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
            message = args == null || args.Length == 0 ? template : string.Format(template, args);
        }
        catch (FormatException)
        {
            message = template; // Fallback on bad format
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
}


