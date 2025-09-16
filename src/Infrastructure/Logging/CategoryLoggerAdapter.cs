using System;
using Darklands.Application.Common;
using Microsoft.Extensions.Logging;

namespace Darklands.Application.Infrastructure.Logging;

/// <summary>
/// Adapter that satisfies Microsoft.Extensions.Logging.ILogger<T> by delegating
/// to the unified ICategoryLogger. This allows legacy code to keep injecting
/// ILogger<T> while routing all logs through the new unified pipeline.
/// </summary>
public sealed class CategoryLoggerAdapter<T> : ILogger<T>
{
    private readonly ICategoryLogger _logger;

    public CategoryLoggerAdapter(ICategoryLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

    public void Log<TState>(
        Microsoft.Extensions.Logging.LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (formatter == null)
            return;

        var message = formatter(state, exception);
        if (exception != null)
            message = message + $" Exception: {exception.Message}";

        var (level, category) = Map(logLevel);
        _logger.Log(level, category, message);
    }

    private static (Application.Common.LogLevel level, LogCategory category) Map(Microsoft.Extensions.Logging.LogLevel level)
    {
        return level switch
        {
            Microsoft.Extensions.Logging.LogLevel.Trace => (Application.Common.LogLevel.Debug, LogCategory.Developer),
            Microsoft.Extensions.Logging.LogLevel.Debug => (Application.Common.LogLevel.Debug, LogCategory.Developer),
            Microsoft.Extensions.Logging.LogLevel.Information => (Application.Common.LogLevel.Information, LogCategory.System),
            Microsoft.Extensions.Logging.LogLevel.Warning => (Application.Common.LogLevel.Warning, LogCategory.System),
            Microsoft.Extensions.Logging.LogLevel.Error => (Application.Common.LogLevel.Error, LogCategory.System),
            Microsoft.Extensions.Logging.LogLevel.Critical => (Application.Common.LogLevel.Error, LogCategory.System),
            _ => (Application.Common.LogLevel.Information, LogCategory.System)
        };
    }
}


