using Reloaded.Universal.Redirector.Lib.Utility;

namespace Reloaded.Universal.Redirector.Lib.Structures.RedirectionTreeManager;

/// <summary>
/// Represents an individual file redirection request that maps one file path to another.
/// </summary>
public struct FileRedirection : IEquatable<FileRedirection>
{
    /// <summary>
    /// Old path for the file.
    /// </summary>
    public string OldPath { get; }
    
    /// <summary>
    /// New path for the file.
    /// </summary>
    public string NewPath { get; }
    
    /// <summary/>
    public FileRedirection(string oldPath, string newPath)
    {
        OldPath = oldPath.NormalizePath();
        NewPath = newPath.NormalizePath();
    }

    /// <summary/>
    public bool Equals(FileRedirection other)
    {
        return string.Equals(OldPath, other.OldPath, StringComparison.Ordinal) && 
               string.Equals(NewPath, other.NewPath, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is FileRedirection other && 
               Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Strings.GetNonRandomizedHashCode(OldPath), Strings.GetNonRandomizedHashCode(NewPath));
    }
}