using Godot;
using System.Collections.Generic;

namespace Darklands.Features.WorldGen.ColorSchemes;

/// <summary>
/// D-8 flow direction visualization: 8 directional colors + sink (black).
/// Each color represents steepest descent direction from cell.
/// Used by FlowDirections view mode (VS_029).
/// </summary>
public class FlowDirectionScheme : IColorScheme
{
    public string Name => "Flow Directions";

    // SSOT: 9 discrete colors (8 directions + sink)
    private static readonly Color[] DirectionColors = new Color[9]
    {
        new(1f, 0f, 0f),      // 0: North - Red
        new(1f, 1f, 0f),      // 1: NE - Yellow
        new(0f, 1f, 0f),      // 2: East - Green
        new(0f, 1f, 1f),      // 3: SE - Cyan
        new(0f, 0f, 1f),      // 4: South - Blue
        new(0.5f, 0f, 0.5f),  // 5: SW - Purple
        new(1f, 0f, 1f),      // 6: West - Magenta
        new(1f, 0.5f, 0f),    // 7: NW - Orange
        new(0f, 0f, 0f)       // -1: Sink - Black
    };

    public List<LegendEntry> GetLegendEntries()
    {
        return new List<LegendEntry>
        {
            new("Red", DirectionColors[0], "North"),
            new("Yellow", DirectionColors[1], "NE"),
            new("Green", DirectionColors[2], "East"),
            new("Cyan", DirectionColors[3], "SE"),
            new("Blue", DirectionColors[4], "South"),
            new("Purple", DirectionColors[5], "SW"),
            new("Magenta", DirectionColors[6], "West"),
            new("Orange", DirectionColors[7], "NW"),
            new("Black", DirectionColors[8], "Sink (no flow)")
        };
    }

    public Color GetColor(float normalizedValue, params object[] context)
    {
        // For flow directions, normalizedValue is actually the direction code (-1 to 7)
        // We'll map it: -1 → index 8, 0-7 → index 0-7
        int direction = (int)normalizedValue;
        int colorIndex = direction == -1 ? 8 : direction;

        // Clamp to valid range
        colorIndex = Mathf.Clamp(colorIndex, 0, 8);

        return DirectionColors[colorIndex];
    }
}
