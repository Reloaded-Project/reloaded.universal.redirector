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
            ✅ NtQueryDirectoryFileEx_Hook -> NtQueryDirectoryFileEx
                                              NtQueryDirectoryFile

            ✅ NtDeleteFile_Hook              -> NtDeleteFile
            ✅ NtQueryAttributesFile_Hook     -> NtQueryAttributesFile
            ✅ NtQueryFullAttributesFile_Hook -> NtQueryFullAttributesFile
            ✅ NtClose_Hook                   -> NtClose [needs ASM; as GC Transition can close threads; leading to infinite recursion]

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
    
    /// <summary>
    /// Shared between standard and Ex version in case one calls into the other; in which case this prevents recursion.
    /// </summary>
    private SemaphoreRecursionLock _queryDirectoryFileLock = new();
    
    private SemaphoreRecursionLock _deleteFileLock = new();
    private SemaphoreRecursionLock _createFileLock = new();
    private SemaphoreRecursionLock _openFileLock = new();
    private SemaphoreRecursionLock _queryAttributesFileLock = new();
    private SemaphoreRecursionLock _queryFullAttributesFileLock = new();
    
    private AHook<NativeHookDefinitions.NtQueryDirectoryFileEx> _ntQueryDirectoryFileExHook = null!;
    private AHook<NativeHookDefinitions.NtQueryDirectoryFile> _ntQueryDirectoryFileHook = null!;
    private AHook<NativeHookDefinitions.NtDeleteFile> _ntDeleteFileHook = null!;
    private AHook<NativeHookDefinitions.NtCreateFile> _ntCreateFileHook = null!;
    private AHook<NativeHookDefinitions.NtOpenFile> _ntOpenFileHook = null!;
    private AHook<NativeHookDefinitions.NtQueryAttributesFile> _ntQueryAttributesFileHook = null!;
    private AHook<NativeHookDefinitions.NtQueryAttributesFile> _ntQueryFullAttributesFileHook = null!;
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
        
        // Force-jit of some methods: We need this otherwise we might be stuck in infinite recursion if JIT needs 
        //                            to load a DLL to compile one of the methods with a recursion lock
        JitFunction(typeof(FileAccessServer), nameof(NtCreateFileHookFn));
        JitFunction(typeof(FileAccessServer), nameof(NtOpenFileHookFn));
        JitFunction(typeof(FileAccessServer), nameof(NtDeleteFileHookFn));
        JitFunction(typeof(FileAccessServer), nameof(NtQueryDirectoryFileHookFn));
        JitFunction(typeof(FileAccessServer), nameof(NtQueryDirectoryFileExHookFn));
        JitFunction(typeof(FileAccessServer), nameof(NtQueryAttributesFile));
        JitFunction(typeof(FileAccessServer), nameof(NtQueryFullAttributesFile));
        
        JitFunction(typeof(FileAccessServer), nameof(NtCreateFileHookImpl));
        JitFunction(typeof(FileAccessServer), nameof(NtOpenFileHookImpl));
        JitFunction(typeof(FileAccessServer), nameof(NtDeleteFileHookImpl));
        JitFunction(typeof(FileAccessServer), nameof(NtQueryDirectoryFileHookImpl));
        JitFunction(typeof(FileAccessServer), nameof(NtQueryAttributesFileImpl));
        JitFunction(typeof(FileAccessServer), nameof(NtQueryFullAttributesFileImpl));
        _queryFullAttributesFileLock.Lock(0);
        _queryFullAttributesFileLock.Unlock();
        
        // Get Hooks
        var ntdllHandle = LoadLibraryW("ntdll");
        var ntCreateFilePointer = GetProcAddress(ntdllHandle, "NtCreateFile");
        var ntOpenFilePointer = GetProcAddress(ntdllHandle, "NtOpenFile");
        var ntDeleteFilePointer = GetProcAddress(ntdllHandle, "NtDeleteFile");
        var ntQueryDirectoryFilePointer = GetProcAddress(ntdllHandle, "NtQueryDirectoryFile");
        var ntQueryDirectoryFileExPointer = GetProcAddress(ntdllHandle, "NtQueryDirectoryFileEx");
        var ntQueryAttributesFilePointer = GetProcAddress(ntdllHandle, "NtQueryAttributesFile");
        var ntQueryFullAttributesFilePointer = GetProcAddress(ntdllHandle, "NtQueryFullAttributesFile");

        // Kick off the server
        HookMethod(ref _ntCreateFileHook, nameof(NtCreateFileHookFn), "NtCreateFile", hooks, log, ntCreateFilePointer);
        HookMethod(ref _ntOpenFileHook, nameof(NtOpenFileHookFn), "NtOpenFile", hooks, log, ntOpenFilePointer);
        HookMethod(ref _ntDeleteFileHook, nameof(NtDeleteFileHookFn), "NtDeleteFile", hooks, log, ntDeleteFilePointer);
        HookMethod(ref _ntQueryDirectoryFileHook, nameof(NtQueryDirectoryFileHookFn), "NtQueryDirectoryFile", hooks, log, ntQueryDirectoryFilePointer);
        HookMethod(ref _ntQueryDirectoryFileExHook, nameof(NtQueryDirectoryFileExHookFn), "NtQueryDirectoryFileEx", hooks, log, ntQueryDirectoryFileExPointer);
        HookMethod(ref _ntQueryAttributesFileHook, nameof(NtQueryAttributesFile), "NtQueryAttributesFile", hooks, log, ntQueryAttributesFilePointer);
        HookMethod(ref _ntQueryFullAttributesFileHook, nameof(NtQueryFullAttributesFile), "NtQueryFullAttributesFile", hooks, log, ntQueryFullAttributesFilePointer);

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
        ReadOnlySpan<char> path = default;

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
            path = ExtractPathFromObjectAttributes(attributes);
            if (!TryResolvePath(path, out string newFilePath))
            {
                PrintFileLoadIfNeeded(path);
                _createFileLock.Unlock();
                goto fastReturn;
            }

            PrintFileRedirectIfNeeded(path, newFilePath);
            fixed (char* address = newFilePath)
            {
                // Backup original string.
                var originalObjectName = attributes->ObjectName;
                var originalDirectory = attributes->RootDirectory;

                // Call function with new file path.
                _ = new UNICODE_STRING(address, newFilePath.Length, attributes);
                var returnValue = _ntCreateFileHook.Original.Value.Invoke(fileHandle, access, objectAttributes, ioStatus,
                    allocSize, fileattributes, share, createDisposition, createOptions, eaBuffer, eaLength);

                if (returnValue == 0)
                    _fileHandles[*fileHandle] = new OpenHandleState(path.ToString());
                
                // Reset original string.
                attributes->ObjectName = originalObjectName;
                attributes->RootDirectory = originalDirectory;
                _createFileLock.Unlock();
                return returnValue;
            }
        }

        fastReturn:
        var result = _ntCreateFileHook.Original.Value.Invoke(fileHandle, access, objectAttributes, ioStatus,
            allocSize, fileattributes, share, createDisposition, createOptions, eaBuffer, eaLength);
        
        if (path != default && result == 0)
            _fileHandles[*fileHandle] = new OpenHandleState(path.ToString());

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int NtOpenFileHookImpl(IntPtr* fileHandle, FileAccess access, OBJECT_ATTRIBUTES* objectAttributes, 
        IO_STATUS_BLOCK* ioStatus, FileShare share, uint openOptions)
    {
        DequeueHandles();
        ReadOnlySpan<char> path = default;
        
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
            path = ExtractPathFromObjectAttributes(attributes);
            if (!TryResolvePath(path, out string newFilePath))
            {
                PrintFileLoadIfNeeded(path);
                _openFileLock.Unlock();
                goto fastReturn;
            }

            PrintFileRedirectIfNeeded(path, newFilePath);
            fixed (char* address = newFilePath)
            {
                // Backup original string.
                var originalObjectName = attributes->ObjectName;
                var originalDirectory = attributes->RootDirectory;

                // Call function with new file path.
                _ = new UNICODE_STRING(address, newFilePath.Length, attributes);
                var returnValue = _ntOpenFileHook.Original.Value.Invoke(fileHandle, access, objectAttributes, ioStatus, share, openOptions);
                
                if (returnValue == 0)
                    _fileHandles[*fileHandle] = new OpenHandleState(path.ToString());
                
                // Reset original string.
                attributes->ObjectName = originalObjectName;
                attributes->RootDirectory = originalDirectory;
                _openFileLock.Unlock();
                return returnValue;
            }
        }
        
        fastReturn:
        var result = _ntOpenFileHook.Original.Value.Invoke(fileHandle, access, objectAttributes, ioStatus, share, openOptions);
        
        if (path != default && result == 0)
            _fileHandles[*fileHandle] = new OpenHandleState(path.ToString());

        return result;
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
    
    private int NtQueryAttributesFileImpl(OBJECT_ATTRIBUTES* attributes, nint fileInformation)
    {
        // Prevent recursion.
        var threadId = Thread.CurrentThread.ManagedThreadId;
        if (_queryAttributesFileLock.IsThisThread(threadId))
            goto fastReturn;
        
        // Get name of file to be loaded.
        if (attributes->ObjectName == null)
            goto fastReturn;
        
        _queryAttributesFileLock.Lock(threadId);

        {
            DequeueHandles();
            var path = ExtractPathFromObjectAttributes(attributes);
            if (!TryResolvePath(path, out string newFilePath))
            {
                PrintGetAttributeIfNeeded(path);
                _queryAttributesFileLock.Unlock();
                goto fastReturn; 
            }

            PrintAttributeRedirectIfNeeded(path, newFilePath);
            fixed (char* address = newFilePath)
            {
                // Backup original string.
                var originalObjectName = attributes->ObjectName;
                var originalDirectory  = attributes->RootDirectory;

                // Call function with new file path.
                _ = new UNICODE_STRING(address, newFilePath.Length, attributes);
                var returnValue = _ntQueryAttributesFileHook.Original.Value.Invoke(attributes, fileInformation);
                
                // Reset original string.
                attributes->ObjectName    = originalObjectName;
                attributes->RootDirectory = originalDirectory;
                _queryAttributesFileLock.Unlock();
                return returnValue;
            }
        }
        
        fastReturn:
        return _ntQueryAttributesFileHook.Original.Value.Invoke(attributes, fileInformation);
    }

    private int NtQueryFullAttributesFileImpl(OBJECT_ATTRIBUTES* attributes, nint fileInformation)
    {
        // Prevent recursion.
        var threadId = Thread.CurrentThread.ManagedThreadId;
        if (_queryFullAttributesFileLock.IsThisThread(threadId))
            goto fastReturn;
        
        // Get name of file to be loaded.
        if (attributes->ObjectName == null)
            goto fastReturn;
        
        _queryFullAttributesFileLock.Lock(threadId);

        {
            DequeueHandles();
            var path = ExtractPathFromObjectAttributes(attributes);
            if (!TryResolvePath(path, out string newFilePath))
            {
                PrintGetAttributeIfNeeded(path);
                _queryFullAttributesFileLock.Unlock();
                goto fastReturn; 
            }

            PrintAttributeRedirectIfNeeded(path, newFilePath);
            fixed (char* address = newFilePath)
            {
                // Backup original string.
                var originalObjectName = attributes->ObjectName;
                var originalDirectory  = attributes->RootDirectory;

                // Call function with new file path.
                _ = new UNICODE_STRING(address, newFilePath.Length, attributes);
                var returnValue = _ntQueryFullAttributesFileHook.Original.Value.Invoke(attributes, fileInformation);
                
                // Reset original string.
                attributes->ObjectName    = originalObjectName;
                attributes->RootDirectory = originalDirectory;
                _queryFullAttributesFileLock.Unlock();
                return returnValue;
            }
        }
        
        fastReturn:
        return _ntQueryFullAttributesFileHook.Original.Value.Invoke(attributes, fileInformation);
    }

    /* Mod loader interface. */
    public void EnableImpl()
    {
        _ntCreateFileHook.Enable();
        _ntOpenFileHook.Enable();
        _closeHandleHook.Enable();
        _ntDeleteFileHook.Enable();
        _ntQueryDirectoryFileHook.Enable();
        _ntQueryDirectoryFileExHook.Enable();
        _ntQueryFullAttributesFileHook.Enable();
        _ntQueryAttributesFileHook.Enable();
    }

    public void DisableImpl()
    {
        _ntCreateFileHook.Disable();
        _ntOpenFileHook.Disable();
        _closeHandleHook.Disable();
        _ntDeleteFileHook.Disable();
        _ntQueryDirectoryFileHook.Disable();
        _ntQueryDirectoryFileExHook.Disable();
        _ntQueryFullAttributesFileHook.Disable();
        _ntQueryAttributesFileHook.Disable();
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
#if DEBUG
        try
        {
#endif
            return _instance.NtCreateFileHookImpl(fileHandle, access, objectAttributes, ioStatus, allocSize, 
                fileAttributes, share, createDisposition, createOptions, eaBuffer, eaLength);
#if DEBUG
        }
        catch (Exception e)
        {
            _instance.LogFatalError(nameof(NtCreateFileHookFn), e);
            throw;
        }
#endif
    }
    
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static int NtOpenFileHookFn(IntPtr* fileHandle, FileAccess access,
        OBJECT_ATTRIBUTES* objectAttributes,
        IO_STATUS_BLOCK* ioStatus, FileShare share, uint openOptions)
    {
#if DEBUG
        try
        {
#endif
        return _instance.NtOpenFileHookImpl(fileHandle, access, objectAttributes, ioStatus, share, openOptions);
#if DEBUG
        }
        catch (Exception e)
        {
            _instance.LogFatalError(nameof(NtOpenFileHookFn), e);
            throw;
        }
#endif
    }
    
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static int NtDeleteFileHookFn(OBJECT_ATTRIBUTES* objectAttributes)
    {
#if DEBUG
        try
        {
#endif
        return _instance.NtDeleteFileHookImpl(objectAttributes);
#if DEBUG
        }
        catch (Exception e)
        {
            _instance.LogFatalError(nameof(NtDeleteFileHookFn), e);
            throw;
        }
#endif
    }
    
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static int NtQueryDirectoryFileHookFn(IntPtr fileHandle, IntPtr @event, IntPtr apcRoutine, IntPtr apcContext,
        IO_STATUS_BLOCK* ioStatusBlock, IntPtr fileInformation, uint length, FILE_INFORMATION_CLASS fileInformationClass, 
        int returnSingleEntry, UNICODE_STRING* fileName, int restartScan)
    {
#if DEBUG
        try
        {
#endif
        return _instance.NtQueryDirectoryFileHookImpl(fileHandle, @event, apcRoutine, apcContext,
            ioStatusBlock, fileInformation, length, fileInformationClass, returnSingleEntry,
            fileName, restartScan);
#if DEBUG
        }
        catch (Exception e)
        {
            _instance.LogFatalError(nameof(NtQueryDirectoryFileHookFn), e);
            throw;
        }
#endif
    }
    
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static int NtQueryDirectoryFileExHookFn(IntPtr fileHandle, IntPtr @event, IntPtr apcRoutine, IntPtr apcContext,
        IO_STATUS_BLOCK* ioStatusBlock, IntPtr fileInformation, uint length, FILE_INFORMATION_CLASS fileInformationClass, 
        int queryFlags, UNICODE_STRING* fileName)
    {
#if DEBUG
        try
        {
#endif
        return _instance.NtQueryDirectoryFileExHookImpl(fileHandle, @event, apcRoutine, apcContext,
            ioStatusBlock, fileInformation, length, fileInformationClass, queryFlags,
            fileName);
#if DEBUG
        }
        catch (Exception e)
        {
            _instance.LogFatalError(nameof(NtQueryDirectoryFileExHookFn), e);
            throw;
        }
#endif
    }
    
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static int NtQueryAttributesFile(OBJECT_ATTRIBUTES* files, IntPtr fileInformation)
    {
#if DEBUG
        try
        {
#endif
        return _instance.NtQueryAttributesFileImpl(files, fileInformation);
#if DEBUG
        }
        catch (Exception e)
        {
            _instance.LogFatalError(nameof(NtQueryAttributesFile), e);
            throw;
        }
#endif
    }
    
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static int NtQueryFullAttributesFile(OBJECT_ATTRIBUTES* files, IntPtr fileInformation)
    {
#if DEBUG
        try
        {
#endif
        return _instance.NtQueryFullAttributesFileImpl(files, fileInformation);
#if DEBUG
        }
        catch (Exception e)
        {
            _instance.LogFatalError(nameof(NtQueryFullAttributesFile), e);
            throw;
        }
#endif
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
        public string QueryFileName { get; set; } = "*";
        
        /// <summary>
        /// Items to be injected into the result.
        /// </summary>
        public SpanOfCharDict<RedirectionTreeTarget>.ItemEntry[]? Items { get; set; }

        /// <summary>
        /// This dictionary holds the set of items already injected into the search results.
        /// </summary>
        public SpanOfCharDict<bool>? AlreadyInjected;

        /// <summary>
        /// Index of the current item to return.
        /// </summary>
        public int CurrentItem;

        /// <summary>
        /// Forces a scan restart on next API call to original function.
        /// </summary>
        public int ForceRestartScan;

        public OpenHandleState(string filePath)
        {
            FilePath = filePath;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Restart()
        {
            CurrentItem = 0;
            AlreadyInjected?.Clear();
            ForceRestartScan = 1;
        }

        /// <summary>
        /// Gets the <see cref="ForceRestartScan"/> value, resetting it as needed.
        /// </summary>
        public int GetForceRestartScan()
        {
            if (ForceRestartScan > 0)
            {
                ForceRestartScan = 0;
                return 1;
            }

            return 0;
        }
    }
}