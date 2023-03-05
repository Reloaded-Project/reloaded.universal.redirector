using Reloaded.Universal.Redirector.Lib.Utility;
using Reloaded.Universal.Redirector.Lib.Utility.Native;
using Reloaded.Universal.Redirector.Tests.Tests.Hooks.Base;
using Reloaded.Universal.Redirector.Tests.Utility;
using static Reloaded.Universal.Redirector.Lib.Utility.Native.Native.FILE_INFORMATION_CLASS;
using static Reloaded.Universal.Redirector.Tests.Utility.WinApiHelpers;

namespace Reloaded.Universal.Redirector.Tests.Tests.Hooks;

public class NtQueryDirectoryFile : BaseHookTest
{
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
        
        const int count = 4096;
        using var items = new TemporaryJunkFolder(count);
        var files = NtQueryDirectoryFileGetAllItems(Strings.PrefixLocalDeviceStr + items.FolderPath, method);
        Assert.Equal(count, files.Count);
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
        
        const int count = 4096;
        using var items = new TemporaryJunkFolder(count);
        using var newItems = new TemporaryJunkFolder(count);
        
        Api.AddRedirectFolder(newItems.FolderPath, items.FolderPath);
        var files = NtQueryDirectoryFileGetAllItems(Strings.PrefixLocalDeviceStr + items.FolderPath, method);
        Assert.Equal(count * 2, files.Count);
    }
    
    // TODO: MapFolder_WithFileName
}