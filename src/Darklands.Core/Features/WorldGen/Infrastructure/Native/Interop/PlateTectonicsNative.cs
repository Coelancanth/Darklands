using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Darklands.Core.Features.WorldGen.Infrastructure.Native.Interop;

/// <summary>
/// PInvoke declarations for plate-tectonics C library.
/// LAYER 1 (Interop): Raw unsafe native calls - internal visibility only.
/// See ADR-007: Native Library Integration Architecture (v1.2 - LibraryImport standard)
/// </summary>
internal static partial class PlateTectonicsNative
{
    private const string LibraryName = "PlateTectonics";
    private static string? _nativeLibraryPath;

    /// <summary>
    /// Static constructor registers DllImportResolver for custom library search path.
    /// This allows .NET to find DLLs in addons/darklands/bin/{platform}/ directory.
    /// </summary>
    static PlateTectonicsNative()
    {
        NativeLibrary.SetDllImportResolver(typeof(PlateTectonicsNative).Assembly, DllImportResolver);
    }

    /// <summary>
    /// Custom DLL resolver that searches in platform-specific bin directories.
    /// Called by .NET runtime before default search paths.
    /// </summary>
    private static IntPtr DllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        // Only handle our specific library (let .NET handle others like System DLLs)
        if (libraryName != LibraryName)
            return IntPtr.Zero;

        // Lazy-initialize library path (expensive operation)
        if (_nativeLibraryPath == null)
        {
            var platform = DetectPlatform();
            var projectRoot = Directory.GetCurrentDirectory(); // Will be overridden by Presentation layer

            // Try Godot project path first (works when running in-engine)
            var godotPath = Path.Combine(projectRoot, "addons", "darklands", "bin", platform, GetLibraryFileName());

            // Fallback: Search relative to Core assembly (works for tests)
            if (!File.Exists(godotPath))
            {
                var assemblyDir = Path.GetDirectoryName(assembly.Location) ?? "";
                var testPath = Path.Combine(assemblyDir, "..", "..", "..", "..", "..", "addons", "darklands", "bin", platform, GetLibraryFileName());
                _nativeLibraryPath = Path.GetFullPath(testPath);
            }
            else
            {
                _nativeLibraryPath = godotPath;
            }
        }

        // Try to load from resolved path
        if (File.Exists(_nativeLibraryPath) && NativeLibrary.TryLoad(_nativeLibraryPath, out var handle))
        {
            return handle;
        }

        // Let .NET's default search handle it (will fail with helpful error)
        return IntPtr.Zero;
    }

    private static string DetectPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return RuntimeInformation.ProcessArchitecture == Architecture.X64 ? "win-x64" : "win-x86";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return RuntimeInformation.ProcessArchitecture == Architecture.X64 ? "linux-x64" : "linux-arm64";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return RuntimeInformation.ProcessArchitecture == Architecture.X64 ? "macos-x64" : "macos-arm64";

        throw new PlatformNotSupportedException();
    }

    private static string GetLibraryFileName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return $"{LibraryName}.dll";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return $"lib{LibraryName}.so";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return $"lib{LibraryName}.dylib";

        throw new PlatformNotSupportedException();
    }

    /// <summary>
    /// Creates a new plate tectonics simulation instance.
    /// </summary>
    /// <param name="seed">Random seed for reproducibility</param>
    /// <param name="width">Map width in pixels</param>
    /// <param name="height">Map height in pixels</param>
    /// <param name="seaLevel">Sea level threshold (0.0-1.0)</param>
    /// <param name="erosionPeriod">Erosion cycles</param>
    /// <param name="foldingRatio">Mountain folding ratio</param>
    /// <param name="aggrOverlapAbs">Absolute aggregation overlap</param>
    /// <param name="aggrOverlapRel">Relative aggregation overlap</param>
    /// <param name="cycleCount">Number of simulation cycles</param>
    /// <param name="numPlates">Number of tectonic plates</param>
    /// <returns>Opaque handle to simulation instance (must be destroyed)</returns>
    [LibraryImport(LibraryName, EntryPoint = "platec_api_create")]
    internal static partial IntPtr Create(
        int seed,
        uint width,
        uint height,
        float seaLevel,
        uint erosionPeriod,
        float foldingRatio,
        uint aggrOverlapAbs,
        float aggrOverlapRel,
        uint cycleCount,
        uint numPlates);

    /// <summary>
    /// Advances simulation by one step.
    /// Call repeatedly until IsFinished returns true.
    /// </summary>
    [LibraryImport(LibraryName, EntryPoint = "platec_api_step")]
    internal static partial void Step(IntPtr handle);

    /// <summary>
    /// Checks if simulation has completed all cycles.
    /// </summary>
    /// <returns>Non-zero if finished, zero if more steps needed</returns>
    [LibraryImport(LibraryName, EntryPoint = "platec_api_is_finished")]
    internal static partial uint IsFinished(IntPtr handle);

    /// <summary>
    /// Retrieves heightmap as 1D array (row-major: [y * width + x]).
    /// Array is managed by native library - do NOT free!
    /// Valid until next Step() or Destroy() call.
    /// </summary>
    /// <returns>Pointer to float array of size (width * height)</returns>
    [LibraryImport(LibraryName, EntryPoint = "platec_api_get_heightmap")]
    internal static partial IntPtr GetHeightmap(IntPtr handle);

    /// <summary>
    /// Retrieves plate ID map (which plate owns each pixel).
    /// Array is managed by native library - do NOT free!
    /// </summary>
    /// <returns>Pointer to uint array of size (width * height)</returns>
    [LibraryImport(LibraryName, EntryPoint = "platec_api_get_platesmap")]
    internal static partial IntPtr GetPlatesMap(IntPtr handle);

    /// <summary>
    /// Gets map width from simulation handle.
    /// </summary>
    [LibraryImport(LibraryName, EntryPoint = "lithosphere_getMapWidth")]
    internal static partial uint GetMapWidth(IntPtr handle);

    /// <summary>
    /// Gets map height from simulation handle.
    /// </summary>
    [LibraryImport(LibraryName, EntryPoint = "lithosphere_getMapHeight")]
    internal static partial uint GetMapHeight(IntPtr handle);

    /// <summary>
    /// Destroys simulation and frees native memory.
    /// Handle becomes invalid after this call.
    /// </summary>
    [LibraryImport(LibraryName, EntryPoint = "platec_api_destroy")]
    internal static partial void Destroy(IntPtr handle);
}
