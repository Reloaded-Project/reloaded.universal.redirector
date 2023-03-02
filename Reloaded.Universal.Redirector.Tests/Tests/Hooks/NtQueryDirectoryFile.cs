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
        const int count = 4096;
        using var items = new TemporaryJunkFolder(count);
        var files = NtQueryDirectoryFileGetAllItems(Strings.PrefixLocalDeviceStr + items.FolderPath, method);
        Assert.Equal(count, files.Count);
    }
}