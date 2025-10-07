using Godot;
using Darklands.Core.Features.WorldGen.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace Darklands.Features.WorldGen;

/// <summary>
/// Handles binary serialization/deserialization of WorldMap data.
/// Format: Versioned header + heightmap + plates map.
/// </summary>
public class WorldMapSerializationService
{
    private readonly ILogger<WorldMapSerializationService> _logger;

    // Binary format constants
    private const string MAGIC_NUMBER = "DWLD"; // Darklands World Data
    private const uint FORMAT_VERSION = 1;
    private const string SAVE_DIR = "user://worldgen_saves/";

    public WorldMapSerializationService(ILogger<WorldMapSerializationService> logger)
    {
        _logger = logger;
        EnsureSaveDirectoryExists();
    }

    /// <summary>
    /// Saves world data to disk in binary format.
    /// </summary>
    public bool SaveWorld(PlateSimulationResult world, int seed, string filename)
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
            file.Store32(FORMAT_VERSION);  // 4 bytes
            file.Store32((uint)seed);      // 4 bytes
            file.Store32(0);               // 4 bytes reserved for future use

            // Dimensions (8 bytes)
            file.Store32((uint)world.Width);
            file.Store32((uint)world.Height);

            // Heightmap (width × height × 4 bytes)
            for (int y = 0; y < world.Height; y++)
            {
                for (int x = 0; x < world.Width; x++)
                {
                    file.StoreFloat(world.Heightmap[y, x]);
                }
            }

            // Plates map (width × height × 4 bytes)
            for (int y = 0; y < world.Height; y++)
            {
                for (int x = 0; x < world.Width; x++)
                {
                    file.Store32(world.PlatesMap[y, x]);
                }
            }

            _logger.LogInformation("Saved world to disk: {Filename}, seed={Seed}, size={Width}x{Height}",
                filename, seed, world.Width, world.Height);
            return true;
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to save world: {Filename}", filename);
            return false;
        }
    }

    /// <summary>
    /// Loads world data from disk.
    /// Returns (success, world, seed).
    /// </summary>
    public (bool Success, PlateSimulationResult? World, int Seed) LoadWorld(string filename)
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
            if (version != FORMAT_VERSION)
            {
                _logger.LogError("Unsupported format version: {Version} (expected {Expected})",
                    version, FORMAT_VERSION);
                return (false, null, 0);
            }

            int seed = (int)file.Get32();
            file.Get32(); // Skip reserved bytes

            // Read dimensions
            int width = (int)file.Get32();
            int height = (int)file.Get32();

            // Read heightmap
            var heightmap = new float[height, width];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    heightmap[y, x] = file.GetFloat();
                }
            }

            // Read plates map
            var platesMap = new uint[height, width];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    platesMap[y, x] = file.Get32();
                }
            }

            var world = new PlateSimulationResult(heightmap, platesMap);

            _logger.LogInformation("Loaded world from disk: {Filename}, seed={Seed}, size={Width}x{Height}",
                filename, seed, width, height);

            return (true, world, seed);
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
}
