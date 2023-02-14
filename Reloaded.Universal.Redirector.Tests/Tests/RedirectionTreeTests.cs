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
        var tree = RedirectionTree<RedirectionTreeTarget>.Create();
        tree.AddPath(@"c:\", @"d:\");
        
        Assert.True(tree.RootNode.Children.ContainsKey("c:"));
        Assert.Equal(1, tree.RootNode.Children.Allocated);
        Assert.Equal(1, tree.RootNode.Children.Count);
    }

    /// <summary>
    /// Tests adding a sub-item to the redirection tree.
    /// </summary>
    [Fact]
    public void Add_WithNestedNode()
    {
        var tree = RedirectionTree<RedirectionTreeTarget>.Create();
        tree.AddPath(@"c:\", @"d:\");
        tree.AddPath(@"c:\kitten", @"d:\kitten"); // should register as subnode of C:\
        
        Assert.True(tree.RootNode.Children.ContainsKey("c:"));
        Assert.Equal(1, tree.RootNode.Children.Allocated);
        Assert.Equal(1, tree.RootNode.Children.Count);

        var child = tree.RootNode.Children.GetValueRef("c:");
        Assert.True(child.Items.ContainsKey("kitten"));
        Assert.Equal(1, child.Items.Allocated);
        Assert.Equal(1, child.Items.Count);
    }
    
    /// <summary>
    /// Tests adding a sub-item to the redirection tree, without adding any other items along the way.
    /// </summary>
    [Fact]
    public void Add_WithNestedNode_NoExistingParent()
    {
        var tree = RedirectionTree<RedirectionTreeTarget>.Create();
        tree.AddPath(@"c:\kitten", @"d:\kitten"); // should register as subnode of C:\
        
        Assert.True(tree.RootNode.Children.ContainsKey("c:"));
        Assert.Equal(1, tree.RootNode.Children.Allocated);
        Assert.Equal(1, tree.RootNode.Children.Count);

        var child = tree.RootNode.Children.GetValueRef("c:");
        Assert.True(child.Items.ContainsKey("kitten"));
        Assert.Equal(1, child.Items.Allocated);
        Assert.Equal(1, child.Items.Count);
    }
    
    /// <summary>
    /// Tests adding a sub-item to the redirection tree, without adding any other items along the way.
    /// </summary>
    [Fact]
    public void Add_WithMultiLevelNestedNode()
    {
        var tree = RedirectionTree<RedirectionTreeTarget>.Create();
        tree.AddPath(@"c:\kitten\kitty", @"d:\kitten\kitty"); // should register as subnode of C:\
        
        Assert.True(tree.RootNode.Children.ContainsKey("c:"));
        Assert.Equal(1, tree.RootNode.Children.Allocated);
        Assert.Equal(1, tree.RootNode.Children.Count);

        var child = tree.RootNode.Children.GetValueRef("c:");
        Assert.True(child.Children.TryGetValue("kitten", out _));
        Assert.Equal(1, child.Children.Allocated);
        Assert.Equal(1, child.Children.Count);

        child = child.Children.GetValueRef("kitten");
        Assert.True(child.Items.ContainsKey("kitty"));
        Assert.Equal(1, child.Items.Allocated);
        Assert.Equal(1, child.Items.Count);
    }
    
    /// <summary>
    /// Tests adding a folder to the redirection tree.
    /// </summary>
    [Fact]
    public void AddFolder()
    {
        var tree = RedirectionTree<RedirectionTreeTarget>.Create();
        tree.AddFolderPaths(@"c:\kitten", new[]
        {
            "cat",
            "kitty",
            "nya-nyan"
        }, @"c:\kitten");

        Assert.True(tree.RootNode.Children.ContainsKey("c:"));
        Assert.Equal(1, tree.RootNode.Children.Allocated);
        Assert.Equal(1, tree.RootNode.Children.Count);

        var child = tree.RootNode.Children.GetValueRef("c:");
        Assert.True(child.Children.TryGetValue("kitten", out _));
        Assert.Equal(1, child.Children.Allocated);
        Assert.Equal(1, child.Children.Count);

        child = child.Children.GetValueRef("kitten");
        Assert.True(child.Items.ContainsKey("kitty"));
        Assert.True(child.Items.ContainsKey("cat"));
        Assert.True(child.Items.ContainsKey("nya-nyan"));
        Assert.Equal(3, child.Items.Allocated);
        Assert.Equal(3, child.Items.Count);
    }
    
    /// <summary>
    /// Test for resolving existing path in redirection tree.
    /// </summary>
    [Fact]
    public void ResolvePath_BaseLine()
    {
        var tree = RedirectionTree<RedirectionTreeTarget>.Create();
        tree.AddPath(@"c:\kitten\kitty", @"d:\kitten\kitty"); // should register as subnode of C:\

        // Should resolve to 'c:\kitten'
        var node = tree.ResolvePartialPath(@"c:\kitten\kittykat.png");
        Assert.Equal(1, node?.Items.Allocated); // 'kitty'
        Assert.Equal(1, node?.Items.Count); // 'kitty'
        Assert.Equal("kitty", node?.Items.GetFirstItem(out _).FileName); // 'kitty'
        
        // Fail to resolve
        node = tree.ResolvePartialPath(@"d:\kitten\kittykat.png");
        Assert.False(node.HasValue);
    }
    
    /// <summary>
    /// Test for resolving existing path in redirection tree.
    /// </summary>
    [Fact]
    public void GetFolder_BaseLine()
    {
        var tree = RedirectionTree<RedirectionTreeTarget>.Create();
        tree.AddPath(@"c:\kitten\kitty", @"d:\kitten\kitty"); // should register as subnode of C:\

        // Should resolve to 'c:\kitten'
        Assert.True(tree.TryGetFolder(@"c:\kitten", out var node));
        Assert.Equal(1, node.Items.Allocated); // 'kitty'
        Assert.Equal(1, node.Items.Count); // 'kitty'
        Assert.Equal("kitty", node.Items.GetFirstItem(out _).FileName); // 'kitty'
        
        // Fail to resolve
        Assert.False(tree.TryGetFolder(@"d:\kitten", out node));
    }
}