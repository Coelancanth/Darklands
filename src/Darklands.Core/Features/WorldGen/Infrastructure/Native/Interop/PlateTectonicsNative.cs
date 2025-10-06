using System;
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
