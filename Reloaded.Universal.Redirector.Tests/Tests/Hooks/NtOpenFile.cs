using Reloaded.Universal.Redirector.Tests.Tests.Hooks.Base;
using static Reloaded.Universal.Redirector.Tests.Utility.WinApiHelpers;

namespace Reloaded.Universal.Redirector.Tests.Tests.Hooks;

public class NtOpenFile : BaseHookTest
{
    [Fact]
    public void NtOpenFile_Baseline()
    {
        Api.Enable();
        
        // Setup.
        var notExpected = NtOpenFileReadAllText(GetBaseFilePrefixed("usvfs-poem.txt"));   // original
        var expected = NtOpenFileReadAllText(GetOverride1FilePrefixed("usvfs-poem.txt")); // expected
        
        // Attach Overlay 1
        Api.AddRedirectFolder(GetOverride1Path(), GetBasePath());
        var actual = NtOpenFileReadAllText(GetBaseFilePrefixed("usvfs-poem.txt")); // redirected
        
        Assert.Equal(expected, actual);
        Assert.NotEqual(notExpected, actual);
        
        // Disable API.
        Api.Disable();
        actual = NtOpenFileReadAllText(GetBaseFilePrefixed("usvfs-poem.txt")); // no longer redirected
        Assert.Equal(notExpected, actual);
        Api.Enable();
    }
}