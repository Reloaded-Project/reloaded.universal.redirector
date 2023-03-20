using Reloaded.Universal.Redirector.Tests.Utility;

namespace Reloaded.Universal.Redirector.Tests.Tests;

public class RedirectorTests
{
    [Fact]
    public void Add_CanAdd_AndRemove_Folder()
    {
        using var tempFolder = new TemporaryClonedFolder(Paths.Base);
        using var redirector = new Lib.Redirector(tempFolder.FolderPath);
        
        redirector.Add(tempFolder.FolderPath);
        
        // Files map to current directory by default.
        Assert.True(redirector.Manager.TryGetFolder(Environment.CurrentDirectory, out _));
        redirector.Remove(tempFolder.FolderPath);
        Assert.False(redirector.Manager.TryGetFolder(Environment.CurrentDirectory, out _));
    }
    
    [Fact]
    public void Add_CanAdd_AndRemove_Folder_WithExplicitSrcAndTarget()
    {
        using var baseFolder = new TemporaryClonedFolder(Paths.Base);
        using var overlayFolder = new TemporaryClonedFolder(Paths.Overlay1);
        using var redirector = new Lib.Redirector(overlayFolder.FolderPath);
        var sourcePath = baseFolder.FolderPath;
        var targetPath = overlayFolder.FolderPath;
        
        redirector.Add(targetPath, sourcePath);
        Assert.True(redirector.Manager.TryGetFolder(sourcePath, out _));
        redirector.Remove(targetPath, sourcePath);
        Assert.False(redirector.Manager.TryGetFolder(sourcePath, out _));
    }
    
    [Fact]
    public void Add_CanAdd_AndRemove_File()
    {
        using var baseFolder = new TemporaryClonedFolder(Paths.Base);
        using var overlayFolder = new TemporaryClonedFolder(Paths.Override1);
        using var redirector = new Lib.Redirector(overlayFolder.FolderPath);
        var sourcePath = Directory.GetFileSystemEntries(baseFolder.FolderPath).First();
        var targetPath = Directory.GetFileSystemEntries(overlayFolder.FolderPath).First();
        
        redirector.AddCustomRedirect(sourcePath, targetPath);
        Assert.True(redirector.Manager.TryGetFile(sourcePath, out _));
        redirector.RemoveCustomRedirect(sourcePath);
        Assert.False(redirector.Manager.TryGetFile(sourcePath, out _));
    }
    
    [Fact]
    public void Add_CanGetFolderWithBackslash_ViaTryGetFile()
    {
        //using var testFolder = new TemporaryFolderAllocation();
        using var baseFolder = new TemporaryClonedFolder(Paths.BaseWithSubfolders);
        using var overlayFolder = new TemporaryClonedFolder(Paths.OverrideWithSubfolders);
        using var redirector = new Lib.Redirector(overlayFolder.FolderPath);
        
        redirector.Add(overlayFolder.FolderPath, baseFolder.FolderPath);
        
        // Baseline
        var path = Path.Combine(baseFolder.FolderPath, "Poem 1");
        Assert.True(redirector.Manager.TryGetFile(path, out _));
        
        // Actual Test
        Assert.True(redirector.Manager.TryGetFile(path + Path.DirectorySeparatorChar, out _));
    }
}