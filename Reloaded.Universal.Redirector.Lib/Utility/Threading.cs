using System.Runtime.InteropServices;
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
    /// A static pinned buffer for temporary use in NtQueryInformationFile P-Invokes in non-multithreaded scenarios.
    /// Careful to not use this with other cases or for when NtQueryInformationFile is hooked from our code..
    /// </summary>
    public static readonly unsafe byte* NtQueryInformationFile64K = (byte*)NativeMemory.Alloc(Buffer64KLength);

    /// <summary>
    /// Rents an array from the pool.
    /// </summary>
    /// <param name="size">Size of array to rent.</param>
    public static ArrayRental<byte> GetArrayRental(int size) => new ArrayRental<byte>(size);
}