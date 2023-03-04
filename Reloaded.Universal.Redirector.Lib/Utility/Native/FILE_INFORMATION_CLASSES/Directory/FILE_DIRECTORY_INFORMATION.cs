using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
// ReSharper disable InconsistentNaming
#pragma warning disable CS1591

// ReSharper disable once CheckNamespace
namespace Reloaded.Universal.Redirector.Lib.Utility.Native;

public partial class Native
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct FILE_DIRECTORY_INFORMATION : IFileDirectoryInformationDerivative
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

        /// <inheritdoc />
        public int GetNextEntryOffset() => (int)NextEntryOffset;
        
        /// <inheritdoc />
        public FileAttributes GetFileAttributes() => FileAttributes;

        /// <inheritdoc />
        public string GetFileName(void* thisPtr)
        {
            var casted = (FILE_DIRECTORY_INFORMATION*)thisPtr;
            return Marshal.PtrToStringUni((nint)(casted + 1), (int)FileNameLength / 2);
        }
        
        /// <inheritdoc />
        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPopulate(void* thisPtr, nint handle)
        {
            throw new NotImplementedException();
        }
    }
}