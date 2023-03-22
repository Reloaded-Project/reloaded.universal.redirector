using Reloaded.Universal.Redirector.Lib.Utility;
using Reloaded.Universal.Redirector.Lib.Utility.Native;
using Reloaded.Universal.Redirector.Tests.Tests.Hooks.Base;
using Reloaded.Universal.Redirector.Tests.Utility;
using static Reloaded.Universal.Redirector.Lib.Utility.Native.Native.FILE_INFORMATION_CLASS;
using static Reloaded.Universal.Redirector.Tests.Utility.WinApiHelpers;

namespace Reloaded.Universal.Redirector.Tests.Tests.Hooks;

public class NtQueryDirectoryFile : BaseHookTest
{
    // The tests in here could be better; we still manually verify strings; for now is good enough though.
    
    [Theory]
    [MemberData(nameof(GetTestCases))]
    public void GetFiles_Baseline(bool ex, Native.FILE_INFORMATION_CLASS method)
    {
        Api.Enable();

        const int count = 512;
        using var items = new TemporaryJunkFolder(count);
        var files = NtQueryDirectoryFileGetAllItems(ex, Strings.PrefixLocalDeviceStr + items.FolderPath, method, new NtQueryDirectoryFileSettings()).Files;
        Assert.Equal(count, files.Count);

        for (int x = 0; x < files.Count; x++)
            Assert.Contains(files[x], items.FileNames);
    }

    [Theory]
    [MemberData(nameof(GetTestCases))]
    public void MapFolder_Baseline(bool ex, Native.FILE_INFORMATION_CLASS method)
    {
        Api.Enable();
        MapFolder_Baseline_Impl(ex, method, 512);
    }
    
    [Theory]
    [MemberData(nameof(GetTestCases))]
    public void MapFolder_Directories(bool ex, Native.FILE_INFORMATION_CLASS method)
    {
        Api.Enable();
        MapFolder_Baseline_Impl(ex, method, 512, true);
    }
    
    [Theory]
    [MemberData(nameof(GetTestCases))]
    public void MapFolder_ReturnSingleEntry(bool ex, Native.FILE_INFORMATION_CLASS method)
    {
        Api.Enable();
        int count = 512;
        using var items = new TemporaryJunkFolder(count);
        using var newItems = new TemporaryJunkFolder(count);

        Api.AddRedirectFolder(newItems.FolderPath, items.FolderPath);
        var files = NtQueryDirectoryFileGetAllItems(ex, Strings.PrefixLocalDeviceStr + items.FolderPath, method, 
            new NtQueryDirectoryFileSettings()
        {
            OneByOne = true,
            RestartAfter = null,
            FileNameFilter = "*"
        }).Files;
        
        Assert.Equal(count * 2, files.Count);
        AssertReturnedFileNames(items, files, newItems);
    }

    [Theory]
    [MemberData(nameof(GetTestCases))]
    public void MapFolder_WithRestart(bool ex, Native.FILE_INFORMATION_CLASS method)
    {
        Api.Enable();
        int count = 512;
        int restartAfter = count / 2;
        using var items = new TemporaryJunkFolder(count);
        using var newItems = new TemporaryJunkFolder(count);

        Api.AddRedirectFolder(newItems.FolderPath, items.FolderPath);
        var files = NtQueryDirectoryFileGetAllItems(ex, Strings.PrefixLocalDeviceStr + items.FolderPath, method, 
            new NtQueryDirectoryFileSettings()
        {
            OneByOne = true,
            RestartAfter = restartAfter,
            FileNameFilter = "*"
        }).Files;
        
        Assert.Equal((count * 2) + restartAfter, files.Count);
        AssertReturnedFileNames(items, files, newItems);
    }
    
    [Theory]
    [MemberData(nameof(GetTestCases))]
    public void MapFolder_WithRestart_InOriginalFiles(bool ex, Native.FILE_INFORMATION_CLASS method)
    {
        Api.Enable();
        const int count = 512;
        const int restartAfter = count + (count / 2);
        using var items = new TemporaryJunkFolder(count);
        using var newItems = new TemporaryJunkFolder(count);

        Api.AddRedirectFolder(newItems.FolderPath, items.FolderPath);
        var files = NtQueryDirectoryFileGetAllItems(ex, Strings.PrefixLocalDeviceStr + items.FolderPath, method, 
            new NtQueryDirectoryFileSettings()
            {
                OneByOne = true,
                RestartAfter = restartAfter,
                FileNameFilter = "*"
            }).Files;
        
        Assert.Equal((count * 2) + restartAfter, files.Count);
        AssertReturnedFileNames(items, files, newItems);
    }
    
    [Theory]
    [MemberData(nameof(GetTestCases))]
    public void MapFolder_WithFileName(bool ex, Native.FILE_INFORMATION_CLASS method)
    {
        Api.Enable();
        const int count = 512;

        int currentName = 0;
        string MakeFileName() => (currentName++).ToString();
        using var items = new TemporaryJunkFolder(count, MakeFileName);
        using var newItems = new TemporaryJunkFolder(count, MakeFileName);

        Api.AddRedirectFolder(newItems.FolderPath, items.FolderPath);
        var files = NtQueryDirectoryFileGetAllItems(ex, Strings.PrefixLocalDeviceStr + items.FolderPath, method, new NtQueryDirectoryFileSettings()
        {
            OneByOne = false,
            RestartAfter = null,
            FileNameFilter = "10*"
        }).Files;
        
        foreach (var file in files)
            file.StartsWith("10");
    }
    
    [Theory]
    [MemberData(nameof(GetTestCases))]
    public void MapFolder_OverEmptyFolder(bool ex, Native.FILE_INFORMATION_CLASS method)
    {
        Api.Enable();
        int count = 512;
        using var items = new TemporaryFolderAllocation();
        using var newItems = new TemporaryJunkFolder(count);

        Api.AddRedirectFolder(newItems.FolderPath, items.FolderPath);
        var files = NtQueryDirectoryFileGetAllItems(ex, Strings.PrefixLocalDeviceStr + items.FolderPath, method, new NtQueryDirectoryFileSettings()).Files;
        Assert.Equal(count, files.Count);
        
        for (int x = 0; x < files.Count; x++)
            Assert.Contains(files[x], newItems.FileNames);
    }
    
    [Theory]
    [MemberData(nameof(GetTestCases))]
    public void MapFolder_Baseline_Single(bool ex, Native.FILE_INFORMATION_CLASS method)
    {
        Api.Enable();
        MapFolder_Baseline_Impl(ex, method, 1);
    }

    private void MapFolder_Baseline_Impl(bool ex, Native.FILE_INFORMATION_CLASS method, int count, bool includeDirectories = false)
    {
        using var items = new TemporaryJunkFolder(count);
        using var newItems = new TemporaryJunkFolder(count);

        Api.AddRedirectFolder(newItems.FolderPath, items.FolderPath);
        var files = NtQueryDirectoryFileGetAllItems(ex, Strings.PrefixLocalDeviceStr + items.FolderPath, method, new NtQueryDirectoryFileSettings()
        {
            OneByOne = false,
            RestartAfter = null,
            FileNameFilter = "*"
        }).GetItems(includeDirectories);
        
        Assert.Equal(count * 2, files.Count);

        for (int x = 0; x < files.Count; x++)
        {
            var itemsContains = items.FileNames.Contains(files[x]);
            var newItemsContains = newItems.FileNames.Contains(files[x]);
            Assert.True(itemsContains | newItemsContains);
        }
    }
    
    private static void AssertReturnedFileNames(TemporaryJunkFolder items, List<string> files, TemporaryJunkFolder newItems)
    {
        for (int x = 0; x < files.Count; x++)
        {
            var itemsContains = items.FileNames.Contains(files[x]);
            var newItemsContains = newItems.FileNames.Contains(files[x]);
            Assert.True(itemsContains | newItemsContains);
        }
    }
    
    public static IEnumerable<object[]> GetTestCases()
    {
        for (int x = 0; x <= 1; x++)
        {
            bool useEx = Convert.ToBoolean(x);
            
            yield return new object[] { useEx, FileDirectoryInformation };
            yield return new object[] { useEx, FileFullDirectoryInformation };
            yield return new object[] { useEx, FileBothDirectoryInformation };
            yield return new object[] { useEx, FileNamesInformation };
            yield return new object[] { useEx, FileIdBothDirectoryInformation };
            yield return new object[] { useEx, FileIdFullDirectoryInformation };
            yield return new object[] { useEx, FileIdGlobalTxDirectoryInformation };
            yield return new object[] { useEx, FileIdExtdDirectoryInformation };
            yield return new object[] { useEx, FileIdExtdBothDirectoryInformation };
        }
    }
}