using System.Runtime.CompilerServices;
using Reloaded.Universal.Redirector.Lib.Structures;

namespace Reloaded.Universal.Redirector.Tests.Tests;

public class SpanOfCharDictTests
{
    /// <summary>
    /// Tests adding an item to the redirection tree.
    /// </summary>
    [Fact]
    public void Add_And_Get_Baseline()
    {
        var dict = new SpanOfCharDict<bool>(2);
        dict.AddOrReplace(@"a", true);
        dict.AddOrReplace(@"b", false);
        dict.AddOrReplace(@"c", true); // Grow
        
        Assert.True(dict.TryGetValue("a", out var valueA));
        Assert.True(dict.TryGetValue("b", out var valueB));
        Assert.True(dict.TryGetValue("c", out var valueC));
        Assert.True(valueA);
        Assert.False(valueB);
        Assert.True(valueC);
    }
    
    /// <summary>
    /// Tests adding an item to the redirection tree.
    /// </summary>
    [Fact]
    public void GetFirstItem_WithSingleNode()
    {
        // Also test unrolled version.
        for (int x = 0; x < 20000; x += 100)
        {
            var dict = new SpanOfCharDict<bool>(x);
            dict.AddOrReplace(@"Kitten", true);

            ref var first = ref dict.GetFirstItem(out var key);
            Assert.False(Unsafe.IsNullRef(ref first));
            Assert.Equal("Kitten", key);
            Assert.True(first);
        }
    }
    
    /// <summary>
    /// Tests adding an item to the redirection tree.
    /// </summary>
    [Fact]
    public void GetFirstItem_WithoutNodes()
    {
        var dict = new SpanOfCharDict<bool>(2);
        ref var first = ref dict.GetFirstItem(out _);
        Assert.True(Unsafe.IsNullRef(ref first));
    }
    
    /// <summary>
    /// Tests adding an item to the redirection tree.
    /// </summary>
    [Fact]
    public void Add_And_Get_Update()
    {
        var dict = new SpanOfCharDict<int>(2);
        dict.AddOrReplace(@"a", 10);
        
        Assert.True(dict.TryGetValue("a", out var valueA));
        Assert.Equal(10, valueA);
        
        dict.AddOrReplace(@"a", 11);
        Assert.True(dict.TryGetValue("a", out valueA));
        Assert.Equal(11, valueA);
    }
    
    /// <summary>
    /// Tests adding an item to the redirection tree.
    /// </summary>
    [Fact]
    public void Add_And_Get_ByRef()
    {
        var dict = new SpanOfCharDict<bool>(2);
        dict.AddOrReplace(@"a", true);
        dict.AddOrReplace(@"b", false);

        ref var valueA = ref dict.GetValueRef("a");
        ref var valueB = ref dict.GetValueRef("b");
        Assert.True(valueA);
        Assert.False(valueB);

        // Verify the 'by ref' part works.
        valueA = false;
        ref var valueAB = ref dict.GetValueRef("a");
        Assert.Equal(valueA, valueAB);
        
        // Verify the 'null ref' part works.
        Assert.True(Unsafe.IsNullRef(ref dict.GetValueRef("c")));
    }
    
    /// <summary>
    /// Tests adding an item to the redirection tree.
    /// </summary>
    [Fact]
    public void Add_And_Get_Large_Values_And_Grow()
    {
        int count = 20000;
        var dict = new SpanOfCharDict<bool>(count / 2);
        for (int x = 0; x < count; x++)
            dict.AddOrReplace(x.ToString(), Convert.ToBoolean(x & 1));

        Assert.Equal(count, dict.Count);
        for (int x = 0; x < count; x++)
        {
            Assert.True(dict.TryGetValue(x.ToString(), out var result));
            Assert.Equal(Convert.ToBoolean(x & 1), result);
        }
    }
}