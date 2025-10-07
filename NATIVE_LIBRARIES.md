# Native Libraries

This document tracks native libraries used by Darklands for features requiring C/C++ integration.

## Overview

Native libraries are stored in `addons/darklands/bin/{platform}/` and loaded at runtime via PInvoke (see [ADR-007: Native Library Integration](Docs/03-Reference/ADR/ADR-007-native-library-integration.md)).

## plate-tectonics (World Generation)

**Purpose**: Physics-based plate tectonics simulation for strategic world map generation

**Source**: https://github.com/Mindwerks/plate-tectonics
**License**: LGPL v2.1
**Version**: Custom build from source (modified for DLL output)
**Used By**: VS_019 WorldEngine Integration MVP

### Binary Checksums

| Platform | File | SHA256 | Date Built |
|----------|------|--------|------------|
| Windows x64 | `PlateTectonics.dll` | `2df0f5db5b37b1db6e1aad73209064619ab0e7d8e9159cb063119aef343c4c0e` | 2025-10-06 |

### Build Instructions (Windows)

**Prerequisites**:
- CMake 3.10+ (verify: `cmake --version`)
- Visual Studio 2022 with C++ Desktop Development workload
- Git (to clone if needed)

**Steps**:

```bash
# 1. Navigate to source (already included in References/)
cd References/plate-tectonics

# 2. Configure with CMake (generates DLL instead of static lib)
cmake .

# 3. Build Release configuration
cmake --build . --config Release

# 4. Copy DLL to addons directory
cp Release/PlateTectonics.dll ../../addons/darklands/bin/win-x64/

# 5. Copy MSVC runtime dependencies (required for DLL loading)
cp /c/Windows/System32/vcruntime140.dll ../../addons/darklands/bin/win-x64/
cp /c/Windows/System32/msvcp140.dll ../../addons/darklands/bin/win-x64/

# 6. Verify checksum (should match table above)
sha256sum ../../addons/darklands/bin/win-x64/PlateTectonics.dll
```

**Note**: The CMakeLists.txt has been modified to build `SHARED` library (line 8). If you regenerate from upstream, you must re-apply this change.

**Runtime Dependencies**: PlateTectonics.dll requires MSVC runtime (vcruntime140.dll, msvcp140.dll). These must be placed in the same directory as the DLL for .NET LibraryImport to find them.

### Build Instructions (Linux) - Future

```bash
# TODO: Test on Linux once we have cross-platform CI
cd References/plate-tectonics
cmake . -G "Unix Makefiles"
make
cp libPlateTectonics.so ../../addons/darklands/bin/linux-x64/
```

### Build Instructions (macOS) - Future

```bash
# TODO: Test on macOS
cd References/plate-tectonics
cmake .
make
cp libPlateTectonics.dylib ../../addons/darklands/bin/macos-x64/
```

### C API Reference

The library exposes a C API via `platecapi.hpp`:

```c
// Create simulation
void* platec_api_create(long seed, uint32_t width, uint32_t height,
                        float sea_level, uint32_t erosion_period,
                        float folding_ratio, uint32_t aggr_overlap_abs,
                        float aggr_overlap_rel, uint32_t cycle_count,
                        uint32_t num_plates);

// Run simulation step
void platec_api_step(void* handle);

// Check if finished
uint32_t platec_api_is_finished(void* handle);

// Get results
float* platec_api_get_heightmap(void* handle);
uint32_t* platec_api_get_platesmap(void* handle);

// Get dimensions
uint32_t lithosphere_getMapWidth(void* handle);
uint32_t lithosphere_getMapHeight(void* handle);

// Cleanup
void platec_api_destroy(void* handle);
```

**See**: `src/Features/WorldGen/Infrastructure/Native/Interop/PlateTectonicsNative.cs` for C# PInvoke declarations.

## Adding New Native Libraries

When adding a new native library:

1. **Source**: Add source to `References/{library-name}/` if building from source
2. **Binary**: Place compiled library in `addons/darklands/bin/{platform}/`
3. **Document**: Add section to this file with checksums and build instructions
4. **Interop**: Create C# wrapper following ADR-007 three-layer pattern
5. **Verify**: Add integration test that library loads successfully

## Troubleshooting

### "DllNotFoundException: Unable to load DLL 'PlateTectonics'"

**Solution**: Verify DLL exists at `addons/darklands/bin/win-x64/PlateTectonics.dll` and checksum matches.

### "BadImageFormatException: An attempt was made to load a program with incorrect format"

**Solution**: Architecture mismatch. Verify:
- DLL is x64: `file addons/darklands/bin/win-x64/PlateTectonics.dll` shows "x86-64"
- Godot is x64 (not x86)

### Build fails with "cmake: command not found"

**Solution**: Install CMake from https://cmake.org/download/ or via package manager.

### Build fails with "MSBuild version ... not found"

**Solution**: Install Visual Studio 2022 with "Desktop development with C++" workload.
