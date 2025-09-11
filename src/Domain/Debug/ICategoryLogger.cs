namespace Darklands.Core.Domain.Debug;

/// <summary>
/// Interface for category-aware logging that respects debug configuration settings.
/// Extends basic logging with category filtering and runtime configuration support.
/// Implementations should check configuration settings before outputting log messages.
/// </summary>
public interface ICategoryLogger
{
    /// <summary>
    /// Logs a message under the specified category, respecting configuration filters.
    /// Only outputs the message if the category is enabled in the debug configuration.
    /// Defaults to Information log level.
    /// </summary>
    /// <param name="category">The category this message belongs to</param>
    /// <param name="message">The message to log</param>
    void Log(LogCategory category, string message);

    /// <summary>
    /// Logs a formatted message under the specified category with arguments.
    /// Only outputs the message if the category is enabled in the debug configuration.
    /// Defaults to Information log level.
    /// </summary>
    /// <param name="category">The category this message belongs to</param>
    /// <param name="template">The message template with placeholders</param>
    /// <param name="args">Arguments to substitute in the template</param>
    void Log(LogCategory category, string template, params object[] args);

    /// <summary>
    /// Logs a message with specified level and category, respecting configuration filters.
    /// Only outputs if both the category is enabled AND the level meets the minimum threshold.
    /// </summary>
    /// <param name="level">The log level of this message</param>
    /// <param name="category">The category this message belongs to</param>
    /// <param name="message">The message to log</param>
    void Log(LogLevel level, LogCategory category, string message);

    /// <summary>
    /// Logs a formatted message with specified level and category.
    /// Only outputs if both the category is enabled AND the level meets the minimum threshold.
    /// </summary>
    /// <param name="level">The log level of this message</param>
    /// <param name="category">The category this message belongs to</param>
    /// <param name="template">The message template with placeholders</param>
    /// <param name="args">Arguments to substitute in the template</param>
    void Log(LogLevel level, LogCategory category, string template, params object[] args);

    /// <summary>
    /// Checks if logging is enabled for the specified category without actually logging.
    /// Useful for performance-sensitive scenarios where message construction is expensive.
    /// </summary>
    /// <param name="category">The category to check</param>
    /// <returns>True if logging is enabled for this category</returns>
    bool IsEnabled(LogCategory category);

    /// <summary>
    /// Checks if logging is enabled for the specified level and category.
    /// Useful for performance-sensitive scenarios where message construction is expensive.
    /// </summary>
    /// <param name="level">The log level to check</param>
    /// <param name="category">The category to check</param>
    /// <returns>True if logging is enabled for this level and category</returns>
    bool IsEnabled(LogLevel level, LogCategory category);
}
