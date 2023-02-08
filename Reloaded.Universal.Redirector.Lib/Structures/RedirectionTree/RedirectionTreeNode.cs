namespace Reloaded.Universal.Redirector.Lib.Structures.RedirectionTree;

/// <summary>
/// Individual node in the redirection tree.
/// </summary>
public struct RedirectionTreeNode<TTarget>
{
    /// <summary>
    /// Child nodes of this nodes.
    /// i.e. Maps 'folder' to next child.
    /// </summary>
    public readonly SpanOfCharDict<RedirectionTreeNode<TTarget>> Children;

    /// <summary>
    /// Files present at this level of the tree.
    /// </summary>
    public readonly SpanOfCharDict<TTarget> Items;

    /// <summary/>
    /// <param name="expectedChildren">Number of expected child directories.</param>
    public RedirectionTreeNode(int expectedChildren)
    {
        Children = new SpanOfCharDict<RedirectionTreeNode<TTarget>>(expectedChildren);
        Items = new SpanOfCharDict<TTarget>(0);
    }
    
    /// <summary/>
    /// <param name="expectedChildren">Number of expected child directories.</param>
    /// <param name="expectedFiles">Number of expected files.</param>
    public RedirectionTreeNode(int expectedChildren, int expectedFiles)
    {
        Children = new SpanOfCharDict<RedirectionTreeNode<TTarget>>(expectedChildren);
        Items = new SpanOfCharDict<TTarget>(expectedFiles);
    }
}