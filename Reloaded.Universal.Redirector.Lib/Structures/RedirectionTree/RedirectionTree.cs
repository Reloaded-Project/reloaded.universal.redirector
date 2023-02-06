using System.Runtime.CompilerServices;

namespace Reloaded.Universal.Redirector.Lib.Structures.RedirectionTree;

/// <summary>
/// Represents that will be used for performing redirections.
/// </summary>
public struct RedirectionTree
{
    public static readonly RedirectionTree Empty = default;
    
    /// <summary>
    /// Root nodes, e.g. would store drive: C:/D:/E: etc.
    /// In most cases there is only one.
    /// </summary>
    public RedirectionTreeNode RootNode { get; private set; } // I expect 3 roots/drives max.
    
    /// <summary/>
    public RedirectionTree() { }

    /// <summary>
    /// Creates a new redirection tree.
    /// </summary>
    public static RedirectionTree Create()
    {
        return new RedirectionTree()
        {
            RootNode = new RedirectionTreeNode(3)
        };
    }

    /// <summary>
    /// Add the path onto the redirection tree.
    /// </summary>
    /// <param name="path">
    ///     The full path to add to the tree.
    ///     String must be canonical, i.e. after Path.GetFullPath and upper case.
    /// </param>
    /// <param name="targetPath">Path said file should be redirected to.</param>
    /// <remarks>
    ///    For use with individual file binds done through external API.
    ///    As such, this is a rare path.
    /// </remarks>
    public void AddPath(string path, string targetPath)
    {
        var pathSpan = path.AsSpan();
        var currentNode = RootNode;
        ReadOnlySpan<char> splitSpan = AddDirectory(pathSpan, ref currentNode);
        
        // Add file/folder to current node.
        if (splitSpan.Length > 0) // In case of folder.
            currentNode.Files.AddOrReplace(splitSpan.ToString(), new RedirectionTreeTarget(targetPath));
    }

    /// <summary>
    /// Adds multiple paths that belong under the same subfolder to the redirection tree.
    /// Assumes input strings are all within same subfolder and canonical [Path.GetFullPath] + upper case.
    /// </summary>
    /// <param name="directory">The directory to add. Not expected to end with slash, upper case.</param>
    /// <param name="files">The files to to add.</param>
    /// <param name="targetDirectory">The target directory where redirected files are pointed to.</param>
    public void AddFolderPaths(string directory, string[] files, string targetDirectory)
    {
        var pathSpan = directory.AsSpan();
        var currentNode = RootNode;
        var lastFolder = AddDirectory(pathSpan, ref currentNode);

        // Path ended without \, as expected.
        if (lastFolder.Length > 0)
        {
            var newNode = new RedirectionTreeNode(16, files.Length);
            currentNode.Children.AddOrReplace(lastFolder.ToString(), newNode);
            currentNode = newNode;
        }
        
        // Add all files.
        foreach (var file in files)
            currentNode.Files.AddOrReplace(file, new RedirectionTreeTarget(targetDirectory, file));
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ReadOnlySpan<char> AddDirectory(ReadOnlySpan<char> pathSpan, ref RedirectionTreeNode currentNode)
    {
        ReadOnlySpan<char> splitSpan;
        while ((splitSpan = SplitDir(pathSpan)) != pathSpan)
        {
            // Todo: Add AddOrGetValue API to dict. For now however, not worth the engineering effort.
            // Navigate to next node if possible.
            if (currentNode.Children.TryGetValue(splitSpan, out var existingNode))
            {
                pathSpan = pathSpan.Slice(splitSpan.Length + 1);
                currentNode = existingNode;
                continue;
            }

            // Lookup current slice of path.
            var newNode = new RedirectionTreeNode(10);
            currentNode.Children.AddOrReplace(splitSpan.ToString(), newNode);

            // Advance
            pathSpan = pathSpan.Slice(splitSpan.Length + 1);
            currentNode = newNode; // Estimate number of subdirectories.
        }

        return splitSpan;
    }

    /// <summary>
    /// Splits the string up to the next directory separator.
    /// </summary>
    /// <param name="text">The text to substring.</param>
    /// <returns>The text up to next directory separator, else unchanged.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ReadOnlySpan<char> SplitDir(ReadOnlySpan<char> text)
    {
        var index = text.IndexOf(Path.DirectorySeparatorChar);
        return index != -1 ? text[..index] : text;
    }
}