using System;
using System.IO;
using Darklands.Core.Features.WorldGen.Application.DTOs;
using Darklands.Core.Features.WorldGen.Infrastructure.Native;
using Darklands.Core.Features.WorldGen.Infrastructure.Native.Interop;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace Darklands.Core.Tests.Features.WorldGen.Infrastructure.Native;

/// <summary>
/// Integration tests for plate-tectonics native library loading.
/// Validates PInvoke layer, SafeHandle cleanup, and library availability.
/// </summary>
[Trait("Category", "WorldGen")]
[Trait("Category", "Integration")]
public class PlateTectonicsIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public PlateTectonicsIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ValidateLibraryExists_WhenDllPresent_ShouldSucceed()
    {
        // ARRANGE
        var projectPath = GetProjectRoot();
        _output.WriteLine($"Project path: {projectPath}");

        // ACT
        var result = NativeLibraryLoader.ValidateLibraryExists(projectPath);

        // ASSERT
        result.IsSuccess.Should().BeTrue(
            "PlateTectonics.dll should exist in addons/darklands/bin/win-x64/. " +
            "If missing, run: cd References/plate-tectonics && cmake --build . --config Release");
    }

    [Fact]
    public void Create_WhenCalledWithValidParams_ShouldReturnNonNullHandle()
    {
        // ARRANGE
        ValidateLibraryOrSkip();

        // ACT
        var handle = PlateTectonicsNative.Create(
            seed: 42,
            width: 512,
            height: 512,
            seaLevel: 0.65f,
            erosionPeriod: 60,
            foldingRatio: 0.02f,
            aggrOverlapAbs: 1_000_000,
            aggrOverlapRel: 0.33f,
            cycleCount: 2,
            numPlates: 10);

        // ASSERT
        try
        {
            handle.Should().NotBe(IntPtr.Zero, "Native library should return valid simulation handle");

            // Verify we can query dimensions
            var width = PlateTectonicsNative.GetMapWidth(handle);
            var height = PlateTectonicsNative.GetMapHeight(handle);

            width.Should().Be(512u);
            height.Should().Be(512u);

            _output.WriteLine($"✓ Simulation created: {width}x{height}");
        }
        finally
        {
            // CLEANUP: Manual cleanup for raw IntPtr test
            if (handle != IntPtr.Zero)
            {
                PlateTectonicsNative.Destroy(handle);
            }
        }
    }

    [Fact]
    public void SafeHandle_WhenDisposed_ShouldCleanupNativeResources()
    {
        // ARRANGE
        ValidateLibraryOrSkip();

        PlateSimulationHandle? handle = null;

        // ACT & ASSERT
        Action createAndDispose = () =>
        {
            // Create handle via raw PInvoke
            var rawHandle = PlateTectonicsNative.Create(
                seed: 123,
                width: 256,
                height: 256,
                seaLevel: 0.65f,
                erosionPeriod: 60,
                foldingRatio: 0.02f,
                aggrOverlapAbs: 1_000_000,
                aggrOverlapRel: 0.33f,
                cycleCount: 1,
                numPlates: 5);

            // Wrap in SafeHandle
            handle = new PlateSimulationHandle(rawHandle);

            handle.IsInvalid.Should().BeFalse("Handle should be valid after creation");
            handle.IsClosed.Should().BeFalse("Handle should not be closed yet");

            // Dispose explicitly (simulates using block)
            handle.Dispose();

            handle.IsClosed.Should().BeTrue("Handle should be closed after Dispose");
        };

        createAndDispose.Should().NotThrow("SafeHandle should cleanup without exceptions");

        _output.WriteLine("✓ SafeHandle RAII cleanup verified");
    }

    [Fact]
    public void Step_WhenCalledUntilFinished_ShouldCompleteSimulation()
    {
        // ARCHITECTURE: This test validates the full simulation lifecycle.
        // Future wrapper layer will hide this manual stepping behind a facade.

        // ARRANGE
        ValidateLibraryOrSkip();

        var handle = PlateTectonicsNative.Create(
            seed: 999,
            width: 128,
            height: 128,
            seaLevel: 0.65f,
            erosionPeriod: 60,
            foldingRatio: 0.02f,
            aggrOverlapAbs: 1_000_000,
            aggrOverlapRel: 0.33f,
            cycleCount: 1, // Small cycle count for fast test
            numPlates: 5);

        try
        {
            // ACT: Run simulation until complete
            int stepCount = 0;
            const int maxSteps = 10000; // Safety limit

            while (PlateTectonicsNative.IsFinished(handle) == 0 && stepCount < maxSteps)
            {
                PlateTectonicsNative.Step(handle);
                stepCount++;
            }

            // ASSERT
            stepCount.Should().BeGreaterThan(0, "Simulation should require at least one step");
            stepCount.Should().BeLessThan(maxSteps, "Simulation should finish in reasonable time");
            PlateTectonicsNative.IsFinished(handle).Should().NotBe(0u, "Simulation should report finished");

            _output.WriteLine($"✓ Simulation completed in {stepCount} steps");

            // Verify heightmap accessible
            var heightmapPtr = PlateTectonicsNative.GetHeightmap(handle);
            heightmapPtr.Should().NotBe(IntPtr.Zero, "Heightmap should be available after completion");
        }
        finally
        {
            if (handle != IntPtr.Zero)
            {
                PlateTectonicsNative.Destroy(handle);
            }
        }
    }

    // HELPER: Fail fast with helpful message if library not built
    private void ValidateLibraryOrSkip()
    {
        var projectPath = GetProjectRoot();
        var result = NativeLibraryLoader.ValidateLibraryExists(projectPath);

        if (result.IsFailure)
        {
            result.IsSuccess.Should().BeTrue(result.Error);
        }
    }

    private static string GetProjectRoot()
    {
        // Navigate from tests/bin/Debug/net9.0/ -> project root
        var testAssemblyPath = AppContext.BaseDirectory;
        var projectRoot = Path.GetFullPath(Path.Combine(testAssemblyPath, "..", "..", "..", "..", ".."));
        return projectRoot;
    }

    [Fact]
    public void Generate_WhenCalledWithValidParams_ShouldReturnCompleteWorldData()
    {
        // ARCHITECTURE: Integration test for NativePlateSimulator (wrapper layer).
        // Validates full pipeline: native simulation -> marshaling -> post-processing (stubs in Phase 2.1).

        // ARRANGE
        ValidateLibraryOrSkip();

        var projectPath = GetProjectRoot();
        var logger = NullLogger<NativePlateSimulator>.Instance;
        var simulator = new NativePlateSimulator(logger, projectPath);

        var parameters = new PlateSimulationParams(
            seed: 42,
            worldSize: 128, // Small for fast test
            plateCount: 5,
            cycleCount: 1); // Single cycle for speed

        // ACT
        var result = simulator.Generate(parameters);

        // ASSERT
        result.IsSuccess.Should().BeTrue("Native simulation and marshaling should succeed");

        var worldData = result.Value;

        // Validate dimensions
        worldData.Width.Should().Be(128);
        worldData.Height.Should().Be(128);

        worldData.Heightmap.GetLength(0).Should().Be(128, "Heightmap height should match");
        worldData.Heightmap.GetLength(1).Should().Be(128, "Heightmap width should match");

        // Validate heightmap has real data (not all zeros, not NaN)
        bool hasNonZeroValues = false;
        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                worldData.Heightmap[y, x].Should().NotBe(float.NaN, $"Heightmap[{y},{x}] should be valid float");
                if (worldData.Heightmap[y, x] > 0.01f)
                    hasNonZeroValues = true;
            }
        }

        hasNonZeroValues.Should().BeTrue("Heightmap should contain terrain data from simulation");

        // Validate plates map is populated
        worldData.PlatesMap.GetLength(0).Should().Be(128);
        worldData.PlatesMap.GetLength(1).Should().Be(128);

        _output.WriteLine($"World generated successfully: {worldData.Width}x{worldData.Height}");
        _output.WriteLine($"Heightmap sample [64,64]: {worldData.Heightmap[64, 64]:F3}");
        _output.WriteLine($"Plate ID at [64,64]: {worldData.PlatesMap[64, 64]}");
    }
}
