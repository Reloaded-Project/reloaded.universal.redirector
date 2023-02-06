using System.Diagnostics.CodeAnalysis;
using Reloaded.Universal.Redirector.Lib.Structures.RedirectionTree;

namespace Reloaded.Universal.Redirector.Tests.Tests;

[SuppressMessage("Assertions", "xUnit2013:Do not use equality check to check for collection size.")]
public class RedirectionTreeTests
{
    /// <summary>
    /// Tests adding an item to the redirection tree.
    /// </summary>
    [Fact]
    public void Add_WithSingleNode()
    {
        var tree = RedirectionTree.Create();
        tree.AddPath(@"c:\", @"d:\");
        
        Assert.True(tree.RootNode.Children.ContainsKey("c:"));
        Assert.Equal(1, tree.RootNode.Children.Count);
    }

    /// <summary>
    /// Tests adding a sub-item to the redirection tree.
    /// </summary>
    [Fact]
    public void Add_WithNestedNode()
    {
        var tree = RedirectionTree.Create();
        tree.AddPath(@"c:\", @"d:\");
        tree.AddPath(@"c:\kitten", @"d:\kitten"); // should register as subnode of C:\
        
        Assert.True(tree.RootNode.Children.ContainsKey("c:"));
        Assert.Equal(1, tree.RootNode.Children.Count);

        var child = tree.RootNode.Children.GetValueRef("c:");
        Assert.True(child.Files.ContainsKey("kitten"));
        Assert.Equal(1, child.Files.Count);
    }
    
    /// <summary>
    /// Tests adding a sub-item to the redirection tree, without adding any other items along the way.
    /// </summary>
    [Fact]
    public void Add_WithNestedNode_NoExistingParent()
    {
        var tree = RedirectionTree.Create();
        tree.AddPath(@"c:\kitten", @"d:\kitten"); // should register as subnode of C:\
        
        Assert.True(tree.RootNode.Children.ContainsKey("c:"));
        Assert.Equal(1, tree.RootNode.Children.Count);

        var child = tree.RootNode.Children.GetValueRef("c:");
        Assert.True(child.Files.ContainsKey("kitten"));
        Assert.Equal(1, child.Files.Count);
    }
    
    /// <summary>
    /// Tests adding a sub-item to the redirection tree, without adding any other items along the way.
    /// </summary>
    [Fact]
    public void Add_WithMultiLevelNestedNode()
    {
        var tree = RedirectionTree.Create();
        tree.AddPath(@"c:\kitten\kitty", @"d:\kitten\kitty"); // should register as subnode of C:\
        
        Assert.True(tree.RootNode.Children.ContainsKey("c:"));
        Assert.Equal(1, tree.RootNode.Children.Count);

        var child = tree.RootNode.Children.GetValueRef("c:");
        Assert.True(child.Children.TryGetValue("kitten", out _));
        Assert.Equal(1, child.Children.Count);

        child = child.Children.GetValueRef("kitten");
        Assert.True(child.Files.ContainsKey("kitty"));
        Assert.Equal(1, child.Files.Count);
    }
    
    /// <summary>
    /// Tests adding a folder to the redirection tree.
    /// </summary>
    [Fact]
    public void AddFolder()
    {
        var tree = RedirectionTree.Create();
        tree.AddFolderPaths(@"c:\kitten", new[]
        {
            "cat",
            "kitty",
            "nya-nyan"
        }, @"c:\kitten");

        Assert.True(tree.RootNode.Children.ContainsKey("c:"));
        Assert.Equal(1, tree.RootNode.Children.Count);

        var child = tree.RootNode.Children.GetValueRef("c:");
        Assert.True(child.Children.TryGetValue("kitten", out _));
        Assert.Equal(1, child.Children.Count);

        child = child.Children.GetValueRef("kitten");
        Assert.True(child.Files.ContainsKey("kitty"));
        Assert.True(child.Files.ContainsKey("cat"));
        Assert.True(child.Files.ContainsKey("nya-nyan"));
        Assert.Equal(3, child.Files.Count);
    }
}