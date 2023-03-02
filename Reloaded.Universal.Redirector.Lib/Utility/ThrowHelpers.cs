using System.ComponentModel;

namespace Reloaded.Universal.Redirector.Lib.Utility;

/// <summary>
/// Classes for throwing exceptions throughout the code base.
/// </summary>
public class ThrowHelpers
{
    /// <summary>
    /// Throws a Win32 exception.
    /// </summary>
    public static void Win32Exception(int queryStatus) => throw new Win32Exception(queryStatus);
}