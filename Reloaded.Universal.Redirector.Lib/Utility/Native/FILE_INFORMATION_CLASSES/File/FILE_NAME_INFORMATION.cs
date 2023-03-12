using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
// ReSharper disable InconsistentNaming
// ReSharper disable CheckNamespace
#pragma warning disable CS1591

namespace Reloaded.Universal.Redirector.Lib.Utility.Native;

[StructLayout(LayoutKind.Sequential)]
public struct FILE_NAME_INFORMATION
{
    internal uint FileNameLength;
    // Inlined file name here right after field.

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe string GetName(FILE_NAME_INFORMATION* thisPtr)
    {
        return new string((char*)(thisPtr + 1), 0, (int)thisPtr->FileNameLength / sizeof(char));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe ReadOnlySpan<char> GetNameTrimmed(FILE_NAME_INFORMATION* thisPtr)
    {
        var nameSpan = new ReadOnlySpan<char>((char*)(thisPtr + 1), (int)thisPtr->FileNameLength / sizeof(char));
        nameSpan = Path.GetFileName(nameSpan);
        return nameSpan;
    }
}