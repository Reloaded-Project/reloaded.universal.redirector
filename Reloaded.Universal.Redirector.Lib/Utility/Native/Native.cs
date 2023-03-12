using System.Runtime.InteropServices;
using System.Security;

// ReSharper disable InconsistentNaming
#pragma warning disable CS1591

namespace Reloaded.Universal.Redirector.Lib.Utility.Native;

/// <summary>
/// Defines all native functions.
/// </summary>
public partial class Native
{
    [SuppressUnmanagedCodeSecurity]
    [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    public static partial IntPtr LoadLibraryW([MarshalAs(UnmanagedType.LPWStr)] string lpFileName);

    [SuppressUnmanagedCodeSecurity]
    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    public static partial IntPtr GetProcAddress(IntPtr hModule, string procName);

    [SuppressUnmanagedCodeSecurity]
    [LibraryImport("ntdll.dll")]
    public static unsafe partial int NtQueryInformationFile(IntPtr fileHandle, ref IO_STATUS_BLOCK ioStatusBlock,
        void* pInfoBlock, uint length, FILE_INFORMATION_CLASS fileInformation);
    
    [LibraryImport("ntdll.dll", SetLastError = true)]
    // ReSharper disable once MemberCanBePrivate.Global
    public static unsafe partial int NtCreateFile(IntPtr* handlePtr, ACCESS_MASK access, OBJECT_ATTRIBUTES* objectAttributes, 
        IO_STATUS_BLOCK* ioStatus, long* allocSize, FileAttributes fileAttributes, FileShare share, CreateDisposition createDisposition, 
        CreateOptions createOptions, IntPtr eaBuffer, uint eaLength);
    
    [LibraryImport("ntdll.dll", SetLastError = true)]
    // ReSharper disable once MemberCanBePrivate.Global
    public static unsafe partial int NtOpenFile(IntPtr* handlePtr, ACCESS_MASK DesiredAccess, OBJECT_ATTRIBUTES* ObjectAttributes,
        IO_STATUS_BLOCK* IoStatusBlock, FileShare ShareAccess, CreateOptions OpenOptions);
    
    [LibraryImport("ntdll.dll", SetLastError = true)]
    // ReSharper disable once MemberCanBePrivate.Global
    public static unsafe partial int NtClose(IntPtr hObject);
    
    [LibraryImport("ntdll.dll", SetLastError = true)]
    // ReSharper disable once MemberCanBePrivate.Global
    public static unsafe partial int NtQueryDirectoryFile(IntPtr fileHandle, IntPtr @event, IntPtr apcRoutine, IntPtr apcContext,
        IO_STATUS_BLOCK* ioStatusBlock, IntPtr fileInformation, uint length, FILE_INFORMATION_CLASS fileInformationClass, 
        int returnSingleEntry, UNICODE_STRING* fileName, int restartScan);
}