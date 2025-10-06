# ADR-007: Native Library Integration Architecture

**Status**: Proposed
**Date**: 2025-10-06
**Last Updated**: 2025-10-06
**Decision Makers**: Tech Lead
**Applies To**: All features requiring C/C++ native library integration

**Changelog**:
- 2025-10-06 (v1.1): Architecture review refinements - Span<T> marshaling, Godot path resolution, build strategy documentation
- 2025-10-06 (v1.0): Initial proposal - Three-layer isolation pattern for PInvoke integration

---

## Context

Game development often benefits from leveraging existing C/C++ libraries for:
- **Performance-critical algorithms** (physics simulation, procedural generation, pathfinding)
- **Mature ecosystems** (plate tectonics simulation, image processing, compression)
- **Platform-specific APIs** (native OS features, hardware acceleration)

**The Challenge**: How do we integrate native libraries while maintaining:
1. ✅ **Clean Architecture** (Core stays pure C#, testable without native dependencies)
2. ✅ **Functional Error Handling** (library load failures, marshaling errors → Result<T>)
3. ✅ **Cross-Platform Support** (Windows, Linux, macOS with different binary formats)
4. ✅ **Memory Safety** (RAII for native resource cleanup, prevent leaks)
5. ✅ **Developer Experience** (helpful error messages, fail-fast on misconfiguration)

**Example Use Cases**:
- **WorldGen**: plate-tectonics C++ library for physics-driven terrain generation
- **Pathfinding**: Recast/Detour C++ library for navmesh generation
- **Audio**: FMOD/Wwise native audio engines

---

## Decision

We adopt a **Three-Layer Isolation Pattern** for native library integration:

1. **Interop Layer** (Unsafe): DllImport declarations, marshaling, SafeHandles
2. **Wrapper Layer** (Safe): Managed wrapper class, Result<T> error handling
3. **Interface Layer** (Pure): Application abstractions, zero native dependencies

This pattern ensures Core stays pure C# while isolating unsafe PInvoke code to Infrastructure.

---

## Architecture Structure

### Folder Organization

```
src/Darklands.Core/
└── Features/
    └── {Feature}/
        ├── Application/
        │   └── Abstractions/
        │       └── INativeService.cs       # Pure C# interface (ZERO native deps)
        │
        └── Infrastructure/
            └── Native/
                ├── NativeServiceWrapper.cs  # Managed wrapper (Result<T>)
                ├── NativeLibraryLoader.cs   # Load validation + platform detection
                │
                ├── Interop/                 # Unsafe code ONLY
                │   ├── NativeMethods.cs     # [DllImport] declarations
                │   ├── NativeStructs.cs     # Marshaling structs
                │   └── SafeHandles.cs       # SafeHandle wrappers
                │
                └── bin/                     # Native binaries (platform-specific)
                    ├── win-x64/
                    │   └── libname.dll
                    ├── linux-x64/
                    │   └── libname.so
                    └── osx-x64/
                        └── libname.dylib
```

**Key Principles**:
- ✅ **Interop/ folder contains ALL unsafe code** (`unsafe`, `IntPtr`, `Marshal.*`)
- ✅ **Wrapper exposes only safe Result<T> APIs** (no IntPtr, no exceptions)
- ✅ **Application layer depends on interface** (never on wrapper implementation)
- ✅ **Native binaries isolated to Infrastructure** (Core has zero binary dependencies)

---

## Three Layers Explained

### Layer 1: Interop (Unsafe Code)

**Purpose**: Minimal PInvoke declarations and marshaling logic.

**Location**: `Infrastructure/Native/Interop/`

**Example**:
```csharp
// Infrastructure/Native/Interop/PlateTectonicsNative.cs
using System.Runtime.InteropServices;

namespace Darklands.Core.Features.WorldGen.Infrastructure.Native.Interop;

internal static class PlateTectonicsNative
{
    private const string LibraryName = "libplatec";

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    internal static extern PlateSimulationHandle platec_create(
        int seed,
        int width,
        int height,
        float seaLevel,
        int erosionPeriod,
        float foldingRatio,
        int aggOverlapAbs,
        float aggOverlapRel,
        int cycleCount,
        int numPlates);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int platec_is_finished(PlateSimulationHandle handle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void platec_step(PlateSimulationHandle handle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr platec_get_heightmap(PlateSimulationHandle handle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr platec_get_platesmap(PlateSimulationHandle handle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void platec_destroy(PlateSimulationHandle handle);
}
```

**SafeHandle Pattern** (RAII for Native Resources):
```csharp
// Infrastructure/Native/Interop/SafeHandles.cs
using Microsoft.Win32.SafeHandles;

namespace Darklands.Core.Features.WorldGen.Infrastructure.Native.Interop;

/// <summary>
/// RAII wrapper for native plate simulation handle.
/// Automatically calls platec_destroy when disposed.
/// </summary>
internal sealed class PlateSimulationHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    private PlateSimulationHandle() : base(ownsHandle: true) { }

    protected override bool ReleaseHandle()
    {
        if (!IsInvalid)
        {
            PlateTectonicsNative.platec_destroy(this);
        }
        return true;
    }
}
```

**Why SafeHandle**:
- ✅ **Automatic cleanup** - Finalizer ensures native resources freed even if exception thrown
- ✅ **Prevents leaks** - GC-aware reference counting
- ✅ **Thread-safe** - Built-in protection against race conditions

---

### Layer 2: Wrapper (Safe Managed Code)

**Purpose**: Convert unsafe PInvoke calls to safe Result<T> APIs.

**Location**: `Infrastructure/Native/`

**Example**:
```csharp
// Infrastructure/Native/NativePlateSimulator.cs
using CSharpFunctionalExtensions;
using Darklands.Core.Features.WorldGen.Application.Abstractions;
using Darklands.Core.Features.WorldGen.Infrastructure.Native.Interop;

namespace Darklands.Core.Features.WorldGen.Infrastructure.Native;

public sealed class NativePlateSimulator : IPlateSimulator
{
    private readonly ILogger<NativePlateSimulator> _logger;
    private readonly NativeLibraryLoader _libraryLoader;

    public NativePlateSimulator(
        ILogger<NativePlateSimulator> logger,
        NativeLibraryLoader libraryLoader)
    {
        _logger = logger;
        _libraryLoader = libraryLoader;
    }

    public Result<PlateSimulationResult> Generate(PlateSimulationParams parameters)
    {
        // 1. Validate library loaded
        return _libraryLoader.EnsureLibraryLoaded()
            .Bind(() => CreateSimulation(parameters))
            .Bind(handle => RunSimulation(handle))
            .Bind(handle => ExtractResults(handle));
    }

    private Result<PlateSimulationHandle> CreateSimulation(PlateSimulationParams p)
    {
        return Result.Of(() =>
            {
                var handle = PlateTectonicsNative.platec_create(
                    p.Seed, p.Width, p.Height, p.SeaLevel,
                    p.ErosionPeriod, p.FoldingRatio,
                    p.AggrOverlapAbs, p.AggrOverlapRel,
                    p.CycleCount, p.NumPlates);

                if (handle.IsInvalid)
                    throw new InvalidOperationException("platec_create returned invalid handle");

                return handle;
            })
            .MapError(ex => $"ERROR_NATIVE_CREATE_FAILED: {ex.Message}");
    }

    private Result<PlateSimulationHandle> RunSimulation(PlateSimulationHandle handle)
    {
        return Result.Try(() =>
        {
            // Run simulation until completion
            while (PlateTectonicsNative.platec_is_finished(handle) == 0)
            {
                PlateTectonicsNative.platec_step(handle);
            }
            return handle;
        }, ex => $"ERROR_NATIVE_SIMULATION_FAILED: {ex.Message}");
    }

    private Result<PlateSimulationResult> ExtractResults(PlateSimulationHandle handle)
    {
        return Result.Try(() =>
        {
            using (handle) // Dispose after extracting (RAII)
            {
                IntPtr heightmapPtr = PlateTectonicsNative.platec_get_heightmap(handle);
                IntPtr platesmapPtr = PlateTectonicsNative.platec_get_platesmap(handle);

                if (heightmapPtr == IntPtr.Zero || platesmapPtr == IntPtr.Zero)
                    throw new InvalidOperationException("Failed to extract simulation results");

                // Marshal native arrays to managed arrays
                float[,] heightmap = MarshalHeightmap(heightmapPtr, handle.Width, handle.Height);
                ushort[,] platesmap = MarshalPlatesmap(platesmapPtr, handle.Width, handle.Height);

                return new PlateSimulationResult(heightmap, platesmap);
            }
        }, ex => $"ERROR_NATIVE_MARSHAL_FAILED: {ex.Message}");
    }

    // ✅ MODERN: Span<T> marshaling (preferred - less unsafe code, optimized)
    private unsafe float[,] MarshalHeightmap(IntPtr ptr, int width, int height)
    {
        return Marshal2DArray<float>(ptr, width, height);
    }

    private unsafe ushort[,] MarshalPlatesmap(IntPtr ptr, int width, int height)
    {
        return Marshal2DArray<ushort>(ptr, width, height);
    }

    /// <summary>
    /// Generic 2D array marshaling using Span&lt;T&gt; for safe, optimized memory copy.
    /// Reduces unsafe code surface area and uses optimized memcpy internally.
    /// </summary>
    private unsafe T[,] Marshal2DArray<T>(IntPtr ptr, int width, int height) where T : unmanaged
    {
        var result = new T[height, width];

        // Create Span from native pointer
        var sourceSpan = new Span<T>(ptr.ToPointer(), width * height);

        // Get memory view of managed array (row-major layout)
        var destinationSpan = MemoryMarshal.CreateSpan(ref result[0, 0], width * height);

        // Optimized block copy (uses memcpy under the hood)
        sourceSpan.CopyTo(destinationSpan);

        return result;
    }

    // ⚠️ LEGACY: Manual pointer marshaling (fallback if Span<T> unavailable)
    // private unsafe float[,] MarshalHeightmapLegacy(IntPtr ptr, int width, int height)
    // {
    //     float[,] result = new float[height, width];
    //     float* native = (float*)ptr;
    //
    //     for (int y = 0; y < height; y++)
    //     for (int x = 0; x < width; x++)
    //     {
    //         result[y, x] = native[y * width + x];
    //     }
    //
    //     return result;
    // }
}
```

**Key Patterns**:
- ✅ **Result.Of()** wraps risky operations (ADR-003 Infrastructure error handling)
- ✅ **using (handle)** ensures disposal even on exception (RAII pattern)
- ✅ **Error keys** follow i18n convention (`ERROR_NATIVE_*` - ADR-005)
- ✅ **All exceptions caught at boundary** → converted to Result.Failure

---

### Layer 3: Interface (Pure C# Abstraction)

**Purpose**: Core Application layer depends on interface, not implementation.

**Location**: `Application/Abstractions/`

**Example**:
```csharp
// Application/Abstractions/IPlateSimulator.cs
using CSharpFunctionalExtensions;

namespace Darklands.Core.Features.WorldGen.Application.Abstractions;

/// <summary>
/// Abstraction for plate tectonics simulation.
/// Implementation may use native library (NativePlateSimulator) or pure C# (ManagedPlateSimulator).
/// </summary>
public interface IPlateSimulator
{
    /// <summary>
    /// Generates heightmap and plates map using plate tectonics simulation.
    /// </summary>
    /// <returns>Result containing heightmap + platesmap, or error if simulation failed.</returns>
    Result<PlateSimulationResult> Generate(PlateSimulationParams parameters);
}

public record PlateSimulationParams(
    int Seed,
    int Width,
    int Height,
    float SeaLevel = 0.65f,
    int ErosionPeriod = 60,
    float FoldingRatio = 0.02f,
    int AggrOverlapAbs = 1000000,
    float AggrOverlapRel = 0.33f,
    int CycleCount = 2,
    int NumPlates = 10
);

public record PlateSimulationResult(
    float[,] Heightmap,
    ushort[,] Platesmap
);
```

**Why Interface**:
- ✅ **Testability** - Mock `IPlateSimulator` in unit tests (no native library required)
- ✅ **Flexibility** - Swap native implementation for pure C# fallback
- ✅ **Core purity** - Application layer has zero native dependencies

---

## Scaffolding: NativeLibraryLoader

**Purpose**: Centralized library load validation with helpful error messages.

**Location**: `Infrastructure/Native/`

**Example**:
```csharp
// Infrastructure/Native/NativeLibraryLoader.cs
using System.Runtime.InteropServices;
using CSharpFunctionalExtensions;

namespace Darklands.Core.Features.WorldGen.Infrastructure.Native;

public sealed class NativeLibraryLoader
{
    private readonly ILogger<NativeLibraryLoader> _logger;
    private bool _isLoaded = false;
    private readonly object _loadLock = new();

    public NativeLibraryLoader(ILogger<NativeLibraryLoader> logger)
    {
        _logger = logger;
    }

    public Result EnsureLibraryLoaded()
    {
        lock (_loadLock)
        {
            if (_isLoaded)
                return Result.Success();

            return LoadNativeLibrary()
                .Tap(() => _isLoaded = true)
                .TapError(err => _logger.LogError("Native library load failed: {Error}", err));
        }
    }

    private Result LoadNativeLibrary()
    {
        return GetLibraryPath()
            .Bind(ValidateLibraryExists)
            .Bind(LoadLibraryHandle)
            .Map(() => Unit.Default);
    }

    private Result<string> GetLibraryPath()
    {
        string libraryName = "libplatec";
        string extension = GetPlatformExtension();
        string platform = GetPlatformIdentifier();

        // ✅ GODOT-AWARE: Use Godot's path resolution (works in editor + exported game)
        string godotPath = $"res://addons/darklands/bin/{platform}/{libraryName}{extension}";
        string fullPath = ProjectSettings.GlobalizePath(godotPath);

        _logger.LogInformation("Attempting to load native library: {Path}", fullPath);
        _logger.LogDebug("Godot resource path: {GodotPath}", godotPath);

        return Result.Success(fullPath);
    }

    private string GetPlatformExtension()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return ".dll";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return ".so";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return ".dylib";

        throw new PlatformNotSupportedException($"Unsupported OS: {RuntimeInformation.OSDescription}");
    }

    private string GetPlatformIdentifier()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "win-x64";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return "linux-x64";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "osx-x64";

        throw new PlatformNotSupportedException();
    }

    private Result<string> ValidateLibraryExists(string path)
    {
        if (!File.Exists(path))
        {
            string helpMessage = @$"
ERROR: Native library not found at: {path}

TROUBLESHOOTING:
1. Check that native binaries are included in build output
2. Expected location: addons/darklands/bin/{{platform}}/libplatec{{.dll|.so|.dylib}}
3. On Windows, ensure MSVC++ Redistributable installed
4. See ADR-007 for deployment instructions

PLATFORM: {RuntimeInformation.OSDescription}
ARCHITECTURE: {RuntimeInformation.OSArchitecture}
";

            _logger.LogError(helpMessage);
            return Result.Failure<string>("ERROR_NATIVE_LIBRARY_MISSING");
        }

        return Result.Success(path);
    }

    private Result<IntPtr> LoadLibraryHandle(string path)
    {
        return Result.Try(() =>
        {
            IntPtr handle = NativeLibrary.Load(path);
            if (handle == IntPtr.Zero)
                throw new DllNotFoundException($"NativeLibrary.Load returned null for {path}");

            _logger.LogInformation("Native library loaded successfully: {Path}", path);
            return handle;
        }, ex => $"ERROR_NATIVE_LIBRARY_LOAD_FAILED: {ex.Message}");
    }
}
```

**Key Features**:
- ✅ **Platform detection** - Automatic Windows/Linux/macOS resolution
- ✅ **Helpful errors** - Troubleshooting guide with platform info
- ✅ **Fail-fast** - Validate at startup, not first use
- ✅ **Thread-safe** - Lock prevents race conditions
- ✅ **i18n compatible** - Error keys for translation

---

## Error Handling Strategy

**Principle**: All native library failures are **Infrastructure Errors** (ADR-003).

**Error Types**:

| Error Scenario | Error Key | Handling Strategy |
|----------------|-----------|-------------------|
| Library not found | `ERROR_NATIVE_LIBRARY_MISSING` | Help message with troubleshooting steps |
| Library load failed | `ERROR_NATIVE_LIBRARY_LOAD_FAILED` | Platform info + exception message |
| Native call failed | `ERROR_NATIVE_CREATE_FAILED` | Return Result.Failure with context |
| Marshaling failed | `ERROR_NATIVE_MARSHAL_FAILED` | Safe fallback or detailed error |
| Invalid handle | `ERROR_NATIVE_INVALID_HANDLE` | Indicates programmer error in wrapper |

**Pattern**:
```csharp
// ✅ CORRECT: Infrastructure errors → Result<T>
public Result<T> NativeOperation()
{
    return Result.Of(() => {
            // Risky native call
            var handle = NativeMethods.Create();
            if (handle.IsInvalid)
                throw new InvalidOperationException("Invalid handle");
            return handle;
        })
        .MapError(ex => $"ERROR_NATIVE_CREATE_FAILED: {ex.Message}");
}

// ❌ WRONG: Letting exceptions propagate
public T NativeOperation()
{
    var handle = NativeMethods.Create();  // May throw - breaks railway!
    return handle;
}
```

---

## Memory Management

**Principle**: Use RAII (Resource Acquisition Is Initialization) via SafeHandle.

**Pattern**:
```csharp
// ✅ CORRECT: SafeHandle ensures cleanup
public Result<Data> ProcessData(Params p)
{
    return CreateNativeHandle(p)
        .Bind(handle =>
        {
            using (handle)  // Disposal guaranteed, even if exception
            {
                return ExtractData(handle);
            }
        });
}

// ❌ WRONG: Manual cleanup (fragile!)
public Result<Data> ProcessData(Params p)
{
    IntPtr handle = IntPtr.Zero;
    try
    {
        handle = NativeMethods.Create();
        return ExtractData(handle);
    }
    finally
    {
        if (handle != IntPtr.Zero)
            NativeMethods.Destroy(handle);  // Might forget, might double-free
    }
}
```

**SafeHandle Benefits**:
- ✅ Automatic cleanup on GC (finalizer)
- ✅ Exception-safe (using statement)
- ✅ Thread-safe reference counting
- ✅ Prevents double-free bugs

---

## Cross-Platform Deployment

### Binary Organization

```
addons/darklands/bin/
├── win-x64/
│   ├── libplatec.dll
│   └── msvcr120.dll       # MSVC++ runtime if needed
├── linux-x64/
│   └── libplatec.so
└── osx-x64/
    └── libplatec.dylib
```

### .gitignore Strategy

**Option 1: Commit binaries** (Simplest for small teams)
```gitignore
# Don't ignore native binaries
!addons/darklands/bin/**/*.dll
!addons/darklands/bin/**/*.so
!addons/darklands/bin/**/*.dylib
```

**Option 2: Document download sources** (Better for large binaries)
```markdown
# NATIVE_LIBRARIES.md

## plate-tectonics

**Download**: https://github.com/Mindwerks/plate-tectonics/releases/tag/v1.5.0
**Platforms**: Windows x64, Linux x64, macOS x64
**License**: MIT

**Installation**:
1. Download release for your platform
2. Extract to addons/darklands/bin/{platform}/
3. Verify with: dotnet test --filter "NativeLibraryTests"
```

### .csproj Integration

```xml
<!-- Darklands.Core.csproj -->
<ItemGroup>
  <!-- Copy native binaries to output directory -->
  <None Include="Features\WorldGen\Infrastructure\Native\bin\**\*.*">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    <Link>addons\darklands\bin\%(RecursiveDir)%(Filename)%(Extension)</Link>
  </None>
</ItemGroup>
```

---

## Native Library Build Strategy

**Critical Decision**: How do we obtain and version native library binaries?

### Option 1: Git Submodule + CI Build (Reproducible Builds)

**When to use**: You control the native library source, need customization, or want reproducible builds.

**Setup**:
```bash
# Add C++ library as submodule
git submodule add https://github.com/Mindwerks/plate-tectonics References/plate-tectonics
git submodule update --init --recursive
```

**CI Pipeline** (.github/workflows/build-native.yml):
```yaml
name: Build Native Libraries

on: [push, pull_request]

jobs:
  build-windows:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
        with:
          submodules: recursive
      - name: Build with CMake
        run: |
          cd References/plate-tectonics
          cmake -B build -DCMAKE_BUILD_TYPE=Release
          cmake --build build --config Release
      - name: Copy binaries
        run: |
          mkdir -p addons/darklands/bin/win-x64
          cp References/plate-tectonics/build/Release/libplatec.dll addons/darklands/bin/win-x64/
      - uses: actions/upload-artifact@v3
        with:
          name: native-windows
          path: addons/darklands/bin/win-x64/

  build-linux:
    runs-on: ubuntu-latest
    steps:
      # Similar steps for Linux
      - name: Build with CMake
        run: |
          cd References/plate-tectonics
          cmake -B build -DCMAKE_BUILD_TYPE=Release
          cmake --build build
      - name: Copy binaries
        run: |
          mkdir -p addons/darklands/bin/linux-x64
          cp References/plate-tectonics/build/libplatec.so addons/darklands/bin/linux-x64/
```

**Pros**:
- ✅ **Reproducible** - Same source → same binaries
- ✅ **Version control** - Git tracks exact library version
- ✅ **Customization** - Can modify C++ source if needed
- ✅ **CI automation** - Builds on every commit

**Cons**:
- ❌ **Build complexity** - Requires CMake, compilers in CI
- ❌ **Build time** - C++ compilation adds 5-15 minutes to CI
- ❌ **Platform toolchains** - Must configure Windows (MSVC), Linux (gcc), macOS (clang)

---

### Option 2: Pre-Compiled Binaries from Releases (Fastest Onboarding)

**When to use**: Using third-party library, no modifications needed, want fast iteration.

**NATIVE_LIBRARIES.md**:
```markdown
# Native Library Dependencies

## plate-tectonics v1.5.0

**Source**: https://github.com/Mindwerks/plate-tectonics
**Download**: https://github.com/Mindwerks/plate-tectonics/releases/tag/v1.5.0
**License**: MIT

**SHA256 Checksums**:
- win-x64: `a1b2c3d4...`
- linux-x64: `e5f6g7h8...`
- osx-x64: `i9j0k1l2...`

**Installation**:
1. Download release for your platform
2. Extract to `addons/darklands/bin/{platform}/`
3. Verify: `dotnet test --filter "Category=Native"`

**Version Lock**: This project requires v1.5.0 exactly. Do not upgrade without testing.
```

**Developer Workflow**:
```bash
# One-time setup
./scripts/download-native-libs.sh  # Script downloads and verifies checksums
dotnet test --filter "Category=Native"  # Validate libraries loaded
```

**Pros**:
- ✅ **Fast onboarding** - Developers download binaries, no build toolchain required
- ✅ **Zero CI overhead** - No compilation step in pipeline
- ✅ **Simple** - `git clone` → download binaries → build works

**Cons**:
- ❌ **Trust third-party** - Binaries not built by your team
- ❌ **Version drift** - Manual process to update to new releases
- ❌ **Manual verification** - Must verify checksums to prevent tampering

**Mitigation**: Use SHA256 checksums in NATIVE_LIBRARIES.md, automate download script.

---

### Option 3: NuGet Package with Runtime Assets (Future)

**When to use**: Native library officially published to NuGet with runtime-specific binaries.

**Setup**:
```xml
<PackageReference Include="PlateTectonics.Native" Version="1.5.0" />
```

**How it works**:
- NuGet package includes `runtimes/win-x64/native/libplatec.dll`, `runtimes/linux-x64/native/libplatec.so`, etc.
- NuGet automatically copies platform-specific binary to output directory
- Version management through .csproj (same as C# dependencies)

**Pros**:
- ✅ **Automatic** - No manual download, NuGet handles everything
- ✅ **Version management** - `dotnet restore` gets correct version
- ✅ **CI-friendly** - Works with standard .NET build process

**Cons**:
- ❌ **Rare** - Most C++ libraries not packaged for NuGet
- ❌ **Packaging effort** - Requires creating .nuspec with runtime assets
- ❌ **Private NuGet** - May need private NuGet feed if library not public

**Status**: Not available for plate-tectonics. Consider for future if we publish our own native libraries.

---

### Recommendation for VS_019 (WorldEngine Integration MVP)

**Use Option 2: Pre-Compiled Binaries from Releases**

**Rationale**:
- ✅ **Fastest path to MVP** - No CI pipeline complexity, developers download once
- ✅ **plate-tectonics has official releases** - Binaries already built by maintainers
- ✅ **Low risk** - SHA256 verification prevents tampering, MIT license allows redistribution
- ⏸️ **Defer Option 1** - Only implement CI builds if we need to customize C++ source

**Action Items**:
1. Create `NATIVE_LIBRARIES.md` with download instructions + checksums
2. Add `scripts/download-native-libs.sh` (automated download + verification)
3. Document in onboarding guide (README.md)
4. Add architecture test: Native binaries present before integration tests run

---

## DI Registration

```csharp
// Infrastructure/DependencyInjection/ServiceRegistration.cs
public static class WorldGenServiceRegistration
{
    public static IServiceCollection AddWorldGenServices(this IServiceCollection services)
    {
        // Singleton: Library loader (load once, reuse)
        services.AddSingleton<NativeLibraryLoader>();

        // Scoped: Simulator (safe for concurrent use)
        services.AddScoped<IPlateSimulator, NativePlateSimulator>();

        // Validate library at startup (fail-fast)
        services.AddHostedService<NativeLibraryValidationService>();

        return services;
    }
}

// Startup validation (fail-fast pattern)
public class NativeLibraryValidationService : IHostedService
{
    private readonly NativeLibraryLoader _loader;
    private readonly ILogger<NativeLibraryValidationService> _logger;

    public NativeLibraryValidationService(
        NativeLibraryLoader loader,
        ILogger<NativeLibraryValidationService> logger)
    {
        _loader = loader;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return _loader.EnsureLibraryLoaded()
            .Tap(() => _logger.LogInformation("Native library validation passed"))
            .TapError(err =>
            {
                _logger.LogCritical("Native library validation FAILED: {Error}", err);
                throw new InvalidOperationException("Native library load failed - see logs");
            })
            .Match(
                onSuccess: () => Task.CompletedTask,
                onFailure: _ => Task.FromException(new InvalidOperationException("Startup failed")));
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

---

## Testing Strategy

### Unit Tests (Mock Interface)

```csharp
// Tests/WorldGen/Application/GenerateWorldCommandHandlerTests.cs
public class GenerateWorldCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidParameters_ReturnsWorldData()
    {
        // Arrange
        var mockSimulator = Substitute.For<IPlateSimulator>();
        mockSimulator.Generate(Arg.Any<PlateSimulationParams>())
            .Returns(Result.Success(new PlateSimulationResult(
                new float[10, 10],
                new ushort[10, 10])));

        var handler = new GenerateWorldCommandHandler(mockSimulator);

        // Act
        var result = await handler.Handle(new GenerateWorldCommand(42, 10, 10));

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
```

### Integration Tests (Real Native Library)

```csharp
// Tests/WorldGen/Infrastructure/NativePlateSimulatorIntegrationTests.cs
[Trait("Category", "Integration")]
[Trait("Category", "Native")]
public class NativePlateSimulatorIntegrationTests
{
    [Fact]
    public void Generate_ValidParameters_ReturnsHeightmap()
    {
        // Arrange
        var logger = Substitute.For<ILogger<NativePlateSimulator>>();
        var loader = new NativeLibraryLoader(Substitute.For<ILogger<NativeLibraryLoader>>());
        var simulator = new NativePlateSimulator(logger, loader);

        var parameters = new PlateSimulationParams(
            Seed: 42,
            Width: 64,
            Height: 64
        );

        // Act
        var result = simulator.Generate(parameters);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Heightmap.GetLength(0).Should().Be(64);
        result.Value.Heightmap.GetLength(1).Should().Be(64);
    }

    [Fact]
    public void Generate_LibraryMissing_ReturnsFailure()
    {
        // Simulate missing library (rename/delete binary before test)
        // This test validates error handling
    }
}
```

---

## Alignment with Existing ADRs

### ADR-002: Godot Integration Architecture

✅ **Native code isolated to Infrastructure layer**
- Core stays pure C# (zero Godot, zero native dependencies)
- Presentation uses native features via Application interfaces
- ServiceLocator pattern acceptable for native library resolution (Godot constraint)

### ADR-003: Functional Error Handling

✅ **Infrastructure errors → Result<T>**
- Library load failures = Infrastructure errors (not programmer errors)
- Use `Result.Of()` for simple wrappers
- Use try-catch for fine-grained control (platform detection, helpful errors)
- Error keys follow i18n convention (`ERROR_NATIVE_*`)

### ADR-004: Feature-Based Clean Architecture

✅ **Feature-based organization**
- Native code in `Features/{Feature}/Infrastructure/Native/`
- Interop subfolder for unsafe code isolation
- Events/Commands remain pure C# (no native types in signatures)

### ADR-005: Internationalization Architecture

✅ **Error keys for translation**
- `ERROR_NATIVE_LIBRARY_MISSING` → translatable
- `ERROR_NATIVE_CREATE_FAILED` → translatable
- Help messages in English (developer-facing, logged not shown to user)

### ADR-006: Data-Driven Entity Design

✅ **Templates don't reference native code**
- Templates use Application interfaces (IPlateSimulator)
- Native implementation swappable (NativePlateSimulator ↔ ManagedPlateSimulator)
- Hot-reload works (templates don't depend on native library lifecycle)

---

## Consequences

### Positive

✅ **Core Purity** - Application layer has zero native dependencies (testable)
✅ **Cross-Platform** - Platform detection automatic, binaries organized by platform
✅ **Memory Safety** - SafeHandle pattern prevents leaks, RAII guarantees cleanup
✅ **Error Clarity** - Helpful troubleshooting messages with platform info
✅ **Maintainability** - Unsafe code isolated to Interop/ folder
✅ **Fail-Fast** - Startup validation catches missing libraries before first use
✅ **Flexibility** - Interface abstraction allows pure C# fallback

### Negative

❌ **PInvoke Complexity** - Marshaling, unsafe code, platform differences
❌ **Deployment Overhead** - Must distribute native binaries for each platform
❌ **Build Complexity** - May need to compile native libraries (CMake, etc.)
❌ **Debugging Difficulty** - Native crashes harder to diagnose than managed exceptions
❌ **Testing Challenges** - Integration tests require native binaries

### Neutral

➖ **Boilerplate** - Three layers (Interop, Wrapper, Interface) adds files
➖ **Performance Overhead** - Marshaling costs (usually negligible vs native call cost)

---

## Alternatives Considered

### 1. Pure C# Port (No Native Library)

**Rejected**: Porting complex algorithms (plate tectonics, physics) from C++ → C# is:
- ❌ High-risk (subtle algorithm bugs)
- ❌ Time-consuming (weeks vs hours for PInvoke)
- ❌ Performance loss (C++ optimizations lost)

**When to use**: Simple algorithms where C# performance is sufficient.

---

### 2. Embedded Mono/NativeAOT (Reverse Interop)

**Rejected**: Calling C# from C++ (opposite direction) is:
- ❌ More complex (hosting .NET runtime in native code)
- ❌ Unnecessary (we control the application entry point)

**When to use**: Plugin systems where native code is the host.

---

### 3. COM Interop / C++/CLI

**Rejected**: COM and C++/CLI are:
- ❌ Windows-only (violates cross-platform requirement)
- ❌ More complex than PInvoke for simple function calls

**When to use**: Legacy Windows COM components.

---

### 4. Process Boundary (Separate Executable)

**Rejected**: Launching native executable and communicating via IPC is:
- ❌ Higher overhead (process startup, serialization)
- ❌ More complex (process management, error handling)
- ❌ Worse performance (IPC vs in-process function call)

**When to use**: Isolation required (sandboxing, crash recovery).

---

## Success Metrics

✅ **Zero native dependencies in Core project** (compile-time enforcement)
✅ **All native calls return Result<T>** (no exceptions propagate to Application layer)
✅ **Startup validation fails fast** (library missing → helpful error within 1 second)
✅ **Cross-platform builds pass CI** (Windows/Linux/macOS)
✅ **Integration tests cover native failure modes** (library missing, invalid parameters)
✅ **No memory leaks** (Valgrind/AddressSanitizer clean on Linux)
✅ **Documentation exists** (NATIVE_LIBRARIES.md with download links)

---

## Future Enhancements

### Graceful Degradation for Non-Critical Features

**Status**: Deferred until 2+ native libraries in use

**Context**: Current fail-fast strategy crashes game if ANY native library fails to load. For critical systems (e.g., audio engine), this is correct. For optional features (e.g., world generation), graceful degradation may be preferable.

**Proposed Enhancement**:

```csharp
// Fatality level enum
public enum LibraryFatality
{
    Critical,   // Missing library = crash game (e.g., audio, core systems)
    Optional    // Missing library = disable feature, warn user (e.g., world generation)
}

// Enhanced startup validation
public class NativeLibraryValidationService : IHostedService
{
    private readonly NativeLibraryLoader _loader;
    private readonly LibraryFatality _fatality;
    private readonly IFeatureToggle _featureToggle;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return _loader.EnsureLibraryLoaded()
            .Match(
                onSuccess: () => Task.CompletedTask,
                onFailure: err =>
                {
                    if (_fatality == LibraryFatality.Critical)
                    {
                        _logger.LogCritical("CRITICAL library failed: {Error}", err);
                        throw new InvalidOperationException("Startup failed");
                    }
                    else
                    {
                        _logger.LogWarning("Optional library failed: {Error}", err);
                        _featureToggle.DisableFeature("WorldGen");  // Graceful degradation
                        ShowUserWarning("ERROR_WORLDGEN_UNAVAILABLE");  // i18n key
                        return Task.CompletedTask;
                    }
                });
    }
}
```

**Use Case**: WorldGen library fails → Disable "New Game" button, show error message, but allow "Load Game", "Settings", etc.

**Deferred Rationale**:
- ⏸️ **Premature optimization** - Currently only one native library (plate-tectonics for VS_019)
- ⏸️ **Adds complexity** - Feature toggle system, UI integration, user messaging
- ⏸️ **Unclear UX** - How to communicate "WorldGen unavailable" to player?

**Revisit When**:
- We have 2+ native libraries with different criticality levels
- User feedback indicates frustration with full crashes for optional features
- Feature toggle system exists (may be built for other reasons)

**Decision**: Keep fail-fast for VS_019. Document this enhancement for future consideration.

---

## References

- [.NET P/Invoke Documentation](https://learn.microsoft.com/en-us/dotnet/standard/native-interop/pinvoke) - Microsoft
- [SafeHandle Design](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.safehandle) - Microsoft
- [NativeLibrary API](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.nativelibrary) - Microsoft
- [ADR-002: Godot Integration Architecture](./ADR-002-godot-integration-architecture.md)
- [ADR-003: Functional Error Handling](./ADR-003-functional-error-handling.md)
- [ADR-004: Feature-Based Clean Architecture](./ADR-004-feature-based-clean-architecture.md)

---

## Decision Log

**2025-10-06 (v1.1)**: Architecture review refinements based on senior architect feedback:
- **Modernized marshaling**: Added `Span<T>` + `MemoryMarshal` pattern (generic `Marshal2DArray<T>` helper)
- **Godot path resolution**: Updated `NativeLibraryLoader` to use `ProjectSettings.GlobalizePath()` (fixes editor vs exported game issue)
- **Build strategy documentation**: Added 3-option comparison (Git submodule CI, pre-compiled binaries, NuGet) with VS_019 recommendation
- **Future enhancement**: Documented graceful degradation pattern (deferred until 2+ libraries)

**2025-10-06 (v1.0)**: Initial proposal after VS_019 (WorldEngine Integration MVP) analysis. Three-layer isolation pattern chosen to balance Clean Architecture purity with pragmatic PInvoke integration. SafeHandle + Result<T> + NativeLibraryLoader scaffold provides production-ready error handling and memory safety.

**Next Steps**:
1. ~~Review with Product Owner~~ ✅ Architecture review complete (v1.1 approved)
2. Prototype NativePlateSimulator for VS_019 (validate pattern works)
3. Update CLAUDE.md with native library integration guidelines
4. Add architecture tests to enforce Core purity (zero native refs)
5. Create NATIVE_LIBRARIES.md for plate-tectonics (download + checksums)

---

**Remember**: This ADR is feature-agnostic - applicable to ANY native library (audio, physics, pathfinding). The pattern scales from single-library features (WorldGen) to multi-library systems (multiple native dependencies per feature).
