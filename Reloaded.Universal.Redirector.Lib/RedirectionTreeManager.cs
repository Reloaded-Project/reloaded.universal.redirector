using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance;
using Reloaded.Universal.Redirector.Lib.Backports.System.Globalization;
using Reloaded.Universal.Redirector.Lib.Extensions;
using Reloaded.Universal.Redirector.Lib.Interfaces;
using Reloaded.Universal.Redirector.Lib.Structures;
using Reloaded.Universal.Redirector.Lib.Structures.RedirectionTree;
using Reloaded.Universal.Redirector.Lib.Structures.RedirectionTreeManager;
using Reloaded.Universal.Redirector.Lib.Utility;

namespace Reloaded.Universal.Redirector.Lib;

/// <summary>
/// Class responsible for building redirection trees at runtime,
/// maintaining list of sources for redirection and building the necessary trees as required.
/// </summary>
public class RedirectionTreeManager : IFolderRedirectionUpdateReceiver
{
    /// <summary>
    /// List of individual file redirections.
    /// These should be small and take priority over folder ones.
    /// </summary>
    public List<FileRedirection> FileRedirections { get; private set; } = new();

    /// <summary>
    /// List of folder redirections.
    /// </summary>
    public List<FolderRedirection> FolderRedirections { get; private set; } = new();

    /// <summary>
    /// The redirection tree currently being built.
    /// </summary>
    public RedirectionTree<RedirectionTreeTarget> RedirectionTree { get; private set; } = new();
    
    /// <summary>
    /// The current lookup tree.
    /// </summary>
    public LookupTree<RedirectionTreeTarget> Lookup { get; private set; }

    /// <summary>
    /// True if the manager is currently using the lookup tree for lookups, else false.
    /// </summary>
    public bool UsingLookupTree { get; private set; }

    /*
       Events that trigger a full rebuild [& reason]:
       - Remove File                                  [We don't keep track of what maps to a file.]
       - Remove Folder Map                            [We don't keep track of what maps to a file.]
       - Remove Folder                                [We don't keep track of what maps to a file.]
       - Folder Mapping Update Event: Remove Item     [We don't keep track of what maps to a file.]
    
       Partial/Conditional Rebuild:
       - Add Folder Map                        [Partial Rebuild. Apply folder then re-apply files].
       - Folder Mapping Update Event: Add File [Rebuild if File Previously Mapped].
       
           Note: Update of existing lookup tree can only be done if the existing prefix is shared.
           If prefix of item does not map; full tree needs reconstruction.
    */

    /// <summary>
    /// Adds an individual file redirection to the manager.
    /// </summary>
    /// <param name="fileRedirection">The file redirection to use.</param>
    public void AddFileRedirection(FileRedirection fileRedirection)
    {
        FileRedirections.Add(fileRedirection);
        
        // We can freely add files until we kick in the 'optimise' button because files are
        // supposed to take priority.
        if (UsingLookupTree)
            Rebuild();
        else
            ApplyFileRedirection(RedirectionTree, fileRedirection);
    }

    /// <summary>
    /// Removes a file redirection from the manager.
    /// </summary>
    /// <param name="fileRedirection">The file redirection to remove.</param>
    public void RemoveFileRedirection(FileRedirection fileRedirection)
    {
        if (!FileRedirections.Remove(fileRedirection)) 
            return;
        
        // We don't know what previously occupied file slot so full rebuild is needed.
        Rebuild();
    }

    /// <summary>
    /// Adds an individual folder redirection to the lookup tree.
    /// </summary>
    /// <param name="folderRedirection">The folder redirection to use.</param>
    public void AddFolderRedirection(FolderRedirection folderRedirection)
    {
        FolderRedirections.Add(folderRedirection);
        
        // We can freely add files until we kick in the 'optimise' button because files are
        // supposed to take priority.
        if (UsingLookupTree)
        {
            Rebuild();
        }
        else
        {
            ApplyFolderRedirection(RedirectionTree, folderRedirection);

            // We need to reapply file redirections just in case there is one that overlaps with our folder redirection.
            ApplyFileRedirections(RedirectionTree);
        }
    }
    
    /// <summary>
    /// Removes the folder redirection from the lookup tree.
    /// </summary>
    /// <param name="folderRedirection">
    ///     The folder redirection originally passed to <see cref="AddFolderRedirection"/>.
    /// </param>
    public void RemoveFolderRedirection(FolderRedirection folderRedirection)
    {
        // We don't know if this redirection overwrote anything, so a full rebuild is needed.
        if (FolderRedirections.Remove(folderRedirection))
            Rebuild();
    }

    /// <summary>
    /// Builds the redirection tree from scratch.
    /// </summary>
    private void Rebuild()
    {
        var tree = RedirectionTree<RedirectionTreeTarget>.Create();
        
        ApplyFolderRedirections(tree);
        ApplyFileRedirections(tree);
        
        RedirectionTree = tree;
        if (UsingLookupTree)
            Optimise_Impl();
    }
    
    /// <inheritdoc />
    public void OnOtherUpdate(FolderRedirection sender) => Rebuild();

    /// <inheritdoc />
    public void OnFileAddition(FolderRedirection sender, ReadOnlySpan<char> relativePath)
    {
        // Check if file is previously mapped.
        // If it is not, we can add it to existing tree(s).
        var src = string.Concat(sender.SourceFolder, relativePath);
        var tgt = string.Concat(sender.TargetFolder, relativePath);
        var fileName = Path.GetFileName(relativePath);
        
        if (UsingLookupTree)
        {
            if (!Lookup.TryGetFileUpper(src, out var folder, out _))
            {
                var relativeDirName = Path.GetDirectoryName(relativePath[1..]).ToString();
                var items = folder ?? new SpanOfCharDict<RedirectionTreeTarget>(0);
                items.AddOrReplace(fileName.ToString(), new RedirectionTreeTarget(tgt));
                Lookup.SubfolderToFiles.AddOrReplace(relativeDirName, items);
                return;
            }

            Rebuild();
        }
        else
        {
            if (!RedirectionTree.TryGetFolder(relativePath, out var node))
            {
                RedirectionTree.AddPath(src, new RedirectionTreeTarget(tgt));
                return;
            }
            
            if (!node.Items.ContainsKey(fileName))
                node.Items.AddOrReplace(fileName.ToString(), new RedirectionTreeTarget(tgt));
            else
                Rebuild();
        }
    }

    /// <summary>
    /// Tries to get a given file from the manager.
    /// </summary>
    /// <param name="filePath">Path to the file to fetch.</param>
    /// <param name="value">The redirection where the file is stored.</param>
    /// <returns>True if found, else false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe bool TryGetFile(ReadOnlySpan<char> filePath, out RedirectionTreeTarget value)
    {
        // Non-zero is hot path, so we nest code inside.
        if (filePath.Length > 0)
        {
            // It's possible the user or Windows API might want to open a folder here,
            // for example for the purposes of getting attribute, and name ends with a backslash.
            // We strip it here; as the underlying lookup trees might not handle it.
            if (filePath.DangerousGetReferenceAt(^1) == Path.DirectorySeparatorChar)
                filePath = filePath.SliceFast(..^1);
        
            var separatorIndex = filePath.LastIndexOf(Path.DirectorySeparatorChar);
            if (TryGetFolder(filePath.SliceFast(..separatorIndex), out var result))
            {
                var fileName = filePath[(separatorIndex + 1)..];
                Span<char> fileNameUpper = stackalloc char[fileName.Length];
            
                TextInfo.ChangeCase<TextInfo.ToUpperConversion>(fileName, fileNameUpper);
                if (result.TryGetValue(fileNameUpper, out value))
                    return true;
            }

            value = default;
            return false;
        }

        value = default;
        return false;
    }
    
    /// <summary>
    /// Tries to get a given folder path from the manager.
    /// </summary>
    /// <param name="filePath">Path to the folder to fetch the files.</param>
    /// <param name="value">Mapping of file names to locations.</param>
    /// <returns>True if found, else false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetFolder(ReadOnlySpan<char> filePath, [MaybeNullWhen(false)] out SpanOfCharDict<RedirectionTreeTarget> value)
    {
        // Convert to uppercase, on stack if possible.
        var pathLength = filePath.Length; // + 1 to potentially add directory separator if needed
        var folderPathUpper = pathLength <= 512 
            ? stackalloc char[pathLength]
            : GC.AllocateUninitializedArray<char>(pathLength); // super cold path, basically never hit
        
        TextInfo.ChangeCase<TextInfo.ToUpperConversion>(filePath, folderPathUpper);
        
        if (UsingLookupTree)
            return Lookup.TryGetFolderUpper(folderPathUpper, out value);

        var result = RedirectionTree.TryGetFolder(folderPathUpper, out var node);
        value = node.Items;
        return result;
    }

    /// <summary>
    /// Optimises the lookup operation by converting the redirection tree to the lookup tree.
    /// </summary>
    /// <remarks>
    ///    Any additions done past this point will require a full tree rebuild.
    ///    So this is only intended to be called after all mods initialise.
    /// </remarks>
    public void Optimise()
    {
        if (UsingLookupTree)
            return;

        Optimise_Impl();
    }

    private void Optimise_Impl()
    {
        Lookup = new LookupTree<RedirectionTreeTarget>(RedirectionTree);
        UsingLookupTree = true;
        RedirectionTree = default;
    }
    
    private void ApplyFileRedirection(RedirectionTree<RedirectionTreeTarget> tree, FileRedirection fileRedirection)
    {
        tree.AddPath(fileRedirection.OldPath, fileRedirection.NewPath);
    }
    
    private void ApplyFolderRedirection(RedirectionTree<RedirectionTreeTarget> tree, FolderRedirection folderRedirection)
    {
        using var enumerator = folderRedirection.SubdirectoryToFilesMap.GetEntryEnumerator();
        while (enumerator.MoveNext())
        {
            var current = enumerator.Current;
            var currentSource = Path.Combine(folderRedirection.SourceFolder, current.Key!);
            var currentTarget = Path.Combine(folderRedirection.TargetFolder, current.Key!);
            using var filesRental = new ArrayRental<string>(current.Value.Count);
            var files = filesRental.RawArray;
            for (int x = 0; x < files.Length; x++)
                files[x] = current.Value[x].FileName;

            tree.AddFolderPaths(currentSource, files, currentTarget);
        }
    }
    
    private void ApplyFileRedirections(RedirectionTree<RedirectionTreeTarget> tree)
    {
        foreach (var fileRedirection in CollectionsMarshal.AsSpan(FileRedirections))
            ApplyFileRedirection(tree, fileRedirection);
    }

    private void ApplyFolderRedirections(RedirectionTree<RedirectionTreeTarget> tree)
    {
        foreach (var folder in CollectionsMarshal.AsSpan(FolderRedirections))
            ApplyFolderRedirection(tree, folder);
    }
}