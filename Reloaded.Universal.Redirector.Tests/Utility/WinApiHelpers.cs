using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Win32.SafeHandles;
using Reloaded.Universal.Redirector.Lib.Utility.Native;
using static Reloaded.Universal.Redirector.Lib.Utility.Native.Native;

// ReSharper disable InconsistentNaming
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Reloaded.Universal.Redirector.Tests.Utility;

/// <summary>
/// Helpers for calling Windows API functions.
/// </summary>
public static class WinApiHelpers
{
    public static unsafe List<string> NtQueryDirectoryFileGetAllItems(string folderPath, FILE_INFORMATION_CLASS method)
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
            var statusBlock = new IO_STATUS_BLOCK();
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
                if (method == FILE_INFORMATION_CLASS.FileDirectoryInformation)
                    GetFiles<FILE_DIRECTORY_INFORMATION>(currentBufferPtr, files);
                else if (method == FILE_INFORMATION_CLASS.FileFullDirectoryInformation)
                    GetFiles<FILE_FULL_DIR_INFORMATION>(currentBufferPtr, files);
                else if (method == FILE_INFORMATION_CLASS.FileBothDirectoryInformation)
                    GetFiles<FILE_BOTH_DIR_INFORMATION>(currentBufferPtr, files);
                else if (method == FILE_INFORMATION_CLASS.FileNamesInformation)
                    GetFiles<FILE_NAMES_INFORMATION>(currentBufferPtr, files);
                else if (method == FILE_INFORMATION_CLASS.FileIdBothDirectoryInformation)
                    GetFiles<FILE_ID_BOTH_DIR_INFORMATION>(currentBufferPtr, files);
                else if (method == FILE_INFORMATION_CLASS.FileIdFullDirectoryInformation)
                    GetFiles<FILE_ID_FULL_DIR_INFORMATION>(currentBufferPtr, files);
                else if (method == FILE_INFORMATION_CLASS.FileIdGlobalTxDirectoryInformation)
                    GetFiles<FILE_ID_GLOBAL_TX_DIR_INFORMATION>(currentBufferPtr, files);                
                else if (method == FILE_INFORMATION_CLASS.FileIdExtdDirectoryInformation)
                    GetFiles<FILE_ID_EXTD_DIR_INFORMATION>(currentBufferPtr, files);                
                else if (method == FILE_INFORMATION_CLASS.FileIdExtdBothDirectoryInformation)
                    GetFiles<FILE_ID_EXTD_BOTH_DIR_INFORMATION>(currentBufferPtr, files);
            }
        }

        return files;
    }

    private static unsafe void GetFiles<T>(nint currentBufferPtr, List<string> files) where T : unmanaged, IFileDirectoryInformationDerivative
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
            currentBufferPtr += info->GetNextEntryOffset();
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
}

public struct ObjectAttributesWrapper : IDisposable
{
    public OBJECT_ATTRIBUTES Attributes;
    public NativeAllocation<UNICODE_STRING> FileNameAllocation;

    public unsafe ObjectAttributesWrapper(char* filePath, int numCharacters)
    {
        Attributes = new OBJECT_ATTRIBUTES();
        FileNameAllocation = new NativeAllocation<UNICODE_STRING>();
        Attributes.ObjectName = FileNameAllocation.Value;
        Attributes.Attributes = 0x00000040; // OBJ_CASE_INSENSITIVE
        UNICODE_STRING.Create(ref Unsafe.AsRef<UNICODE_STRING>(FileNameAllocation.Value), filePath, numCharacters);
    }

    public void Dispose() => FileNameAllocation.Dispose();
}

public struct NtOpenWrapper : IDisposable
{
    public ObjectAttributesWrapper AttributesWrapper;
    public IntPtr Handle;
    public IO_STATUS_BLOCK StatusBlock;
    public long AllocSize;

    public unsafe NtOpenWrapper(char* fileName, int filePathLength) => AttributesWrapper = new ObjectAttributesWrapper(fileName, filePathLength);

    public void Dispose() => AttributesWrapper.Dispose();
}