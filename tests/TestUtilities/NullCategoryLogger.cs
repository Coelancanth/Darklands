using Darklands.Application.Common;

namespace Darklands.Core.Tests.TestUtilities;

/// <summary>
/// Null object implementation of ICategoryLogger for unit testing.
/// Provides the required logger dependency without any actual logging behavior.
/// </summary>
public sealed class NullCategoryLogger : ICategoryLogger
{
    /// <summary>
    /// Singleton instance for efficient reuse across tests.
    /// </summary>
    public static readonly NullCategoryLogger Instance = new();

    public NullCategoryLogger() { }

    public void Log(LogCategory category, string message) { }

    public void Log(LogCategory category, string template, params object[] args) { }

    public void Log(LogLevel level, LogCategory category, string message) { }

    public void Log(LogLevel level, LogCategory category, string template, params object[] args) { }

    public bool IsEnabled(LogCategory category) => false;

    public bool IsEnabled(LogLevel level, LogCategory category) => false;

    public void Flush() { }
}
