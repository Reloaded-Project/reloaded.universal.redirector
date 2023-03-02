using FileEmulationFramework.Lib.IO;
using Reloaded.Hooks.Definitions.Structs;
using Reloaded.Universal.Redirector.Lib.Utility.Native.Structures;

namespace Reloaded.Universal.Redirector.Lib.Utility.Native;

/// <summary>
/// All definitions related to the functions we hook throughout the final solution.
/// </summary>
public class NativeHookDefinitions
{
    /// <summary>
    /// Creates a new file or directory, or opens an existing file, device, directory, or volume.
    /// (The description here is a partial, lazy copy from MSDN)
    /// </summary>
    [Hooks.Definitions.X64.Function(Hooks.Definitions.X64.CallingConventions.Microsoft)]
    [Hooks.Definitions.X86.Function(Hooks.Definitions.X86.CallingConventions.Stdcall)]
    public struct NtCreateFile
    {
        /// <summary/>
        public FuncPtr
        <
            BlittablePointer<IntPtr>,               // handle
            FileAccess,                             // access 
            BlittablePointer<Native.OBJECT_ATTRIBUTES>,    // objectAttributes
            BlittablePointer<Native.IO_STATUS_BLOCK>,      // ioStatus
            BlittablePointer<long>,                 // allocSize 
            uint,                                   // fileAttributes
            FileShare,                              // share
            uint,                                   // createDisposition
            uint,                                   // createOptions
            IntPtr,                                 // eaBuffer
            uint,                                   // eaLength
            int                                     // Return Value
        > Value;    
    }
    
    /// <summary>
    /// Creates a new file or directory, or opens an existing file, device, directory, or volume.
    /// (The description here is a partial, lazy copy from MSDN)
    /// </summary>
    [Hooks.Definitions.X64.Function(Hooks.Definitions.X64.CallingConventions.Microsoft)]
    [Hooks.Definitions.X86.Function(Hooks.Definitions.X86.CallingConventions.Stdcall)]
    public struct NtOpenFile
    {
        /// <summary/>
        public FuncPtr
        <
            BlittablePointer<IntPtr>,               // handle
            FileAccess,                             // access 
            BlittablePointer<Native.OBJECT_ATTRIBUTES>,    // objectAttributes
            BlittablePointer<Native.IO_STATUS_BLOCK>,      // ioStatus
            FileShare,                              // share
            uint,                                   // openOptions
            int                                     // Return Value
        > Value;    
    }
    
    /// <summary>
    /// Deletes a file or directory from disk.
    /// </summary>
    [Hooks.Definitions.X64.Function(Hooks.Definitions.X64.CallingConventions.Microsoft)]
    [Hooks.Definitions.X86.Function(Hooks.Definitions.X86.CallingConventions.Stdcall)]
    public struct NtDeleteFile
    {
        /// <summary/>
        public FuncPtr
        <
            BlittablePointer<Native.OBJECT_ATTRIBUTES>,    // objectAttributes
            int                                     // Return Value
        > Value;    
    }
    
    /// <summary>
    /// The NtQueryDirectoryFile routine returns various kinds of information about files in the directory specified by a given file handle.
    /// </summary>
    [Hooks.Definitions.X64.Function(Hooks.Definitions.X64.CallingConventions.Microsoft)]
    [Hooks.Definitions.X86.Function(Hooks.Definitions.X86.CallingConventions.Stdcall)]
    public struct NtQueryDirectoryFile
    {
        /// <summary/>
        public FuncPtr
        <
            IntPtr,   // fileHandle
            IntPtr,   // event,
            IntPtr,   // apcRoutine
            IntPtr,   // apcContext
            BlittablePointer<Native.IO_STATUS_BLOCK>, // ioStatusBlock
            IntPtr,   // fileInformation [buffer]
            uint,     // length [of buffer]
            Native.FILE_INFORMATION_CLASS, // fileInformationClass
            int,      // returnSingleEntry
            BlittablePointer<Native.UNICODE_STRING>, // fileName
            int, // restart scan
            int  // Return Value
        > Value;    
    }
    
    /// <summary>
    /// The NtQueryDirectoryFile routine returns various kinds of information about files in the directory specified by a given file handle.
    /// </summary>
    [Hooks.Definitions.X64.Function(Hooks.Definitions.X64.CallingConventions.Microsoft)]
    [Hooks.Definitions.X86.Function(Hooks.Definitions.X86.CallingConventions.Stdcall)]
    public struct NtQueryDirectoryFileEx
    {
        /// <summary/>
        public FuncPtr
        <
            IntPtr,   // fileHandle
            IntPtr,   // event,
            IntPtr,   // apcRoutine
            IntPtr,   // apcContext
            BlittablePointer<Native.IO_STATUS_BLOCK>, // ioStatusBlock
            IntPtr,   // fileInformation [buffer]
            uint,     // length [of buffer]
            Native.FILE_INFORMATION_CLASS, // fileInformationClass
            int,      // queryFlags
            BlittablePointer<Native.UNICODE_STRING>, // fileName
            int  // Return Value
        > Value;    
    }
}