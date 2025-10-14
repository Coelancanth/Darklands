using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Darklands.Core.Features.WorldGen.Application.Common;
using Darklands.Core.Features.WorldGen.Application.DTOs;

namespace Darklands.Features.WorldGen.ColorSchemes;

/// <summary>
/// Base class for marker-based visualizations (grayscale + colored markers).
/// Subclasses define marker color and meaning.
/// Used by Sinks (red), RiverSources (cyan), and Hotspots (magenta) view modes.
/// </summary>
public abstract class MarkerScheme : IColorScheme
{
    public abstract string Name { get; }
    protected abstract Color MarkerColor { get; }
    protected abstract string MarkerLabel { get; }
    protected abstract string MarkerDescription { get; }

    public List<LegendEntry> GetLegendEntries()
    {
        return new List<LegendEntry>
        {
            new("Grayscale", new Color(0.5f, 0.5f, 0.5f), "Elevation (low→high)"),
            new(MarkerLabel, MarkerColor, MarkerDescription)
        };
    }

    public Color GetColor(float normalizedValue, params object[] context)
    {
        // Context[0] = bool isMarker (if true, return marker color; else grayscale)
        if (context.Length > 0 && context[0] is bool isMarker && isMarker)
        {
            return MarkerColor;
        }

        // Default: Grayscale elevation
        return new Color(normalizedValue, normalizedValue, normalizedValue);
    }

    /// <summary>
    /// [TD_025] Base implementation - renders grayscale elevation + colored markers.
    /// Subclasses override to customize data sources and marker coordinates.
    /// </summary>
    public virtual Image? Render(WorldGenerationResult data, MapViewMode viewMode)
    {
        // Subclasses must override
        return null;
    }

    /// <summary>
    /// Helper method: Renders grayscale elevation base + colored markers on top.
    /// Used by simple marker schemes (Sinks, RiverSources).
    /// </summary>
    protected Image RenderGrayscaleWithMarkers(float[,] heightmap, List<(int x, int y)> markerPositions)
    {
        int h = heightmap.GetLength(0);
        int w = heightmap.GetLength(1);
        var image = Image.CreateEmpty(w, h, false, Image.Format.Rgb8);

        // Step 1: Render grayscale elevation base
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

        float delta = Math.Max(1e-6f, max - min);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float t = (heightmap[y, x] - min) / delta;
                image.SetPixel(x, y, new Color(t, t, t));
            }
        }

        // Step 2: Mark positions with marker color
        foreach (var (x, y) in markerPositions)
        {
            if (x >= 0 && x < w && y >= 0 && y < h)
            {
                image.SetPixel(x, y, MarkerColor);
            }
        }

        return image;
    }
}

/// <summary>
/// Sinks visualization: Grayscale elevation + Red markers at local minima.
/// Used by SinksPreFilling and SinksPostFilling view modes (VS_029).
/// </summary>
public class SinksMarkerScheme : MarkerScheme
{
    public override string Name => "Sinks";
    protected override Color MarkerColor => new(1f, 0f, 0f);  // Bright red
    protected override string MarkerLabel => "Red Dots";
    protected override string MarkerDescription => "Local minima (sinks)";

    public override Image? Render(WorldGenerationResult data, MapViewMode viewMode)
    {
        // SinksPreFilling: Uses PostProcessedHeightmap + PreFillingLocalMinima
        // SinksPostFilling: Uses FilledHeightmap + lake centers from PreservedBasins

        float[,]? heightmap = null;
        List<(int x, int y)>? sinkPositions = null;

        if (viewMode == MapViewMode.SinksPreFilling)
        {
            // Pre-filling: Show ALL local minima (artifacts + real pits)
            if (data.PostProcessedHeightmap == null || data.PreFillingLocalMinima == null)
                return null;

            heightmap = data.PostProcessedHeightmap;
            sinkPositions = data.PreFillingLocalMinima;
        }
        else if (viewMode == MapViewMode.SinksPostFilling)
        {
            // Post-filling: Show preserved lake centers only
            if (data.Phase1Erosion?.FilledHeightmap == null || data.Phase1Erosion?.PreservedBasins == null)
                return null;

            heightmap = data.Phase1Erosion.FilledHeightmap;
            // Extract lake centers from basin metadata
            sinkPositions = data.Phase1Erosion.PreservedBasins.Select(b => b.Center).ToList();
        }

        if (heightmap == null || sinkPositions == null)
            return null;

        return RenderGrayscaleWithMarkers(heightmap, sinkPositions);
    }
}

/// <summary>
/// River sources visualization: Grayscale elevation + Red markers at spawn points.
/// Used by RiverSources view mode (VS_029).
/// </summary>
public class RiverSourcesMarkerScheme : MarkerScheme
{
    public override string Name => "River Sources";
    protected override Color MarkerColor => new(1f, 0f, 0f);  // Bright red (consistent with sinks)
    protected override string MarkerLabel => "Red Dots";
    protected override string MarkerDescription => "River origins";

    public override Image? Render(WorldGenerationResult data, MapViewMode viewMode)
    {
        // River sources require FilledHeightmap + RiverSources list
        if (data.Phase1Erosion?.FilledHeightmap == null || data.Phase1Erosion?.RiverSources == null)
            return null;

        return RenderGrayscaleWithMarkers(data.Phase1Erosion.FilledHeightmap, data.Phase1Erosion.RiverSources);
    }
}

/// <summary>
/// Erosion hotspots visualization: Grayscale elevation + Magenta markers at high-energy zones.
/// Used by ErosionHotspots view mode (VS_029).
/// </summary>
public class HotspotsMarkerScheme : MarkerScheme
{
    public override string Name => "Erosion Hotspots";
    protected override Color MarkerColor => new(1f, 0f, 1f);  // Magenta
    protected override string MarkerLabel => "Magenta Dots";
    protected override string MarkerDescription => "High elev + high flow";

    public override Image? Render(WorldGenerationResult data, MapViewMode viewMode)
    {
        // Erosion hotspots require FilledHeightmap, FlowAccumulation, and Thresholds
        if (data.Phase1Erosion?.FilledHeightmap == null ||
            data.Phase1Erosion?.FlowAccumulation == null ||
            data.Thresholds == null)
            return null;

        float[,] heightmap = data.Phase1Erosion.FilledHeightmap;
        float[,] flowAccumulation = data.Phase1Erosion.FlowAccumulation;
        var thresholds = data.Thresholds;

        int h = heightmap.GetLength(0);
        int w = heightmap.GetLength(1);

        // Calculate p95 accumulation threshold for "high flow"
        var flowValues = new List<float>(w * h);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                flowValues.Add(flowAccumulation[y, x]);

        flowValues.Sort();
        float accumulationP95 = GetPercentileFromSorted(flowValues, 0.95f);

        // Detect hotspot positions: high elevation + high flow
        var hotspotPositions = new List<(int x, int y)>();
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool isHighElevation = heightmap[y, x] >= thresholds.MountainLevel;
                bool isHighFlow = flowAccumulation[y, x] >= accumulationP95;

                if (isHighElevation && isHighFlow)
                {
                    hotspotPositions.Add((x, y));
                }
            }
        }

        return RenderGrayscaleWithMarkers(heightmap, hotspotPositions);
    }

    private float GetPercentileFromSorted(List<float> sortedValues, float percentile)
    {
        if (sortedValues.Count == 0) return 0f;
        int index = (int)Math.Floor(percentile * (sortedValues.Count - 1));
        index = Math.Clamp(index, 0, sortedValues.Count - 1);
        return sortedValues[index];
    }
}
