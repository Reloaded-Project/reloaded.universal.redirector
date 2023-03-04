using System.Runtime.InteropServices;
// ReSharper disable InconsistentNaming
#pragma warning disable CS1591

namespace Reloaded.Universal.Redirector.Lib.Utility.Native;

public partial class Native
{
    /// <summary>
    /// A driver sets an IRP's I/O status block to indicate the final status of an I/O request, before calling IoCompleteRequest for the IRP.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct IO_STATUS_BLOCK
    {
        public UInt32 status;
        public IntPtr information;
    }
}