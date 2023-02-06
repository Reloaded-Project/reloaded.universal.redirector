using System.Diagnostics;

namespace Reloaded.Universal.Redirector.Lib.Structures.RedirectionTree;

/// <summary>
/// Target for a file covered by the redirection tree.
/// </summary>
public struct RedirectionTreeTarget
{
    /// <summary>
    /// Path to the directory storing the file.
    /// </summary>
    public string Directory; // (This is deduplicated, saving memory)
    
    /// <summary>
    /// Name of the file in the directory.
    /// </summary>
    public string FileName;

    /// <summary/>
    /// <param name="directory">Directory path.</param>
    /// <param name="fileName">File name.</param>
    public RedirectionTreeTarget(string directory, string fileName)
    {
        Directory = directory;
        FileName = fileName;
    }
    
    /// <summary/>
    /// <param name="fullPath">Full path, must be canonical, i.e. use correct separator char..</param>
    public RedirectionTreeTarget(string fullPath)
    {
        var separatorIndex = fullPath.LastIndexOf(Path.DirectorySeparatorChar);
        Debug.Assert(separatorIndex != -1, "Must be a full path.");
        
        Directory = fullPath.Substring(0, separatorIndex);
        FileName = fullPath.Substring(separatorIndex + 1);
    }

    /// <summary>
    /// Returns the full path of the file.
    /// </summary>
    public string GetFullPath()
    {
        return string.Concat(Directory, Path.DirectorySeparatorChar, FileName);
    }
}