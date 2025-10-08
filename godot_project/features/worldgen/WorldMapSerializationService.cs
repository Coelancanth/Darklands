using Godot;
using Darklands.Core.Features.WorldGen.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace Darklands.Features.WorldGen;

/// <summary>
/// Handles binary serialization/deserialization of WorldMap data.
/// Format v2: Versioned header + heightmap + plates + VS_024 post-processing data.
/// Backward compatible with v1 files (loads with graceful degradation).
/// </summary>
public class WorldMapSerializationService
{
    private readonly ILogger<WorldMapSerializationService> _logger;

    // Binary format constants
    private const string MAGIC_NUMBER = "DWLD"; // Darklands World Data
    private const uint FORMAT_VERSION = 2;
    private const string SAVE_DIR = "user://worldgen_saves/";

    public WorldMapSerializationService(ILogger<WorldMapSerializationService> logger)
    {
        _logger = logger;
        EnsureSaveDirectoryExists();
    }

    /// <summary>
    /// Saves world data to disk in binary format v2.
    /// Includes VS_024 post-processing data (thresholds, ocean mask, sea depth).
    /// </summary>
    public bool SaveWorld(WorldGenerationResult world, int seed, string filename)
    {
        string fullPath = SAVE_DIR + filename;

        try
        {
            using var file = FileAccess.Open(fullPath, FileAccess.ModeFlags.Write);
            if (file == null)
            {
                _logger.LogError("Failed to open file for writing: {Path}", fullPath);
                return false;
            }

            // Header (16 bytes)
            file.StoreBuffer(System.Text.Encoding.ASCII.GetBytes(MAGIC_NUMBER)); // 4 bytes
            file.Store32(FORMAT_VERSION);  // 4 bytes (v2)
            file.Store32((uint)seed);      // 4 bytes
            file.Store32(0);               // 4 bytes reserved for future use

            // Dimensions (8 bytes)
            file.Store32((uint)world.Width);
            file.Store32((uint)world.Height);

            // Core data (v1 compatibility section)
            SaveFloatArray(file, world.Heightmap, world.Width, world.Height);
            SaveUIntArray(file, world.PlatesMap, world.Width, world.Height);

            // VS_024 post-processing data (optional section)
            SaveOptionalFloatArray(file, world.PostProcessedHeightmap, world.Width, world.Height);

            if (world.Thresholds != null)
            {
                file.Store8(1); // HasThresholds flag
                file.StoreFloat(world.Thresholds.SeaLevel);
                file.StoreFloat(world.Thresholds.HillLevel);
                file.StoreFloat(world.Thresholds.MountainLevel);
                file.StoreFloat(world.Thresholds.PeakLevel);
            }
            else
            {
                file.Store8(0); // No thresholds
            }

            // Min/Max elevation (for realistic meters mapping - fixes 50km ocean depth bug)
            file.Store8(1); // HasMinMaxElevation flag (always true for new saves)
            file.StoreFloat(world.MinElevation);
            file.StoreFloat(world.MaxElevation);

            SaveOptionalBoolArray(file, world.OceanMask, world.Width, world.Height);
            SaveOptionalFloatArray(file, world.SeaDepth, world.Width, world.Height);

            // Future: TemperatureMap (VS_025) - will append here without version bump

            _logger.LogInformation("Saved world v{Version} to disk: {Filename}, seed={Seed}, size={Width}x{Height}, hasPostProcessing={HasPost}",
                FORMAT_VERSION, filename, seed, world.Width, world.Height, world.PostProcessedHeightmap != null);
            return true;
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to save world: {Filename}", filename);
            return false;
        }
    }

    /// <summary>
    /// Loads world data from disk with version detection.
    /// v1: Returns WorldGenerationResult with nulls (graceful degradation).
    /// v2: Returns full WorldGenerationResult with post-processing data.
    /// Returns (success, world, seed).
    /// </summary>
    public (bool Success, WorldGenerationResult? World, int Seed) LoadWorld(string filename)
    {
        string fullPath = SAVE_DIR + filename;

        if (!FileAccess.FileExists(fullPath))
        {
            _logger.LogWarning("World file does not exist: {Filename}", filename);
            return (false, null, 0);
        }

        try
        {
            using var file = FileAccess.Open(fullPath, FileAccess.ModeFlags.Read);
            if (file == null)
            {
                _logger.LogError("Failed to open file for reading: {Path}", fullPath);
                return (false, null, 0);
            }

            // Read header
            var magicBytes = file.GetBuffer(4);
            string magic = System.Text.Encoding.ASCII.GetString(magicBytes);
            if (magic != MAGIC_NUMBER)
            {
                _logger.LogError("Invalid file format: Expected magic '{Expected}', got '{Actual}'",
                    MAGIC_NUMBER, magic);
                return (false, null, 0);
            }

            uint version = file.Get32();
            int seed = (int)file.Get32();
            file.Get32(); // Skip reserved bytes

            // Version-specific loading
            return version switch
            {
                1 => LoadV1(file, seed, filename),
                2 => LoadV2(file, seed, filename),
                _ => (false, null, 0)  // Unsupported version
            };
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to load world: {Filename}", filename);
            return (false, null, 0);
        }
    }

    /// <summary>
    /// Lists all saved world files.
    /// </summary>
    public string[] ListSavedWorlds()
    {
        if (!DirAccess.DirExistsAbsolute(SAVE_DIR))
            return System.Array.Empty<string>();

        using var dir = DirAccess.Open(SAVE_DIR);
        if (dir == null)
            return System.Array.Empty<string>();

        var files = new System.Collections.Generic.List<string>();
        dir.ListDirBegin();

        string filename;
        while ((filename = dir.GetNext()) != "")
        {
            if (!dir.CurrentIsDir() && filename.EndsWith(".dwld"))
            {
                files.Add(filename);
            }
        }

        dir.ListDirEnd();
        return files.ToArray();
    }

    private void EnsureSaveDirectoryExists()
    {
        if (!DirAccess.DirExistsAbsolute(SAVE_DIR))
        {
            DirAccess.MakeDirAbsolute(SAVE_DIR);
            _logger.LogInformation("Created save directory: {Dir}", SAVE_DIR);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Version-Specific Loaders
    // ═══════════════════════════════════════════════════════════════════════

    private (bool, WorldGenerationResult?, int) LoadV1(FileAccess file, int seed, string filename)
    {
        // Read dimensions
        int width = (int)file.Get32();
        int height = (int)file.Get32();

        // Read v1 data (heightmap + plates only)
        var heightmap = LoadFloatArray(file, width, height);
        var platesMap = LoadUIntArray(file, width, height);

        // Validate dimensions
        if (!ValidateDimensions(heightmap, width, height, "Heightmap") ||
            !ValidateDimensions(platesMap, width, height, "PlatesMap"))
        {
            return (false, null, 0);
        }

        // Wrap in WorldGenerationResult with nulls (graceful degradation)
        var rawNative = new PlateSimulationResult(heightmap, platesMap);
        var world = new WorldGenerationResult(
            heightmap: heightmap,
            platesMap: platesMap,
            rawNativeOutput: rawNative,
            postProcessedHeightmap: null,
            thresholds: null,
            oceanMask: null,
            seaDepth: null,
            temperatureMap: null,
            precipitationMap: null
        );

        _logger.LogInformation("Loaded world v1 (backward compat): {Filename}, seed={Seed}, size={Width}x{Height}",
            filename, seed, width, height);

        return (true, world, seed);
    }

    private (bool, WorldGenerationResult?, int) LoadV2(FileAccess file, int seed, string filename)
    {
        // Read dimensions
        int width = (int)file.Get32();
        int height = (int)file.Get32();

        // Core data (v1 compatibility section)
        var heightmap = LoadFloatArray(file, width, height);
        var platesMap = LoadUIntArray(file, width, height);

        if (!ValidateDimensions(heightmap, width, height, "Heightmap") ||
            !ValidateDimensions(platesMap, width, height, "PlatesMap"))
        {
            return (false, null, 0);
        }

        // VS_024 post-processing data (optional)
        var postProcessedHeightmap = LoadOptionalFloatArray(file, width, height);

        ElevationThresholds? thresholds = null;
        if (file.Get8() == 1)
        {
            thresholds = new ElevationThresholds(
                seaLevel: file.GetFloat(),
                hillLevel: file.GetFloat(),
                mountainLevel: file.GetFloat(),
                peakLevel: file.GetFloat()
            );
        }

        // Min/Max elevation (backward compatible - old v2 files won't have this)
        float minElevation = 0.1f;  // Default fallback (old v2 behavior)
        float maxElevation = 20.0f; // Default fallback (old v2 behavior)

        if (file.GetPosition() < file.GetLength())  // Check if more data exists
        {
            byte hasMinMax = file.Get8();
            if (hasMinMax == 1)
            {
                minElevation = file.GetFloat();
                maxElevation = file.GetFloat();
            }
        }

        var oceanMask = LoadOptionalBoolArray(file, width, height);
        var seaDepth = LoadOptionalFloatArray(file, width, height);

        // Dimension validation for optional arrays
        if (postProcessedHeightmap != null && !ValidateDimensions(postProcessedHeightmap, width, height, "PostProcessedHeightmap"))
            return (false, null, 0);
        if (oceanMask != null && !ValidateDimensions(oceanMask, width, height, "OceanMask"))
            return (false, null, 0);
        if (seaDepth != null && !ValidateDimensions(seaDepth, width, height, "SeaDepth"))
            return (false, null, 0);

        // Future: TemperatureMap (VS_025) - will read here

        var rawNative = new PlateSimulationResult(heightmap, platesMap);
        var world = new WorldGenerationResult(
            heightmap: heightmap,
            platesMap: platesMap,
            rawNativeOutput: rawNative,
            postProcessedHeightmap: postProcessedHeightmap,
            thresholds: thresholds,
            minElevation: minElevation,
            maxElevation: maxElevation,
            oceanMask: oceanMask,
            seaDepth: seaDepth,
            temperatureMap: null,  // VS_025
            precipitationMap: null
        );

        _logger.LogInformation("Loaded world v2: {Filename}, seed={Seed}, size={Width}x{Height}, hasPostProcessing={HasPost}",
            filename, seed, width, height, postProcessedHeightmap != null);

        return (true, world, seed);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Array Serialization Helpers
    // ═══════════════════════════════════════════════════════════════════════

    private void SaveFloatArray(FileAccess file, float[,] array, int width, int height)
    {
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                file.StoreFloat(array[y, x]);
    }

    private float[,] LoadFloatArray(FileAccess file, int width, int height)
    {
        var array = new float[height, width];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                array[y, x] = file.GetFloat();
        return array;
    }

    private void SaveUIntArray(FileAccess file, uint[,] array, int width, int height)
    {
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                file.Store32(array[y, x]);
    }

    private uint[,] LoadUIntArray(FileAccess file, int width, int height)
    {
        var array = new uint[height, width];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                array[y, x] = file.Get32();
        return array;
    }

    private void SaveOptionalFloatArray(FileAccess file, float[,]? array, int width, int height)
    {
        if (array == null)
        {
            file.Store8(0); // HasData flag = false
            return;
        }

        file.Store8(1); // HasData flag = true
        SaveFloatArray(file, array, width, height);
    }

    private float[,]? LoadOptionalFloatArray(FileAccess file, int width, int height)
    {
        if (file.Get8() == 0) return null;
        return LoadFloatArray(file, width, height);
    }

    private void SaveOptionalBoolArray(FileAccess file, bool[,]? array, int width, int height)
    {
        if (array == null)
        {
            file.Store8(0); // HasData flag = false
            return;
        }

        file.Store8(1); // HasData flag = true

        // Bit-pack for 8x space savings
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x += 8)
            {
                byte packed = 0;
                for (int bit = 0; bit < 8 && x + bit < width; bit++)
                {
                    if (array[y, x + bit])
                        packed |= (byte)(1 << bit);
                }
                file.Store8(packed);
            }
        }
    }

    private bool[,]? LoadOptionalBoolArray(FileAccess file, int width, int height)
    {
        if (file.Get8() == 0) return null;

        var array = new bool[height, width];

        // Unpack bits
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x += 8)
            {
                byte packed = file.Get8();
                for (int bit = 0; bit < 8 && x + bit < width; bit++)
                {
                    array[y, x + bit] = (packed & (1 << bit)) != 0;
                }
            }
        }

        return array;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Validation
    // ═══════════════════════════════════════════════════════════════════════

    private bool ValidateDimensions<T>(T[,] array, int expectedWidth, int expectedHeight, string arrayName)
    {
        int actualHeight = array.GetLength(0);
        int actualWidth = array.GetLength(1);

        if (actualHeight != expectedHeight || actualWidth != expectedWidth)
        {
            _logger.LogError("Dimension mismatch: {Array} {ActualH}x{ActualW} != declared {H}x{W}",
                arrayName, actualHeight, actualWidth, expectedHeight, expectedWidth);
            return false;
        }

        return true;
    }
}
