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
    public unsafe struct FILE_ID_BOTH_DIR_INFORMATION : IFileDirectoryInformationDerivative
    {
        public uint NextEntryOffset;
        public uint FileIndex;
        public long CreationTime;
        public long LastAccessTime;
        public long LastWriteTime;
        public long ChangeTime;
        public long EndOfFile;
        public long AllocationSize;
        public FileAttributes FileAttributes;
        public uint FileNameLength;
        public uint EaSize;
        public byte ShortNameLength;
        public fixed char ShortName[12];
        public long FileId;

        /// <inheritdoc />
        public int GetNextEntryOffset() => (int)NextEntryOffset;
        
        /// <inheritdoc />
        public void SetNextEntryOffset(int offset) => NextEntryOffset = (uint)offset;

        /// <inheritdoc />
        public FileAttributes GetFileAttributes() => FileAttributes;

        /// <inheritdoc />
        public ReadOnlySpan<char> GetFileName(void* thisPtr)
        {
            var casted = (FILE_ID_BOTH_DIR_INFORMATION*)thisPtr;
            return new ReadOnlySpan<char>((casted + 1), (int)FileNameLength / 2);
        }
        
        /// <inheritdoc />
        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryPopulate(void* thisPtr, int length, nint handle)
        {
            var thisItem = (FILE_ID_BOTH_DIR_INFORMATION*)thisPtr;
            var result = NtQueryInformationFileHelper(handle);
            var fileName = FILE_NAME_INFORMATION.GetNameTrimmed(&result->NameInformation);

            if (!SufficientSize<FILE_ID_BOTH_DIR_INFORMATION>(length, fileName.Length))
                return false;

            thisItem->CreationTime = result->BasicInformation.CreationTime;
            thisItem->LastAccessTime = result->BasicInformation.LastAccessTime;
            thisItem->LastWriteTime = result->BasicInformation.LastWriteTime;
            thisItem->ChangeTime = result->BasicInformation.ChangeTime;
            thisItem->EndOfFile = result->StandardInformation.EndOfFile;
            thisItem->AllocationSize = result->StandardInformation.AllocationSize;
            thisItem->FileAttributes = result->BasicInformation.FileAttributes;
            thisItem->FileNameLength = (uint)fileName.Length * sizeof(char);
            thisItem->EaSize = result->EaInformation.EaSize;
            thisItem->FileId = result->InternalInformation.FileId;
            thisItem->NextEntryOffset = (uint)(sizeof(FILE_ID_BOTH_DIR_INFORMATION) + thisItem->FileNameLength);

            // TODO: Short names are not supported [for performance reasons]
            // Unknowns
            thisItem->ShortNameLength = 0;
            thisItem->FileIndex = 0;
            
            CopyString(fileName, thisItem, thisItem->FileNameLength);
            return true;
        }
    }
}