using Reloaded.Universal.Redirector.Lib.Structures.RedirectionTreeManager;
using Reloaded.Universal.Redirector.Tests.Utility;

namespace Reloaded.Universal.Redirector.Tests.Tests;

/// <summary>
/// Tests related to the folder redirection class.
/// </summary>
public class FolderRedirectionTests
{
    /// <summary>
    /// Ensures folder redirection is correctly initialised.
    /// </summary>
    [Fact]
    public void Initialise_No_Subfolders()
    {
        var folderRedirection = new FolderRedirection(Paths.Base, Paths.Base);
        Assert.True(folderRedirection.SubdirectoryToFilesMap.TryGetValue("", out var results));
        Assert.NotEqual(default, results.Find(x => x.FileName == "USVFS-POEM.TXT"));
        Assert.NotEqual(default, results.Find(x => x.FileName == "USVFS-POEM-2.TXT"));
    }
    
    /// <summary>
    /// Ensures folder redirection is correctly initialised.
    /// </summary>
    [Fact]
    public void Initialise_With_Subfolders()
    {
        var folderRedirection = new FolderRedirection(Paths.BaseWithSubfolders, Paths.BaseWithSubfolders);
        Assert.True(folderRedirection.SubdirectoryToFilesMap.TryGetValue("POEM 1", out var results));
        Assert.NotEqual(default, results.Find(x => x.FileName == "USVFS-POEM.TXT"));
        
        Assert.True(folderRedirection.SubdirectoryToFilesMap.TryGetValue("POEM 2", out results));
        Assert.NotEqual(default, results.Find(x => x.FileName == "USVFS-POEM-2.TXT"));
    }
}