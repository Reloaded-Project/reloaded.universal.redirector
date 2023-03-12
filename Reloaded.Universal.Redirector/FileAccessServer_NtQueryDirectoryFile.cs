using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.HighPerformance;
using Reloaded.Universal.Redirector.Lib.Backports.System.Globalization;
using Reloaded.Universal.Redirector.Lib.Structures;
using Reloaded.Universal.Redirector.Lib.Structures.RedirectionTree;
using Reloaded.Universal.Redirector.Lib.Utility;
using Reloaded.Universal.Redirector.Lib.Utility.Native;
using Reloaded.Universal.Redirector.Lib.Extensions;
using static Reloaded.Universal.Redirector.Lib.Utility.Native.FileDirectoryInformationDerivativeExtensions;
using static Reloaded.Universal.Redirector.Lib.Utility.Native.Native;

namespace Reloaded.Universal.Redirector;

// Contains the parts of FileAccessServer responsible for handling NtQueryDirectoryFile callbacks
public unsafe partial class FileAccessServer
{
    /// <returns>True if handled, false if to delegate to original function [unsupported feature].</returns>
    private bool HandleNtQueryDirectoryFileHook<TDirectoryInformation>(nint fileHandle, nint @event, nint apcRoutine, nint apcContext, 
        IO_STATUS_BLOCK* ioStatusBlock, nint fileInformation, uint length, FILE_INFORMATION_CLASS fileInformationClass, 
        int returnSingleEntry, UNICODE_STRING* fileName, int restartScan, int threadId, out int returnValue) 
        where TDirectoryInformation : unmanaged, IFileDirectoryInformationDerivative
    {
        returnValue = default;
        
        if (!InitHandleNtQueryDirectoryFileHook(fileHandle, fileName, restartScan, out var handleItem)) 
            return false;

        // Okay here our items.
        _queryDirectoryFileLock.Lock(threadId);
        var items = handleItem.Items;
        bool moreFiles = true;
        int remainingBytes = (int)length;
        
        while (moreFiles)
        {
            var currentBufferPtr = (TDirectoryInformation*)fileInformation;
            if (handleItem.CurrentItem < items.Length)
            {
                // Populate with custom files.
                QueryCustomFile(ref fileInformation, ref remainingBytes, ref handleItem.CurrentItem, items, currentBufferPtr, ref moreFiles, handleItem.AlreadyInjected!);
                if (returnSingleEntry != 0)
                {
                    _queryDirectoryFileLock.Unlock();
                    return true;
                }
            }
            else
            {
                // We finished with custom files, now get the originals that haven't been replaced.
                returnValue = _ntQueryDirectoryFileHook.Original.Value.Invoke(fileHandle, @event, apcRoutine, apcContext, ioStatusBlock, 
                    fileInformation, (uint)remainingBytes, fileInformationClass, returnSingleEntry, fileName, restartScan);

                if (returnSingleEntry != 0)
                {
                    _queryDirectoryFileLock.Unlock();
                    return true;
                }
                
                FilterNtQueryDirectoryFileResults(currentBufferPtr, handleItem.AlreadyInjected!);
                _queryDirectoryFileLock.Unlock();
                return true;
            }
        }

        _queryDirectoryFileLock.Unlock();
        return true;
    }

    private void FilterNtQueryDirectoryFileResults<TDirectoryInformation>(TDirectoryInformation* currentBufferPtr,
        SpanOfCharDict<bool> alreadyInjected)
        where TDirectoryInformation : unmanaged, IFileDirectoryInformationDerivative
    {
        // Now we need to walk through the results to make sure a file previously returned is not present.
        // There is no pointer back, so we need to be clever about how to do this.
        var currentEntry = currentBufferPtr;
        var fileNameBuffer = new Span<char>(Threading.Get64KBuffer(), Threading.Buffer64KLength / sizeof(char));
        
        do
        {
            if (ShouldRemoveEntry(alreadyInjected, currentEntry, fileNameBuffer))
            {
                // Find next valid entry
                var nextItem = currentEntry;
                while (true)
                {
                    if (!GoToNext(ref nextItem))
                    {
                        // There is no 'next item'; we set pointer to 0 [default].
                        currentEntry->SetNextEntryOffset(0);
                        break;
                    }
                    
                    if (ShouldRemoveEntry(alreadyInjected, nextItem, fileNameBuffer))
                        continue;
                    
                    currentEntry->SetNextEntryOffset((int)((byte*)nextItem - (byte*)currentEntry));
                    break;
                }
                
            }

            // Update current entry [end of loop].
            if (!GoToNext(ref currentEntry))
                return; // no more elements
        } 
        while (true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ShouldRemoveEntry<TDirectoryInformation>(SpanOfCharDict<bool> alreadyInjected,
        TDirectoryInformation* currentEntry, Span<char> fileNameBuffer)
        where TDirectoryInformation : unmanaged, IFileDirectoryInformationDerivative
    {
        // Check if we have done this fileName.
        var fileName = currentEntry->GetFileName(currentEntry);
        TextInfo.ChangeCase<TextInfo.ToUpperConversion>(fileName, fileNameBuffer);
        fileNameBuffer = fileNameBuffer.SliceFast(0, fileName.Length);
        return alreadyInjected.ContainsKey(fileNameBuffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool InitHandleNtQueryDirectoryFileHook(nint fileHandle, UNICODE_STRING* fileName,
        int restartScan, out OpenHandleState? handleItem)
    {
        // Check if this is a handle we picked up/are redirecting.
        // If this is not one of those handles.
        if (!_fileHandles.TryGetValue(fileHandle, out handleItem))
        {
#if DEBUG
            _logger?.Warning($"File Handle for {fileHandle} not found. This is likely a result of a bug.");
#endif
            return false;
        }

        // Fetch items we need 
        if (handleItem.Items == null)
        {
            if (!_redirectorApi.Redirector.Manager.TryGetFolder(handleItem.FilePath, out var dict))
                return false;

            // Creates a copy; we need to ensure collection is unchanged during operation.
            handleItem.Items = dict.GetValues();
            handleItem.AlreadyInjected = new SpanOfCharDict<bool>(handleItem.Items.Length);
        }

        // Reset state if restart is requested.
        if (restartScan != 0)
            handleItem.Reset();

        // TODO: Handle This
        if (fileName != null)
        {
            handleItem.QueryFileName = fileName->ToSpan().ToString();
            return false;
        }

        return true;
    }

    /// <summary/>
    /// <exception cref="Win32Exception">Failed to query file. [file will be skipped]</exception>
    private void QueryCustomFile<TDirectoryInformation>(ref nint fileInformation, ref int remainingBytes,
        ref int currentItem,
        SpanOfCharDict<RedirectionTreeTarget>.ItemEntry[] items, TDirectoryInformation* currentBufferPtr,
        ref bool moreFiles, SpanOfCharDict<bool> alreadyInjected)
        where TDirectoryInformation : unmanaged, IFileDirectoryInformationDerivative
    {
        var item = Lib.Extensions.ArrayExtensions.DangerousGetReferenceAt(items, currentItem);
        var filePath = item.Value.GetFullPathWithDevicePrefix();
        nint handle = -1; // windows will error if this is never set
        
        try
        {
            handle = NtOpenFileOpen(filePath);
            if (TDirectoryInformation.TryPopulate(currentBufferPtr, remainingBytes, handle))
            {
                var nextOfs = currentBufferPtr->GetNextEntryOffset();
                fileInformation += nextOfs;
                remainingBytes -= nextOfs;
                NtClose(handle);
                currentItem++;
                
                // TODO: Make a HashSet for this.
                // Casing of custom files can differ from original, we need to normalize here.
                var fileNameStr = currentBufferPtr->GetFileName(currentBufferPtr).ToString();
                TextInfo.ChangeCaseInPlace<TextInfo.ToUpperConversion>(fileNameStr);
                alreadyInjected.AddOrReplace(fileNameStr, true);
            }
            else
            {
                // Not enough room.
                moreFiles = false;
                NtClose(handle);
            }
        }
        catch (Win32Exception)
        {
            NtClose(handle);
            currentItem++;
            _logger?.Error($"Failed to query file name {filePath}. Will not show in search results.");
        }
    }
}