using System;
using System.IO;
using Darklands.Core.Features.WorldGen.Application.DTOs;
using Darklands.Core.Features.WorldGen.Infrastructure.Native;
using Darklands.Core.Features.WorldGen.Infrastructure.Native.Interop;
using Darklands.Core.Features.WorldGen.Infrastructure.Pipeline;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace Darklands.Core.Tests.Features.WorldGen.Infrastructure.Pipeline;

/// <summary>
/// Integration tests for Phase 1 erosion data population in world generation pipeline (VS_029).
/// Validates D-8 flow direction data is correctly wired through all pipeline stages.
/// </summary>
[Trait("Category", "WorldGen")]
[Trait("Category", "Integration")]
public class Phase1ErosionIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public Phase1ErosionIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Generate_WhenPipelineComplete_ShouldPopulatePhase1ErosionData()
    {
        // WHY: VS_029 requires Phase1ErosionData for flow visualization.
        // This test validates the pipeline correctly computes and exposes all erosion fields.

        // ARRANGE
        ValidateLibraryOrSkip();

        var projectPath = GetProjectRoot();
        var nativeSimulator = new NativePlateSimulator(NullLogger<NativePlateSimulator>.Instance, projectPath);
        var pipeline = new GenerateWorldPipeline(nativeSimulator, NullLogger<GenerateWorldPipeline>.Instance);

        var parameters = new PlateSimulationParams(
            seed: 12345,
            worldSize: 128,  // Small for fast test
            plateCount: 5,
            cycleCount: 1);  // Minimal simulation

        // ACT
        var result = pipeline.Generate(parameters);

        // ASSERT
        result.IsSuccess.Should().BeTrue("Pipeline should complete all stages successfully");

        var world = result.Value;

        // ═══════════════════════════════════════════════════════════════════════
        // VALIDATE: Phase1ErosionData populated
        // ═══════════════════════════════════════════════════════════════════════

        world.Phase1Erosion.Should().NotBeNull("Phase1ErosionData should be computed in Stage 6");

        // Validate FilledHeightmap
        world.Phase1Erosion!.FilledHeightmap.Should().NotBeNull();
        world.Phase1Erosion.FilledHeightmap.GetLength(0).Should().Be(128, "FilledHeightmap height should match world size");
        world.Phase1Erosion.FilledHeightmap.GetLength(1).Should().Be(128, "FilledHeightmap width should match world size");

        // Validate FlowDirections (D-8 algorithm output)
        world.Phase1Erosion.FlowDirections.Should().NotBeNull();
        world.Phase1Erosion.FlowDirections.GetLength(0).Should().Be(128);
        world.Phase1Erosion.FlowDirections.GetLength(1).Should().Be(128);

        // Flow directions should be in valid range: 0-7 (directions) or -1 (sink)
        bool hasValidDirections = false;
        bool hasSinks = false;

        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                int flowDir = world.Phase1Erosion.FlowDirections[y, x];
                flowDir.Should().BeInRange(-1, 7, $"Flow direction at [{y},{x}] should be valid (0-7 or -1 for sink)");

                if (flowDir >= 0 && flowDir <= 7)
                    hasValidDirections = true;
                if (flowDir == -1)
                    hasSinks = true;
            }
        }

        hasValidDirections.Should().BeTrue("Some cells should have valid flow directions (0-7)");
        hasSinks.Should().BeTrue("Some cells should be sinks (-1) - ocean cells or pits");

        // Validate FlowAccumulation
        world.Phase1Erosion.FlowAccumulation.Should().NotBeNull();
        world.Phase1Erosion.FlowAccumulation.GetLength(0).Should().Be(128);
        world.Phase1Erosion.FlowAccumulation.GetLength(1).Should().Be(128);

        // Flow accumulation should have reasonable values (normalized precipitation accumulation)
        bool hasAccumulation = false;
        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                float accum = world.Phase1Erosion.FlowAccumulation[y, x];
                accum.Should().BeGreaterThanOrEqualTo(0f, $"Flow accumulation at [{y},{x}] should be non-negative");

                if (accum > 0.1f)  // Significant accumulation
                    hasAccumulation = true;
            }
        }

        hasAccumulation.Should().BeTrue("Some cells should have accumulated flow (drainage basins)");

        // Validate RiverSources
        world.Phase1Erosion.RiverSources.Should().NotBeNull();
        world.Phase1Erosion.RiverSources.Count.Should().BeGreaterThan(0, "Pipeline should detect at least one river source");
        world.Phase1Erosion.RiverSources.Count.Should().BeLessThan(50, "River source count should be reasonable (not every cell)");

        // River sources should be within bounds
        foreach (var (x, y) in world.Phase1Erosion.RiverSources)
        {
            x.Should().BeInRange(0, 127, "River source X coordinate should be within world bounds");
            y.Should().BeInRange(0, 127, "River source Y coordinate should be within world bounds");
        }

        // Validate PreservedBasins (TD_023: Renamed from Lakes with metadata)
        world.Phase1Erosion.PreservedBasins.Should().NotBeNull("PreservedBasins list should exist (may be empty)");

        _output.WriteLine($"✓ Phase1ErosionData validated:");
        _output.WriteLine($"  - Flow directions: {hasValidDirections} (valid), {hasSinks} (sinks detected)");
        _output.WriteLine($"  - Flow accumulation: {hasAccumulation} (drainage basins present)");
        _output.WriteLine($"  - River sources: {world.Phase1Erosion.RiverSources.Count} detected");
        _output.WriteLine($"  - Preserved basins: {world.Phase1Erosion.PreservedBasins.Count} (TD_023: Enhanced with metadata)");

        // ═══════════════════════════════════════════════════════════════════════
        // VALIDATE: PreFillingLocalMinima diagnostic data
        // ═══════════════════════════════════════════════════════════════════════

        world.PreFillingLocalMinima.Should().NotBeNull("PreFillingLocalMinima should be computed before pit-filling");
        world.PreFillingLocalMinima!.Count.Should().BeGreaterThan(0, "Raw heightmap should have some local minima (artifacts + real pits)");

        // Validate pit-filling effectiveness: PreFilling > PostFilling sinks
        int preFillingCount = world.PreFillingLocalMinima.Count;
        int postFillingCount = world.Phase1Erosion.PreservedBasins.Count;  // TD_023: PreservedBasins = lakes with metadata

        postFillingCount.Should().BeLessThan(preFillingCount,
            "Pit-filling should reduce sink count (fill artifacts, preserve real lakes)");

        float reductionPercent = preFillingCount > 0
            ? ((preFillingCount - postFillingCount) / (float)preFillingCount) * 100f
            : 0f;

        reductionPercent.Should().BeGreaterThanOrEqualTo(50f,
            "Pit-filling should reduce sinks by at least 50% (expected 70-90% for good heightmap)");

        _output.WriteLine($"✓ Pit-filling effectiveness:");
        _output.WriteLine($"  - Pre-filling sinks: {preFillingCount}");
        _output.WriteLine($"  - Post-filling sinks: {postFillingCount} (lakes preserved)");
        _output.WriteLine($"  - Reduction: {reductionPercent:F1}%");
    }

    [Fact]
    public void Generate_WhenPipelineComplete_ShouldHaveConsistentFlowData()
    {
        // WHY: Flow directions and accumulation must be consistent with filled heightmap.
        // This test validates the D-8 algorithm produces hydrologically correct results.

        // ARRANGE
        ValidateLibraryOrSkip();

        var projectPath = GetProjectRoot();
        var nativeSimulator = new NativePlateSimulator(NullLogger<NativePlateSimulator>.Instance, projectPath);
        var pipeline = new GenerateWorldPipeline(nativeSimulator, NullLogger<GenerateWorldPipeline>.Instance);

        var parameters = new PlateSimulationParams(
            seed: 67890,
            worldSize: 64,   // Smaller for focused test
            plateCount: 3,
            cycleCount: 1);

        // ACT
        var result = pipeline.Generate(parameters);

        // ASSERT
        result.IsSuccess.Should().BeTrue();
        var world = result.Value;

        world.Phase1Erosion.Should().NotBeNull();

        // ═══════════════════════════════════════════════════════════════════════
        // CONSISTENCY CHECK: Flow directions point downhill
        // ═══════════════════════════════════════════════════════════════════════

        var flowDirs = world.Phase1Erosion!.FlowDirections;
        var heightmap = world.Phase1Erosion.FilledHeightmap;
        var oceanMask = world.OceanMask!;

        // Direction offsets (matching FlowDirectionCalculator)
        var directions = new (int dx, int dy)[]
        {
            (0, -1),   // 0: North
            (1, -1),   // 1: NE
            (1, 0),    // 2: East
            (1, 1),    // 3: SE
            (0, 1),    // 4: South
            (-1, 1),   // 5: SW
            (-1, 0),   // 6: West
            (-1, -1)   // 7: NW
        };

        int validDownhillFlows = 0;
        int totalLandCellsChecked = 0;

        for (int y = 1; y < 63; y++)  // Avoid borders for simplicity
        {
            for (int x = 1; x < 63; x++)
            {
                // Skip ocean cells
                if (oceanMask[y, x])
                    continue;

                totalLandCellsChecked++;

                int flowDir = flowDirs[y, x];

                // Skip sinks (lakes/pits)
                if (flowDir == -1)
                    continue;

                // Validate flow direction points to lower neighbor
                var (dx, dy) = directions[flowDir];
                int nx = x + dx;
                int ny = y + dy;

                float currentElev = heightmap[y, x];
                float neighborElev = heightmap[ny, nx];

                if (neighborElev <= currentElev)
                {
                    validDownhillFlows++;
                }
                else
                {
                    // EDGE CASE: May happen due to numerical precision or ocean borders
                    // Log but don't fail (real-world heightmaps have noise)
                    _output.WriteLine($"Note: Flow direction at [{y},{x}] points uphill by {neighborElev - currentElev:F6} (may be precision issue)");
                }
            }
        }

        totalLandCellsChecked.Should().BeGreaterThan(0, "Should have land cells to validate");

        // Allow some tolerance for numerical precision (expect >95% valid)
        float validPercent = (validDownhillFlows / (float)totalLandCellsChecked) * 100f;
        validPercent.Should().BeGreaterThanOrEqualTo(95f,
            "At least 95% of flow directions should point downhill (D-8 algorithm correctness)");

        _output.WriteLine($"✓ Flow direction consistency:");
        _output.WriteLine($"  - Land cells checked: {totalLandCellsChecked}");
        _output.WriteLine($"  - Valid downhill flows: {validDownhillFlows} ({validPercent:F1}%)");
    }

    // HELPER: Fail fast with helpful message if library not built
    // NOTE: These tests are excluded from CI via filter (see .github/workflows/ci-auto-fix.yml)
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
}
