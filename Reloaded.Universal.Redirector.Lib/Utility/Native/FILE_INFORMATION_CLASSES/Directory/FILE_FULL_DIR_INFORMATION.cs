using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
// ReSharper disable InconsistentNaming
#pragma warning disable CS1591

// ReSharper disable once CheckNamespace
namespace Reloaded.Universal.Redirector.Lib.Utility.Native;

public partial class Native
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct FILE_FULL_DIR_INFORMATION : IFileDirectoryInformationDerivative
    {
        public uint NextEntryOffset;
        public uint FileIndex;
        public ulong CreationTime;
        public ulong LastAccessTime;
        public ulong LastWriteTime;
        public ulong ChangeTime;
        public ulong EndOfFile;
        public ulong AllocationSize;
        public FileAttributes FileAttributes;
        public uint FileNameLength;
        public uint EaSize;

        /// <inheritdoc />
        public int GetNextEntryOffset() => (int)NextEntryOffset;
        
        /// <inheritdoc />
        public FileAttributes GetFileAttributes() => FileAttributes;

        /// <inheritdoc />
        public string GetFileName(void* thisPtr)
        {
            var casted = (FILE_FULL_DIR_INFORMATION*)thisPtr;
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