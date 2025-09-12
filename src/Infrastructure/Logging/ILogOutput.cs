using System;
using Darklands.Core.Domain.Debug;

namespace Darklands.Core.Infrastructure.Logging;

/// <summary>
/// Abstraction for log output destinations. Implementations write formatted
/// lines to a specific destination (console, file, etc.).
/// </summary>
public interface ILogOutput : IDisposable
{
    /// <summary>
    /// Writes a single line to the output. Both raw and pre-formatted messages are provided.
    /// </summary>
    /// <param name="level">Severity level</param>
    /// <param name="category">Category name</param>
    /// <param name="message">Raw message content</param>
    /// <param name="formattedMessage">Fully formatted line for plain outputs</param>
    void WriteLine(LogLevel level, string category, string message, string formattedMessage);

    /// <summary>
    /// Flushes any buffered data to the destination.
    /// </summary>
    void Flush();

    /// <summary>
    /// Enables or disables this output at runtime.
    /// </summary>
    bool IsEnabled { get; set; }
}


