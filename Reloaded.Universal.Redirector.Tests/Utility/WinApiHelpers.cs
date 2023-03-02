using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Reloaded.Universal.Redirector.Lib.Utility.Native;
// ReSharper disable InconsistentNaming
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Reloaded.Universal.Redirector.Tests.Utility;

/// <summary>
/// Helpers for calling Windows API functions.
/// </summary>
public static partial class WinApiHelpers
{
    public static unsafe List<string> NtQueryDirectoryFileGetAllItems(string folderPath, Native.FILE_INFORMATION_CLASS method)
    {
        using var handle = new SafeFileHandle(NtCreateFileDirectoryOpen(folderPath), true);
        
        // Note: Thanks to SkipLocalsInit, this memory is not zero'd so the allocation is virtually free.
        const int bufferSize = 8192;
        var files = new List<string>();
        byte* bufferPtr = stackalloc byte[bufferSize];
        
        // Read remaining files while possible.
        bool moreFiles = true;
        while (moreFiles)
        {
            var statusBlock = new Native.IO_STATUS_BLOCK();
            var ntstatus = NtQueryDirectoryFile(handle.DangerousGetHandle(), // Our directory handle.
                IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, &statusBlock, // Pointers we don't care about 
                (IntPtr)bufferPtr, bufferSize, method, // Buffer info.
                0, null, 0);

            var currentBufferPtr = (IntPtr)bufferPtr;
            if (ntstatus != 0)
            {
                moreFiles = false;
            }
            else
            {
                if (method == Native.FILE_INFORMATION_CLASS.FileDirectoryInformation)
                    GetFiles<FILE_DIRECTORY_INFORMATION>(currentBufferPtr, files);
                else if (method == Native.FILE_INFORMATION_CLASS.FileFullDirectoryInformation)
                    GetFiles<FILE_FULL_DIR_INFORMATION>(currentBufferPtr, files);
                else if (method == Native.FILE_INFORMATION_CLASS.FileBothDirectoryInformation)
                    GetFiles<FILE_BOTH_DIR_INFORMATION>(currentBufferPtr, files);
                else if (method == Native.FILE_INFORMATION_CLASS.FileNamesInformation)
                    GetFiles<FILE_NAMES_INFORMATION>(currentBufferPtr, files);
                else if (method == Native.FILE_INFORMATION_CLASS.FileIdBothDirectoryInformation)
                    GetFiles<FILE_ID_BOTH_DIR_INFORMATION>(currentBufferPtr, files);
                else if (method == Native.FILE_INFORMATION_CLASS.FileIdFullDirectoryInformation)
                    GetFiles<FILE_ID_FULL_DIR_INFORMATION>(currentBufferPtr, files);
                else if (method == Native.FILE_INFORMATION_CLASS.FileIdGlobalTxDirectoryInformation)
                    GetFiles<FILE_ID_GLOBAL_TX_DIR_INFORMATION>(currentBufferPtr, files);                
                else if (method == Native.FILE_INFORMATION_CLASS.FileIdExtdDirectoryInformation)
                    GetFiles<FILE_ID_EXTD_DIR_INFORMATION>(currentBufferPtr, files);                
                else if (method == Native.FILE_INFORMATION_CLASS.FileIdExtdBothDirectoryInformation)
                    GetFiles<FILE_ID_EXTD_BOTH_DIR_INFORMATION>(currentBufferPtr, files);
            }
        }

        return files;
    }

    private static unsafe void GetFiles<T>(nint currentBufferPtr, List<string> files) where T : IFileDirectoryInformationDerivative
    {
        T* info = default;
        do
        {
            info = (T*)currentBufferPtr;

            // Not symlink or symlink to offline file.
            if ((info->GetFileAttributes() & FileAttributes.ReparsePoint) != 0 &&
                (info->GetFileAttributes() & FileAttributes.Offline) == 0)
                goto nextfile;

            var fileName = info->GetFileName(info);

            if (fileName == "." || fileName == "..")
                goto nextfile;

            var isDirectory = (info->GetFileAttributes() & FileAttributes.Directory) > 0;
            if (!isDirectory)
                files.Add(fileName);

            nextfile:
            currentBufferPtr += (int)info->GetNextEntryOffset();
        } while (info->GetNextEntryOffset() != 0);
    }

    public static string NtOpenFileReadAllText(string filePath)
    {
        using var fileStream = new FileStream(new SafeFileHandle(NtOpenFileOpen(filePath), true), FileAccess.Read);
        using StreamReader sr = new StreamReader(fileStream, detectEncodingFromByteOrderMarks: true);
        return sr.ReadToEnd();
    }
    
    public static string NtCreateFileReadAllText(string filePath)
    {
        using var fileStream = new FileStream(new SafeFileHandle(NtCreateFileOpen(filePath), true), FileAccess.Read);
        using StreamReader sr = new StreamReader(fileStream, detectEncodingFromByteOrderMarks: true);
        return sr.ReadToEnd();
    }
    
    public static unsafe IntPtr NtOpenFileOpen(string filePath)
    {
        fixed (char* fileName = filePath)
        {
            using var ntOpenWrapper = new NtOpenWrapper(fileName, filePath.Length);
            var status = NtOpenFile(&ntOpenWrapper.Handle, ACCESS_MASK.FILE_GENERIC_READ, &ntOpenWrapper.AttributesWrapper.Attributes, 
                &ntOpenWrapper.StatusBlock, FileShare.ReadWrite, CreateOptions.SynchronousIoAlert);
            
            if (status != 0)
                throw new Win32Exception(status);

            return ntOpenWrapper.Handle;
        }
    }
    
    public static unsafe IntPtr NtCreateFileOpen(string filePath)
    {
        fixed (char* fileName = filePath)
        {
            using var ntOpenWrapper = new NtOpenWrapper(fileName, filePath.Length);
            var status = NtCreateFile(&ntOpenWrapper.Handle, ACCESS_MASK.FILE_GENERIC_READ, &ntOpenWrapper.AttributesWrapper.Attributes, 
                &ntOpenWrapper.StatusBlock, &ntOpenWrapper.AllocSize, FileAttributes.Normal, FileShare.ReadWrite, 
                CreateDisposition.Open, CreateOptions.SynchronousIoAlert, 0, 0);
            
            if (status != 0)
                throw new Win32Exception(status);

            return ntOpenWrapper.Handle;
        }
    }

    public static unsafe IntPtr NtCreateFileDirectoryOpen(string dirPath)
    {
        fixed (char* fileName = dirPath)
        {
            using var ntOpenWrapper = new NtOpenWrapper(fileName, dirPath.Length);
            var status = NtCreateFile(&ntOpenWrapper.Handle, ACCESS_MASK.FILE_GENERIC_READ | ACCESS_MASK.SYNCHRONIZE, 
                &ntOpenWrapper.AttributesWrapper.Attributes, 
                &ntOpenWrapper.StatusBlock, &ntOpenWrapper.AllocSize, FileAttributes.Directory, FileShare.Read, 
                CreateDisposition.Open, CreateOptions.SynchronousIoNonAlert | CreateOptions.DirectoryFile, 0, 0);
            
            if (status != 0)
                throw new Win32Exception(status);

            return ntOpenWrapper.Handle;
        }
    }
    
    [LibraryImport("ntdll.dll", SetLastError = true)]
    // ReSharper disable once MemberCanBePrivate.Global
    public static unsafe partial int NtCreateFile(IntPtr* handlePtr, ACCESS_MASK access, Native.OBJECT_ATTRIBUTES* objectAttributes, 
        Native.IO_STATUS_BLOCK* ioStatus, long* allocSize, FileAttributes fileAttributes, FileShare share, CreateDisposition createDisposition, 
        CreateOptions createOptions, IntPtr eaBuffer, uint eaLength);
    
    [LibraryImport("ntdll.dll", SetLastError = true)]
    // ReSharper disable once MemberCanBePrivate.Global
    public static unsafe partial int NtOpenFile(IntPtr* handlePtr, ACCESS_MASK DesiredAccess, Native.OBJECT_ATTRIBUTES* ObjectAttributes,
        Native.IO_STATUS_BLOCK* IoStatusBlock, FileShare ShareAccess, CreateOptions OpenOptions);
    
    [LibraryImport("ntdll.dll", SetLastError = true)]
    // ReSharper disable once MemberCanBePrivate.Global
    public static unsafe partial int NtQueryDirectoryFile(IntPtr fileHandle, IntPtr @event, IntPtr apcRoutine, IntPtr apcContext,
        Native.IO_STATUS_BLOCK* ioStatusBlock, IntPtr fileInformation, uint length, Native.FILE_INFORMATION_CLASS fileInformationClass, 
        int returnSingleEntry, Native.UNICODE_STRING* fileName, int restartScan);
}

public struct ObjectAttributesWrapper : IDisposable
{
    public Native.OBJECT_ATTRIBUTES Attributes;
    public NativeAllocation<Native.UNICODE_STRING> FileNameAllocation;

    public unsafe ObjectAttributesWrapper(char* filePath, int numCharacters)
    {
        Attributes = new Native.OBJECT_ATTRIBUTES();
        FileNameAllocation = new NativeAllocation<Native.UNICODE_STRING>();
        Attributes.ObjectName = FileNameAllocation.Value;
        Attributes.Attributes = 0x00000040; // OBJ_CASE_INSENSITIVE
        Native.UNICODE_STRING.Create(ref Unsafe.AsRef<Native.UNICODE_STRING>(FileNameAllocation.Value), filePath, numCharacters);
    }

    public void Dispose() => FileNameAllocation.Dispose();
}

public struct NtOpenWrapper : IDisposable
{
    public ObjectAttributesWrapper AttributesWrapper;
    public IntPtr Handle;
    public Native.IO_STATUS_BLOCK StatusBlock;
    public long AllocSize;

    public unsafe NtOpenWrapper(char* fileName, int filePathLength) => AttributesWrapper = new ObjectAttributesWrapper(fileName, filePathLength);

    public void Dispose() => AttributesWrapper.Dispose();
}

[Flags]
public enum FileAttributes : uint
{
    Readonly = 0x00000001,
    Hidden = 0x00000002,
    System = 0x00000004,
    Directory = 0x00000010,
    Archive = 0x00000020,
    Device = 0x00000040,
    Normal = 0x00000080,
    Temporary = 0x00000100,
    SparseFile = 0x00000200,
    ReparsePoint = 0x00000400,
    Compressed = 0x00000800,
    Offline = 0x00001000,
    NotContentIndexed = 0x00002000,
    Encrypted = 0x00004000,
    IntegrityStream = 0x00008000,
    NoScrubData = 0x00020000,
    Pinned = 0x00080000,
    Unpinned = 0x00100000,
    RecallOnOpen = 0x00040000,
    RecallOnDataAccess = 0x00400000
}   

public enum CreateDisposition : uint
{
    Supersede = 0x00000000,
    Open = 0x00000001,
    Create = 0x00000002,
    OpenIf = 0x00000003,
    Overwrite = 0x00000004,
    OverwriteIf = 0x00000005
}

// Enumeration for file creation options
[Flags]
public enum CreateOptions : uint
{
    DirectoryFile = 0x00000001,
    WriteThrough = 0x00000002,
    SequentialOnly = 0x00000004,
    NoIntermediateBuffering = 0x00000008,
    SynchronousIoNonAlert = 0x00000010,
    SynchronousIoAlert = 0x00000020,
    NonDirectoryFile = 0x00000040,
    CreateTreeConnection = 0x00000080,
    CompleteIfOplocked = 0x00000100,
    NoEaKnowledge = 0x00000200,
    OpenRemoteInstance = 0x00000400,
    RandomAccess = 0x00000800,
    DeleteOnClose = 0x00001000,
    OpenByFileId = 0x00002000,
    OpenForBackupIntent = 0x00004000,
    NoCompression = 0x00008000,
    OpenRequiringOplock = 0x00010000,
    DisallowExclusive = 0x00020000,
    SessionAware = 0x00040000,
    ReserveOpfilter = 0x00100000,
    OpenReparsePoint = 0x00200000,
    OpenNoRecall = 0x00400000,
    OpenForFreeSpaceQuery = 0x00800000
}

[Flags]
public enum ACCESS_MASK : uint
{
    DELETE = 0x00010000,
    READ_CONTROL = 0x00020000,
    WRITE_DAC = 0x00040000,
    WRITE_OWNER = 0x00080000,
    SYNCHRONIZE = 0x00100000,
    STANDARD_RIGHTS_REQUIRED = 0x000F0000,
    STANDARD_RIGHTS_READ = READ_CONTROL,
    STANDARD_RIGHTS_WRITE = READ_CONTROL,
    STANDARD_RIGHTS_EXECUTE = READ_CONTROL,
    STANDARD_RIGHTS_ALL = 0x001F0000,
    SPECIFIC_RIGHTS_ALL = 0x0000FFFF,
    FILE_READ_DATA = 0x0001,
    FILE_LIST_DIRECTORY = 0x0001,
    FILE_WRITE_DATA = 0x0002,
    FILE_ADD_FILE = 0x0002,
    FILE_APPEND_DATA = 0x0004,
    FILE_ADD_SUBDIRECTORY = 0x0004,
    FILE_CREATE_PIPE_INSTANCE = 0x0004,
    FILE_READ_EA = 0x0008,
    FILE_WRITE_EA = 0x0010,
    FILE_EXECUTE = 0x0020,
    FILE_TRAVERSE = 0x0020,
    FILE_DELETE_CHILD = 0x0040,
    FILE_READ_ATTRIBUTES = 0x0080,
    FILE_WRITE_ATTRIBUTES = 0x0100,
    FILE_ALL_ACCESS = STANDARD_RIGHTS_REQUIRED | SYNCHRONIZE | 0x1FF,
    FILE_GENERIC_READ = STANDARD_RIGHTS_READ | FILE_READ_DATA | FILE_READ_ATTRIBUTES | FILE_READ_EA | SYNCHRONIZE,
    FILE_GENERIC_WRITE = STANDARD_RIGHTS_WRITE | FILE_WRITE_DATA | FILE_WRITE_ATTRIBUTES | FILE_WRITE_EA | FILE_APPEND_DATA | SYNCHRONIZE,
    FILE_GENERIC_EXECUTE = STANDARD_RIGHTS_EXECUTE | FILE_READ_ATTRIBUTES | FILE_EXECUTE | SYNCHRONIZE
}

public unsafe interface IFileDirectoryInformationDerivative
{
    public int GetNextEntryOffset();
    public FileAttributes GetFileAttributes();
    public string GetFileName(void* thisPtr);
}

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
    public char FileName;
    
    public int GetNextEntryOffset() => (int)NextEntryOffset;
    public FileAttributes GetFileAttributes() => FileAttributes;

    public string GetFileName(void* thisPtr)
    {
        var casted = (FILE_DIRECTORY_INFORMATION*)thisPtr;
        var addr = &casted->FileName;
        return Marshal.PtrToStringUni((nint)addr, (int)FileNameLength / 2);
    }
}

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
    public char FileName;
    
    public int GetNextEntryOffset() => (int)NextEntryOffset;
    public FileAttributes GetFileAttributes() => FileAttributes;

    public string GetFileName(void* thisPtr)
    {
        var casted = (FILE_FULL_DIR_INFORMATION*)thisPtr;
        var addr = &casted->FileName;
        return Marshal.PtrToStringUni((nint)addr, (int)FileNameLength / 2);
    }
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct FILE_BOTH_DIR_INFORMATION : IFileDirectoryInformationDerivative
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
    
    // Short name
    public fixed char ShortName[12];
    public char FileName;
    
    public int GetNextEntryOffset() => (int)NextEntryOffset;
    public FileAttributes GetFileAttributes() => FileAttributes;

    public string GetFileName(void* thisPtr)
    {
        var casted = (FILE_BOTH_DIR_INFORMATION*)thisPtr;
        var addr = &casted->FileName;
        return Marshal.PtrToStringUni((nint)addr, (int)FileNameLength / 2);
    }
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct FILE_NAMES_INFORMATION : IFileDirectoryInformationDerivative
{
    public uint NextEntryOffset;
    public uint FileIndex;
    public uint FileNameLength;
    public char FileName;
    
    public int GetNextEntryOffset() => (int)NextEntryOffset;
    public FileAttributes GetFileAttributes() => FileAttributes.Normal;

    public string GetFileName(void* thisPtr)
    {
        var casted = (FILE_NAMES_INFORMATION*)thisPtr;
        var addr = &casted->FileName;
        return Marshal.PtrToStringUni((nint)addr, (int)FileNameLength / 2);
    }
}

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
    public char FileName;
    
    public int GetNextEntryOffset() => (int)NextEntryOffset;
    public FileAttributes GetFileAttributes() => FileAttributes;

    public string GetFileName(void* thisPtr)
    {
        var casted = (FILE_ID_BOTH_DIR_INFORMATION*)thisPtr;
        var addr = &casted->FileName;
        return Marshal.PtrToStringUni((nint)addr, (int)FileNameLength / 2);
    }
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct FILE_ID_FULL_DIR_INFORMATION : IFileDirectoryInformationDerivative
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
    public ulong FileId;
    public char FileName;
    
    public int GetNextEntryOffset() => (int)NextEntryOffset;
    public FileAttributes GetFileAttributes() => FileAttributes;

    public string GetFileName(void* thisPtr)
    {
        var casted = (FILE_ID_FULL_DIR_INFORMATION*)thisPtr;
        var addr = &casted->FileName;
        return Marshal.PtrToStringUni((nint)addr, (int)FileNameLength / 2);
    }
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct FILE_ID_GLOBAL_TX_DIR_INFORMATION : IFileDirectoryInformationDerivative
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
    public long FileId;
    public Guid LockingTransactionId;
    public int TxInfoFlags;
    public char FileName;
    
    public int GetNextEntryOffset() => (int)NextEntryOffset;
    public FileAttributes GetFileAttributes() => FileAttributes;

    public string GetFileName(void* thisPtr)
    {
        var casted = (FILE_ID_GLOBAL_TX_DIR_INFORMATION*)thisPtr;
        var addr = &casted->FileName;
        return Marshal.PtrToStringUni((nint)addr, (int)FileNameLength / 2);
    }
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct FILE_ID_EXTD_DIR_INFORMATION : IFileDirectoryInformationDerivative
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
    public uint ReparsePointTag;
    public ulong FileId;
    public char FileName;
    
    public int GetNextEntryOffset() => (int)NextEntryOffset;
    public FileAttributes GetFileAttributes() => FileAttributes;

    public string GetFileName(void* thisPtr)
    {
        var casted = (FILE_ID_EXTD_DIR_INFORMATION*)thisPtr;
        var addr = &casted->FileName;
        return Marshal.PtrToStringUni((nint)addr, (int)FileNameLength / 2);
    }
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct FILE_ID_EXTD_BOTH_DIR_INFORMATION : IFileDirectoryInformationDerivative
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
    public uint ReparsePointTag;
    public long FileId;
    public byte ShortNameLength;
    public fixed char ShortName[12];
    public char FileName;
    
    public int GetNextEntryOffset() => (int)NextEntryOffset;
    public FileAttributes GetFileAttributes() => FileAttributes;

    public string GetFileName(void* thisPtr)
    {
        var casted = (FILE_ID_EXTD_BOTH_DIR_INFORMATION*)thisPtr;
        var addr = &casted->FileName;
        return Marshal.PtrToStringUni((nint)addr, (int)FileNameLength / 2);
    }
}