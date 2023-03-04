using Reloaded.Universal.Redirector.Tests.Tests.Hooks.Base;

namespace Reloaded.Universal.Redirector.Tests.Tests.Hooks;

public class NtDeleteFile : BaseHookTest
{
    [Fact]
    public void NtDeleteFile_Baseline()
    {
        var basePath = GetBaseFile("usvfs-poem.txt");
        var overridePath = GetOverride1File("usvfs-poem.txt");
        
        // Setup
        Assert.True(File.Exists(basePath));
        Assert.True(File.Exists(overridePath));
        
        // Attach Overlay 1
        Api.AddRedirectFolder(GetOverride1Path(), GetBasePath());
        
        // Deleting base should delete override due to redirection
        File.Delete(basePath); 
        Assert.False(File.Exists(overridePath));
        
        // Not deleting base should delete actual file.
        Api.Disable();
        File.Delete(basePath);
        Assert.False(File.Exists(basePath));
        Api.Enable();
    }
}