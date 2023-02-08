using Reloaded.Universal.Redirector.Lib.Structures;
using Reloaded.Universal.Redirector.Lib.Structures.RedirectionTree;

namespace Reloaded.Universal.Redirector.Tests.Tests;

public class LookupTreeTests
{
    /// <summary>
    /// Tests creating a redirection tree from a lookup tree in cases where sub-paths do not exist.
    /// </summary>
    [Fact]
    public void Create_FromRedirectionTree_WithNoSubPaths()
    {
        var tree = RedirectionTree<RedirectionTreeTarget>.Create();
        tree.AddPath(@"C:\KITTEN\CAT", @"D:\KITTEN\CAT");
        tree.AddPath(@"C:\KITTEN\NEKO", @"D:\KITTEN\NEKO");

        var lookup = new LookupTree<RedirectionTreeTarget>(tree);
        
        Assert.Equal(@"C:\KITTEN", lookup.Prefix, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(1, lookup.SubfolderToFiles.Count);
        Assert.True(lookup.SubfolderToFiles.GetFirstItem(out _).ContainsKey("CAT"));
        Assert.True(lookup.SubfolderToFiles.GetFirstItem(out _).ContainsKey("NEKO"));
    }
    
    /// <summary>
    /// Tests creating a redirection tree from a lookup tree in cases where sub-paths exist.
    /// </summary>
    [Fact]
    public void Create_FromRedirectionTree_WithSubPaths()
    {
        var tree = RedirectionTree<RedirectionTreeTarget>.Create();
        tree.AddPath(@"C:\KITTEN\CAT.PNG", @"D:\KITTEN\CAT.PNG");
        tree.AddPath(@"C:\KITTEN\NEKO.PNG", @"D:\KITTEN\NEKO.PNG");
        tree.AddPath(@"C:\KITTEN\NEKO\CAR\VROOM.PNG", @"D:\KITTEN\NEKO\CAR\VROOM.PNG");
        tree.AddPath(@"C:\KITTEN\NEKO\MOBILE\BURENYA.PNG", @"D\KITTEN\NEKO\MOBILE\BURENYA.PNG");

        var lookup = new LookupTree<RedirectionTreeTarget>(tree);
        
        Assert.Equal(@"C:\KITTEN", lookup.Prefix, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(3, lookup.SubfolderToFiles.Count);
        Assert.True(lookup.SubfolderToFiles.GetValueRef("").ContainsKey("CAT.PNG"));
        Assert.True(lookup.SubfolderToFiles.GetValueRef("").ContainsKey("NEKO.PNG"));
        
        Assert.True(lookup.SubfolderToFiles.GetValueRef(@"NEKO\CAR").ContainsKey("VROOM.PNG"));
        Assert.True(lookup.SubfolderToFiles.GetValueRef(@"NEKO\MOBILE").ContainsKey("BURENYA.PNG"));
    }
    
    /// <summary>
    /// Tests getting a folder from the lookup tree.
    /// </summary>
    [Fact]
    public void Get_Folder_WithNoSubfolders()
    {
        var tree = RedirectionTree<RedirectionTreeTarget>.Create();
        tree.AddPath(@"C:\KITTEN\CAT.PNG", @"D:\KITTEN\CAT.PNG");
        tree.AddPath(@"C:\KITTEN\NEKO.PNG", @"D:\KITTEN\NEKO.PNG");

        var lookup = new LookupTree<RedirectionTreeTarget>(tree);
        Assert.True(lookup.TryGetFolder(@"C:\KITTEN", out var result));
        Assert.Equal(2, result.Count);
    }
    
    /// <summary>
    /// Tests getting a file from the lookup tree.
    /// </summary>
    [Fact]
    public void Get_File_WithNoSubfolders()
    {
        var tree = RedirectionTree<RedirectionTreeTarget>.Create();
        tree.AddPath(@"C:\KITTEN\CAT.PNG", @"D:\KITTEN\CAT.PNG");
        tree.AddPath(@"C:\KITTEN\NEKO.PNG", @"D:\KITTEN\NEKO.PNG");

        var lookup = new LookupTree<RedirectionTreeTarget>(tree);
        Assert.True(lookup.TryGetFile(@"C:\KITTEN\CAT.PNG", out var result));
        Assert.Equal(@"D:\KITTEN", result.Directory);
        Assert.Equal(@"CAT.PNG", result.FileName);
    }
    
    /// <summary>
    /// Tests getting a file from the lookup tree.
    /// </summary>
    [Fact]
    public void Get_File_WithSubfolders()
    {
        var tree = RedirectionTree<RedirectionTreeTarget>.Create();
        tree.AddPath(@"C:\KITTEN\CAT.PNG", @"D:\KITTEN\CAT.PNG");
        tree.AddPath(@"C:\KITTEN\NEKO\CAR\VROOM.PNG", @"D:\KITTEN\NEKO\CAR\VROOM.PNG");

        var lookup = new LookupTree<RedirectionTreeTarget>(tree);
        Assert.True(lookup.TryGetFile(@"C:\KITTEN\CAT.PNG", out var result));
        Assert.Equal(@"D:\KITTEN", result.Directory);
        Assert.Equal(@"CAT.PNG", result.FileName);
        
        Assert.True(lookup.TryGetFile(@"C:\KITTEN\NEKO\CAR\VROOM.PNG", out result));
        Assert.Equal(@"D:\KITTEN\NEKO\CAR", result.Directory);
        Assert.Equal(@"VROOM.PNG", result.FileName);
    }
    
    /// <summary>
    /// Tests creating a redirection tree from a lookup tree in cases where sub-paths exist.
    /// </summary>
    [Fact]
    public void Get_Folder_WithSubfolders()
    {
        var tree = RedirectionTree<RedirectionTreeTarget>.Create();
        tree.AddPath(@"C:\KITTEN\CAT.PNG", @"D:\KITTEN\CAT.PNG");
        tree.AddPath(@"C:\KITTEN\NEKO.PNG", @"D:\KITTEN\NEKO.PNG");
        tree.AddPath(@"C:\KITTEN\NEKO\CAR\VROOM.PNG", @"D:\KITTEN\NEKO\CAR\VROOM.PNG");
        tree.AddPath(@"C:\KITTEN\NEKO\MOBILE\BURENYA.PNG", @"D\KITTEN\NEKO\MOBILE\BURENYA.PNG");

        var lookup = new LookupTree<RedirectionTreeTarget>(tree);
        
        // No subfolder
        Assert.True(lookup.TryGetFolder(@"C:\KITTEN", out var result));
        Assert.Equal(2, result.Count);
        
        // Subfolder
        Assert.True(lookup.TryGetFolder(@"C:\KITTEN\NEKO\CAR", out result));
        Assert.Equal(1, result.Count);
        
        Assert.True(lookup.TryGetFolder(@"C:\KITTEN\NEKO\MOBILE", out result));
        Assert.Equal(1, result.Count);
    }
    
    /// <summary>
    /// Tests getting a folder from the lookup tree, when the case does not match.
    /// [Windows is case insensitive, we therefore need to be too; games and/or game mods
    /// might request files using different casing]
    /// </summary>
    [Fact]
    public void Get_Folder_WithNoSubfolders_WithIncorrectCase()
    {
        var tree = RedirectionTree<RedirectionTreeTarget>.Create();
        tree.AddPath(@"C:\KITTEN\CAT.PNG", @"D:\KITTEN\CAT.PNG");
        tree.AddPath(@"C:\KITTEN\NEKO.PNG", @"D:\KITTEN\NEKO.PNG");

        var lookup = new LookupTree<RedirectionTreeTarget>(tree);
        Assert.True(lookup.TryGetFolder(@"C:\Kitten", out var result)); // <= incorrect case
        Assert.Equal(2, result.Count);
    }
}