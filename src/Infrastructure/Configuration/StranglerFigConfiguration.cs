namespace Darklands.Core.Infrastructure.Configuration;

/// <summary>
/// Configuration for Strangler Fig migration feature toggles.
/// Controls which implementation to use during parallel operation phase.
/// </summary>
public sealed class StranglerFigConfiguration
{
    /// <summary>
    /// Whether to use the new Diagnostics context for vision performance monitoring.
    /// When true: Uses new Diagnostics VisionPerformanceMonitor
    /// When false: Uses legacy Infrastructure VisionPerformanceMonitor
    /// During TD_042: Both run in parallel regardless of this setting for validation
    /// </summary>
    public bool UseDiagnosticsContext { get; set; } = false;

    /// <summary>
    /// Whether to use the new Tactical context for combat operations.
    /// When true: Uses new Tactical.Application handlers
    /// When false: Uses legacy Application.Combat handlers
    /// During TD_043: Both run in parallel regardless of this setting for validation
    /// </summary>
    public bool UseTacticalContext { get; set; } = false;

    /// <summary>
    /// Whether to log parallel operation comparisons for validation.
    /// Should be true during migration phases for debugging.
    /// </summary>
    public bool EnableValidationLogging { get; set; } = true;
}
