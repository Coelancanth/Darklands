namespace Darklands.Application.Common;

/// <summary>
/// Defines logging levels for controlling message verbosity.
/// Higher levels include all messages from lower levels.
/// Follows standard logging conventions for consistency.
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Detailed diagnostic information for troubleshooting.
    /// Only useful during development and debugging sessions.
    /// </summary>
    Debug = 0,

    /// <summary>
    /// General informational messages about application flow.
    /// Useful for understanding system behavior in production.
    /// </summary>
    Information = 1,

    /// <summary>
    /// Warning messages about potentially problematic situations.
    /// Indicates issues that don't prevent operation but should be noted.
    /// </summary>
    Warning = 2,

    /// <summary>
    /// Error messages indicating failure of operations.
    /// Represents problems that prevent successful completion of tasks.
    /// </summary>
    Error = 3
}
