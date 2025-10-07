using System;
using System.IO;
using System.Runtime.InteropServices;
using CSharpFunctionalExtensions;

namespace Darklands.Core.Features.WorldGen.Infrastructure.Native;

/// <summary>
/// Validates native library availability and provides helpful error messages.
/// LAYER 1 (Interop): Platform-specific library path resolution.
///
/// NOTE: Actual DLL loading is handled by .NET runtime via DllImport.
/// This class performs pre-flight checks for fail-fast error reporting.
/// See ADR-007: Native Library Integration Architecture
/// </summary>
public static class NativeLibraryLoader
{
    private const string LibraryName = "PlateTectonics";

    /// <summary>
    /// Validates that plate-tectonics library exists at expected path.
    /// Returns helpful error message if missing.
    /// </summary>
    /// <param name="projectPath">Godot project root path (from ProjectSettings.GlobalizePath)</param>
    /// <returns>Success if library found, Failure with diagnostic message otherwise</returns>
    public static Result ValidateLibraryExists(string projectPath)
    {
        var platform = DetectPlatform();
        var expectedPath = Path.Combine(
            projectPath,
            "addons",
            "darklands",
            "bin",
            platform,
            GetLibraryFileName());

        if (!File.Exists(expectedPath))
        {
            return Result.Failure(
                $"ERROR_NATIVE_LIBRARY_MISSING: {LibraryName} not found at '{expectedPath}'. " +
                $"Platform: {platform}. " +
                $"See NATIVE_LIBRARIES.md for build instructions.");
        }

        return Result.Success();
    }

    /// <summary>
    /// Detects current platform for native library selection.
    /// </summary>
    private static string DetectPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return RuntimeInformation.ProcessArchitecture == Architecture.X64
                ? "win-x64"
                : "win-x86";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return RuntimeInformation.ProcessArchitecture == Architecture.X64
                ? "linux-x64"
                : "linux-arm64";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return RuntimeInformation.ProcessArchitecture == Architecture.X64
                ? "macos-x64"
                : "macos-arm64";
        }

        throw new PlatformNotSupportedException(
            $"Unsupported platform: {RuntimeInformation.OSDescription}");
    }

    /// <summary>
    /// Gets platform-specific library file name.
    /// </summary>
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
}
