using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Reloaded.Hooks.Definitions;
using Reloaded.Universal.Redirector.Lib.Utility;
using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace Reloaded.Universal.Redirector;

/// <summary>
/// Intercepts I/O access on the Win32
/// </summary>
public unsafe class FileAccessServer
{
/*
    Sewer's Grand API Mapping Table:
    
    This comment shows a listing of hooks and their corresponding APIs they successfully handle.
    i.e. Often API 1 will call API 2 under the hood in Windows.
    
    Confirmed by looking at Windows (7, 8.1, 10 & 11), older versions are no longer 
    supported by me, MSFT or .NET runtime.
    
    Windows Native APIs:
        NtCreateFile_Hook           -> NtCreateFile 
        NtOpenFile_Hook             -> NtOpenFile
        NtQueryDirectoryFileEx_Hook -> NtQueryDirectoryFileEx
                                       NtQueryDirectoryFile
                                       
        Note: NtQueryDirectoryFileEx on Win10 >=, hook NtQueryDirectoryFile on Wine and Earlier
    
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
            
    Potentially Not Hooking or only Hooking based on OS: 
        NtQueryAttributesFile 
        NtQueryFullAttributesFile
    
        Justification: 
            
            These attributes are considered internal Windows APIs and can be changed or removed 
            between individual versions of Windows. [and in fact, they have been; NtQueryFullAttributesFile is no
            longer with us].  
            
            End user applications should never hit these APIs, directly or indirectly without going through
            any of the other hooked APIs; so this should be okay.
*/
    
    private static Lib.Redirector _redirector;
    private static RedirectorApi _redirectorApi = null!;
    private static IHook<Native.NtCreateFile> _ntCreateFileHook = null!;
    private const string _prefix = "\\??\\";

    public static void Initialize(IReloadedHooks hooks, Lib.Redirector redirector, RedirectorApi redirectorApi)
    {
        _redirector = redirector;
        _redirectorApi = redirectorApi;

        // Get Hooks
        var ntdllHandle = Native.LoadLibraryW("ntdll");
        var ntCreateFilePointer = Native.GetProcAddress(ntdllHandle, "NtCreateFile");

        // Kick off the server
        if (ntCreateFilePointer != IntPtr.Zero)
            _ntCreateFileHook = hooks.CreateHook<Native.NtCreateFile>((delegate* unmanaged[Stdcall]<IntPtr*, FileAccess, Native.OBJECT_ATTRIBUTES*, Native.IO_STATUS_BLOCK*, long*, uint, FileShare, uint, uint, IntPtr, uint, int>)&NtCreateFileHookFn, (long)ntCreateFilePointer).Activate();
    }

    /* Hooks */

    [UnmanagedCallersOnly(CallConvs = new []{ typeof(CallConvStdcall) })]
    private static int NtCreateFileHookFn(IntPtr* fileHandle, FileAccess access, Native.OBJECT_ATTRIBUTES* objectAttributes, Native.IO_STATUS_BLOCK* ioStatus, long* allocSize, uint fileattributes, FileShare share, uint createDisposition, uint createOptions, IntPtr eaBuffer, uint eaLength)
    {
        // Get name of file to be loaded.
        var attributes = objectAttributes;
        if (attributes->ObjectName == null)
            return _ntCreateFileHook.OriginalFunction.Value.Invoke(fileHandle, access, objectAttributes, ioStatus,
                allocSize, fileattributes, share, createDisposition, createOptions, eaBuffer, eaLength);
            
        if (!TryGetNewPath(attributes->ObjectName->ToString(), out string newFilePath))
            return _ntCreateFileHook.OriginalFunction.Value.Invoke(fileHandle, access, objectAttributes, ioStatus,
                allocSize, fileattributes, share, createDisposition, createOptions, eaBuffer, eaLength);

        var newObjectPath = _prefix + newFilePath;
        fixed (char* address = newObjectPath)
        {
            // Backup original string.
            var originalObjectName = attributes->ObjectName;
            var originalDirectory  = attributes->RootDirectory;

            // Set new file path
            var newObjectName = new Native.UNICODE_STRING(address, newObjectPath.Length);
            attributes->ObjectName = &newObjectName;
            attributes->RootDirectory = IntPtr.Zero;

            // Call function with new file path.
            var returnValue = _ntCreateFileHook.OriginalFunction.Value.Invoke(fileHandle, access, objectAttributes, ioStatus, allocSize, fileattributes, share, createDisposition, createOptions, eaBuffer, eaLength);

            // Reset original string.
            attributes->ObjectName    = originalObjectName;
            attributes->RootDirectory = originalDirectory;
            return returnValue;
        }
    }

    private static bool TryGetNewPath(string oldFilePath, out string newFilePath)
    {
        oldFilePath = oldFilePath.TrimStart(_prefix);
        if (!String.IsNullOrEmpty(oldFilePath))
        {
            oldFilePath = Path.GetFullPath(oldFilePath);

            // Get redirected path.
            _redirectorApi.Loading?.Invoke(oldFilePath);

            throw new NotImplementedException();
            /*
            if (_redirector.TryRedirect(oldFilePath, out newFilePath))
            {
                string newPath = newFilePath;
                _redirectorApi.Redirecting?.Invoke(oldFilePath, newPath);

                return true;
            }
            */
        }

        newFilePath = oldFilePath;
        return false;
    }

    /* Mod loader interface. */
    public static void Enable()  => _ntCreateFileHook.Enable();
    public static void Disable() => _ntCreateFileHook.Disable();
}