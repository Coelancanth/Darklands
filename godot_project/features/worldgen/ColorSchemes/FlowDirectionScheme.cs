using Godot;
using System.Collections.Generic;
using Darklands.Core.Features.WorldGen.Application.Common;
using Darklands.Core.Features.WorldGen.Application.DTOs;

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

    /// <summary>
    /// [TD_025] Complete rendering pipeline - renders D-8 flow directions with discrete color mapping.
    /// Migrated from WorldMapRendererNode.RenderFlowDirections().
    /// Each cell colored by flow direction: N=Red, NE=Yellow, E=Green, SE=Cyan, S=Blue, SW=Purple, W=Magenta, NW=Orange, Sink=Black.
    /// </summary>
    public Image? Render(WorldGenerationResult data, MapViewMode viewMode)
    {
        // FlowDirections only available in Phase1Erosion
        if (data.Phase1Erosion?.FlowDirections == null)
        {
            return null;  // Flow directions not available - fall back to legacy rendering
        }

        int[,] flowDirections = data.Phase1Erosion.FlowDirections;
        int h = flowDirections.GetLength(0);
        int w = flowDirections.GetLength(1);
        var image = Image.CreateEmpty(w, h, false, Image.Format.Rgb8);

        // Render each cell with direction-based color
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int dir = flowDirections[y, x];
                int colorIndex = dir == -1 ? 8 : dir;  // -1 (sink) → index 8, 0-7 → 0-7
                image.SetPixel(x, y, DirectionColors[colorIndex]);
            }
        }

        return image;
    }
}
