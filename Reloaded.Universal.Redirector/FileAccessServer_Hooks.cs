using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FileEmulationFramework.Lib.Utilities;
using FileEmulationFramework.Utilities;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Universal.Redirector.Lib;
using Reloaded.Universal.Redirector.Lib.Utility;
using Reloaded.Universal.Redirector.Lib.Utility.Native;
using Reloaded.Universal.Redirector.Structures;
using static Reloaded.Universal.Redirector.Structures.NativeIntList;
using Native = Reloaded.Universal.Redirector.Lib.Utility.Native.Native;

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
    private readonly Dictionary<nint, int> _handleMap = new();

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
        
        // Get Hooks
        var ntdllHandle = Native.LoadLibraryW("ntdll");
        var ntCreateFilePointer = Native.GetProcAddress(ntdllHandle, "NtCreateFile");
        var ntOpenFilePointer = Native.GetProcAddress(ntdllHandle, "NtOpenFile");
        var ntDeleteFilePointer = Native.GetProcAddress(ntdllHandle, "NtDeleteFile");
        var ntQueryDirectoryFilePointer = Native.GetProcAddress(ntdllHandle, "NtQueryDirectoryFile");
        var ntQueryDirectoryFileExPointer = Native.GetProcAddress(ntdllHandle, "NtQueryDirectoryFileEx");

        // Kick off the server
        HookMethod(ref _ntCreateFileHook, nameof(NtCreateFileHookFn), "NtCreateFile", hooks, log, ntCreateFilePointer);
        HookMethod(ref _ntOpenFileHook, nameof(NtOpenFileHookFn), "NtOpenFile", hooks, log, ntOpenFilePointer);
        HookMethod(ref _ntDeleteFileHook, nameof(NtDeleteFileHookFn), "NtDeleteFile", hooks, log, ntDeleteFilePointer);
        HookMethod(ref _ntQueryDirectoryFileHook, nameof(NtQueryDirectoryFileHookFn), "NtQueryDirectoryFile", hooks, log, ntQueryDirectoryFilePointer);
        //HookMethod(ref _ntQueryDirectoryFileExHook, nameof(NtQueryDirectoryFileExHookFn), "NtQueryDirectoryFileEx", hooks, log, ntQueryDirectoryFilePointer);

        // We need to cook some assembly for NtClose, because Native->Managed
        // transition can invoke thread setup code which will call CloseHandle again
        // and that will lead to infinite recursion; also unable to do Coop <=> Preemptive GC transition

        // Win32 APIs are guaranteed to exist.

        var kernel32Handle = Native.LoadLibraryW("kernel32");
        var closeHandle = Native.GetProcAddress(kernel32Handle, "CloseHandle");
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
            if (!_handleMap.Remove(item, out var value))
                continue;

            // TODO: Something with removed handles.
            //value.File.CloseHandle(item, value);
            //_logger.Debug("[FileAccessServer] Closed emulated handle: {0}, File: {1}", item, value.FilePath);
        }

        nativeList.NumItems = 0;
        nativeList.CurrentThread = DefaultThreadHandle;
    }

    /* Hooks */

    // Note: Some code below might be repeated for perf. reasons.
    // Note 2: Try-Finally below is now allowed because it prevents inlining.

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int NtCreateFileHookImpl(IntPtr* fileHandle, FileAccess access, Native.OBJECT_ATTRIBUTES* objectAttributes, Native.IO_STATUS_BLOCK* ioStatus, long* allocSize, uint fileattributes, FileShare share, uint createDisposition, uint createOptions, IntPtr eaBuffer, uint eaLength)
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
            if (!TryResolvePath(attributes, out string newFilePath))
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
                _ = new Native.UNICODE_STRING(address, newFilePath.Length, attributes);
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
    private int NtOpenFileHookImpl(IntPtr* fileHandle, FileAccess access, Native.OBJECT_ATTRIBUTES* objectAttributes, 
        Native.IO_STATUS_BLOCK* ioStatus, FileShare share, uint openOptions)
    {
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
            DequeueHandles();
            if (!TryResolvePath(attributes, out string newFilePath))
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
                _ = new Native.UNICODE_STRING(address, newFilePath.Length, attributes);
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
    private int NtDeleteFileHookImpl(Native.OBJECT_ATTRIBUTES* objectAttributes)
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
                _ = new Native.UNICODE_STRING(address, newFilePath.Length, attributes);
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
        Native.IO_STATUS_BLOCK* ioStatusBlock, nint fileInformation, uint length, Native.FILE_INFORMATION_CLASS fileInformationClass, 
        int returnSingleEntry, Native.UNICODE_STRING* fileName, int restartScan)
    {

        return _ntQueryDirectoryFileHook.Original.Value.Invoke(fileHandle, @event, apcRoutine, apcContext, ioStatusBlock, 
            fileInformation, length, fileInformationClass, returnSingleEntry, fileName, restartScan);
    }
    
    // TODO: fix
    private int NtQueryDirectoryFileExHookImpl(nint fileHandle, nint @event, nint apcRoutine, nint apcContext, Native.IO_STATUS_BLOCK* ioStatusBlock, 
        nint fileInformation, uint length, Native.FILE_INFORMATION_CLASS fileInformationClass, int boolReturnSingleEntry, nint fileName, int boolRestartScan)
    {
        // TODO: Determine if we arrived from `NtQueryDirectoryFileHookImpl`
        
        throw new NotImplementedException();
        
    }

    /* Mod loader interface. */
    public void EnableImpl()
    {
        _ntCreateFileHook.Enable();
        _ntOpenFileHook.Enable();
        _closeHandleHook.Enable();
        _ntDeleteFileHook.Enable();
        _ntQueryDirectoryFileHook.Enable();
        //_ntQueryDirectoryFileExHook.Enable();
    }

    public void DisableImpl()
    {
        _ntCreateFileHook.Disable();
        _ntOpenFileHook.Disable();
        _closeHandleHook.Disable();
        _ntDeleteFileHook.Disable();
        _ntQueryDirectoryFileHook.Disable();
        //_ntQueryDirectoryFileExHook.Disable();
    }

    #region Static API
    public static void Enable() => _instance.EnableImpl();
    public static void Disable() => _instance.DisableImpl();

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static int NtCreateFileHookFn(IntPtr* fileHandle, FileAccess access,
        Native.OBJECT_ATTRIBUTES* objectAttributes, Native.IO_STATUS_BLOCK* ioStatus, long* allocSize,
        uint fileAttributes, FileShare share, uint createDisposition, uint createOptions, IntPtr eaBuffer,
        uint eaLength)
    {
        return _instance.NtCreateFileHookImpl(fileHandle, access, objectAttributes, ioStatus, allocSize, 
            fileAttributes, share, createDisposition, createOptions, eaBuffer, eaLength);
    }
    
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static int NtOpenFileHookFn(IntPtr* fileHandle, FileAccess access,
        Native.OBJECT_ATTRIBUTES* objectAttributes,
        Native.IO_STATUS_BLOCK* ioStatus, FileShare share, uint openOptions)
    {
        return _instance.NtOpenFileHookImpl(fileHandle, access, objectAttributes, ioStatus, share, openOptions);
    }
    
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static int NtDeleteFileHookFn(Native.OBJECT_ATTRIBUTES* objectAttributes)
    {
        return _instance.NtDeleteFileHookImpl(objectAttributes);
    }
    
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static int NtQueryDirectoryFileHookFn(IntPtr fileHandle, IntPtr @event, IntPtr apcRoutine, IntPtr apcContext,
        Native.IO_STATUS_BLOCK* ioStatusBlock, IntPtr fileInformation, uint length, Native.FILE_INFORMATION_CLASS fileInformationClass, 
        int returnSingleEntry, Native.UNICODE_STRING* fileName, int restartScan)
    {
        return _instance.NtQueryDirectoryFileHookImpl(fileHandle, @event, apcRoutine, apcContext,
            ioStatusBlock, fileInformation, length, fileInformationClass, returnSingleEntry,
            fileName, restartScan);
    }
    
    // TODO: fix
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static int NtQueryDirectoryFileExHookFn(IntPtr fileHandle, IntPtr @event, IntPtr apcRoutine, IntPtr apcContext,
        Native.IO_STATUS_BLOCK* ioStatusBlock, IntPtr fileInformation, uint length, Native.FILE_INFORMATION_CLASS fileInformationClass, 
        int returnSingleEntry, IntPtr fileName, int restartScan)
    {
        return _instance.NtQueryDirectoryFileExHookImpl(fileHandle, @event, apcRoutine, apcContext,
            ioStatusBlock, fileInformation, length, fileInformationClass, returnSingleEntry,
            fileName, restartScan);
    }
    #endregion
}