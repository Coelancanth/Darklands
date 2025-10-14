using Godot;

namespace Darklands.Features.WorldGen.ColorSchemes;

/// <summary>
/// Represents one row in a map legend.
/// Immutable metadata linking color to human-readable description.
/// </summary>
/// <param name="Label">Short color name (e.g., "Deep Blue", "Red")</param>
/// <param name="Color">The actual color swatch</param>
/// <param name="Description">What this color represents (e.g., "Deep ocean", "Polar climate")</param>
public record LegendEntry(string Label, Color Color, string Description);
