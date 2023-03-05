using System.Runtime.InteropServices;

// ReSharper disable InconsistentNaming
// ReSharper disable CheckNamespace
#pragma warning disable CS1591

namespace Reloaded.Universal.Redirector.Lib.Utility.Native;

[StructLayout(LayoutKind.Sequential)]
public struct FILE_ALIGNMENT_INFORMATION
{
    public uint AlignmentRequirement;
}