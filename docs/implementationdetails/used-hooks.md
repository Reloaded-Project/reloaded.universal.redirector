# Hook Flowchart

This page provides a flowchart showing how common Win32 and NT File APIs are handled by the hooks in this mod under the hood.

The goal is to handle every API which:  

- Accepts a File Path.  
    - In this case we set a new path to our redirected file.  

- Returns Files at Given Path.  
    - In this case we inject new files into the result.  

```mermaid
flowchart LR
    subgraph Win32
    
    %% Definitions
    FindFirstFileA
    FindFirstFileExA
    FindFirstFileW
    FindFirstFileExW
    FindFirstFileExFromAppW
    FindNextFileA
    FindNextFileW
    
    CreateDirectoryA
    CreateDirectoryW
    CreateFileA
    CreateFileW
    CreateFile2
    CreateFile2FromAppW
    CreateFileFromAppW
    CreateDirectoryExW
    CreateDirectoryFromAppW
    DeleteFileA
    DeleteFileW
    DeleteFileFromAppW
    GetCompressedFileSizeA
    GetCompressedFileSizeW
    CloseHandle

    GetFileAttributesA
    GetFileAttributesExA
    GetFileAttributesExFromAppW
    GetFileAttributesExW
    GetFileAttributesW
    SetFileAttributesA
    SetFileAttributesFromAppW
    SetFileAttributesW

    RemoveDirectoryA
    RemoveDirectoryFromAppW
    RemoveDirectoryW

    %%% Win32 Internal Redirects
    FindFirstFileA --> FindFirstFileExW
    FindFirstFileExA --> FindFirstFileExW
    FindFirstFileExFromAppW --> FindFirstFileExW
    FindNextFileA --> FindNextFileW
    CreateDirectoryA --> CreateDirectoryW
    CreateFile2FromAppW --> CreateFile2
    CreateDirectoryFromAppW --> CreateDirectoryExW
    CreateFileFromAppW --> CreateFile2FromAppW
    DeleteFileFromAppW --> DeleteFileW
    DeleteFileA --> DeleteFileW
    GetCompressedFileSizeA --> GetCompressedFileSizeW
    GetFileAttributesA --> GetFileAttributesW
    GetFileAttributesExA --> GetFileAttributesExW
    GetFileAttributesExFromAppW --> GetFileAttributesExW
    RemoveDirectoryA --> RemoveDirectoryW
    RemoveDirectoryFromAppW --> RemoveDirectoryW
    SetFileAttributesFromAppW --> SetFileAttributesW
    SetFileAttributesA --> SetFileAttributesW
    end

    subgraph NT API
    %% Definitions
    NtCreateFile
    NtOpenFile
    NtQueryDirectoryFile
    NtQueryDirectoryFileEx
    NtDeleteFile
    NtQueryAttributesFile
    NtQueryFullAttributesFile
    NtClose

    %%% Win32 -> NT API
    FindFirstFileExW --> NtOpenFile
    FindFirstFileExW --> NtQueryDirectoryFileEx
    FindFirstFileW --> NtOpenFile
    FindFirstFileW --> NtQueryDirectoryFileEx
    FindNextFileW --> NtQueryDirectoryFileEx
    CreateFileA --> NtCreateFile
    CreateFileW --> NtCreateFile
    CreateFile2 --> NtCreateFile
    CreateDirectoryW --> NtCreateFile
    CreateDirectoryExW --> NtOpenFile
    CreateDirectoryExW --> NtCreateFile
    DeleteFileW --> NtOpenFile
    RemoveDirectoryW --> NtOpenFile
    GetCompressedFileSizeW --> NtOpenFile
    CloseHandle --> NtClose
    GetFileAttributesExW --> NtQueryFullAttributesFile
    GetFileAttributesW --> NtQueryAttributesFile
    SetFileAttributesW --> NtOpenFile
    end

    %%% Hooks
    subgraph Hooks
    NtCreateFile_Hook
    NtOpenFile_Hook
    NtQueryDirectoryFileEx_Hook
    NtDeleteFile_Hook
    NtQueryAttributesFile_Hook
    NtQueryFullAttributesFile_Hook
    NtClose_Hook

    %% NT API -> Hooks
    NtCreateFile --> NtCreateFile_Hook
    NtOpenFile --> NtOpenFile_Hook
    NtQueryDirectoryFileEx --> NtQueryDirectoryFileEx_Hook
    NtQueryDirectoryFile --> NtQueryDirectoryFile_Hook

    NtDeleteFile --> NtDeleteFile_Hook
    NtQueryAttributesFile --> NtQueryAttributesFile_Hook
    NtQueryFullAttributesFile --> NtQueryFullAttributesFile_Hook
    NtClose --> NtClose_Hook
    end
```

Confirmed by looking at Windows (7, 8.1, 10 & 11) binaries and Wine 7.x source. Older Windows versions are no longer
supported by me, MSFT or .NET runtime.

## Wine & Windows < 10 Note

On Windows versions greater 10 and greater, `NtQueryDirectoryFileEx` API becomes available and `NtQueryDirectoryFile` acts as a wrapper around it. On Wine and earlier Windows, only `NtQueryDirectoryFile` exists.

In this VFS we hook both (just in case), and detect if one recurses to the other using a semaphore; if we're recursing from `NtQueryDirectoryFile` to `NtQueryDirectoryFileEx`, we skip our hook code.