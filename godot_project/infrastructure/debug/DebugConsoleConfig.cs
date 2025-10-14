using Godot;

namespace Darklands.Presentation.Infrastructure.Debug;

/// <summary>
/// Configuration resource for Debug Console default settings.
///
/// WHY RESOURCE:
/// - Hot-reloadable: Designers can edit .tres file without code changes
/// - Version controlled: Committed defaults shared across team
/// - Godot-native: Editable in inspector with proper UI controls
///
/// SEPARATION OF CONCERNS:
/// - This .tres file = Design-time defaults (committed, team-shared)
/// - JSON user state = Runtime preferences (local, gitignored)
///
/// Usage: Create DebugConsoleConfig.tres instance, assign defaults in inspector,
///        controller loads this for fallback values when JSON state doesn't exist.
/// </summary>
[GlobalClass]
public partial class DebugConsoleConfig : Resource
{
    /// <summary>
    /// Godot-friendly log level enum (maps to Serilog's LogEventLevel in controller).
    /// Using local enum ensures proper Inspector UI (dropdown, not raw int).
    /// </summary>
    public enum LogLevel
    {
        Verbose = 0,
        Debug = 1,
        Information = 2,
        Warning = 3,
        Error = 4,
        Fatal = 5
    }

    /// <summary>
    /// Default minimum log level when no user preference exists.
    /// Sane default: Information (hides Debug/Verbose noise).
    /// </summary>
    [Export]
    public LogLevel DefaultLogLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Default enabled categories when no user preference exists.
    /// Empty = all categories enabled (discovered at runtime).
    ///
    /// WHY EMPTY: Categories are discovered dynamically from Features.* namespaces,
    ///            different builds may have different features, can't hardcode.
    /// </summary>
    [Export]
    public string[] DefaultEnabledCategories { get; set; } = System.Array.Empty<string>();

    /// <summary>
    /// Map our Godot-friendly enum to Serilog's enum.
    /// Called by controller during initialization.
    /// </summary>
    public Serilog.Events.LogEventLevel ToSerilogLevel() => DefaultLogLevel switch
    {
        LogLevel.Verbose => Serilog.Events.LogEventLevel.Verbose,
        LogLevel.Debug => Serilog.Events.LogEventLevel.Debug,
        LogLevel.Information => Serilog.Events.LogEventLevel.Information,
        LogLevel.Warning => Serilog.Events.LogEventLevel.Warning,
        LogLevel.Error => Serilog.Events.LogEventLevel.Error,
        LogLevel.Fatal => Serilog.Events.LogEventLevel.Fatal,
        _ => Serilog.Events.LogEventLevel.Information // Sane fallback
    };

    /// <summary>
    /// Map OptionButton index to Serilog log level.
    /// Used when user changes dropdown selection.
    /// </summary>
    public static Serilog.Events.LogEventLevel IndexToSerilogLevel(int index) => index switch
    {
        0 => Serilog.Events.LogEventLevel.Debug,
        1 => Serilog.Events.LogEventLevel.Information,
        2 => Serilog.Events.LogEventLevel.Warning,
        3 => Serilog.Events.LogEventLevel.Error,
        _ => Serilog.Events.LogEventLevel.Information
    };

    /// <summary>
    /// Map Serilog log level to OptionButton index.
    /// Used when initializing dropdown to match current level.
    /// </summary>
    public static int SerilogLevelToIndex(Serilog.Events.LogEventLevel level) => level switch
    {
        Serilog.Events.LogEventLevel.Debug => 0,
        Serilog.Events.LogEventLevel.Information => 1,
        Serilog.Events.LogEventLevel.Warning => 2,
        Serilog.Events.LogEventLevel.Error => 3,
        _ => 1 // Default to Information
    };
}
