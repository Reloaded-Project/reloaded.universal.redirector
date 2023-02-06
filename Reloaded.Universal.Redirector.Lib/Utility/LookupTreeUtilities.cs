using System.Text;
using CommunityToolkit.HighPerformance.Buffers;
using Reloaded.Universal.Redirector.Lib.Structures;
using Reloaded.Universal.Redirector.Lib.Structures.RedirectionTree;

namespace Reloaded.Universal.Redirector.Lib.Utility;

/// <summary>
/// Utilities used for helping building lookup trees.
/// </summary>
public class LookupTreeUtilities
{
    /// <summary>
    /// Builds the subfolder to files listing of each lookup tree by recursively iterating the redirection tree nodes.
    /// </summary>
    /// <param name="currentNode">Node from which to build the tree.</param>
    /// <param name="currentSubfolder">Preallocated string builder instance.</param>
    /// <param name="subToFiles">Dictionary that maps subfolders to individual files.</param>
    public static void BuildSubfolderToFilesRecursive(RedirectionTreeNode currentNode, StringBuilder currentSubfolder, SpanOfCharDict<SpanOfCharDict<RedirectionTreeTarget>> subToFiles)
    {
        // Add if we have files at this level.
        // Note: We subtract 1 to remove trailing slash.
        if (currentNode.Files.Count > 0)
        {
            var folderName = currentSubfolder.ToString(0, currentSubfolder.Length > 0 ? currentSubfolder.Length - 1 : currentSubfolder.Length);
            subToFiles.AddOrReplace(StringPool.Shared.GetOrAdd(folderName), currentNode.Files.Clone());
        }

        int stringBuilderOriginalLength = currentSubfolder.Length;
        var enumerator = currentNode.Children.GetEntryEnumerator();
        while (enumerator.MoveNext())
        {
            var subFolder = enumerator.Current;
            currentSubfolder.Length = stringBuilderOriginalLength;
            currentSubfolder.Append(subFolder.Key);
            currentSubfolder.Append(Path.DirectorySeparatorChar);
            BuildSubfolderToFilesRecursive(subFolder.Value, currentSubfolder, subToFiles);
        }
        
        currentSubfolder.Length = stringBuilderOriginalLength;
    }
    
    /// <summary>
    /// Counts all subdirectories of a redirection tree node recursively.
    /// </summary>
    /// <param name="currentNode">The node to count subdirectories from.</param>
    public static int CountSubdirectoriesRecursive(RedirectionTreeNode currentNode)
    {
        int result = 0;
        CountSubdirectoriesRecursive(currentNode, ref result);
        return result;
    }
    
    /// <summary>
    /// Counts all subdirectories of a redirection tree node recursively.
    /// </summary>
    /// <param name="currentNode">The node to count subdirectories from.</param>
    /// <param name="numSubdirectories">Total number of subdirectories.</param>
    public static void CountSubdirectoriesRecursive(RedirectionTreeNode currentNode, ref int numSubdirectories)
    {
        // Note: Converted to iteration by JIT; so kept recursive for readability.
        var enumerator = currentNode.Children.GetEntryEnumerator();
        while (enumerator.MoveNext())
        {
            var subDirectory = enumerator.Current;
            numSubdirectories += 1;
            CountSubdirectoriesRecursive(subDirectory.Value, ref numSubdirectories);
        }
    }
}