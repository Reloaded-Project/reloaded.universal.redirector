using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
// ReSharper disable InconsistentNaming
#pragma warning disable CS1591

// ReSharper disable once CheckNamespace
namespace Reloaded.Universal.Redirector.Lib.Utility.Native;

public partial class Native
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct FILE_NAMES_INFORMATION : IFileDirectoryInformationDerivative
    {
        public uint NextEntryOffset;
        public uint FileIndex;
        public uint FileNameLength;

        /// <inheritdoc />
        public int GetNextEntryOffset() => (int)NextEntryOffset;
        
        /// <inheritdoc />
        public FileAttributes GetFileAttributes() => FileAttributes.Normal;

        /// <inheritdoc />
        public string GetFileName(void* thisPtr)
        {
            var casted = (FILE_NAMES_INFORMATION*)thisPtr;
            return Marshal.PtrToStringUni((nint)casted + 1, (int)FileNameLength / 2);
        }
        
        /// <inheritdoc />
        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPopulate(void* thisPtr, nint handle)
        {
            var thisItem = (FILE_NAMES_INFORMATION*)thisPtr;
            var ioStatus = new IO_STATUS_BLOCK();
            var buf      = Threading.NtQueryInformationFile64K;
            var status   = NtQueryInformationFile(handle, ref ioStatus, buf, Threading.Buffer64KLength, FILE_INFORMATION_CLASS.FileNameInformation);
            if (status != 0)
                ThrowHelpers.Win32Exception(status);

            var result = (FILE_NAME_INFORMATION*)buf;
            var size   = FILE_NAME_INFORMATION.GetSize(result);

            // 8 is amount this struct is bigger by
            if (size > sizeof(FILE_NAMES_INFORMATION) - sizeof(FILE_NAME_INFORMATION))
            {
                
            }
            
            var fileNamePtr = &thisItem->FileName;
            return true;
        }

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SufficientSize(int stringLength)
        {
            
        }
    }
}