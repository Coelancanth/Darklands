using Godot;
using System;
using System.Collections.Generic;
using Darklands.Core.Features.WorldGen.Application.Common;
using Darklands.Core.Features.WorldGen.Application.DTOs;

namespace Darklands.Features.WorldGen.ColorSchemes;

/// <summary>
/// Precipitation visualization: Smooth 3-stop moisture gradient.
/// Yellow (dry deserts) → Green (moderate vegetation) → Blue (wet tropics).
/// Used by all precipitation view modes (NoiseOnly, TemperatureShaped, Base, WithRainShadow, Final).
/// </summary>
public class PrecipitationScheme : IColorScheme
{
    public string Name => "Precipitation";

    // SSOT: Color definitions (Yellow → Green → Blue)
    private static readonly Color DryColor = new(1f, 1f, 0f);               // Yellow (RGB: 255, 255, 0)
    private static readonly Color ModerateColor = new(0f, 200f/255f, 0f);   // Green (RGB: 0, 200, 0)
    private static readonly Color WetColor = new(0f, 0f, 1f);               // Blue (RGB: 0, 0, 255)

    public List<LegendEntry> GetLegendEntries()
    {
        return new List<LegendEntry>
        {
            new("Yellow", DryColor, "Dry (<400mm/year)"),
            new("Green", ModerateColor, "Medium (400-800mm)"),
            new("Blue", WetColor, "High (>800mm/year)")
        };
    }

    public Color GetColor(float normalizedValue, params object[] context)
    {
        // 3-stop gradient: Yellow (0.0) → Green (0.5) → Blue (1.0)
        if (normalizedValue < 0.5f)
        {
            // Dry to moderate: Yellow → Green
            return Gradient(normalizedValue, 0.0f, 0.5f, DryColor, ModerateColor);
        }
        else
        {
            // Moderate to wet: Green → Blue
            return Gradient(normalizedValue, 0.5f, 1.0f, ModerateColor, WetColor);
        }
    }

    /// <summary>
    /// Linear interpolation between two colors.
    /// </summary>
    private static Color Gradient(float value, float min, float max, Color colorA, Color colorB)
    {
        float delta = max - min;
        if (delta < 0.00001f) return colorA;

        float t = Mathf.Clamp((value - min) / delta, 0f, 1f);
        return colorA.Lerp(colorB, t);
    }

    /// <summary>
    /// [TD_025] Complete rendering pipeline - renders precipitation map with smooth 3-stop gradient.
    /// Migrated from WorldMapRendererNode.RenderPrecipitationMap().
    /// Supports all 5 precipitation view modes (NoiseOnly, TemperatureShaped, Base, WithRainShadow, Final).
    /// </summary>
    public Image? Render(WorldGenerationResult data, MapViewMode viewMode)
    {
        // Select the correct precipitation map based on view mode
        float[,]? precipitationMap = viewMode switch
        {
            MapViewMode.PrecipitationNoiseOnly => data.BaseNoisePrecipitationMap,
            MapViewMode.PrecipitationTemperatureShaped => data.TemperatureShapedPrecipitationMap,
            MapViewMode.PrecipitationBase => data.FinalPrecipitationMap,
            MapViewMode.PrecipitationWithRainShadow => data.WithRainShadowPrecipitationMap,
            MapViewMode.PrecipitationFinal => data.PrecipitationFinal,
            _ => null  // Not a precipitation view mode
        };

        if (precipitationMap == null)
        {
            return null;  // Precipitation data not available for this mode - fall back to legacy rendering
        }

        int h = precipitationMap.GetLength(0);
        int w = precipitationMap.GetLength(1);
        var image = Image.CreateEmpty(w, h, false, Image.Format.Rgb8);

        // Render with smooth 3-stop gradient (Yellow → Green → Blue)
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float p = precipitationMap[y, x];  // Normalized [0, 1]

                // 3-stop gradient: Yellow (0.0) → Green (0.5) → Blue (1.0)
                Color color;
                if (p < 0.5f)
                {
                    // Dry to moderate: Yellow → Green
                    color = Gradient(p, 0.0f, 0.5f, DryColor, ModerateColor);
                }
                else
                {
                    // Moderate to wet: Green → Blue
                    color = Gradient(p, 0.5f, 1.0f, ModerateColor, WetColor);
                }

                image.SetPixel(x, y, color);
            }
        }

        return image;
    }
}
