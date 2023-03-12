using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Reloaded.Universal.Redirector.Lib.Utility.Native.FileDirectoryInformationDerivativeExtensions;

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
        public void SetNextEntryOffset(int offset) => NextEntryOffset = (uint)offset;

        /// <inheritdoc />
        public FileAttributes GetFileAttributes() => FileAttributes.Normal;

        /// <inheritdoc />
        public ReadOnlySpan<char> GetFileName(void* thisPtr)
        {
            var casted = (FILE_NAMES_INFORMATION*)thisPtr;
            return new ReadOnlySpan<char>((casted + 1), (int)FileNameLength / 2);
        }
        
        /// <inheritdoc />
        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryPopulate(void* thisPtr, int length, nint handle)
        {
            var thisItem = (FILE_NAMES_INFORMATION*)thisPtr;
            var ioStatus = new IO_STATUS_BLOCK();
            
            var buf      = Threading.Get64KBuffer();
            var status   = NtQueryInformationFile(handle, ref ioStatus, buf, Threading.Buffer64KLength, FILE_INFORMATION_CLASS.FileNameInformation);
            if (status != 0)
                ThrowHelpers.Win32Exception(status);

            // Verify available size.
            var result = (FILE_NAME_INFORMATION*)buf;
            if (!SufficientSize<FILE_NAMES_INFORMATION>(length, result->FileNameLength))
                return false;

            thisItem->NextEntryOffset = (uint)(sizeof(FILE_NAMES_INFORMATION) + result->FileNameLength * sizeof(char));
            thisItem->FileNameLength  = result->FileNameLength;
            CopyString((char*)(result + 1), thisItem, thisItem->FileNameLength);
            return true;
        }
    }
}