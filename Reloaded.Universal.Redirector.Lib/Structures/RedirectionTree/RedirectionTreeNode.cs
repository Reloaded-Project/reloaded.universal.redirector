namespace Reloaded.Universal.Redirector.Lib.Structures.RedirectionTree;

/// <summary>
/// Individual node in the redirection tree.
/// </summary>
public struct RedirectionTreeNode
{
    /// <summary>
    /// Child nodes of this nodes.
    /// i.e. Maps 'folder' to next child.
    /// </summary>
    public readonly SpanOfCharDict<RedirectionTreeNode> Children;

    /// <summary>
    /// Files present at this level of the tree.
    /// </summary>
    public readonly SpanOfCharDict<RedirectionTreeTarget> Files;

    /// <summary/>
    /// <param name="expectedChildren">Number of expected child directories.</param>
    public RedirectionTreeNode(int expectedChildren)
    {
        Children = new SpanOfCharDict<RedirectionTreeNode>(expectedChildren);
        Files = new SpanOfCharDict<RedirectionTreeTarget>(8);
    }
    
    /// <summary/>
    /// <param name="expectedChildren">Number of expected child directories.</param>
    /// <param name="expectedFiles">Number of expected files.</param>
    public RedirectionTreeNode(int expectedChildren, int expectedFiles)
    {
        Children = new SpanOfCharDict<RedirectionTreeNode>(expectedChildren);
        Files = new SpanOfCharDict<RedirectionTreeTarget>(expectedFiles);
    }
}