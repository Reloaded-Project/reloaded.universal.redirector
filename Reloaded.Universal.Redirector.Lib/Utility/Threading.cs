using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance;
using Reloaded.Universal.Redirector.Lib.Utility.Native.Structures;
using static Reloaded.Universal.Redirector.Lib.Utility.Native.Native;

namespace Reloaded.Universal.Redirector.Lib.Utility;

/// <summary>
/// Thread related utilities.
/// </summary>
public class Threading
{
    /// <summary>
    /// Length of 64K buffer.
    /// </summary>
    public const int Buffer64KLength = 65536;

    [ThreadStatic]
    private static byte[]? _buffer64K;
    
    [ThreadStatic]
    private static byte[]? _buffer64K_2;

    [ThreadStatic]
    private static UNICODE_STRING[]? _staticString;
    
    /// <summary>
    /// Gets a static pinned buffer for temporary use in APIs e.g. NtQueryInformationFile.
    /// Each thread has its own buffer. 
    /// </summary>
    /// <remarks>
    ///     Returns single buffer only, ensure you're done/happy with previous result first.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe byte* Get64KBuffer()
    {
        if (_buffer64K == null)
            _buffer64K = GC.AllocateUninitializedArray<byte>(Buffer64KLength, true);

        // on Pinned Object Heap and thus would never be moved.
        return (byte*)Unsafe.AsPointer(ref _buffer64K.DangerousGetReferenceAt(0));
    }
    
    /// <summary>
    /// Gets a static pinned buffer for temporary use in APIs e.g. NtQueryInformationFile.
    /// Each thread has its own buffer. This returns 2nd buffer which can be concurrently used with the first.
    /// </summary>
    /// <remarks>
    ///     Returns single buffer only, ensure you're done/happy with previous result first.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe byte* Get64KBuffer_2()
    {
        if (_buffer64K_2 == null)
            _buffer64K_2 = GC.AllocateUninitializedArray<byte>(Buffer64KLength, true);

        // on Pinned Object Heap and thus would never be moved.
        return (byte*)Unsafe.AsPointer(ref _buffer64K_2.DangerousGetReferenceAt(0));
    }
    
    /// <summary>
    /// Gets a static pinned buffer for temporary use in APIs e.g. NtQueryInformationFile.
    /// Each thread has its own buffer. 
    /// </summary>
    /// <remarks>
    ///     Returns single buffer only, ensure you're done/happy with previous result first.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe byte* GetUnicodeString()
    {
        if (_staticString == null)
            _staticString = GC.AllocateUninitializedArray<UNICODE_STRING>(1, true);

        // on Pinned Object Heap and thus would never be moved.
        return (byte*)Unsafe.AsPointer(ref _staticString.DangerousGetReferenceAt(0));
    }
    
    /// <summary>
    /// Rents an array from the pool.
    /// </summary>
    /// <param name="size">Size of array to rent.</param>
    public static ArrayRental<byte> GetArrayRental(int size) => new ArrayRental<byte>(size);
}