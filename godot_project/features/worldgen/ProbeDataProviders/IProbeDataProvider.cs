using Godot;
using Darklands.Core.Features.WorldGen.Application.Common;
using Darklands.Core.Features.WorldGen.Application.DTOs;

namespace Darklands.Features.WorldGen.ProbeDataProviders;

/// <summary>
/// Defines a probe data provider for map cell inspection.
/// Implementations provide formatted text data for specific view modes.
///
/// TD_026: Mirrors IColorScheme pattern from TD_025 for architectural consistency.
/// Each provider owns complete probe text formatting logic for one or more view modes.
///
/// Benefits:
/// - Single Responsibility: Each provider formats one type of probe data
/// - Open/Closed: Add new providers without modifying existing code
/// - Testable: Providers are pure functions (data in → text out)
/// - Self-contained: No external dependencies beyond WorldGenerationResult
/// </summary>
public interface IProbeDataProvider
{
    /// <summary>
    /// Human-readable name for this provider (e.g., "Elevation", "Temperature").
    /// Used for debugging and logging.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets formatted probe text for a specific cell and view mode.
    /// </summary>
    /// <param name="data">Complete world generation result with all available data</param>
    /// <param name="x">Cell X coordinate</param>
    /// <param name="y">Cell Y coordinate</param>
    /// <param name="viewMode">Current view mode (for multi-mode providers)</param>
    /// <param name="debugTexture">Optional rendered texture for color debugging (used by ElevationProbeProvider)</param>
    /// <returns>Formatted probe text with line breaks, or error message if data unavailable</returns>
    string GetProbeText(
        WorldGenerationResult data,
        int x,
        int y,
        MapViewMode viewMode,
        ImageTexture? debugTexture = null);
}
