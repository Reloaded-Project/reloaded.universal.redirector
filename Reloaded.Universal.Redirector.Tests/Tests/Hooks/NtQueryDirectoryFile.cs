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
    public void Baseline_CanGetFiles(bool ex, Native.FILE_INFORMATION_CLASS method)
    {
        if (ex && !FileAccessServer.SupportsNtQueryDirectoryFileEx)
            return;
        
        Api.Enable();

        const int count = 256;
        using var items = new TemporaryJunkFolder(count);
        var files = NtQueryDirectoryFileGetAllItems(ex, Strings.PrefixLocalDeviceStr + items.FolderPath, method, new NtQueryDirectoryFileSettings()).Files;
        Assert.Equal(count, files.Count);

        for (int x = 0; x < files.Count; x++)
            Assert.Contains(files[x], items.FileNames);
    }

    [Theory]
    [MemberData(nameof(GetTestCases))]
    public void CanMapFolder_WithOnlyFiles(bool ex, Native.FILE_INFORMATION_CLASS method)
    {
        MapFolder_Common(ex, method, 256);
    }
    
    [Theory]
    [MemberData(nameof(GetTestCases))]
    public void CanMapFolder_WithFilesAndDirectories(bool ex, Native.FILE_INFORMATION_CLASS method)
    {
        MapFolder_Common(ex, method, 256, true);
    }
    
    [Theory]
    [MemberData(nameof(GetTestCases))]
    public void CanMapFolder_WhileReturningSingleEntry(bool ex, Native.FILE_INFORMATION_CLASS method)
    {
        if (ex && !FileAccessServer.SupportsNtQueryDirectoryFileEx)
            return;
        
        Api.Enable();
        const int count = 256;
        var items = GetFiles_ReturningSingleEntry(ex, method, count, int.MaxValue, out var newItems, out var files);
        Assert.Equal(count * 2, files.Count);
        AssertReturnedFileNames(items, files, newItems);
    }

    [Theory]
    [MemberData(nameof(GetTestCases))]
    public void CanMapFolder_WithRestart(bool ex, Native.FILE_INFORMATION_CLASS method)
    {
        if (ex && !FileAccessServer.SupportsNtQueryDirectoryFileEx)
            return;
        
        Api.Enable();
        const int count = 256;
        const int restartAfter = count / 2;
        var items = GetFiles_ReturningSingleEntry(ex, method, count, restartAfter, out var newItems, out var files);
        Assert.Equal((count * 2) + restartAfter, files.Count);
        AssertReturnedFileNames(items, files, newItems);
    }
    
    [Theory]
    [MemberData(nameof(GetTestCases))]
    public void CanMapFolder_WithRestart_InOriginalFiles(bool ex, Native.FILE_INFORMATION_CLASS method)
    {
        if (ex && !FileAccessServer.SupportsNtQueryDirectoryFileEx)
            return;
        
        Api.Enable();
        const int count = 256;
        const int restartAfter = count + (count / 2);
        var items = GetFiles_ReturningSingleEntry(ex, method, count, restartAfter, out var newItems, out var files);
        Assert.Equal((count * 2) + restartAfter, files.Count);
        AssertReturnedFileNames(items, files, newItems);
    }

    [Theory]
    [MemberData(nameof(GetTestCases))]
    public void CanMapFolder_WithWin32Filter(bool ex, Native.FILE_INFORMATION_CLASS method)
    {
        if (ex && !FileAccessServer.SupportsNtQueryDirectoryFileEx)
            return;
        
        Api.Enable();
        const int count = 256;

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
    public void CanMapFolder_DiscardsDuplicates_WithAllItemsDuplicated(bool ex, Native.FILE_INFORMATION_CLASS method)
    {
        if (ex && !FileAccessServer.SupportsNtQueryDirectoryFileEx)
            return;
        
        Api.Enable();
        const int count = 256;

        int currentName = 0;
        string MakeFileName() => (currentName++).ToString("000");
        using var items = new TemporaryJunkFolder(count, MakeFileName);
        currentName = 0;
        using var newItems = new TemporaryJunkFolder(count, MakeFileName);

        Api.AddRedirectFolder(newItems.FolderPath, items.FolderPath);
        var files = NtQueryDirectoryFileGetAllItems(ex, Strings.PrefixLocalDeviceStr + items.FolderPath, method, new NtQueryDirectoryFileSettings()
        {
            OneByOne = false,
            RestartAfter = null,
            FileNameFilter = "*"
        }).Files;
        
        // No files are injected.
        Assert.Equal(count, files.Count);
    }
    
    [Theory]
    [MemberData(nameof(GetTestCases))]
    public void CanMapFolder_DoesNotRedirectSubFolderHandle(bool ex, Native.FILE_INFORMATION_CLASS method)
    {
        if (ex && !FileAccessServer.SupportsNtQueryDirectoryFileEx)
            return;
        
        // Ensures the file path to a subfolder doesn't get redirected,
        // which could lead to erroneous behaviour 
        Api.Enable();
        const int count = 8;

        using var source = new TemporaryFolderAllocation();
        using var target = new TemporaryFolderAllocation();
        var srcSubfolder = Path.Combine(source.FolderPath, "Data"); 
        var tgtSubfolder = Path.Combine(target.FolderPath, "Data");
        Directory.CreateDirectory(srcSubfolder);
        Directory.CreateDirectory(tgtSubfolder);

        for (int x = 0; x < count; x++)
            File.Create(Path.Combine(srcSubfolder, x.ToString())).Dispose();
        
        File.Create(Path.Combine(tgtSubfolder, 0.ToString())).Dispose();
        
        // We not have two Data Subfolders in src and target. 
        // Now if all goes well, folder handle is not redirected, and we get correct file count.
        Api.AddRedirectFolder(target.FolderPath, source.FolderPath);
        
        var files = NtQueryDirectoryFileGetAllItems(ex, Strings.PrefixLocalDeviceStr + srcSubfolder, method, new NtQueryDirectoryFileSettings()
        {
            OneByOne = false,
            RestartAfter = null,
            FileNameFilter = "*"
        }).Files;
        
        Assert.Equal(count, files.Count);
    }
    
    [Theory]
    [MemberData(nameof(GetTestCases))]
    public void CanMapFolder_TargetDiscoversNewFolders(bool ex, Native.FILE_INFORMATION_CLASS method)
    {
        if (ex && !FileAccessServer.SupportsNtQueryDirectoryFileEx)
            return;
        
        Api.Enable();
        const int count = 256;

        int currentName = 0;
        string MakeFileName() => (currentName++).ToString();
        using var items = new TemporaryJunkFolder(count, MakeFileName, true);
        using var newItems = new TemporaryJunkFolder(count, MakeFileName, true);

        // Note: FileNamesInformation can't discern files and directories, so we return both
        Api.AddRedirectFolder(newItems.FolderPath, items.FolderPath);
        var dirs = NtQueryDirectoryFileGetAllItems(ex, Strings.PrefixLocalDeviceStr + items.FolderPath, method, new NtQueryDirectoryFileSettings()).GetItems(true);
        
        Assert.Equal(count * 2, dirs.Count);
    }
    
    [Theory]
    [MemberData(nameof(GetTestCases))]
    public void CanMapFolder_WithNullFileNameOnSubsequentCalls(bool ex, Native.FILE_INFORMATION_CLASS method)
    {
        if (ex && !FileAccessServer.SupportsNtQueryDirectoryFileEx)
            return;
        
        Api.Enable();
        const int count = 256;

        int currentName = 0;
        string MakeFileName() => (currentName++).ToString();
        using var items = new TemporaryJunkFolder(count, MakeFileName);
        using var newItems = new TemporaryJunkFolder(count, MakeFileName);

        Api.AddRedirectFolder(newItems.FolderPath, items.FolderPath);
        var files = NtQueryDirectoryFileGetAllItems(ex, Strings.PrefixLocalDeviceStr + items.FolderPath, method, new NtQueryDirectoryFileSettings()
        {
            OneByOne = false,
            RestartAfter = null,
            SetNullFileNameOnSubsequentCalls = true,
            FileNameFilter = "10*"
        }).Files;
        
        foreach (var file in files)
            file.StartsWith("10");
    }
    
    [Theory]
    [MemberData(nameof(GetTestCases))]
    public void CanMapFolder_OverEmptyFolder(bool ex, Native.FILE_INFORMATION_CLASS method)
    {
        if (ex && !FileAccessServer.SupportsNtQueryDirectoryFileEx)
            return;
        
        Api.Enable();
        int count = 256;
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
    public void CanMapFolder_FileDoesNotExist(bool ex, Native.FILE_INFORMATION_CLASS method) => GetFiles_DoesNotExist(ex, method, false);

    [Theory]
    [MemberData(nameof(GetTestCases))]
    public void CanMapFolder_FileDoesNotExist_SingleFile(bool ex, Native.FILE_INFORMATION_CLASS method) => GetFiles_DoesNotExist(ex, method, true);
    
    private void GetFiles_DoesNotExist(bool ex, Native.FILE_INFORMATION_CLASS method, bool singleFile)
    {
        if (ex && !FileAccessServer.SupportsNtQueryDirectoryFileEx)
            return;
        
        Api.Enable();
        const int count = 8;

        int currentName = 0;
        string MakeFileName() => (currentName++).ToString();
        using var items = new TemporaryJunkFolder(count, MakeFileName);
        using var newItems = new TemporaryJunkFolder(count, MakeFileName);

        Api.AddRedirectFolder(newItems.FolderPath, items.FolderPath);
        
        var files = NtQueryDirectoryFileGetAllItems(ex, Strings.PrefixLocalDeviceStr + items.FolderPath, method,
            new NtQueryDirectoryFileSettings()
            {
                OneByOne = singleFile,
                FileNameFilter = "DoesNotExist"
            }).Files;
        
        Assert.Empty(files);
    }
    
    [Theory]
    [MemberData(nameof(GetTestCases))]
    public void CanMapFolder_Baseline_Single(bool ex, Native.FILE_INFORMATION_CLASS method)
    {
        MapFolder_Common(ex, method, 1);
    }
    
    private TemporaryJunkFolder GetFiles_ReturningSingleEntry(bool ex, Native.FILE_INFORMATION_CLASS method, int count,
        int restartAfter, out TemporaryJunkFolder newItems, out List<string> files)
    {
        using var items = new TemporaryJunkFolder(count);
        newItems = new TemporaryJunkFolder(count);

        Api.AddRedirectFolder(newItems.FolderPath, items.FolderPath);
        files = NtQueryDirectoryFileGetAllItems(ex, Strings.PrefixLocalDeviceStr + items.FolderPath, method,
            new NtQueryDirectoryFileSettings()
            {
                OneByOne = true,
                RestartAfter = restartAfter,
                FileNameFilter = "*"
            }).Files;
        
        newItems.Dispose();
        return items;
    }

    private void MapFolder_Common(bool ex, Native.FILE_INFORMATION_CLASS method, int count, bool includeDirectories = false)
    {
        if (ex && !FileAccessServer.SupportsNtQueryDirectoryFileEx)
            return;
        
        Api.Enable();
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
        AssertReturnedFileNames(items, files, newItems);
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