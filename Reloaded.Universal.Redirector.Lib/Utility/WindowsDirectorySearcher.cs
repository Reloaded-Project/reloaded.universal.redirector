using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using static System.IO.FileAttributes;
using static Reloaded.Universal.Redirector.Lib.Utility.Native.Native;
using static Reloaded.Universal.Redirector.Lib.Utility.Native.Native.ACCESS_MASK;
using static Reloaded.Universal.Redirector.Lib.Utility.Native.Native.CreateDisposition;
using static Reloaded.Universal.Redirector.Lib.Utility.Native.Native.CreateOptions;

[assembly: InternalsVisibleTo("Reloaded.Universal.Redirector.Benchmarks")]
namespace Reloaded.Universal.Redirector.Lib.Utility;

/// <summary>
/// Class that provides WinAPI based utility methods for fast file enumeration in directories.
/// </summary>
[SupportedOSPlatform("windows5.1.2600")]
internal class WindowsDirectorySearcher
{
    private const string Prefix = "\\??\\";
    
    /// <summary>
    /// Retrieves the total contents of a directory and all sub directories.
    /// </summary>
    /// <param name="path">The path to search inside. Should not end with a backslash.</param>
    /// <param name="groups">Groupings of files to their corresponding directories.</param>
    /// <returns>True if the operation succeeded, else false.</returns>
    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void GetDirectoryContentsRecursiveGrouped(string path, out List<DirectoryFilesGroup> groups)
    {
        groups = new List<DirectoryFilesGroup>();
        GetDirectoryContentsRecursiveGrouped(path, groups);
    }

    /// <summary>
    /// Retrieves the total contents of a directory and all sub directories.
    /// </summary>
    /// <param name="path">The path to search inside. Should not end with a backslash.</param>
    /// <param name="groups">Groupings of files+directories to their corresponding directories.</param>
    /// <returns>True if the operation succeeded, else false.</returns>
    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GetDirectoryContentsRecursiveGrouped(string path, List<DirectoryFilesGroup> groups)
    {
        var newFiles = new List<FileInformation>();
        var initialDirectories = new List<DirectoryInformation>();

        path = Path.GetFullPath(path);
        var initialDirSuccess = TryGetDirectoryContents_Internal(path, newFiles.Add, initialDirectories.Add);
        if (!initialDirSuccess)
            return;

        // Add initial files
        groups.Add(new DirectoryFilesGroup(new DirectoryInformation(Path.GetFullPath(path)), newFiles));
        if (initialDirectories.Count <= 0)
            return;

        // Loop in single stack until all done.
        var remainingDirectories = new Stack<DirectoryInformation>(initialDirectories);
        while (remainingDirectories.TryPop(out var dir))
        {
            newFiles.Clear();
            initialDirectories.Clear();
            TryGetDirectoryContents_Internal(dir.FullPath, newFiles.Add, initialDirectories.Add);

            // Add to accumulator
            groups.Add(new DirectoryFilesGroup(dir, newFiles));

            // Add to remaining dirs
            foreach (var newDir in initialDirectories)
                remainingDirectories.Push(newDir);
        }
    }

    #region Algorithm Constants
    // Stack allocated, so we need to restrict ourselves. I hope no file has longer name than 8000 character.
    const int StackBufferSize = 1024 * 16; 
    const uint STATUS_SUCCESS = 0x00000000;
    #endregion

    /// <summary>
    /// Retrieves the total contents of a directory for a single directory.
    /// </summary>
    /// <param name="dirPath">The path for which to get the directory for. Must be full path.</param>
    /// <param name="onAddFile">The files present in this directory.</param>
    /// <param name="onAddDirectory">The directories present in this directory.</param>
    /// <returns>True on success, else false.</returns>
    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe bool TryGetDirectoryContents_Internal(string dirPath, Action<FileInformation> onAddFile, Action<DirectoryInformation> onAddDirectory)
    {
        // Note: Thanks to SkipLocalsInit, this memory is not zero'd so the allocation is virtually free.
        byte* bufferPtr = stackalloc byte[StackBufferSize];

        // Add prefix if needed.
        var originalDirPath = dirPath;
        if (!dirPath.StartsWith(Prefix))
            dirPath = $"{Prefix}{dirPath}";

        // Open the folder for reading.
        var hFolder = IntPtr.Zero;
        var objectAttributes = new OBJECT_ATTRIBUTES();
        var statusBlock = new IO_STATUS_BLOCK();
        long allocSize = 0;
        IntPtr result;

        fixed (char* dirString = dirPath)
        {
            var objectName = new UNICODE_STRING(dirString, dirPath.Length);
            objectAttributes.ObjectName = &objectName;
            result = NtCreateFile(&hFolder, FILE_LIST_DIRECTORY | SYNCHRONIZE, &objectAttributes, &statusBlock, &allocSize, Normal, FileShare.Read, Open, DirectoryFile | SynchronousIoNonAlert, IntPtr.Zero, 0);
        }

        if ((ulong)result != STATUS_SUCCESS)
            return false;

        try
        {
            // Read remaining files while possible.
            bool moreFiles = true;
            while (moreFiles)
            {
                statusBlock = new IO_STATUS_BLOCK();
                var ntstatus = NtQueryDirectoryFile(hFolder,   // Our directory handle.
                    IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, &statusBlock,  // Pointers we don't care about 
                    (IntPtr)bufferPtr, StackBufferSize, FILE_INFORMATION_CLASS.FileDirectoryInformation, // Buffer info.
                    0, (UNICODE_STRING*)IntPtr.Zero, 0);

                var currentBufferPtr = (IntPtr)bufferPtr;
                if (ntstatus != STATUS_SUCCESS)
                {
                    moreFiles = false;
                }
                else
                {
                    FILE_DIRECTORY_INFORMATION* info;
                    do
                    {
                        info = (FILE_DIRECTORY_INFORMATION*)currentBufferPtr;

                        // Not symlink or symlink to offline file.
                        if ((info->FileAttributes & ReparsePoint) != 0 &&
                            (info->FileAttributes & Offline) == 0)
                            goto nextfile;

                        var fileName = info->GetFileName(info).ToString();
                        if (fileName == "." || fileName == "..")
                            goto nextfile;

                        var isDirectory = (info->FileAttributes & FileAttributes.Directory) > 0;
                        if (isDirectory)
                        {
                            onAddDirectory(new DirectoryInformation
                            {
                                FullPath = $@"{originalDirPath}\{fileName}"
                            });
                            
                            onAddFile(new FileInformation
                            {
                                FileName = fileName,
                                IsDirectory = true,
                            });
                        }
                        else if (!isDirectory)
                        {
                            onAddFile(new FileInformation
                            {
                                FileName = fileName,
                                IsDirectory = false
                            });
                        }

                    nextfile:
                        currentBufferPtr += (int)info->NextEntryOffset;
                    }
                    while (info->NextEntryOffset != 0);
                }
            }
        }
        finally
        {
            NtClose(hFolder);
        }

        return true;
    }
}

/// <summary>
/// Represents information tied to an individual file.
/// </summary>
public struct FileInformation
{
    /// <summary>
    /// Name of the file relative to directory.
    /// </summary>
    public string FileName;

    /// <summary>
    /// True if this is a directory, else false.
    /// </summary>
    public bool IsDirectory;

    /// <summary/>
    public FileInformation(string fileName, bool isDirectory)
    {
        FileName = fileName;
        IsDirectory = isDirectory;
    }

    /// <inheritdoc/>
    public override string ToString() => FileName;
}

/// <summary>
/// Represents information tied to an individual directory.
/// </summary>
public struct DirectoryInformation
{
    /// <summary>
    /// Full path to the directory.
    /// </summary>
    public string FullPath;

    /// <summary/>
    public DirectoryInformation(string fullPath)
    {
        FullPath = fullPath;
    }

    /// <inheritdoc/>
    public override string ToString() => FullPath;
}

/// <summary>
/// Groups a single directory and a list of files associated with it.
/// </summary>
public class DirectoryFilesGroup
{
    /// <summary>
    /// The directory in question.
    /// </summary>
    public DirectoryInformation Directory;

    /// <summary>
    /// The relative file/directory paths tied to this directory.
    /// </summary>
    public FileInformation[] Items;

    /// <summary/>
    public DirectoryFilesGroup(DirectoryInformation directory, List<FileInformation> files)
    {
        Directory = directory;
        Items = files.ToArray();
    }

    /// <inheritdoc/>
    public override string ToString() => Directory.FullPath;
}