using System;
using CSharpFunctionalExtensions;
using Darklands.Core.Features.WorldGen.Application.Abstractions;
using Darklands.Core.Features.WorldGen.Application.DTOs;
using Darklands.Core.Features.WorldGen.Domain;
using Darklands.Core.Features.WorldGen.Infrastructure.Native.Interop;
using Microsoft.Extensions.Logging;

namespace Darklands.Core.Features.WorldGen.Infrastructure.Native;

/// <summary>
/// Implementation of IPlateSimulator using native plate-tectonics library.
/// LAYER 2 (Wrapper): Manages native lifecycle, marshaling, and error handling.
/// See ADR-007: Native Library Integration Architecture
/// </summary>
public class NativePlateSimulator : IPlateSimulator
{
    private readonly ILogger<NativePlateSimulator> _logger;
    private readonly string _projectPath;

    public NativePlateSimulator(ILogger<NativePlateSimulator> logger, string projectPath)
    {
        _logger = logger;
        _projectPath = projectPath;
    }

    public Result<PlateSimulationResult> Generate(PlateSimulationParams parameters)
    {
        _logger.LogInformation(
            "Generating world (seed: {Seed}, size: {Size}x{Size}, plates: {Plates})",
            parameters.Seed, parameters.WorldSize, parameters.WorldSize, parameters.PlateCount);

        return EnsureLibraryLoaded()
            .Bind(() => RunNativeSimulation(parameters))
            .Map(nativeOut => new PlateSimulationResult(
                nativeOut.Heightmap,
                nativeOut.Plates));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PHASE 2.1: Native Simulation & Marshaling
    // ═══════════════════════════════════════════════════════════════════════

    private Result EnsureLibraryLoaded() =>
        NativeLibraryLoader.ValidateLibraryExists(_projectPath);

    private Result<NativeOut> RunNativeSimulation(PlateSimulationParams p)
    {
        try
        {
            _logger.LogDebug("Creating native simulation handle with params: seed={Seed}, size={Size}, plates={Plates}",
                p.Seed, p.WorldSize, p.PlateCount);

            var handle = PlateTectonicsNative.Create(
                seed: p.Seed,
                width: (uint)p.WorldSize,
                height: (uint)p.WorldSize,
                seaLevel: p.SeaLevel,  // TD_021: Generation param (land/ocean ratio), not physics threshold!
                erosionPeriod: (uint)p.ErosionPeriod,
                foldingRatio: p.FoldingRatio,
                aggrOverlapAbs: (uint)p.AggrOverlapAbs,
                aggrOverlapRel: p.AggrOverlapRel,
                cycleCount: (uint)p.CycleCount,
                numPlates: (uint)p.PlateCount);

            _logger.LogDebug("Native Create() returned handle: {Handle}", handle);

            if (handle == IntPtr.Zero)
            {
                _logger.LogError("Native Create() returned NULL handle");
                return Result.Failure<NativeOut>("ERROR_WORLDGEN_SIMULATION_FAILED");
            }

            using var safeHandle = new PlateSimulationHandle(handle);

            // Step until simulation completes
            int stepCount = 0;
            const int maxSteps = 10000;

            // Optional frame capture controls via environment variables
            bool captureFrames = string.Equals(Environment.GetEnvironmentVariable("PLATEC_CAPTURE_FRAMES"), "1", StringComparison.OrdinalIgnoreCase);
            int captureEvery = int.TryParse(Environment.GetEnvironmentVariable("PLATEC_CAPTURE_EVERY"), out var ce) && ce > 0 ? ce : 1;
            string captureDir = Environment.GetEnvironmentVariable("PLATEC_CAPTURE_DIR") ?? System.IO.Path.Combine("logs", "platec_frames", $"seed_{p.Seed}_size_{p.WorldSize}");

            while (PlateTectonicsNative.IsFinished(handle) == 0 && stepCount < maxSteps)
            {
                PlateTectonicsNative.Step(handle);
                stepCount++;

                if (captureFrames && (stepCount % captureEvery == 0))
                {
                    try
                    {
                        var framePtr = PlateTectonicsNative.GetHeightmap(handle);
                        if (framePtr != IntPtr.Zero)
                        {
                            System.IO.Directory.CreateDirectory(captureDir);
                            var framePath = System.IO.Path.Combine(captureDir, $"frame_{stepCount:0000}.pgm");
                            WriteHeightmapAsPGM(framePtr, p.WorldSize, p.WorldSize, framePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to capture frame at step {Step}", stepCount);
                    }
                }
            }

            if (stepCount >= maxSteps)
            {
                _logger.LogWarning("Simulation reached max steps ({MaxSteps})", maxSteps);
                return Result.Failure<NativeOut>("ERROR_WORLDGEN_SIMULATION_TIMEOUT");
            }

            _logger.LogInformation("Simulation completed in {Steps} steps", stepCount);

            // Marshal heightmap to C# 2D array
            var heightmapPtr = PlateTectonicsNative.GetHeightmap(handle);
            if (heightmapPtr == IntPtr.Zero)
                return Result.Failure<NativeOut>("ERROR_WORLDGEN_MARSHALING_FAILED");

            var heightmap = Marshal2DArray(heightmapPtr, p.WorldSize, p.WorldSize);

            // Plates map
            var platesPtr = PlateTectonicsNative.GetPlatesMap(handle);
            var plates = Marshal2DArrayUInt(platesPtr, p.WorldSize, p.WorldSize);

            return Result.Success(new NativeOut(heightmap, plates));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Native simulation exception: {ExceptionType} - {Message}",
                ex.GetType().Name, ex.Message);
            _logger.LogError("Exception stack trace: {StackTrace}", ex.StackTrace);

            if (ex.InnerException != null)
            {
                _logger.LogError("Inner exception: {InnerType} - {InnerMessage}",
                    ex.InnerException.GetType().Name, ex.InnerException.Message);
            }

            return Result.Failure<NativeOut>($"ERROR_WORLDGEN_NATIVE_EXCEPTION: {ex.Message}");
        }
    }

    /// <summary>
    /// Marshals native 1D float array to C# 2D array using Span for performance.
    /// Pattern from ADR-007 v1.2.
    /// </summary>
    private static unsafe float[,] Marshal2DArray(IntPtr ptr, int width, int height)
    {
        var result = new float[height, width];
        var span = new Span<float>(ptr.ToPointer(), width * height);

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                result[y, x] = span[y * width + x];

        return result;
    }

    private static unsafe uint[,] Marshal2DArrayUInt(IntPtr ptr, int width, int height)
    {
        var result = new uint[height, width];
        var span = new Span<uint>(ptr.ToPointer(), width * height);

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                result[y, x] = span[y * width + x];

        return result;
    }

    /// <summary>
    /// Writes a native heightmap (float*) to an 8-bit binary PGM image (P5).
    /// </summary>
    private static unsafe void WriteHeightmapAsPGM(IntPtr floatPtr, int width, int height, string filePath)
    {
        var span = new Span<float>(floatPtr.ToPointer(), width * height);
        float min = float.MaxValue, max = float.MinValue;
        for (int i = 0; i < span.Length; i++) { var v = span[i]; if (v < min) min = v; if (v > max) max = v; }
        float delta = Math.Max(1e-6f, max - min);

        using var fs = System.IO.File.Create(filePath);
        var header = System.Text.Encoding.ASCII.GetBytes($"P5\n{width} {height}\n255\n");
        fs.Write(header, 0, header.Length);

        // Stream rows to avoid large allocations
        var row = new byte[width];
        for (int y = 0; y < height; y++)
        {
            int o = y * width;
            for (int x = 0; x < width; x++)
            {
                float v = span[o + x];
                int b = (int)Math.Clamp(((v - min) / delta) * 255f, 0f, 255f);
                row[x] = (byte)b;
            }
            fs.Write(row, 0, row.Length);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Helper Records
    // ═══════════════════════════════════════════════════════════════════════

    private record NativeOut(float[,] Heightmap, uint[,] Plates);
}
