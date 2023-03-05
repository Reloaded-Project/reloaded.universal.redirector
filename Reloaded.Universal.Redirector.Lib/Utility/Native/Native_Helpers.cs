using System.Runtime.CompilerServices;
using FileEmulationFramework.Lib.IO;

namespace Reloaded.Universal.Redirector.Lib.Utility.Native;

public partial class Native
{
    /// <summary>
    /// Helper class for calling <see cref="NtQueryInformationFile"/>.
    /// </summary>
    /// <param name="handle">Handle for which to get all information.</param>
    /// <returns>
    ///     The information.
    /// </returns>
    /// <remarks>
    ///     Uses a static buffer.
    ///     Please ensure you are done using the last result before calling again,
    ///     and do not use in multithreaded scenarios.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe FILE_ALL_INFORMATION* NtQueryInformationFileHelper(nint handle)
    {
        var ioStatus = new IO_STATUS_BLOCK();
        var buf = Threading.Get64KBuffer();
        var status = NtQueryInformationFile(handle, ref ioStatus, buf, Threading.Buffer64KLength, Native.FILE_INFORMATION_CLASS.FileAllInformation);
            
        if (status != 0)
            ThrowHelpers.Win32Exception(status);

        return (FILE_ALL_INFORMATION*)buf;
    }
    
}