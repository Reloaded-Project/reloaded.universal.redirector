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
    public struct NtQueryDirectoryFileSettings
    {
        /// <summary>
        /// If true, returns results one by one.
        /// </summary>
        public bool OneByOne = false;
        
        /// <summary>
        /// Restarts search after item with specified index.
        /// </summary>
        public int? RestartAfter = null;
        
        /// <summary>
        /// Filter for the file name.
        /// </summary>
        public string FileNameFilter = "*";
        
        /// <summary>
        /// Buffer size used to hold results of search.
        /// </summary>
        public int BufferSize = 4096;

        public NtQueryDirectoryFileSettings() { }
    }
    
    public struct NtQueryDirectoryFileResult
    {
        /// <summary>
        /// List of returned files.
        /// </summary>
        public List<string> Files { get; set; }
        
        /// <summary>
        /// List of returned directories.
        /// </summary>
        public List<string> Directories { get; set; }

        public List<string> GetItems(bool includeDirectories)
        {
            var files = new List<string>(Files);
            if (includeDirectories)
                files.AddRange(Directories);
            
            return files;
        }
    }

    public static unsafe NtQueryDirectoryFileResult NtQueryDirectoryFileGetAllItems(bool ex, string folderPath, FILE_INFORMATION_CLASS method, NtQueryDirectoryFileSettings settings)
    {
        return ex ? NtQueryDirectoryFileExGetAllItems(folderPath, method, settings) : 
            NtQueryDirectoryFileGetAllItems(folderPath, method, settings);
    }

    public static unsafe NtQueryDirectoryFileResult NtQueryDirectoryFileGetAllItems(string folderPath, FILE_INFORMATION_CLASS method, NtQueryDirectoryFileSettings settings)
    {
        var handleUnsafe = NtCreateFileDirectoryOpen(folderPath);
        using var handle = new SafeFileHandle(handleUnsafe, true);
        
        // Note: Thanks to SkipLocalsInit, this memory is not zero'd so the allocation is virtually free.
        var files = new List<string>();
        var directories = new List<string>();
        byte* bufferPtr = stackalloc byte[settings.BufferSize];
        
        // Read remaining files while possible.
        bool moreFiles = true;
        int returnSingleEntry = settings.OneByOne ? 1 : 0;
        fixed (char* fileNamePtr = settings.FileNameFilter)
        {
            var fileNameString = new UNICODE_STRING(fileNamePtr, settings.FileNameFilter.Length);

            while (moreFiles)
            {
                int restartScan = 0;
                int restartAfterValue = settings.RestartAfter.GetValueOrDefault(int.MaxValue);
                if (files.Count >= restartAfterValue)
                {
                    restartScan = 1;
                    restartAfterValue = int.MaxValue;
                    settings.RestartAfter = null;
                }

                var statusBlock = new IO_STATUS_BLOCK();
                var ntstatus = NtQueryDirectoryFile(handle.DangerousGetHandle(), // Our directory handle.
                    IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, &statusBlock, // Pointers we don't care about 
                    (IntPtr)bufferPtr, (uint)settings.BufferSize, method, // Buffer info.
                    returnSingleEntry, &fileNameString, restartScan);

                var currentBufferPtr = (IntPtr)bufferPtr;
                if (ntstatus != 0)
                {
                    moreFiles = false;
                }
                else
                {
                    GetFiles(method, currentBufferPtr, files, directories, restartAfterValue);
                }
            }

            return new NtQueryDirectoryFileResult()
            {
                Files = files,
                Directories = directories
            };
        }
    }

    public static unsafe NtQueryDirectoryFileResult NtQueryDirectoryFileExGetAllItems(string folderPath,
        FILE_INFORMATION_CLASS method, NtQueryDirectoryFileSettings settings)
    {
        var handleUnsafe = NtCreateFileDirectoryOpen(folderPath);
        using var handle = new SafeFileHandle(handleUnsafe, true);
        
        // Note: Thanks to SkipLocalsInit, this memory is not zero'd so the allocation is virtually free.
        var files = new List<string>();
        var directories = new List<string>();
        byte* bufferPtr = stackalloc byte[settings.BufferSize];
        
        // Read remaining files while possible.
        bool moreFiles = true;
        int returnSingleEntry = settings.OneByOne ? 1 : 0;
        fixed (char* fileNamePtr = settings.FileNameFilter)
        {
            var fileNameString = new UNICODE_STRING(fileNamePtr, settings.FileNameFilter.Length);

            while (moreFiles)
            {
                int restartScan = 0;
                int restartAfterValue = settings.RestartAfter.GetValueOrDefault(int.MaxValue);
                if (files.Count >= restartAfterValue)
                {
                    restartScan = 1;
                    restartAfterValue = int.MaxValue;
                    settings.RestartAfter = null;
                }

                var statusBlock = new IO_STATUS_BLOCK();
                int queryFlags = 0;

                if (returnSingleEntry == 1)
                    queryFlags |= 2;
                
                if (restartScan == 1)
                    queryFlags |= 1;
                
                var ntstatus = NtQueryDirectoryFileEx(handle.DangerousGetHandle(), // Our directory handle.
                    IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, &statusBlock, // Pointers we don't care about 
                    (IntPtr)bufferPtr, (uint)settings.BufferSize, method, // Buffer info.
                    queryFlags, &fileNameString);

                var currentBufferPtr = (IntPtr)bufferPtr;
                if (ntstatus != 0)
                {
                    moreFiles = false;
                }
                else
                {
                    GetFiles(method, currentBufferPtr, files, directories, restartAfterValue);
                }
            }

            return new NtQueryDirectoryFileResult()
            {
                Files = files,
                Directories = directories
            };
        }
    }
    
    private static void GetFiles(FILE_INFORMATION_CLASS method, nint currentBufferPtr, List<string> files, List<string> directories,
        int restartAfterValue)
    {
        if (method == FILE_INFORMATION_CLASS.FileDirectoryInformation)
            GetFiles<FILE_DIRECTORY_INFORMATION>(currentBufferPtr, files, directories, restartAfterValue);
        else if (method == FILE_INFORMATION_CLASS.FileFullDirectoryInformation)
            GetFiles<FILE_FULL_DIR_INFORMATION>(currentBufferPtr, files, directories, restartAfterValue);
        else if (method == FILE_INFORMATION_CLASS.FileBothDirectoryInformation)
            GetFiles<FILE_BOTH_DIR_INFORMATION>(currentBufferPtr, files, directories, restartAfterValue);
        else if (method == FILE_INFORMATION_CLASS.FileNamesInformation)
            GetFiles<FILE_NAMES_INFORMATION>(currentBufferPtr, files, directories, restartAfterValue);
        else if (method == FILE_INFORMATION_CLASS.FileIdBothDirectoryInformation)
            GetFiles<FILE_ID_BOTH_DIR_INFORMATION>(currentBufferPtr, files, directories, restartAfterValue);
        else if (method == FILE_INFORMATION_CLASS.FileIdFullDirectoryInformation)
            GetFiles<FILE_ID_FULL_DIR_INFORMATION>(currentBufferPtr, files, directories, restartAfterValue);
        else if (method == FILE_INFORMATION_CLASS.FileIdGlobalTxDirectoryInformation)
            GetFiles<FILE_ID_GLOBAL_TX_DIR_INFORMATION>(currentBufferPtr, files, directories, restartAfterValue);
        else if (method == FILE_INFORMATION_CLASS.FileIdExtdDirectoryInformation)
            GetFiles<FILE_ID_EXTD_DIR_INFORMATION>(currentBufferPtr, files, directories, restartAfterValue);
        else if (method == FILE_INFORMATION_CLASS.FileIdExtdBothDirectoryInformation)
            GetFiles<FILE_ID_EXTD_BOTH_DIR_INFORMATION>(currentBufferPtr, files, directories, restartAfterValue);
    }

    private static unsafe void GetFiles<T>(nint currentBufferPtr, List<string> files, List<string> directories,
        int returnAfterItem = int.MaxValue) where T : unmanaged, IFileDirectoryInformationDerivative
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
            if (fileName.SequenceEqual(".".AsSpan()) || fileName.SequenceEqual("..".AsSpan()))
                goto nextfile;

            if ((info->GetFileAttributes() & FileAttributes.Directory) == FileAttributes.Directory)
                directories.Add(fileName.ToString());
            else
                files.Add(fileName.ToString());

            if (files.Count > returnAfterItem)
                return;
            
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
    
    public static unsafe FILE_BASIC_INFORMATION NtQueryAttributesFileHelper(string filePath)
    {
        fixed (char* fileName = filePath)
        {
            var ntOpenWrapper = new NtOpenWrapper(fileName, filePath.Length);
            using var handle = new SafeFileHandle(NtCreateFileOpen(filePath), true);
            var info = new FILE_BASIC_INFORMATION();
            NtQueryAttributesFile(&ntOpenWrapper.AttributesWrapper.Attributes, &info);
            return info;
        }
    }
    
    public static unsafe FILE_NETWORK_OPEN_INFORMATION NtQueryFullAttributesFileHelper(string filePath)
    {
        fixed (char* fileName = filePath)
        {
            var ntOpenWrapper = new NtOpenWrapper(fileName, filePath.Length);
            using var handle = new SafeFileHandle(NtCreateFileOpen(filePath), true);
            var info = new FILE_NETWORK_OPEN_INFORMATION();
            NtQueryFullAttributesFile(&ntOpenWrapper.AttributesWrapper.Attributes, &info);
            return info;
        }
    }

    public static unsafe IntPtr NtCreateFileOpen(string filePath)
    {
        fixed (char* fileName = filePath)
        {
            var ntOpenWrapper = new NtOpenWrapper(fileName, filePath.Length);
            var status = NtCreateFile(&ntOpenWrapper.Handle, ACCESS_MASK.FILE_GENERIC_READ, &ntOpenWrapper.AttributesWrapper.Attributes, 
                &ntOpenWrapper.StatusBlock, &ntOpenWrapper.AllocSize, FileAttributes.Normal, FileShare.ReadWrite, 
                CreateDisposition.Open, CreateOptions.SynchronousIoAlert, 0, 0);
            
            if (status != 0)
                throw new Win32Exception(status);

            return ntOpenWrapper.Handle;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static unsafe IntPtr NtCreateFileDirectoryOpen(string dirPath)
    {
        fixed (char* fileName = dirPath)
        {
            var ntOpenWrapper = new NtOpenWrapper(fileName, dirPath.Length);
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

