using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance;
using Reloaded.Universal.Redirector.Lib.Utility.Native.Structures;

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

    /// <summary>
    /// 
    /// 
    /// </summary>
    [ThreadStatic]
    private static byte[]? _ntQueryInformationFile64K;

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
        if (_ntQueryInformationFile64K == null)
            _ntQueryInformationFile64K = GC.AllocateUninitializedArray<byte>(Buffer64KLength, true);

        // _ntQueryInformationFile64K is on Pinned Object Heap and thus would never be moved.
        return (byte*)Unsafe.AsPointer(ref _ntQueryInformationFile64K.DangerousGetReferenceAt(0));
    }
    
    /// <summary>
    /// Rents an array from the pool.
    /// </summary>
    /// <param name="size">Size of array to rent.</param>
    public static ArrayRental<byte> GetArrayRental(int size) => new ArrayRental<byte>(size);
}