using Reloaded.Universal.Redirector.Lib.Utility.Native;
using static Reloaded.Universal.Redirector.Lib.Utility.Native.Native;
using static Reloaded.Universal.Redirector.Lib.Utility.Native.Native.FILE_INFORMATION_CLASS;

namespace Reloaded.Universal.Redirector;

// Contains the parts of FileAccessServer responsible for handling NtQueryDirectoryFile callbacks
public unsafe partial class FileAccessServer
{
    private int NtQueryDirectoryFileHookImpl(nint fileHandle, nint @event, nint apcRoutine, nint apcContext, 
        IO_STATUS_BLOCK* ioStatusBlock, nint fileInformation, uint length, FILE_INFORMATION_CLASS fileInformationClass, 
        int returnSingleEntry, UNICODE_STRING* fileName, int restartScan)
    {
        // Prevent recursion.
        var threadId = Thread.CurrentThread.ManagedThreadId;
        if (_queryDirectoryFileLock.IsThisThread(threadId))
        {
            LogDebugOnly("Exiting due to recursion lock on {0}", nameof(NtQueryDirectoryFileHookImpl));
            goto fastReturn;
        }

        // Handle any of the possible intercepted types.
        if (fileInformationClass == FileDirectoryInformation)
            if (!HandleNtQueryDirectoryFileHook<FILE_DIRECTORY_INFORMATION>(fileHandle, @event, apcRoutine, apcContext, ioStatusBlock, fileInformation, length, fileInformationClass, returnSingleEntry, fileName, restartScan, threadId, out var result))
                goto fastReturn;
            else return result;
        
        if (fileInformationClass == FileFullDirectoryInformation)
            if (!HandleNtQueryDirectoryFileHook<FILE_FULL_DIR_INFORMATION>(fileHandle, @event, apcRoutine, apcContext, ioStatusBlock, fileInformation, length, fileInformationClass, returnSingleEntry, fileName, restartScan, threadId, out var result))
                goto fastReturn;
            else return result;
        
        if (fileInformationClass == FileBothDirectoryInformation)
            if (!HandleNtQueryDirectoryFileHook<FILE_BOTH_DIR_INFORMATION>(fileHandle, @event, apcRoutine, apcContext, ioStatusBlock, fileInformation, length, fileInformationClass, returnSingleEntry, fileName, restartScan, threadId, out var result))
                goto fastReturn;
            else return result;
        
        if (fileInformationClass == FileNamesInformation)
            if (!HandleNtQueryDirectoryFileHook<FILE_NAMES_INFORMATION>(fileHandle, @event, apcRoutine, apcContext, ioStatusBlock, fileInformation, length, fileInformationClass, returnSingleEntry, fileName, restartScan, threadId, out var result))
                goto fastReturn;
            else return result;
        
        if (fileInformationClass == FileIdBothDirectoryInformation)
            if (!HandleNtQueryDirectoryFileHook<FILE_ID_BOTH_DIR_INFORMATION>(fileHandle, @event, apcRoutine, apcContext, ioStatusBlock, fileInformation, length, fileInformationClass, returnSingleEntry, fileName, restartScan, threadId, out var result))
                goto fastReturn;
            else return result;
        
        if (fileInformationClass == FileIdFullDirectoryInformation)
            if (!HandleNtQueryDirectoryFileHook<FILE_ID_FULL_DIR_INFORMATION>(fileHandle, @event, apcRoutine, apcContext, ioStatusBlock, fileInformation, length, fileInformationClass, returnSingleEntry, fileName, restartScan, threadId, out var result))
                goto fastReturn;
            else return result;
        
        if (fileInformationClass == FileIdGlobalTxDirectoryInformation)
            if (!HandleNtQueryDirectoryFileHook<FILE_ID_GLOBAL_TX_DIR_INFORMATION>(fileHandle, @event, apcRoutine, apcContext, ioStatusBlock, fileInformation, length, fileInformationClass, returnSingleEntry, fileName, restartScan, threadId, out var result))
                goto fastReturn;
            else return result;
        
        if (fileInformationClass == FileIdExtdDirectoryInformation)
            if (!HandleNtQueryDirectoryFileHook<FILE_ID_EXTD_DIR_INFORMATION>(fileHandle, @event, apcRoutine, apcContext, ioStatusBlock, fileInformation, length, fileInformationClass, returnSingleEntry, fileName, restartScan, threadId, out var result))
                goto fastReturn;
            else return result;
        
        if (fileInformationClass == FileIdExtdBothDirectoryInformation)
            if (!HandleNtQueryDirectoryFileHook<FILE_ID_EXTD_BOTH_DIR_INFORMATION>(fileHandle, @event, apcRoutine, apcContext, ioStatusBlock, fileInformation, length, fileInformationClass, returnSingleEntry, fileName, restartScan, threadId, out var result))
                goto fastReturn;
            else return result;
        
        fastReturn:
        return _ntQueryDirectoryFileHook.Original.Value.Invoke(fileHandle, @event, apcRoutine, apcContext, ioStatusBlock, 
            fileInformation, length, fileInformationClass, returnSingleEntry, fileName, restartScan);
    }
    
    /// <returns>True if handled, false if to delegate to original function [unsupported feature].</returns>
    private bool HandleNtQueryDirectoryFileHook<TDirectoryInformation>(nint fileHandle, nint @event, nint apcRoutine, nint apcContext, 
        IO_STATUS_BLOCK* ioStatusBlock, nint fileInformation, uint length, FILE_INFORMATION_CLASS fileInformationClass, 
        int returnSingleEntry, UNICODE_STRING* fileName, int restartScan, int threadId, out int returnValue) 
        where TDirectoryInformation : unmanaged, IFileDirectoryInformationDerivative
    {
        returnValue = default;
        
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

        var initialItem = handleItem.CurrentItem;
        while (moreFiles)
        {
            var currentBufferPtr = (TDirectoryInformation*)fileInformation;
            if (handleItem.CurrentItem < items!.Length)
            {
                // Populate with custom files.
                LogDebugOnly("Injecting File Index {0} in {1}, Struct: [{2}]", handleItem.CurrentItem, nameof(HandleNtQueryDirectoryFileHook), typeof(TDirectoryInformation).Name);
                var lastItem = handleItem.CurrentItem;
                var success = QueryCustomFile(ref lastFileInformation, ref fileInformation, ref remainingBytes, ref handleItem.CurrentItem, items, currentBufferPtr, ref moreFiles, handleItem.AlreadyInjected!, handleItem.QueryFileName);
                if (!success)
                {
                    // Not enough space for next element.
                    if (lastItem > initialItem)
                    {
                        ((TDirectoryInformation*)lastFileInformation)->SetNextEntryOffset(0);
                        returnValue = STATUS_SUCCESS;
                        break;
                    }

                    // Not enough space for any element
                    if (lastItem == handleItem.CurrentItem)
                    {
                        returnValue = STATUS_BUFFER_TOO_SMALL;
                        break;
                    }

                    LogDebugOnly("Filtered Out {0} in {1}", handleItem.CurrentItem, nameof(HandleNtQueryDirectoryFileHook));
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
                returnValue = _ntQueryDirectoryFileHook.Original.Value.Invoke(fileHandle, @event, apcRoutine, apcContext, ioStatusBlock, 
                    fileInformation, (uint)remainingBytes, fileInformationClass, returnSingleEntry, fileName, handleItem.GetForceRestartScan());

                if (returnValue != 0)
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