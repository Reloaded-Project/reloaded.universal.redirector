using Reloaded.Universal.Redirector.Lib;
using Reloaded.Universal.Redirector.Lib.Structures.RedirectionTreeManager;
using Reloaded.Universal.Redirector.Tests.Utility;

namespace Reloaded.Universal.Redirector.Tests.Tests;

public class RedirectionTreeManagerTests
{
    [Fact]
    public void Can_Add_Folder_WithRedirectionTree()
    {
        Can_Add_Folder_Common(new RedirectionTreeManager());
    }

    [Fact]
    public void Can_Add_Folder_WithLookupTree()
    {
        var manager = new RedirectionTreeManager();
        manager.Optimise();
        Can_Add_Folder_Common(manager);
    }

    private static void Can_Add_Folder_Common(RedirectionTreeManager manager)
    {
        manager.AddFolderRedirection(new FolderRedirection(Paths.Base, Paths.Base));

        Assert.True(manager.TryGetFolder(Paths.Base, out var result));
        Assert.Single(manager.FolderRedirections);
        Assert.Equal(2, result.Count);
        
        // File pull test
        Assert.True(manager.TryGetFile(Path.Combine(Paths.Base, "usvfs-poem.txt"), out _));
    }

    [Fact]
    public void Can_Add_And_Remove_Folder_WithRedirectionTree()
    {
        var manager = new RedirectionTreeManager();
        Can_Add_And_Remove_Folder_Common(manager);
    }
    
    [Fact]
    public void Can_Add_And_Remove_Folder_WithLookupTree()
    {
        var manager = new RedirectionTreeManager();
        manager.Optimise();
        Can_Add_And_Remove_Folder_Common(manager);
    }

    private static void Can_Add_And_Remove_Folder_Common(RedirectionTreeManager manager)
    {
        var redirection = new FolderRedirection(Paths.Base, Paths.Base);

        manager.AddFolderRedirection(redirection);
        manager.RemoveFolderRedirection(redirection);

        Assert.False(manager.TryGetFolder(Paths.Base, out _));
        Assert.Empty(manager.FolderRedirections);
    }
    
    [Fact]
    public void Can_Fast_Append_New_Files_WithLookupTree()
    { 
        var manager = new RedirectionTreeManager();
        manager.Optimise();
        Can_Fast_Append_New_Files_Common(manager);
    }

    [Fact]
    public void Can_Fast_Append_New_Files_WithRedirectionTree()
    { 
        Can_Fast_Append_New_Files_Common(new RedirectionTreeManager());
    }
    
    private static void Can_Fast_Append_New_Files_Common(RedirectionTreeManager manager)
    {
        var redirection = new FolderRedirection(Paths.Base, Paths.Base);
        manager.AddFolderRedirection(redirection);

        // Does not rebuild because it hasn't existed yet.
        var dc = Path.DirectorySeparatorChar.ToString();
        manager.OnFileAddition(redirection, $"{dc}FOO{dc}BAR.TXT");
        manager.OnFileAddition(redirection, $"{dc}FOO{dc}BAZ.TXT");
        Assert.True(manager.TryGetFile(Path.Combine(Paths.Base, $"foo{dc}bar.txt"), out _));
    }
}