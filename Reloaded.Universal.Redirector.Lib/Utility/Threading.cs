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
    /// A static pinned buffer for temporary use in P-Invokes in non-multithreaded scenarios.
    /// </summary>
    public static byte[] Buffer64K = GC.AllocateUninitializedArray<byte>(Buffer64KLength, true);
}