using Reloaded.Universal.Redirector.Lib.Utility;
using Reloaded.Universal.Redirector.Tests.Tests.Hooks.Base;
using Reloaded.Universal.Redirector.Tests.Utility;
using static Reloaded.Universal.Redirector.Lib.Utility.Native.Native;
using static Reloaded.Universal.Redirector.Tests.Utility.WinApiHelpers;

namespace Reloaded.Universal.Redirector.Tests.Tests.Hooks;

public class NtCreateFile : BaseHookTest
{
    [Fact]
    public void NtCreateFile_Baseline() => Baseline(false);
    
    [Fact]
    public void NtOpenFile_Baseline() => Baseline(true);

    private void Baseline(bool useNtOpenFile)
    {
        Api.Enable();

        // Setup.
        var notExpected = NtFileReadAllText(useNtOpenFile, GetBaseFilePrefixed("usvfs-poem.txt")); // original
        var expected = NtFileReadAllText(useNtOpenFile, GetOverride1FilePrefixed("usvfs-poem.txt")); // expected

        // Attach Overlay 1
        Api.AddRedirectFolder(GetOverride1Path(), GetBasePath());
        var actual = NtFileReadAllText(useNtOpenFile, GetBaseFilePrefixed("usvfs-poem.txt")); // redirected

        Assert.Equal(expected, actual);
        Assert.NotEqual(notExpected, actual);

        // Disable API.
        Api.Disable();
        actual = NtFileReadAllText(useNtOpenFile, GetBaseFilePrefixed("usvfs-poem.txt")); // no longer redirected
        Assert.Equal(notExpected, actual);
        Api.Enable();
    }
    
    [Fact]
    public void NtCreateFile_OpenPreviouslyMissingDirectory() => OpenPreviouslyMissingDirectory(false);
    
    [Fact]
    public void NtOpenFile_OpenPreviouslyMissingDirectorye() => OpenPreviouslyMissingDirectory(true);
    
    private void OpenPreviouslyMissingDirectory(bool useOpen)
    {
        Api.Enable();

        // Setup.
        const int count = 8;
        using var source = new TemporaryFolderAllocation();
        using var target = new TemporaryFolderAllocation();
        var srcSubfolder = Path.Combine(source.FolderPath, "Data");
        var tgtSubfolder = Path.Combine(target.FolderPath, "NewFolder");
        Directory.CreateDirectory(srcSubfolder);
        Directory.CreateDirectory(tgtSubfolder);

        for (int x = 0; x < count; x++)
            File.Create(Path.Combine(srcSubfolder, x.ToString())).Dispose();

        File.Create(Path.Combine(tgtSubfolder, 0.ToString())).Dispose();

        // Assert directory previously doesn't exist by getting error.
        var testPath = Strings.PrefixLocalDeviceStr + Path.Combine(source.FolderPath, "NewFolder"); // <= not on disk
        var handle = NtFileDirectoryOpen(useOpen, testPath, false, out var result);
        Assert.Equal(unchecked((int)0xC0000034), result); // not found
        NtClose(handle);
        
        // Now do the redirect. 
        Api.AddRedirectFolder(target.FolderPath, source.FolderPath);

        // Okay now try open the handle.
        // This will throw if failed.
        handle = NtFileDirectoryOpen(useOpen, testPath, false, out result);
        NtClose(handle);
        Assert.Equal(0, result);
    }
}