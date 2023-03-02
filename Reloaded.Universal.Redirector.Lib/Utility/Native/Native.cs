using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using FileEmulationFramework.Lib.Utilities;
// ReSharper disable InconsistentNaming
#pragma warning disable CS1591

namespace Reloaded.Universal.Redirector.Lib.Utility.Native;

/// <summary>
/// Defines all native functions.
/// </summary>
public partial class Native
{
    /// <summary>
    /// A driver sets an IRP's I/O status block to indicate the final status of an I/O request, before calling IoCompleteRequest for the IRP.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct IO_STATUS_BLOCK
    {
        public UInt32 status;
        public IntPtr information;
    }

    /// <summary>
    /// The OBJECT_ATTRIBUTES structure specifies attributes that can be applied to objects or object
    /// handles by routines that create objects and/or return handles to objects.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct OBJECT_ATTRIBUTES
    {
        /// <summary>
        /// Length of this structure.
        /// </summary>
        public int Length;

        /// <summary>
        /// Optional handle to the root object directory for the path name specified by the ObjectName member.
        /// If RootDirectory is NULL, ObjectName must point to a fully qualified object name that includes the full path to the target object.
        /// If RootDirectory is non-NULL, ObjectName specifies an object name relative to the RootDirectory directory.
        /// The RootDirectory handle can refer to a file system directory or an object directory in the object manager namespace.
        /// </summary>
        public IntPtr RootDirectory;

        /// <summary>
        /// Pointer to a Unicode string that contains the name of the object for which a handle is to be opened.
        /// This must either be a fully qualified object name, or a relative path name to the directory specified by the RootDirectory member.
        /// </summary>
        public unsafe UNICODE_STRING* ObjectName;

        /// <summary>
        /// Bitmask of flags that specify object handle attributes. This member can contain one or more of the flags in the following table (See MSDN)
        /// </summary>
        public uint Attributes;

        /// <summary>
        /// Specifies a security descriptor (SECURITY_DESCRIPTOR) for the object when the object is created.
        /// If this member is NULL, the object will receive default security settings.
        /// </summary>
        public IntPtr SecurityDescriptor;

        /// <summary>
        /// Optional quality of service to be applied to the object when it is created.
        /// Used to indicate the security impersonation level and context tracking mode (dynamic or static).
        /// Currently, the InitializeObjectAttributes macro sets this member to NULL.
        /// </summary>
        public IntPtr SecurityQualityOfService;

        /// <summary/>
        /// <param name="fileName">The file name/path.</param>
        /// <param name="unicodeString">Pointer to stack stored string inside which to embed fileName.</param>
        public OBJECT_ATTRIBUTES()
        {
            Length = sizeof(OBJECT_ATTRIBUTES);
            RootDirectory = 0;
            ObjectName = (UNICODE_STRING*)0;
            Attributes = 0;
            SecurityDescriptor = 0;
            SecurityQualityOfService = 0;
        }

        /// <summary>
        /// Tries to obtain the root directory, if it is not null.
        /// </summary>
        /// <returns>True if extracted, else false.</returns>
        public unsafe bool TryGetRootDirectory(out string result)
        {
            result = "";
            if (RootDirectory == IntPtr.Zero)
                return false;

            // Cold Path
            var statusBlock = new IO_STATUS_BLOCK();
            fixed (byte* fileNameBuf = &Threading.Buffer64K[0])
            {
                int queryStatus = NtQueryInformationFile(RootDirectory, ref statusBlock, fileNameBuf, Threading.Buffer64KLength, FILE_INFORMATION_CLASS.FileNameInformation);
                if (queryStatus != 0)
                {
                    ThrowHelpers.Win32Exception(queryStatus);
                    return false;
                }

                var fileName = (FILE_NAME_INFORMATION*)fileNameBuf;
                result = new string((char*)(fileName + 1), 0, (int)fileName->FileNameLength / sizeof(char));
                return true;
            }
        }
    }

    /// <summary>
    /// Represents a singular unicode string.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct UNICODE_STRING
    {
        public ushort Length;
        public ushort MaximumLength;
        private IntPtr buffer;
        
        /// <summary/>
        /// <param name="item">The item to create the string from.</param>
        /// <param name="pointer">Pointer to the first character.</param>
        /// <param name="length">Number of characters.</param>
        public unsafe UNICODE_STRING(char* pointer, int length) => Create(ref this, pointer, length);
        
        /// <summary/>
        /// <param name="item">The item to create the string from.</param>
        /// <param name="pointer">Pointer to the first character.</param>
        /// <param name="length">Number of characters.</param>
        /// <param name="attributes">The attributes to write the string to.</param>
        public unsafe UNICODE_STRING(char* pointer, int length, OBJECT_ATTRIBUTES* attributes) => Create(ref this, pointer, length, attributes);

        /// <summary/>
        /// <param name="item">The item to create the string from.</param>
        /// <param name="pointer">Pointer to the first character.</param>
        /// <param name="length">Number of characters.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Create(ref UNICODE_STRING item, char* pointer, int length)
        {
            item.Length = (ushort)(length * 2);
            item.MaximumLength = (ushort)(item.Length + 2);
            item.buffer = (IntPtr) pointer;
        }

        /// <summary/>
        /// <param name="item">The item to create the string from.</param>
        /// <param name="pointer">Pointer to the first character.</param>
        /// <param name="length">Number of characters.</param>
        /// <param name="attributes">The attributes to write the string to.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Create(ref UNICODE_STRING item, char* pointer, int length, OBJECT_ATTRIBUTES* attributes)
        {
            Create(ref item, pointer, length);
            attributes->ObjectName = (UNICODE_STRING*)Unsafe.AsPointer(ref item);
            attributes->RootDirectory = IntPtr.Zero;
        }
        
        /// <summary>
        /// Returns a string with the contents
        /// </summary>
        /// <returns></returns>
        public unsafe ReadOnlySpan<char> ToSpan()
        {
            if (buffer != IntPtr.Zero)
                return new ReadOnlySpan<char>((char*)buffer, Length / sizeof(char));

            return default;
        }
    }

    [SuppressUnmanagedCodeSecurity]
    [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    public static partial IntPtr LoadLibraryW([MarshalAs(UnmanagedType.LPWStr)] string lpFileName);

    [SuppressUnmanagedCodeSecurity]
    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    public static partial IntPtr GetProcAddress(IntPtr hModule, string procName);
    
    [SuppressUnmanagedCodeSecurity]
    [LibraryImport("ntdll.dll")] 
    public static unsafe partial int NtQueryInformationFile(IntPtr fileHandle, ref IO_STATUS_BLOCK ioStatusBlock, void* pInfoBlock, uint length, FILE_INFORMATION_CLASS fileInformation);

    public enum FILE_INFORMATION_CLASS
    {
        // ✅ Implemented
        // 🚸 Read Only
        // ❌ Not applicable OR implemented by redirecting handle in Create/Open file
        
        FileDirectoryInformation = 1, // 1 ✅ 
        FileFullDirectoryInformation, // 2 ✅ 
        FileBothDirectoryInformation, // 3 ✅ 
        
        FileBasicInformation,         // 4 ❌ 
        FileStandardInformation,      // 5 ❌ 
        FileInternalInformation,      // 6 ❌ 
        FileEaInformation,            // 7 ❌
        FileAccessInformation,        // 8 ❌
        FileNameInformation,          // 9 ❌
        FileRenameInformation,        // 10 🚸
        FileLinkInformation,          // 11 🚸
        FileNamesInformation,         // 12 ✅ 
        FileDispositionInformation,   // 13 ❌
        FilePositionInformation,      // 14 ❌
        FileFullEaInformation,        // 15 ❌
        FileModeInformation = 16,     // 16 ❌
        FileAlignmentInformation,     // 17 ❌
        FileAllInformation,           // 18 ❌
        FileAllocationInformation,    // 19 ❌
        FileEndOfFileInformation,     // 20 ❌
        FileAlternateNameInformation, // 21 ❌
        FileStreamInformation,        // 22 ❌
        FilePipeInformation,          // 23 ❌
        FilePipeLocalInformation,     // 24 ❌
        FilePipeRemoteInformation,    // 25 ❌
        FileMailslotQueryInformation, // 26 ❌
        FileMailslotSetInformation,   // 27 ❌
        FileCompressionInformation,   // 28 ❌
        FileObjectIdInformation,      // 29 ❌
        FileCompletionInformation,    // 30 ❌
        FileMoveClusterInformation,   // 31 ❌
        FileQuotaInformation,         // 32 ❌
        FileReparsePointInformation,  // 33 ❌
        FileNetworkOpenInformation,   // 34 ❌
        FileAttributeTagInformation,  // 35 ❌
        FileTrackingInformation,      // 36 ❌
        FileIdBothDirectoryInformation, // 37 ✅
        FileIdFullDirectoryInformation, // 38 ✅
        FileValidDataLengthInformation, // 39 ❌
        FileShortNameInformation,       // 40 ❌
        FileIoCompletionNotificationInformation, // 41 ❌
        FileIoStatusBlockRangeInformation,       // 42 ❌
        FileIoPriorityHintInformation,           // 43 ❌
        FileSfioReserveInformation,              // 44 ❌
        FileSfioVolumeInformation,               // 45 ❌
        FileHardLinkInformation,                 // 46 ❌
        FileProcessIdsUsingFileInformation,      // 47 ❌
        FileNormalizedNameInformation,           // 48 ❌
        FileNetworkPhysicalNameInformation,      // 49 ❌
        FileIdGlobalTxDirectoryInformation,      // 50 ✅
        FileIsRemoteDeviceInformation,           // 51 ❌
        FileUnusedInformation,                   // 52 ❌
        FileNumaNodeInformation,                 // 53 ❌
        FileStandardLinkInformation,             // 54 ❌
        FileRemoteProtocolInformation,           // 55 ❌
        
        //
        //  These are special versions of these operations (defined earlier)
        //  which can be used by kernel mode drivers only to bypass security
        //  access checks for Rename and HardLink operations.  These operations
        //  are only recognized by the IOManager, a file system should never
        //  receive these.
        //

        FileRenameInformationBypassAccessCheck,         // 56 ❌
        FileLinkInformationBypassAccessCheck,           // 57 ❌
        
        //
        // End of special information classes reserved for IOManager.
        //

        FileVolumeNameInformation,                      // 58 ❌
        FileIdInformation,                              // 59 ❌
        FileIdExtdDirectoryInformation,                 // 60 ✅
        FileReplaceCompletionInformation,               // 61 ❌
        FileHardLinkFullIdInformation,                  // 62 ❌
        FileIdExtdBothDirectoryInformation,             // 63 ✅
        FileDispositionInformationEx,                   // 64 ❌
        FileRenameInformationEx,                        // 65 ❌
        FileRenameInformationExBypassAccessCheck,       // 66 ❌
        FileDesiredStorageClassInformation,             // 67 ❌
        FileStatInformation,                            // 68 ❌
        FileMemoryPartitionInformation,                 // 69 ❌
        FileStatLxInformation,                          // 70 ❌
        FileCaseSensitiveInformation,                   // 71 ❌
        FileLinkInformationEx,                          // 72 ❌
        FileLinkInformationExBypassAccessCheck,         // 73 ❌
        FileStorageReserveIdInformation,                // 74 ❌
        FileCaseSensitiveInformationForceAccessCheck,   // 75 ❌
        FileKnownFolderInformation,                     // 76 ❌

        FileMaximumInformation // ❌ <= undocumented
        
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct FILE_NAME_INFORMATION 
    {
        internal uint FileNameLength;
        // Inlined file name here right after field.
    }
}