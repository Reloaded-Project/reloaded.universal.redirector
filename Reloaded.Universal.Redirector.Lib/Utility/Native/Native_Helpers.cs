using System.ComponentModel;
using System.Runtime.CompilerServices;
using static Reloaded.Universal.Redirector.Lib.Utility.Native.Native;

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
        var status = NtQueryInformationFile(handle, ref ioStatus, buf, Threading.Buffer64KLength, FILE_INFORMATION_CLASS.FileAllInformation);

        if (status == 0) 
            return (FILE_ALL_INFORMATION*)buf;
        
        ThrowHelpers.Win32Exception(status);
        return (FILE_ALL_INFORMATION*)buf;
    }
    
    /// <summary>
    /// Utility method for opening files with <see cref="NtOpenFile"/>.
    /// </summary>
    /// <param name="filePath">Path of the file to open, including device prefix.</param>
    /// <returns></returns>
    /// <exception cref="Win32Exception">Failed to open file.</exception>
    public static unsafe IntPtr NtOpenFileOpen(string filePath)
    {
        fixed (char* fileName = filePath)
        {
            var ntOpenWrapper = new NtOpenWrapper(fileName, filePath.Length);
            var status = NtOpenFile(&ntOpenWrapper.Handle, ACCESS_MASK.FILE_GENERIC_READ, &ntOpenWrapper.AttributesWrapper.Attributes, 
                &ntOpenWrapper.StatusBlock, FileShare.ReadWrite, CreateOptions.SynchronousIoAlert);

            if (status == 0) 
                return ntOpenWrapper.Handle;
            
            NtClose(ntOpenWrapper.Handle);
            ThrowHelpers.Win32Exception(status);
            return ntOpenWrapper.Handle;
        }
    }
    
    internal struct ObjectAttributesWrapper
    {
        public OBJECT_ATTRIBUTES Attributes;

        public unsafe ObjectAttributesWrapper(char* filePath, int numCharacters)
        {
            var alloc = (UNICODE_STRING*)Threading.GetUnicodeString();
            Attributes = new OBJECT_ATTRIBUTES();
            Attributes.ObjectName = alloc;
            Attributes.Attributes = 0x00000040; // OBJ_CASE_INSENSITIVE
            UNICODE_STRING.Create(ref Unsafe.AsRef<UNICODE_STRING>(alloc), filePath, numCharacters);
        }
    }

    internal struct NtOpenWrapper
    {
        public ObjectAttributesWrapper AttributesWrapper;
        public IntPtr Handle;
        public IO_STATUS_BLOCK StatusBlock;
        public long AllocSize;

        public unsafe NtOpenWrapper(char* fileName, int filePathLength) => AttributesWrapper = new ObjectAttributesWrapper(fileName, filePathLength);
    }
}

