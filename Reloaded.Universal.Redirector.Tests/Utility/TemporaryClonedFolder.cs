namespace Reloaded.Universal.Redirector.Tests.Utility;

/// <summary>
/// Temporarily clones a folder for testing purposes; then disposes it.
/// </summary>
public class TemporaryClonedFolder : TemporaryFolderAllocation
{
    /// <summary/>
    public TemporaryClonedFolder(string sourceFolder, string? path = null) : base(path)
    {
        IO.CopyFilesRecursively(sourceFolder, FolderPath);
    }
}