using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
// ReSharper disable InconsistentNaming
// ReSharper disable CheckNamespace

namespace Reloaded.Universal.Redirector.Lib.Utility.Native;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct FILE_NAME_INFORMATION
{
    internal uint FileNameLength;
    // Inlined file name here right after field.

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe int GetSize(FILE_NAME_INFORMATION* thisPtr)
    {
        return (int)(thisPtr->FileNameLength * sizeof(char)) + sizeof(FILE_NAME_INFORMATION);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe string GetName(FILE_NAME_INFORMATION* thisPtr)
    {
        return new string((char*)(thisPtr + 1), 0, (int)thisPtr->FileNameLength / sizeof(char));
    }
}