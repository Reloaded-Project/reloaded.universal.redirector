using System.Net;
using Reloaded.Universal.Redirector.Lib.Utility;
using Reloaded.Universal.Redirector.Lib.Utility.Native;
using Reloaded.Universal.Redirector.Tests.Tests.Hooks.Base;
using Reloaded.Universal.Redirector.Tests.Utility;
using static Reloaded.Universal.Redirector.Lib.Utility.Native.Native.FILE_INFORMATION_CLASS;
using static Reloaded.Universal.Redirector.Tests.Utility.WinApiHelpers;

namespace Reloaded.Universal.Redirector.Tests.Tests.Hooks;

public class NtQueryDirectoryFileEx : BaseHookTest
{
    // The tests in here could be better; we still manually verify strings; for now is good enough though.
    
    [Theory]
    [InlineData(FileDirectoryInformation)]
    [InlineData(FileFullDirectoryInformation)]
    [InlineData(FileBothDirectoryInformation)]
    [InlineData(FileNamesInformation)]
    [InlineData(FileIdBothDirectoryInformation)]
    [InlineData(FileIdFullDirectoryInformation)]
    [InlineData(FileIdGlobalTxDirectoryInformation)]
    [InlineData(FileIdExtdDirectoryInformation)]
    [InlineData(FileIdExtdBothDirectoryInformation)]
    public void GetFiles_Baseline(Native.FILE_INFORMATION_CLASS method)
    {
        Api.Enable();
        
        const int count = 512;
        using var items = new TemporaryJunkFolder(count);
        var files = NtQueryDirectoryFileExGetAllItems(Strings.PrefixLocalDeviceStr + items.FolderPath, method);
        Assert.Equal(count, files.Count);

        for (int x = 0; x < files.Count; x++)
            Assert.Contains(files[x], items.FileNames);
    }
    
    [Theory]
    [InlineData(FileDirectoryInformation)]
    [InlineData(FileFullDirectoryInformation)]
    [InlineData(FileBothDirectoryInformation)]
    [InlineData(FileNamesInformation)]
    [InlineData(FileIdBothDirectoryInformation)]
    [InlineData(FileIdFullDirectoryInformation)]
    [InlineData(FileIdGlobalTxDirectoryInformation)]
    [InlineData(FileIdExtdDirectoryInformation)]
    [InlineData(FileIdExtdBothDirectoryInformation)]
    public void MapFolder_Baseline(Native.FILE_INFORMATION_CLASS method)
    {
        Api.Enable();
        MapFolder_Baseline_Impl(method, 512);
    }
    
    [Theory]
    [InlineData(FileDirectoryInformation)]
    [InlineData(FileFullDirectoryInformation)]
    [InlineData(FileBothDirectoryInformation)]
    [InlineData(FileNamesInformation)]
    [InlineData(FileIdBothDirectoryInformation)]
    [InlineData(FileIdFullDirectoryInformation)]
    [InlineData(FileIdGlobalTxDirectoryInformation)]
    [InlineData(FileIdExtdDirectoryInformation)]
    [InlineData(FileIdExtdBothDirectoryInformation)]
    public void MapFolder_Directories(Native.FILE_INFORMATION_CLASS method)
    {
        Api.Enable();
        MapFolder_Baseline_Impl(method, 512, true);
    }
    
    [Theory]
    [InlineData(FileDirectoryInformation)]
    [InlineData(FileFullDirectoryInformation)]
    [InlineData(FileBothDirectoryInformation)]
    [InlineData(FileNamesInformation)]
    [InlineData(FileIdBothDirectoryInformation)]
    [InlineData(FileIdFullDirectoryInformation)]
    [InlineData(FileIdGlobalTxDirectoryInformation)]
    [InlineData(FileIdExtdDirectoryInformation)]
    [InlineData(FileIdExtdBothDirectoryInformation)]
    public void MapFolder_ReturnSingleEntry(Native.FILE_INFORMATION_CLASS method)
    {
        Api.Enable();
        int count = 512;
        using var items = new TemporaryJunkFolder(count);
        using var newItems = new TemporaryJunkFolder(count);

        Api.AddRedirectFolder(newItems.FolderPath, items.FolderPath);
        var files = NtQueryDirectoryFileExGetAllItems(Strings.PrefixLocalDeviceStr + items.FolderPath, method, true);
        Assert.Equal(count * 2, files.Count);
        AssertReturnedFileNames(items, files, newItems);
    }
    
    [Theory]
    [InlineData(FileDirectoryInformation)]
    [InlineData(FileFullDirectoryInformation)]
    [InlineData(FileBothDirectoryInformation)]
    [InlineData(FileNamesInformation)]
    [InlineData(FileIdBothDirectoryInformation)]
    [InlineData(FileIdFullDirectoryInformation)]
    [InlineData(FileIdGlobalTxDirectoryInformation)]
    [InlineData(FileIdExtdDirectoryInformation)]
    [InlineData(FileIdExtdBothDirectoryInformation)]
    public void MapFolder_WithRestart(Native.FILE_INFORMATION_CLASS method)
    {
        Api.Enable();
        int count = 512;
        const int restartAfter = 256;
        using var items = new TemporaryJunkFolder(count);
        using var newItems = new TemporaryJunkFolder(count);

        Api.AddRedirectFolder(newItems.FolderPath, items.FolderPath);
        var files = NtQueryDirectoryFileExGetAllItems(Strings.PrefixLocalDeviceStr + items.FolderPath, method, true, restartAfter);
        Assert.Equal((count * 2) + restartAfter, files.Count);
        AssertReturnedFileNames(items, files, newItems);
    }
    
    [Theory]
    [InlineData(FileDirectoryInformation)]
    [InlineData(FileFullDirectoryInformation)]
    [InlineData(FileBothDirectoryInformation)]
    [InlineData(FileNamesInformation)]
    [InlineData(FileIdBothDirectoryInformation)]
    [InlineData(FileIdFullDirectoryInformation)]
    [InlineData(FileIdGlobalTxDirectoryInformation)]
    [InlineData(FileIdExtdDirectoryInformation)]
    [InlineData(FileIdExtdBothDirectoryInformation)]
    public void MapFolder_WithRestart_InOriginalFiles(Native.FILE_INFORMATION_CLASS method)
    {
        Api.Enable();
        const int count = 512;
        const int restartAfter = count + 256;
        using var items = new TemporaryJunkFolder(count);
        using var newItems = new TemporaryJunkFolder(count);

        Api.AddRedirectFolder(newItems.FolderPath, items.FolderPath);
        var files = NtQueryDirectoryFileExGetAllItems(Strings.PrefixLocalDeviceStr + items.FolderPath, method, true, restartAfter);
        Assert.Equal((count * 2) + restartAfter, files.Count);
        AssertReturnedFileNames(items, files, newItems);
    }
    
    [Theory]
    [InlineData(FileDirectoryInformation)]
    [InlineData(FileFullDirectoryInformation)]
    [InlineData(FileBothDirectoryInformation)]
    [InlineData(FileNamesInformation)]
    [InlineData(FileIdBothDirectoryInformation)]
    [InlineData(FileIdFullDirectoryInformation)]
    [InlineData(FileIdGlobalTxDirectoryInformation)]
    [InlineData(FileIdExtdDirectoryInformation)]
    [InlineData(FileIdExtdBothDirectoryInformation)]
    public void MapFolder_WithFileName(Native.FILE_INFORMATION_CLASS method)
    {
        Api.Enable();
        const int count = 512;

        int currentName = 0;
        string MakeFileName() => (currentName++).ToString();
        using var items = new TemporaryJunkFolder(count, MakeFileName);
        using var newItems = new TemporaryJunkFolder(count, MakeFileName);

        Api.AddRedirectFolder(newItems.FolderPath, items.FolderPath);
        var files = NtQueryDirectoryFileExGetAllItems(Strings.PrefixLocalDeviceStr + items.FolderPath, method, false, null, "10*");
        foreach (var file in files)
            file.StartsWith("10");
    }
    
    [Theory]
    [InlineData(FileDirectoryInformation)]
    [InlineData(FileFullDirectoryInformation)]
    [InlineData(FileBothDirectoryInformation)]
    [InlineData(FileNamesInformation)]
    [InlineData(FileIdBothDirectoryInformation)]
    [InlineData(FileIdFullDirectoryInformation)]
    [InlineData(FileIdGlobalTxDirectoryInformation)]
    [InlineData(FileIdExtdDirectoryInformation)]
    [InlineData(FileIdExtdBothDirectoryInformation)]
    public void MapFolder_OverEmptyFolder(Native.FILE_INFORMATION_CLASS method)
    {
        Api.Enable();
        int count = 512;
        using var items = new TemporaryFolderAllocation();
        using var newItems = new TemporaryJunkFolder(count);

        Api.AddRedirectFolder(newItems.FolderPath, items.FolderPath);
        var files = NtQueryDirectoryFileExGetAllItems(Strings.PrefixLocalDeviceStr + items.FolderPath, method);
        Assert.Equal(count, files.Count);
        
        for (int x = 0; x < files.Count; x++)
            Assert.Contains(files[x], newItems.FileNames);
    }
    
    [Theory]
    [InlineData(FileDirectoryInformation)]
    [InlineData(FileFullDirectoryInformation)]
    [InlineData(FileBothDirectoryInformation)]
    [InlineData(FileNamesInformation)]
    [InlineData(FileIdBothDirectoryInformation)]
    [InlineData(FileIdFullDirectoryInformation)]
    [InlineData(FileIdGlobalTxDirectoryInformation)]
    [InlineData(FileIdExtdDirectoryInformation)]
    [InlineData(FileIdExtdBothDirectoryInformation)]
    public void MapFolder_Baseline_Single(Native.FILE_INFORMATION_CLASS method)
    {
        Api.Enable();
        MapFolder_Baseline_Impl(method, 1);
    }

    private void MapFolder_Baseline_Impl(Native.FILE_INFORMATION_CLASS method, int count, bool directories = false)
    {
        using var items = new TemporaryJunkFolder(count);
        using var newItems = new TemporaryJunkFolder(count, null, directories);

        Api.AddRedirectFolder(newItems.FolderPath, items.FolderPath);
        var files = NtQueryDirectoryFileExGetAllItems(Strings.PrefixLocalDeviceStr + items.FolderPath, method, false, null, "*", directories);
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

    // TODO: MapFolder_WithFileName
}