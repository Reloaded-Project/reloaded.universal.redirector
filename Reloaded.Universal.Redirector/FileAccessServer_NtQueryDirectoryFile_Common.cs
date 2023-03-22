using System.ComponentModel;
using System.IO.Enumeration;
using System.Runtime.CompilerServices;
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
    // ReSharper disable InconsistentNaming
    private const int STATUS_BUFFER_TOO_SMALL = unchecked((int)0xC0000023);
    private const int STATUS_BUFFER_OVERFLOW = unchecked((int)0x80000005);
    private const int NO_MORE_FILES = unchecked((int)0x80000006);
    // ReSharper restore InconsistentNaming
    
    private void FilterNtQueryDirectoryFileResults<TDirectoryInformation>(TDirectoryInformation* lastBufferPtr,
        TDirectoryInformation* currentBufferPtr,
        SpanOfCharDict<bool> alreadyInjected)
        where TDirectoryInformation : unmanaged, IFileDirectoryInformationDerivative
    {
        // If no injected items, nothing to filter.
        if (alreadyInjected.Count == 0)
            return;
        
        // We need to walk through the results to filter them.
        // There is no pointer back, so we need to be clever about how to do this.
        var fileNameBuffer = new Span<char>(Threading.Get64KBuffer(), Threading.Buffer64KLength / sizeof(char));
        var lastEntry      = lastBufferPtr;
        var currentEntry   = currentBufferPtr;
        
        do
        {
            if (ShouldRemoveEntry(alreadyInjected, currentEntry, fileNameBuffer))
            {
                // Find next valid entry
                var nextItem = currentEntry;
                
                // Check if this is only entry; if so, skip via last entry
                if (!HasNext(nextItem))
                {
                    lastEntry->SetNextEntryOffset(0);
                    return;
                }
                
                while (true)
                {
                    if (!GoToNext(ref nextItem))
                    {
                        // There is no 'next item'; we set pointer to 0 [default].
                        currentEntry->SetNextEntryOffset(0);
                        break;
                    }
                    
                    // Skip if we need to remove this
                    if (ShouldRemoveEntry(alreadyInjected, nextItem, fileNameBuffer))
                        continue;
                    
                    // Set to next item.
                    lastEntry->SetNextEntryOffset((int)((byte*)nextItem - (byte*)lastEntry));
                    break;
                }
            }

            // Update current entry [end of loop].
            lastEntry = currentEntry;
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
    private bool InitHandleNtQueryDirectoryFileHook(nint fileHandle, UNICODE_STRING* fileName, out OpenHandleState? handleItem)
    {
        // Check if this is a handle we picked up/are redirecting.
        // If this is not one of those handles.
        if (!_fileHandles.TryGetValue(fileHandle, out handleItem))
        {
            _logger?.Warning($"File Handle for {fileHandle} not found. This is likely a result of a bug.");
            return false;
        }
        
        LogDebugOnly("Call {0} in {1}",handleItem.FilePath, nameof(InitHandleNtQueryDirectoryFileHook));

        // Fetch items we need 
        if (handleItem.Items == null)
        {
            if (!_redirectorApi.Redirector.Manager.TryGetFolder(handleItem.FilePath, out var dict))
                return false;
            
            LogDebugOnly("Init {0} in {1}", handleItem.FilePath, nameof(InitHandleNtQueryDirectoryFileHook));
            
            // Creates a copy; we need to ensure collection is unchanged during operation.
            handleItem.Items = dict.GetValues();
            handleItem.AlreadyInjected = new SpanOfCharDict<bool>(handleItem.Items.Length);
        }

        // TODO: Handle This
        if (fileName != null)
        {
            handleItem.QueryFileName = fileName->ToSpan().ToString();
            LogDebugOnly("Set FileName {0} in {1}", handleItem.QueryFileName, nameof(InitHandleNtQueryDirectoryFileHook));
            return true;
        }

        return true;
    }

    /// <summary/>
    /// <exception cref="Win32Exception">Failed to query file. [file will be skipped]</exception>
    /// <returns>True on success, else false if failed or filtered out.</returns>
    /// <remarks>
    ///     If the item is not advanced, buffer is insufficient; else item was filtered out or an error occurred.
    ///     Item is advanced regardless of success.
    /// </remarks>
    private bool QueryCustomFile<TDirectoryInformation>(ref nint lastFileInformation, ref nint fileInformation,
        ref int remainingBytes,
        ref int currentItem,
        SpanOfCharDict<RedirectionTreeTarget>.ItemEntry[] items, TDirectoryInformation* currentBufferPtr,
        ref bool moreFiles, SpanOfCharDict<bool> alreadyInjected, string queryFileName)
        where TDirectoryInformation : unmanaged, IFileDirectoryInformationDerivative
    {
        var item = items.DangerousGetReferenceAt(currentItem);
        var filePath = item.Value.GetFullPathWithDevicePrefix();
        nint handle = -1; // windows will error if this is never set
        
        try
        {
            handle = NtOpenFileOpen(filePath);
            if (TDirectoryInformation.TryPopulate(currentBufferPtr, remainingBytes, handle))
            {
                // Filter out bad result.
                if (!FileSystemName.MatchesWin32Expression(queryFileName, currentBufferPtr->GetFileName(currentBufferPtr)))
                {
                    NtClose(handle);
                    currentItem++;
                    return false;
                }
                
                var nextOfs = currentBufferPtr->GetNextEntryOffset();
                lastFileInformation = fileInformation;
                fileInformation += nextOfs;
                remainingBytes -= nextOfs;
                NtClose(handle);
                currentItem++;
                
                // TODO: Make a HashSet for this.
                // Casing of custom files can differ from original, we need to normalize here.
                var fileNameStr = currentBufferPtr->GetFileName(currentBufferPtr).ToString();
                TextInfo.ChangeCaseInPlace<TextInfo.ToUpperConversion>(fileNameStr);
                alreadyInjected.AddOrReplace(fileNameStr, true);
                return true;
            }
            else
            {
                // Not enough room.
                ((TDirectoryInformation*)lastFileInformation)->SetNextEntryOffset(0); // no next item
                moreFiles = false;
                NtClose(handle);
                return false;
            }
        }
        catch (Win32Exception)
        {
            // Not enough room.
            ((TDirectoryInformation*)lastFileInformation)->SetNextEntryOffset(0); // no next item
            NtClose(handle);
            currentItem++;
            _logger?.Error($"Failed to query file name {filePath}. Will not show in search results.");
            return false;
        }
    }
}