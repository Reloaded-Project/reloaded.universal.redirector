using Reloaded.Universal.Redirector.Lib.Utility;
using Reloaded.Universal.Redirector.Tests.Tests.Hooks.Base;
using Reloaded.Universal.Redirector.Tests.Utility;
using static Reloaded.Universal.Redirector.Tests.Utility.WinApiHelpers;

namespace Reloaded.Universal.Redirector.Tests.Tests.Hooks;

public class NtQueryAttributesFile : BaseHookTest
{
    [Fact]
    public void NtQueryAttributesFile_Baseline() => Baseline(NtQueryAttributesFileHelper);

    [Fact]
    public void NtQueryFullAttributesFile_Baseline() => Baseline(NtQueryFullAttributesFileHelper);

    private void Baseline<T>(Func<string, T> ntQueryAttributesFile)
    {
        Api.Enable();

        // Setup.
        var notExpected = ntQueryAttributesFile(GetBaseFilePrefixed("usvfs-poem.txt")); // original
        var expected = ntQueryAttributesFile(GetOverride1FilePrefixed("usvfs-poem.txt")); // expected

        // Attach Overlay 1
        Api.AddRedirectFolder(GetOverride1Path(), GetBasePath());
        var actual = ntQueryAttributesFile(GetBaseFilePrefixed("usvfs-poem.txt")); // redirected

        Assert.Equal(expected, actual);
        Assert.NotEqual(notExpected, actual);

        // Disable API.
        Api.Disable();
        actual = ntQueryAttributesFile(GetBaseFilePrefixed("usvfs-poem.txt")); // no longer redirected
        Assert.Equal(notExpected, actual);
        Api.Enable();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NtQueryAttributesFile_OpenPreviouslyMissingDirectory(bool useFullAttributesFile) => OpenPreviouslyMissingDirectory(useFullAttributesFile);
    
    private void OpenPreviouslyMissingDirectory(bool useFull)
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
        NtQueryAttributesFileHelper(useFull, testPath, out var result);
        Assert.Equal(unchecked((int)0xC0000034), result); // not found

        // Now do the redirect. 
        Api.AddRedirectFolder(target.FolderPath, source.FolderPath);

        // Okay now try open the handle.
        // This will throw if failed.
        NtQueryAttributesFileHelper(useFull, testPath, out result);
        Assert.Equal(0, result);
    }
}