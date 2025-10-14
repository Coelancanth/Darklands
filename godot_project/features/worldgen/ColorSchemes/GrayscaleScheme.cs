using Godot;
using System.Collections.Generic;
using Darklands.Core.Features.WorldGen.Application.Common;
using Darklands.Core.Features.WorldGen.Application.DTOs;

namespace Darklands.Features.WorldGen.ColorSchemes;

/// <summary>
/// Simple grayscale elevation rendering (min=black, max=white).
/// Used by RawElevation view mode and as base layer for marker-based visualizations.
/// </summary>
public class GrayscaleScheme : IColorScheme
{
    public string Name => "Grayscale Elevation";

    public List<LegendEntry> GetLegendEntries()
    {
        return new List<LegendEntry>
        {
            new("Black", new Color(0f, 0f, 0f), "Low elevation"),
            new("Gray", new Color(0.5f, 0.5f, 0.5f), "Mid elevation"),
            new("White", new Color(1f, 1f, 1f), "High elevation")
        };
    }

    public Color GetColor(float normalizedValue, params object[] context)
    {
        // Simple grayscale: normalized value [0,1] maps directly to gray [black, white]
        return new Color(normalizedValue, normalizedValue, normalizedValue);
    }

    /// <summary>
    /// [TD_025] Complete rendering pipeline - renders grayscale elevation map.
    /// Migrated from WorldMapRendererNode.RenderRawElevation().
    /// </summary>
    public Image? Render(WorldGenerationResult data, MapViewMode viewMode)
    {
        // Use original heightmap for RawElevation view mode
        var heightmap = data.Heightmap;
        int h = heightmap.GetLength(0);
        int w = heightmap.GetLength(1);

        var image = Image.CreateEmpty(w, h, false, Image.Format.Rgb8);

        // Find min/max for normalization
        float min = float.MaxValue, max = float.MinValue;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float v = heightmap[y, x];
                if (v < min) min = v;
                if (v > max) max = v;
            }
        }

        float delta = System.Math.Max(1e-6f, max - min);

        // Render grayscale (normalized [0,1])
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float t = (heightmap[y, x] - min) / delta;
                image.SetPixel(x, y, new Color(t, t, t));
            }
        }

        return image;
    }
}
