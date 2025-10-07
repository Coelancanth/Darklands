using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Darklands.Core.Features.WorldGen.Infrastructure.Native.Interop;

/// <summary>
/// SafeHandle wrapper for plate tectonics simulation pointer.
/// LAYER 1 (Interop): Ensures RAII cleanup of native resources.
/// See ADR-007: Native Library Integration Architecture
/// </summary>
internal sealed class PlateSimulationHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    /// <summary>
    /// Constructs handle that owns the native resource.
    /// </summary>
    public PlateSimulationHandle() : base(ownsHandle: true)
    {
    }

    /// <summary>
    /// Constructs handle wrapping existing pointer.
    /// Used by PInvoke marshaler.
    /// </summary>
    public PlateSimulationHandle(IntPtr handle) : base(ownsHandle: true)
    {
        SetHandle(handle);
    }

    /// <summary>
    /// Releases native simulation handle via platec_api_destroy.
    /// Called automatically by GC or Dispose().
    /// </summary>
    /// <returns>True if cleanup succeeded</returns>
    protected override bool ReleaseHandle()
    {
        if (!IsInvalid)
        {
            PlateTectonicsNative.Destroy(handle);
        }
        return true;
    }
}
