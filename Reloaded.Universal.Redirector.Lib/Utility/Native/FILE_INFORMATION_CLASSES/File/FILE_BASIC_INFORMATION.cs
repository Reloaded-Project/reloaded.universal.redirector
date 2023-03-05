using System.Runtime.InteropServices;
// ReSharper disable InconsistentNaming
// ReSharper disable CheckNamespace
#pragma warning disable CS1591

namespace Reloaded.Universal.Redirector.Lib.Utility.Native;

[StructLayout(LayoutKind.Sequential)]
public struct FILE_BASIC_INFORMATION
{
    public long CreationTime;
    public long LastAccessTime;
    public long LastWriteTime;
    public long ChangeTime;
    public FileAttributes FileAttributes;
}