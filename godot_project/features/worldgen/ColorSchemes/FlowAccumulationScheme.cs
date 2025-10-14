using Godot;
using System;
using System.Collections.Generic;

namespace Darklands.Features.WorldGen.ColorSchemes;

/// <summary>
/// Flow accumulation visualization: Two-layer naturalistic rendering.
/// Layer 1: Subtle terrain canvas (earth tones based on elevation).
/// Layer 2: Bright water network (cyan overlay with alpha based on flow magnitude).
/// Context: Requires heightmap, ocean mask, and flow stats for proper rendering.
/// Used by FlowAccumulation view mode (VS_029).
/// </summary>
public class FlowAccumulationScheme : IColorScheme
{
    public string Name => "Flow Accumulation";

    // SSOT: Two-layer color palette
    // Layer 1: Terrain canvas (muted earth tones)
    private static readonly Color TerrainLowlands = new(180f/255f, 170f/255f, 150f/255f);  // Sandy Beige
    private static readonly Color TerrainHills = new(189f/255f, 183f/255f, 107f/255f);     // Khaki
    private static readonly Color TerrainPeaks = new(176f/255f, 196f/255f, 222f/255f);     // Light Steel Blue

    // Layer 2: Water overlay (bright cyan, varying alpha)
    private static readonly Color WaterLowFlow = new(0f, 0f, 139f/255f, 0.05f);           // Deep blue, 5% alpha
    private static readonly Color WaterHighFlow = new(0f, 191f/255f, 255f/255f, 1.0f);    // Deep Sky Blue, 100% alpha

    // Ocean color
    private static readonly Color OceanColor = new(0f, 49f/255f, 83f/255f);               // Prussian Blue

    public List<LegendEntry> GetLegendEntries()
    {
        return new List<LegendEntry>
        {
            new("Ocean", OceanColor, "Deep prussian blue"),
            new("Terrain Base", TerrainLowlands, "Earth tones (context layer)"),
            new("Faint Blue", WaterLowFlow, "Low flow (barely visible)"),
            new("Bright Cyan", WaterHighFlow, "High flow (rivers)")
        };
    }

    public Color GetColor(float normalizedValue, params object[] context)
    {
        // Context must contain:
        // [0] = bool isOcean
        // [1] = float elevation (normalized [0,1])
        // [2] = float flow (actual value, not normalized)
        // [3] = float minFlow
        // [4] = float maxFlow
        if (context.Length < 5)
        {
            throw new ArgumentException("FlowAccumulationScheme requires [isOcean, elevation, flow, minFlow, maxFlow] in context");
        }

        bool isOcean = (bool)context[0];
        float elevation = (float)context[1];
        float flow = (float)context[2];
        float minFlow = (float)context[3];
        float maxFlow = (float)context[4];

        // OCEAN: Deep blue, no water overlay
        if (isOcean)
        {
            return OceanColor;
        }

        // LAND: Two-layer blend

        // --- Layer 1: Get terrain base color from elevation ---
        Color terrainColor = GetTerrainColor(elevation);

        // --- Layer 2: Get water overlay color from flow (LOG SCALED) ---
        float deltaFlow = Math.Max(1e-6f, maxFlow - minFlow);
        float minVisibleFlowThreshold = minFlow + deltaFlow * 0.01f;  // Bottom 1% transparent

        // Below threshold? Show pure terrain (no water overlay)
        if (flow < minVisibleFlowThreshold)
        {
            return terrainColor;
        }

        // Log scale the flow for better visual distribution
        float logFlowNorm = (float)Math.Log(1 + flow - minFlow) / (float)Math.Log(1 + maxFlow - minFlow);

        // Interpolate water color (brightness + alpha increase together!)
        Color waterColor = WaterLowFlow.Lerp(WaterHighFlow, logFlowNorm);

        // --- Blend terrain + water using water's alpha ---
        return terrainColor.Lerp(waterColor, waterColor.A);
    }

    /// <summary>
    /// Gets terrain color from normalized elevation (0-1).
    /// Smooth 3-stop gradient: Lowlands (beige) → Hills (khaki) → Peaks (light blue).
    /// </summary>
    private static Color GetTerrainColor(float elevNorm)
    {
        if (elevNorm < 0.4f)
        {
            // Lowlands to Hills (0.0 - 0.4)
            return Gradient(elevNorm, 0.0f, 0.4f, TerrainLowlands, TerrainHills);
        }
        else
        {
            // Hills to Peaks (0.4 - 1.0)
            return Gradient(elevNorm, 0.4f, 1.0f, TerrainHills, TerrainPeaks);
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
}
