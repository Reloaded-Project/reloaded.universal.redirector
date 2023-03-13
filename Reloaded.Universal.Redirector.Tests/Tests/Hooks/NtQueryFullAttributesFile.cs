using Reloaded.Universal.Redirector.Tests.Tests.Hooks.Base;
using static Reloaded.Universal.Redirector.Tests.Utility.WinApiHelpers;

namespace Reloaded.Universal.Redirector.Tests.Tests.Hooks;

public class NtQueryFullAttributesFile : BaseHookTest
{
    [Fact]
    public void NtQueryFullAttributesFile_Baseline()
    {
        Api.Enable();
        
        // Setup.
        var notExpected = NtQueryFullAttributesFileHelper(GetBaseFilePrefixed("usvfs-poem.txt"));   // original
        var expected = NtQueryFullAttributesFileHelper(GetOverride1FilePrefixed("usvfs-poem.txt")); // expected
        
        // Attach Overlay 1
        Api.AddRedirectFolder(GetOverride1Path(), GetBasePath());
        var actual = NtQueryFullAttributesFileHelper(GetBaseFilePrefixed("usvfs-poem.txt")); // redirected
        
        Assert.Equal(expected, actual);
        Assert.NotEqual(notExpected, actual);
        
        // Disable API.
        Api.Disable();
        actual = NtQueryFullAttributesFileHelper(GetBaseFilePrefixed("usvfs-poem.txt")); // no longer redirected
        Assert.Equal(notExpected, actual);
        Api.Enable();
    }
}