using System.Runtime.InteropServices;
using static Reloaded.Universal.Redirector.Lib.Utility.Native.Native;

// ReSharper disable InconsistentNaming
// ReSharper disable CheckNamespace
#pragma warning disable CS1591

namespace Reloaded.Universal.Redirector.Lib.Utility.Native;

[StructLayout(LayoutKind.Sequential)]
public struct FILE_ACCESS_INFORMATION
{
    public ACCESS_MASK AccessFlags;
}