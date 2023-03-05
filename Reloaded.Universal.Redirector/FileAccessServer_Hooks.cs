using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FileEmulationFramework.Lib.Utilities;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Universal.Redirector.Lib;
using Reloaded.Universal.Redirector.Lib.Structures;
using Reloaded.Universal.Redirector.Lib.Structures.RedirectionTree;
using Reloaded.Universal.Redirector.Lib.Utility;
using Reloaded.Universal.Redirector.Lib.Utility.Native;
using Reloaded.Universal.Redirector.Structures;
using static Reloaded.Universal.Redirector.Lib.Utility.Native.Native;
using static Reloaded.Universal.Redirector.Lib.Utility.Native.Native.FILE_INFORMATION_CLASS;
using static Reloaded.Universal.Redirector.Structures.NativeIntList;

namespace Reloaded.Universal.Redirector;

// Contains the parts of FileAccessServer responsible for the hooking.

/// <summary>
/// Intercepts I/O access on the Win32
/// </summary>
public unsafe partial class FileAccessServer
{
    /*
        Sewer's Grand API Mapping Table:

        This comment shows a listing of hooks and their corresponding APIs they successfully handle.
        i.e. Often API 1 will call API 2 under the hood in Windows.

        Confirmed by looking at Windows (7, 8.1, 10 & 11), older versions are no longer 
        supported by me, MSFT or .NET runtime.

        Windows Native APIs:
            ✅ NtCreateFile_Hook           -> NtCreateFile 
            ✅ NtOpenFile_Hook             -> NtOpenFile
            NtQueryDirectoryFileEx_Hook -> NtQueryDirectoryFileEx
                                           NtQueryDirectoryFile

            NtDeleteFile_Hook              -> NtDeleteFile
            NtQueryAttributesFile_Hook     -> NtQueryAttributesFile
            NtQueryFullAttributesFile_Hook -> NtQueryFullAttributesFile
            NtClose_Hook                   -> NtClose [needs ASM; as GC Transition can close threads; leading to infinite recursion]

            Note: NtQueryDirectoryFileEx on Win10 >=, hook NtQueryDirectoryFile on Wine and Earlier
            Check explicitly if methods present, else bail out fast.

        Win32 APIs:

            Listing of Win32 counterparts and the corresponding NT hooks that handle them.

            FindFirstFileA   -> FindFirstFileExW -> NtOpenFile & NtQueryDirectoryFileEx Hooks.
            FindFirstFileExA -> FindFirstFileExW -> NtOpenFile & NtQueryDirectoryFileEx Hooks.
            FindFirstFileW   -> NtOpenFile & NtQueryDirectoryFileEx Hooks.
            FindFirstFileExW -> NtOpenFile & NtQueryDirectoryFileEx Hooks.
            FindFirstFileExFromAppW -> FindFirstFileExW -> NtOpenFile & NtQueryDirectoryFileEx Hooks.

            FindNextFileA -> FindNextFileW -> NtQueryDirectoryFileEx Hook
            FindNextFileW -> NtQueryDirectoryFileEx Hook

            Ignore: 
                FindFirstFileName | We don't deal with hardlinks.

    */

    private static FileAccessServer _instance = null!;

    private Lib.Redirector GetRedirector() => _redirectorApi.Redirector;
    private RedirectionTreeManager GetManager() => _redirectorApi.Redirector.Manager;
    
    private bool _hooksApplied;
    private RedirectorApi _redirectorApi = null!;
    
    private SemaphoreRecursionLock _queryDirectoryFileLock = new();
    private SemaphoreRecursionLock _deleteFileLock = new();
    private SemaphoreRecursionLock _createFileLock = new();
    private SemaphoreRecursionLock _openFileLock = new();
    
    private AHook<NativeHookDefinitions.NtQueryDirectoryFileEx> _ntQueryDirectoryFileExHook = null!;
    private AHook<NativeHookDefinitions.NtQueryDirectoryFile> _ntQueryDirectoryFileHook = null!;
    private AHook<NativeHookDefinitions.NtDeleteFile> _ntDeleteFileHook = null!;
    private AHook<NativeHookDefinitions.NtCreateFile> _ntCreateFileHook = null!;
    private AHook<NativeHookDefinitions.NtOpenFile> _ntOpenFileHook = null!;
    private IAsmHook _closeHandleHook = null!;
    private Pinnable<NativeIntList> _closedHandleList = new(new NativeIntList());
    private readonly Dictionary<nint, OpenHandleState> _fileHandles = new();
    private Logger? _logger;

    public static void Initialize(IReloadedHooks hooks, RedirectorApi redirectorApi, string programDirectory, Logger? log = null)
    {
        // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
        _instance ??= new FileAccessServer();
        _instance.InitializeImpl(hooks, redirectorApi, programDirectory, log);    
    }

    private void InitializeImpl(IReloadedHooks hooks, RedirectorApi redirectorApi, string programDirectory, Logger? log = null)
    {
        _redirectorApi = redirectorApi;
        _redirectorApi.OnDisable += Disable;
        _redirectorApi.OnEnable += Enable;

        if (_hooksApplied)
            return;

        _hooksApplied = true;
        _logger = log;
        
        // Get Hooks
        var ntdllHandle = LoadLibraryW("ntdll");
        var ntCreateFilePointer = GetProcAddress(ntdllHandle, "NtCreateFile");
        var ntOpenFilePointer = GetProcAddress(ntdllHandle, "NtOpenFile");
        var ntDeleteFilePointer = GetProcAddress(ntdllHandle, "NtDeleteFile");
        //var ntQueryDirectoryFilePointer = GetProcAddress(ntdllHandle, "NtQueryDirectoryFile");
        //var ntQueryDirectoryFileExPointer = Native.GetProcAddress(ntdllHandle, "NtQueryDirectoryFileEx");

        // Kick off the server
        HookMethod(ref _ntCreateFileHook, nameof(NtCreateFileHookFn), "NtCreateFile", hooks, log, ntCreateFilePointer);
        HookMethod(ref _ntOpenFileHook, nameof(NtOpenFileHookFn), "NtOpenFile", hooks, log, ntOpenFilePointer);
        HookMethod(ref _ntDeleteFileHook, nameof(NtDeleteFileHookFn), "NtDeleteFile", hooks, log, ntDeleteFilePointer);
        //HookMethod(ref _ntQueryDirectoryFileHook, nameof(NtQueryDirectoryFileHookFn), "NtQueryDirectoryFile", hooks, log, ntQueryDirectoryFilePointer);
        //HookMethod(ref _ntQueryDirectoryFileExHook, nameof(NtQueryDirectoryFileExHookFn), "NtQueryDirectoryFileEx", hooks, log, ntQueryDirectoryFileExPointer);

        // We need to cook some assembly for NtClose, because Native->Managed
        // transition can invoke thread setup code which will call CloseHandle again
        // and that will lead to infinite recursion; also unable to do Coop <=> Preemptive GC transition

        // Win32 APIs are guaranteed to exist.

        var kernel32Handle = LoadLibraryW("kernel32");
        var closeHandle = GetProcAddress(kernel32Handle, "CloseHandle");
        var listPtr = (long)_closedHandleList.Pointer;
        if (IntPtr.Size == 4)
        {
            var asm = string.Format(File.ReadAllText(Path.Combine(programDirectory, "Asm/NativeIntList_X86.asm")), listPtr);
            _closeHandleHook = hooks.CreateAsmHook(asm, closeHandle, AsmHookBehaviour.ExecuteFirst);
        }
        else
        {
            var asm = string.Format(File.ReadAllText(Path.Combine(programDirectory, "Asm/NativeIntList_X64.asm")), listPtr);
            _closeHandleHook = hooks.CreateAsmHook(asm, closeHandle, AsmHookBehaviour.ExecuteFirst);
        }

        _closeHandleHook.Activate();
    }

    private void HookMethod<THook>(ref AHook<THook> hook, string methodName, string origFunctionName, IReloadedHooks hooks, Logger? log, nint nativePointer)
    {
        if (nativePointer != IntPtr.Zero)
            hook = hooks.CreateHook<THook>(typeof(FileAccessServer), methodName, nativePointer).ActivateAHook();
        else
            log?.Error($"{origFunctionName} not found.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DequeueHandles()
    {
        ref var nativeList = ref Unsafe.AsRef<NativeIntList>(_closedHandleList.Pointer);
        if (nativeList.NumItems <= 0)
            return;
        
        DoDequeueHandles(ref nativeList);
    }
    
    private void DoDequeueHandles(ref NativeIntList nativeList)
    {
        var threadId = nativeList.GetCurrentThreadId();
        while (Interlocked.CompareExchange(ref nativeList.CurrentThread, threadId, DefaultThreadHandle) != DefaultThreadHandle) { }

        for (int x = 0; x < nativeList.NumItems; x++)
        {
            var item = nativeList.ListPtr[x];
            if (_fileHandles.Remove(item, out var value))
                _logger?.Debug("[FileAccessServer] Closed handle: {0}, File: {1}", item, value.FilePath);
        }

        nativeList.NumItems = 0;
        nativeList.CurrentThread = DefaultThreadHandle;
    }

    /* Hooks */

    // Note: Some code below might be repeated for perf. reasons.
    // Note 2: Try-Finally below is now allowed because it prevents inlining.

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int NtCreateFileHookImpl(IntPtr* fileHandle, FileAccess access, OBJECT_ATTRIBUTES* objectAttributes, IO_STATUS_BLOCK* ioStatus, long* allocSize, uint fileattributes, FileShare share, uint createDisposition, uint createOptions, IntPtr eaBuffer, uint eaLength)
    {
        DequeueHandles();

        // Prevent recursion.
        var threadId = Thread.CurrentThread.ManagedThreadId;
        if (_createFileLock.IsThisThread(threadId))
            goto fastReturn;
        
        // Get name of file to be loaded.
        var attributes = objectAttributes;
        if (attributes->ObjectName == null)
            goto fastReturn;

        _createFileLock.Lock(threadId);

        {
            var path = ExtractPathFromObjectAttributes(attributes);
            _fileHandles[*fileHandle] = new OpenHandleState(path.ToString());
            if (!TryResolvePath(path, out string newFilePath))
            {
                _createFileLock.Unlock();
                goto fastReturn;
            }

            fixed (char* address = newFilePath)
            {
                // Backup original string.
                var originalObjectName = attributes->ObjectName;
                var originalDirectory = attributes->RootDirectory;

                // Call function with new file path.
                _ = new UNICODE_STRING(address, newFilePath.Length, attributes);
                var returnValue = _ntCreateFileHook.Original.Value.Invoke(fileHandle, access, objectAttributes, ioStatus,
                    allocSize, fileattributes, share, createDisposition, createOptions, eaBuffer, eaLength);

                // Reset original string.
                attributes->ObjectName = originalObjectName;
                attributes->RootDirectory = originalDirectory;
                _createFileLock.Unlock();
                return returnValue;
            }
        }

        fastReturn:
        return _ntCreateFileHook.Original.Value.Invoke(fileHandle, access, objectAttributes, ioStatus,
            allocSize, fileattributes, share, createDisposition, createOptions, eaBuffer, eaLength);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int NtOpenFileHookImpl(IntPtr* fileHandle, FileAccess access, OBJECT_ATTRIBUTES* objectAttributes, 
        IO_STATUS_BLOCK* ioStatus, FileShare share, uint openOptions)
    {
        DequeueHandles();
        
        // Prevent recursion.
        var threadId = Thread.CurrentThread.ManagedThreadId;
        if (_openFileLock.IsThisThread(threadId))
            goto fastReturn;
        
        // Get name of file to be loaded.
        var attributes = objectAttributes;
        if (attributes->ObjectName == null)
            goto fastReturn;
        
        _openFileLock.Lock(threadId);

        {
            var path = ExtractPathFromObjectAttributes(attributes);
            _fileHandles[*fileHandle] = new OpenHandleState(path.ToString());
            if (!TryResolvePath(path, out string newFilePath))
            {
                _openFileLock.Unlock();
                goto fastReturn;
            }

            fixed (char* address = newFilePath)
            {
                // Backup original string.
                var originalObjectName = attributes->ObjectName;
                var originalDirectory = attributes->RootDirectory;

                // Call function with new file path.
                _ = new UNICODE_STRING(address, newFilePath.Length, attributes);
                var returnValue = _ntOpenFileHook.Original.Value.Invoke(fileHandle, access, objectAttributes, ioStatus, share, openOptions);

                // Reset original string.
                attributes->ObjectName = originalObjectName;
                attributes->RootDirectory = originalDirectory;
                _openFileLock.Unlock();
                return returnValue;
            }
        }
        
        fastReturn:
        return _ntOpenFileHook.Original.Value.Invoke(fileHandle, access, objectAttributes, ioStatus, share, openOptions);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int NtDeleteFileHookImpl(OBJECT_ATTRIBUTES* objectAttributes)
    {
        // Prevent recursion.
        var threadId = Thread.CurrentThread.ManagedThreadId;
        if (_deleteFileLock.IsThisThread(threadId))
            goto fastReturn;
        
        // Get name of file to be loaded.
        var attributes = objectAttributes;
        if (attributes->ObjectName == null)
            goto fastReturn;
        
        _deleteFileLock.Lock(threadId);

        {
            DequeueHandles();
            if (!TryResolvePath(attributes, out string newFilePath))
            {
                _deleteFileLock.Unlock();
                goto fastReturn; 
            }

            fixed (char* address = newFilePath)
            {
                // Backup original string.
                var originalObjectName = attributes->ObjectName;
                var originalDirectory  = attributes->RootDirectory;

                // Call function with new file path.
                _ = new UNICODE_STRING(address, newFilePath.Length, attributes);
                var returnValue = _ntDeleteFileHook.Original.Value.Invoke(objectAttributes);
                
                // Reset original string.
                attributes->ObjectName    = originalObjectName;
                attributes->RootDirectory = originalDirectory;
                _deleteFileLock.Unlock();
                return returnValue;
            }
        }
        
        fastReturn:
        return _ntDeleteFileHook.Original.Value.Invoke(objectAttributes);
    }
    
    private int NtQueryDirectoryFileHookImpl(nint fileHandle, nint @event, nint apcRoutine, nint apcContext, 
        IO_STATUS_BLOCK* ioStatusBlock, nint fileInformation, uint length, FILE_INFORMATION_CLASS fileInformationClass, 
        int returnSingleEntry, UNICODE_STRING* fileName, int restartScan)
    {
        // Prevent recursion.
        var threadId = Thread.CurrentThread.ManagedThreadId;
        if (_queryDirectoryFileLock.IsThisThread(threadId))
            goto fastReturn;
        
        // Handle any of the possible intercepted types.
        if (fileInformationClass == FileDirectoryInformation)
            if (!HandleNtQueryDirectoryFileHook<FILE_DIRECTORY_INFORMATION>(fileHandle, fileInformation, length, fileName, restartScan, threadId))
                goto fastReturn;
        
        if (fileInformationClass == FileFullDirectoryInformation)
            if (!HandleNtQueryDirectoryFileHook<FILE_FULL_DIR_INFORMATION>(fileHandle, fileInformation, length, fileName, restartScan, threadId))
                goto fastReturn;
        
        if (fileInformationClass == FileBothDirectoryInformation)
            if (!HandleNtQueryDirectoryFileHook<FILE_BOTH_DIR_INFORMATION>(fileHandle, fileInformation, length, fileName, restartScan, threadId))
                goto fastReturn;
        
        if (fileInformationClass == FileNamesInformation)
            if (!HandleNtQueryDirectoryFileHook<FILE_NAMES_INFORMATION>(fileHandle, fileInformation, length, fileName, restartScan, threadId))
                goto fastReturn;
        
        if (fileInformationClass == FileIdBothDirectoryInformation)
            if (!HandleNtQueryDirectoryFileHook<FILE_ID_BOTH_DIR_INFORMATION>(fileHandle, fileInformation, length, fileName, restartScan, threadId))
                goto fastReturn;
        
        if (fileInformationClass == FileIdFullDirectoryInformation)
            if (!HandleNtQueryDirectoryFileHook<FILE_ID_FULL_DIR_INFORMATION>(fileHandle, fileInformation, length, fileName, restartScan, threadId))
                goto fastReturn;
        
        if (fileInformationClass == FileIdGlobalTxDirectoryInformation)
            if (!HandleNtQueryDirectoryFileHook<FILE_ID_GLOBAL_TX_DIR_INFORMATION>(fileHandle, fileInformation, length, fileName, restartScan, threadId))
                goto fastReturn;
        
        if (fileInformationClass == FileIdExtdDirectoryInformation)
            if (!HandleNtQueryDirectoryFileHook<FILE_ID_EXTD_DIR_INFORMATION>(fileHandle, fileInformation, length, fileName, restartScan, threadId))
                goto fastReturn;
        
        if (fileInformationClass == FileIdExtdBothDirectoryInformation)
            if (!HandleNtQueryDirectoryFileHook<FILE_ID_EXTD_BOTH_DIR_INFORMATION>(fileHandle, fileInformation, length, fileName, restartScan, threadId))
                goto fastReturn;

        fastReturn:
        return _ntQueryDirectoryFileHook.Original.Value.Invoke(fileHandle, @event, apcRoutine, apcContext, ioStatusBlock, 
            fileInformation, length, fileInformationClass, returnSingleEntry, fileName, restartScan);
    }

    private bool HandleNtQueryDirectoryFileHook<TDirectoryInformation>(nint fileHandle, nint fileInformation, uint length, UNICODE_STRING* fileName, int restartScan,
        int threadId)
    {
        // Check if this is a handle we picked up/are redirecting.
        // If this is not one of those handles.
        if (!_fileHandles.TryGetValue(fileHandle, out var handleItem))
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
            handleItem.AlreadyInjected = new Dictionary<nint, bool>();
        }

        // Reset state if restart is requested.
        if (restartScan == 1)
            handleItem.Reset();

        // TODO: Handle This
        if (fileName != null)
        {
            handleItem.QueryFileName = fileName->ToSpan().ToString();
            return false;
        }

        // Okay here our items.
        var items = handleItem.Items;
        _queryDirectoryFileLock.Lock(threadId);

        bool moreFiles = true;
        var remainingBytes = length;
        
        while (moreFiles)
        {
            var currentBufferPtr = (IntPtr)fileInformation;
            if (handleItem.CurrentItem < handleItem.Items.Length)
            {
                
            }
        }

        // Okay, we first query the original directory.
        _queryDirectoryFileLock.Unlock();
        return true;
    }

    // TODO: fix
    private int NtQueryDirectoryFileExHookImpl(nint fileHandle, nint @event, nint apcRoutine, nint apcContext, IO_STATUS_BLOCK* ioStatusBlock, 
        nint fileInformation, uint length, FILE_INFORMATION_CLASS fileInformationClass, int boolReturnSingleEntry, nint fileName, int boolRestartScan)
    {
        // TODO: Determine if we arrived from `NtQueryDirectoryFileHookImpl`, by checking semaphore count and prevent call again.
        throw new NotImplementedException();
    }

    /* Mod loader interface. */
    public void EnableImpl()
    {
        _ntCreateFileHook.Enable();
        _ntOpenFileHook.Enable();
        _closeHandleHook.Enable();
        _ntDeleteFileHook.Enable();
        //_ntQueryDirectoryFileHook.Enable();
        //_ntQueryDirectoryFileExHook.Enable();
    }

    public void DisableImpl()
    {
        _ntCreateFileHook.Disable();
        _ntOpenFileHook.Disable();
        _closeHandleHook.Disable();
        _ntDeleteFileHook.Disable();
        //_ntQueryDirectoryFileHook.Disable();
        //_ntQueryDirectoryFileExHook.Disable();
    }

    #region Static API
    public static void Enable() => _instance.EnableImpl();
    public static void Disable() => _instance.DisableImpl();

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static int NtCreateFileHookFn(IntPtr* fileHandle, FileAccess access,
        OBJECT_ATTRIBUTES* objectAttributes, IO_STATUS_BLOCK* ioStatus, long* allocSize,
        uint fileAttributes, FileShare share, uint createDisposition, uint createOptions, IntPtr eaBuffer,
        uint eaLength)
    {
        return _instance.NtCreateFileHookImpl(fileHandle, access, objectAttributes, ioStatus, allocSize, 
            fileAttributes, share, createDisposition, createOptions, eaBuffer, eaLength);
    }
    
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static int NtOpenFileHookFn(IntPtr* fileHandle, FileAccess access,
        OBJECT_ATTRIBUTES* objectAttributes,
        IO_STATUS_BLOCK* ioStatus, FileShare share, uint openOptions)
    {
        return _instance.NtOpenFileHookImpl(fileHandle, access, objectAttributes, ioStatus, share, openOptions);
    }
    
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static int NtDeleteFileHookFn(OBJECT_ATTRIBUTES* objectAttributes)
    {
        return _instance.NtDeleteFileHookImpl(objectAttributes);
    }
    
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static int NtQueryDirectoryFileHookFn(IntPtr fileHandle, IntPtr @event, IntPtr apcRoutine, IntPtr apcContext,
        IO_STATUS_BLOCK* ioStatusBlock, IntPtr fileInformation, uint length, FILE_INFORMATION_CLASS fileInformationClass, 
        int returnSingleEntry, UNICODE_STRING* fileName, int restartScan)
    {
        return _instance.NtQueryDirectoryFileHookImpl(fileHandle, @event, apcRoutine, apcContext,
            ioStatusBlock, fileInformation, length, fileInformationClass, returnSingleEntry,
            fileName, restartScan);
    }
    
    // TODO: fix
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static int NtQueryDirectoryFileExHookFn(IntPtr fileHandle, IntPtr @event, IntPtr apcRoutine, IntPtr apcContext,
        IO_STATUS_BLOCK* ioStatusBlock, IntPtr fileInformation, uint length, FILE_INFORMATION_CLASS fileInformationClass, 
        int returnSingleEntry, IntPtr fileName, int restartScan)
    {
        return _instance.NtQueryDirectoryFileExHookImpl(fileHandle, @event, apcRoutine, apcContext,
            ioStatusBlock, fileInformation, length, fileInformationClass, returnSingleEntry,
            fileName, restartScan);
    }
    #endregion
    
    private class OpenHandleState
    {
        /// <summary>
        /// Path to redirected/handled file or folder.
        /// </summary>
        public string FilePath { get; set; }
        
        /// <summary>
        /// File name set by call to <see cref="NtQueryDirectoryFile"/>.
        /// </summary>
        public string? QueryFileName { get; set; }
        
        /// <summary>
        /// Items to be injected into the result.
        /// </summary>
        public SpanOfCharDict<RedirectionTreeTarget>.ItemEntry[]? Items { get; set; }

        /// <summary>
        /// This dictionary holds the set of items already injected into the search results.
        /// </summary>
        public Dictionary<nint, bool>? AlreadyInjected;

        /// <summary>
        /// Index of the current item to return.
        /// </summary>
        public int CurrentItem;

        public OpenHandleState(string filePath)
        {
            FilePath = filePath;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            CurrentItem = 0;
            AlreadyInjected?.Clear();
        }
    }
}