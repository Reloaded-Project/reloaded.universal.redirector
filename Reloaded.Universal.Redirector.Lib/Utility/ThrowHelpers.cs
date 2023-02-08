using System.Runtime.CompilerServices;

namespace Reloaded.Universal.Redirector.Lib.Utility;

/// <summary>
/// Methods for throwing exceptions without breaking inlining.
/// </summary>
internal class ThrowHelpers
{
    /// <summary/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowNotImplementedException(string message) => throw new NotImplementedException(message);
}