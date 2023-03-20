using Reloaded.Universal.Redirector.Lib.Utility.Native;
using static Reloaded.Universal.Redirector.Lib.Utility.Native.Native;
using static Reloaded.Universal.Redirector.Lib.Utility.Native.Native.FILE_INFORMATION_CLASS;

namespace Reloaded.Universal.Redirector;

// Contains the parts of FileAccessServer responsible for handling NtQueryDirectoryFile callbacks
public unsafe partial class FileAccessServer
{
    private const int RestartScanFlag = 1;
    private const int ReturnSingleFileFlag = 2;

    private int NtQueryDirectoryFileExHookImpl(IntPtr fileHandle, IntPtr @event, IntPtr apcRoutine, IntPtr apcContext,
        IO_STATUS_BLOCK* ioStatusBlock, IntPtr fileInformation, uint length, FILE_INFORMATION_CLASS fileInformationClass, 
        int queryFlags, UNICODE_STRING* fileName)
    {
        // Prevent recursion.
        var threadId = Thread.CurrentThread.ManagedThreadId;
        if (_queryDirectoryFileLock.IsThisThread(threadId))
        {
            LogDebugOnly("Exiting due to recursion lock on {0}", nameof(NtQueryDirectoryFileExHookImpl));
            goto fastReturn;
        }

        // Handle any of the possible intercepted types.
        if (fileInformationClass == FileDirectoryInformation)
            if (!HandleNtQueryDirectoryFileExHook<FILE_DIRECTORY_INFORMATION>(fileHandle, @event, apcRoutine, apcContext, ioStatusBlock, fileInformation, length, fileInformationClass, queryFlags, fileName, threadId, out var result))
                goto fastReturn;
            else return result;
        
        if (fileInformationClass == FileFullDirectoryInformation)
            if (!HandleNtQueryDirectoryFileExHook<FILE_FULL_DIR_INFORMATION>(fileHandle, @event, apcRoutine, apcContext, ioStatusBlock, fileInformation, length, fileInformationClass, queryFlags, fileName, threadId, out var result))
                goto fastReturn;
            else return result;
        
        if (fileInformationClass == FileBothDirectoryInformation)
            if (!HandleNtQueryDirectoryFileExHook<FILE_BOTH_DIR_INFORMATION>(fileHandle, @event, apcRoutine, apcContext, ioStatusBlock, fileInformation, length, fileInformationClass, queryFlags, fileName, threadId, out var result))
                goto fastReturn;
            else return result;
        
        if (fileInformationClass == FileNamesInformation)
            if (!HandleNtQueryDirectoryFileExHook<FILE_NAMES_INFORMATION>(fileHandle, @event, apcRoutine, apcContext, ioStatusBlock, fileInformation, length, fileInformationClass, queryFlags, fileName, threadId, out var result))
                goto fastReturn;
            else return result;
        
        if (fileInformationClass == FileIdBothDirectoryInformation)
            if (!HandleNtQueryDirectoryFileExHook<FILE_ID_BOTH_DIR_INFORMATION>(fileHandle, @event, apcRoutine, apcContext, ioStatusBlock, fileInformation, length, fileInformationClass, queryFlags, fileName, threadId, out var result))
                goto fastReturn;
            else return result;
        
        if (fileInformationClass == FileIdFullDirectoryInformation)
            if (!HandleNtQueryDirectoryFileExHook<FILE_ID_FULL_DIR_INFORMATION>(fileHandle, @event, apcRoutine, apcContext, ioStatusBlock, fileInformation, length, fileInformationClass, queryFlags, fileName, threadId, out var result))
                goto fastReturn;
            else return result;
        
        if (fileInformationClass == FileIdGlobalTxDirectoryInformation)
            if (!HandleNtQueryDirectoryFileExHook<FILE_ID_GLOBAL_TX_DIR_INFORMATION>(fileHandle, @event, apcRoutine, apcContext, ioStatusBlock, fileInformation, length, fileInformationClass, queryFlags, fileName, threadId, out var result))
                goto fastReturn;
            else return result;
        
        if (fileInformationClass == FileIdExtdDirectoryInformation)
            if (!HandleNtQueryDirectoryFileExHook<FILE_ID_EXTD_DIR_INFORMATION>(fileHandle, @event, apcRoutine, apcContext, ioStatusBlock, fileInformation, length, fileInformationClass, queryFlags, fileName, threadId, out var result))
                goto fastReturn;
            else return result;
        
        if (fileInformationClass == FileIdExtdBothDirectoryInformation)
            if (!HandleNtQueryDirectoryFileExHook<FILE_ID_EXTD_BOTH_DIR_INFORMATION>(fileHandle, @event, apcRoutine, apcContext, ioStatusBlock, fileInformation, length, fileInformationClass, queryFlags, fileName, threadId, out var result))
                goto fastReturn;
            else return result;
        
        fastReturn:
        return _ntQueryDirectoryFileExHook.Original.Value.Invoke(fileHandle, @event, apcRoutine, apcContext, ioStatusBlock, 
            fileInformation, length, fileInformationClass, queryFlags, fileName);
    }
    
    /// <returns>True if handled, false if to delegate to original function [unsupported feature].</returns>
    private bool HandleNtQueryDirectoryFileExHook<TDirectoryInformation>(IntPtr fileHandle, IntPtr @event, IntPtr apcRoutine, IntPtr apcContext,
        IO_STATUS_BLOCK* ioStatusBlock, IntPtr fileInformation, uint length, FILE_INFORMATION_CLASS fileInformationClass, 
        int queryFlags, UNICODE_STRING* fileName, int threadId, out int returnValue) 
        where TDirectoryInformation : unmanaged, IFileDirectoryInformationDerivative
    {
        returnValue = default;

        int restartScan = Convert.ToInt32((queryFlags & RestartScanFlag) == RestartScanFlag);
        int returnSingleEntry = Convert.ToInt32((queryFlags & ReturnSingleFileFlag) == ReturnSingleFileFlag);
        if (!InitHandleNtQueryDirectoryFileHook(fileHandle, fileName, out var handleItem)) 
            return false;
        
        // Reset state if restart is requested.
        if (restartScan != 0)
            handleItem!.Restart();
        
        // Okay here our items.
        _queryDirectoryFileLock.Lock(threadId);
        var items = handleItem!.Items;
        bool moreFiles = true;
        int remainingBytes = (int)length;
        var lastFileInformation = fileInformation;
        
        while (moreFiles)
        {
            var currentBufferPtr = (TDirectoryInformation*)fileInformation;
            if (handleItem.CurrentItem < items!.Length)
            {
                // Populate with custom files.
                LogDebugOnly("Injecting File Index {0} in {1}, Struct: [{2}]", handleItem.CurrentItem, nameof(HandleNtQueryDirectoryFileExHook), typeof(TDirectoryInformation).Name);
                var lastItem = handleItem.CurrentItem;
                var success = QueryCustomFile(ref lastFileInformation, ref fileInformation, ref remainingBytes, ref handleItem.CurrentItem, items, currentBufferPtr, ref moreFiles, handleItem.AlreadyInjected!, handleItem.QueryFileName);
                if (!success)
                {
                    // Not enough space.
                    if (lastItem == handleItem.CurrentItem)
                    {
                        returnValue = STATUS_BUFFER_TOO_SMALL;
                        break;
                    }
                    
                    LogDebugOnly("Filtered Out {0} in {1}", handleItem.CurrentItem, nameof(HandleNtQueryDirectoryFileExHook));
                }

                if ((returnSingleEntry != 0 && success) || !moreFiles)
                {
                    ((TDirectoryInformation*)lastFileInformation)->SetNextEntryOffset(0);
                    break;
                }
            }
            else
            {
                // We finished with custom files, now get the originals that haven't been replaced.
                if (handleItem.GetForceRestartScan() >= 1)
                    queryFlags |= RestartScanFlag;
                
                returnValue = _ntQueryDirectoryFileExHook.Original.Value.Invoke(fileHandle, @event, apcRoutine, apcContext, ioStatusBlock, 
                    fileInformation, (uint)remainingBytes, fileInformationClass, queryFlags, fileName);

                if (returnValue == NO_MORE_FILES)
                {
                    ((TDirectoryInformation*)lastFileInformation)->SetNextEntryOffset(0);
                    break;
                }
                
                if (returnSingleEntry != 0)
                    break;

                FilterNtQueryDirectoryFileResults((TDirectoryInformation*)lastFileInformation, currentBufferPtr, handleItem.AlreadyInjected!);
                break;
            }
        }

        _queryDirectoryFileLock.Unlock();
        return true;
    }
}