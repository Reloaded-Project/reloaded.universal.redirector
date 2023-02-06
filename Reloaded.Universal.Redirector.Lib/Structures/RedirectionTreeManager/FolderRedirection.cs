using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance.Buffers;
using FileEmulationFramework.Lib.IO;
using Reloaded.Universal.Redirector.Lib.Backports.System.Globalization;
using Reloaded.Universal.Redirector.Lib.Interfaces;
using Reloaded.Universal.Redirector.Lib.Structures.RedirectionTree;
using Reloaded.Universal.Redirector.Lib.Utility;

namespace Reloaded.Universal.Redirector.Lib.Structures.RedirectionTreeManager;

/// <summary>
/// Represents a request to redirect a full folder.
/// </summary>
public class FolderRedirection : IEquatable<FolderRedirection>
{
    /*
         Note:
         
         In the path between FolderRedirection to LookupTree we must not clone or modify 
         the string containing file data.  
         
         We want the the string instances to reference the same string data; this deduplication
         in memory allows us to keep files in this folder cached allowing for near instant rebuilds.  
         
         i.e. We store a map of relative path -> RedirectionTreeTarget
    */

    /// <summary>
    /// Path of the old folder.
    /// </summary>
    public string SourceFolder { get; }

    /// <summary>
    /// Path of the new folder.
    /// </summary>
    public string TargetFolder { get; }

    /// <summary>
    /// The owner to whom update events are sent to.
    /// </summary>
    public IFolderRedirectionUpdateReceiver Receiver { get; }

    /// <summary>
    /// A map of all known subdirectories to files.
    /// </summary>
    /// <remarks>
    ///     By storing RedirectionTreeTarget here directly, we can ensure or better communicate the strings here are to be reused.  
    ///     The key of this is deduplicated with <see cref="LookupTree"/> by using a string pool.  
    /// </remarks>
    public SpanOfCharDict<List<RedirectionTreeTarget>> SubdirectoryToFilesMap { get; private set; } = null!;

    /// <summary>
    /// Creates a new folder redirection command/info.
    /// </summary>
    /// <param name="sourceFolder">The folder to map files from.</param>
    /// <param name="targetFolder">The folder to map files to.</param>
    /// <param name="receiver">The item to send update events to.</param>
    public FolderRedirection(string sourceFolder, string targetFolder, Lib.RedirectionTreeManager receiver)
    {
        SourceFolder = TextInfo.ChangeCase<TextInfo.ToUpperConversion>(Path.GetFullPath(sourceFolder));
        TargetFolder = TextInfo.ChangeCase<TextInfo.ToUpperConversion>(Path.GetFullPath(targetFolder));
        Receiver = receiver;
        Initialise();
    }

    private void Initialise()
    {
        // Initialise.
        WindowsDirectorySearcher.GetDirectoryContentsRecursiveGrouped(SourceFolder, out var groups);
        var subdirToFiles = new SpanOfCharDict<List<RedirectionTreeTarget>>(groups.Count);
        
        foreach (var group in CollectionsMarshal.AsSpan(groups))
            InitGroup(group, subdirToFiles);

        SubdirectoryToFilesMap = subdirToFiles;
    }

    private void InitGroup(DirectoryFilesGroup group, SpanOfCharDict<List<RedirectionTreeTarget>> subdirToFiles)
    {
        // Get Normalized Subfolder
        var dirPath          = group.Directory.FullPath;
        var hasSubfolder     = SourceFolder.Length != dirPath.Length;
        var hasSubfolderByte = Unsafe.As<bool, byte>(ref hasSubfolder);
        var nextFolder       = dirPath.AsSpan(SourceFolder.Length + hasSubfolderByte);

        var dirPathUpper = nextFolder.Length <= 512
            ? stackalloc char[nextFolder.Length]
            : GC.AllocateUninitializedArray<char>(nextFolder.Length); // super cold path, basically never hit

        TextInfo.ChangeCase<TextInfo.ToUpperConversion>(nextFolder, dirPathUpper);
        var directory = StringPool.Shared.GetOrAdd(dirPathUpper);
        var targets   = new List<RedirectionTreeTarget>(group.Files.Length);
        
        foreach (var file in group.Files)
        {
            var fileUpper = TextInfo.ChangeCase<TextInfo.ToUpperConversion>(file);
            targets.Add(new RedirectionTreeTarget(directory, fileUpper));
        }

        subdirToFiles.AddOrReplace(directory, targets);
    }

    /// <summary/>
    public bool Equals(FolderRedirection? other)
    {
        return SourceFolder == other?.SourceFolder && TargetFolder == other.TargetFolder;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((FolderRedirection)obj);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Strings.GetNonRandomizedHashCode(SourceFolder), Strings.GetNonRandomizedHashCode(TargetFolder));
    }
}