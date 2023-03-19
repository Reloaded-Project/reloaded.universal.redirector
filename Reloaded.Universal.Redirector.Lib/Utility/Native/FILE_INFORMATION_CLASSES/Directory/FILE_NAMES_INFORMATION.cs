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
        
        // First letter of file name
        public char FileName;
        
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
            return new ReadOnlySpan<char>(&casted->FileName, (int)FileNameLength / 2);
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
            var fileName = FILE_NAME_INFORMATION.GetNameTrimmed(result);
            
            if (!SufficientSize<FILE_NAMES_INFORMATION>(length, fileName.Length))
                return false;
            
            thisItem->FileIndex = 0;
            thisItem->FileNameLength  = (uint)fileName.Length * sizeof(char);
            thisItem->NextEntryOffset = (uint)(sizeof(FILE_NAMES_INFORMATION) + thisItem->FileNameLength);
            CopyString(fileName, &thisItem->FileName, thisItem->FileNameLength);
            return true;
        }
    }
}